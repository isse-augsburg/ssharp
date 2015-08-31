// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace SafetySharp.Runtime
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Represents a model checking counter example.
	/// </summary>
	internal class CounterExample : DisposableObject
	{
		/// <summary>
		///   The character that is used to split the individual states in the counter example.
		/// </summary>
		private static readonly string[] _splitCharacter = { "," };

		/// <summary>
		///   The delegate for the generated method that deserializes the state of the model.
		/// </summary>
		private readonly SerializationDelegate _deserialize;

		/// <summary>
		///   The model the counter example was generated for.
		/// </summary>
		private readonly Model _model;

		/// <summary>
		///   The object lookup table that can be used to map between serialized objects and identifiers.
		/// </summary>
		private readonly ObjectTable _objectTable;

		/// <summary>
		///   The size of the state vector in number of slots.
		/// </summary>
		private readonly int _slotCount;

		/// <summary>
		///   The state cache that is used to store a serialized state of the counter example.
		/// </summary>
		private readonly StateCache _stateCache;

		/// <summary>
		///   The serialized counter example.
		/// </summary>
		private int[][] _counterExample;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the counter example was generated for.</param>
		public CounterExample(Model model)
		{
			Requires.NotNull(model, nameof(model));

			_model = model;
			_objectTable = new ObjectTable(_model);
			_slotCount = _objectTable.Objects.Sum(obj => _model.SerializationRegistry.GetStateSlotCount(obj));
			_stateCache = new StateCache(_slotCount);
			_deserialize = model.SerializationRegistry.GenerateDeserializationDelegate();
		}

		/// <summary>
		///   Loads a LtsMin counter example from the <paramref name="file" />.
		/// </summary>
		/// <param name="file">The path to the file the counter example should be loaded from.</param>
		public void LoadLtsMin(string file)
		{
			Requires.NotNullOrWhitespace(file, nameof(file));

			using (var csvFile = new TemporaryFile("csv"))
			{
				var printTrace = new ExternalProcess("ltsmin-printtrace.exe", "{0} {1}", file, csvFile.FilePath);
				printTrace.Run();

				_counterExample = ParseCsv(File.ReadAllLines(csvFile.FilePath).Skip(1)).ToArray();
			}
		}

		/// <summary>
		///   Parses the comma-separated values in the <paramref name="lines" /> into state vectors.
		/// </summary>
		/// <param name="lines">The lines that should be parsed.</param>
		private IEnumerable<int[]> ParseCsv(IEnumerable<string> lines)
		{
			foreach (var serializedState in lines)
			{
				var values = serializedState.Split(_splitCharacter, StringSplitOptions.RemoveEmptyEntries);
				Assert.That(values.Length > _slotCount, "Counter example contains too few slots per state.");

				var state = new int[_slotCount];
				for (var i = 0; i < _slotCount; ++i)
					state[i] = Int32.Parse(values[i]);

				yield return state;
			}
		}

		/// <summary>
		///   Deserializes the state at the <paramref name="position" /> of the counter example.
		/// </summary>
		/// <param name="position">The position of the state within the counter example that should be deserialized.</param>
		public unsafe Model DeserializeState(int position)
		{
			Requires.That(_counterExample != null, "No counter example has been loaded.");
			Requires.InRange(position, nameof(position), 0, _counterExample.Length);

			_stateCache.Clear();
			var state = _stateCache.Allocate();

			Marshal.Copy(_counterExample[position], 0, new IntPtr(state), _slotCount);
			_deserialize(state, _model.ObjectTable.Objects);

			return _model;
		}

		/// <summary>
		///   Outputs the model state at the indicated <paramref name="position" /> in the counter example using the
		///   <paramref name="outputCallback" />.
		/// </summary>
		/// <param name="outputCallback">The callback that should be used to output the model state.</param>
		/// <param name="position">The zero-based position of the state within the counter example that should be output.</param>
		public void OutputState(Action<string> outputCallback, int position)
		{
			Requires.NotNull(outputCallback, nameof(outputCallback));

			DeserializeState(position);

			var writer = new CodeWriter();
			outputCallback(writer.ToString());
		}

		/// <summary>
		///   Outputs the model state of the entire counter example using the <paramref name="outputCallback" />.
		/// </summary>
		/// <param name="outputCallback">The callback that should be used to output the model state.</param>
		public void OutputCounterExample(Action<string> outputCallback)
		{
			for (var i = 0; i < _counterExample.Length; ++i)
				OutputState(outputCallback, i);
		}

		/// <summary>
		///   Outputs the name, type, and unique identifier of <paramref name="obj" />.
		/// </summary>
		private void OutputObject(CodeWriter writer, object obj, string objName)
		{
			writer.AppendLine("{0} : {1} (#{2})", objName, obj.GetType().Name, _model.ObjectTable[obj]);
		}

		/// <summary>
		///   Outputs the name, type, and value of the <paramref name="obj" />'s <paramref name="field" />.
		/// </summary>
		private static void OutputField(CodeWriter writer, object obj, FieldInfo field)
		{
			writer.AppendLine("{0} : {1} = {2};", field.Name, field.FieldType.Name, field.GetValue(obj));
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			_stateCache.SafeDispose();
		}
	}
}