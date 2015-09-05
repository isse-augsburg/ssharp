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

namespace SafetySharp.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Runtime;
	using Utilities;

	/// <summary>
	///   Represents a model checking counter example.
	/// </summary>
	public class CounterExample : DisposableObject
	{
		/// <summary>
		///   The character that is used to split the individual states in the counter example.
		/// </summary>
		private static readonly string[] _splitCharacter = { "," };

		/// <summary>
		///   The model the counter example was generated for.
		/// </summary>
		private readonly RuntimeModel _model;

		/// <summary>
		///   The serialized counter example.
		/// </summary>
		private int[][] _counterExample;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the counter example was generated for.</param>
		internal CounterExample(RuntimeModel model)
		{
			Requires.NotNull(model, nameof(model));
			_model = model;
		}

		/// <summary>
		///   Gets the number of steps the counter example consists of.
		/// </summary>
		public int StepCount
		{
			get
			{
				Requires.That(_counterExample != null, "No counter example has been loaded.");
				return _counterExample.Length;
			}
		}

		/// <summary>
		///   Deserializes the state at the <paramref name="position" /> of the counter example.
		/// </summary>
		/// <param name="position">The position of the state within the counter example that should be deserialized.</param>
		public unsafe RuntimeModel DeserializeState(int position)
		{
			Requires.That(_counterExample != null, "No counter example has been loaded.");
			Requires.InRange(position, nameof(position), _counterExample);

			using (var pointer = PinnedPointer.Create(_counterExample[position]))
				_model.Deserialize((int*)pointer);

			return _model;
		}

		/// <summary>
		///   Executs the <paramref name="action" /> for each step of the counter example.
		/// </summary>
		/// <param name="action">The action that should be executed.</param>
		public void ForEachStep(Action<RuntimeModel> action)
		{
			Requires.NotNull(action, nameof(action));

			for (var i = 0; i < StepCount; ++i)
				action(DeserializeState(i));
		}

		/// <summary>
		///   Loads a LtsMin counter example from the <paramref name="file" />.
		/// </summary>
		/// <param name="file">The path to the file the counter example should be loaded from.</param>
		internal void LoadLtsMin(string file)
		{
			Requires.NotNullOrWhitespace(file, nameof(file));

			using (var csvFile = new TemporaryFile("csv"))
			{
				var printTrace = new ExternalProcess("ltsmin-printtrace.exe", $"{file} {csvFile.FilePath}");
				printTrace.Run();

				if (printTrace.ExitCode != 0)
				{
					var outputs = printTrace.Outputs.Select(output => output.Message).ToArray();

					// ltsmin-printtrace segfaults when the trace has length 0
					// So we have to try to detect this annoying situation and create an empty trace file instead
					if (outputs.Any(output => output.Contains("length of trace is 0")))
						File.WriteAllText(csvFile.FilePath, "");
					else
						throw new InvalidOperationException($"Failed to read LtsMin counter example:\n{String.Join("\n", outputs)}");
				}

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
				Assert.That(values.Length > _model.StateSlotCount, "Counter example contains too few slots per state.");

				var state = new int[_model.StateSlotCount];
				for (var i = 0; i < _model.StateSlotCount; ++i)
					state[i] = Int32.Parse(values[i]);

				yield return state;
			}
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			_model.SafeDispose();
		}
	}
}