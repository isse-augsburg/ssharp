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
	using System.Runtime.Serialization.Formatters.Binary;
	using System.Text;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;

	/// <summary>
	///   Represents a model checking counter example.
	/// </summary>
	public class CounterExample : DisposableObject
	{
		/// <summary>
		///   The file extension used by counter example files.
		/// </summary>
		public const string FileExtension = ".ssharp";

		/// <summary>
		///   The first few bytes that indicate that a file is a valid S# counter example file.
		/// </summary>
		private const int FileHeader = 0x3FE0DD02;

		/// <summary>
		///   The character that is used to split the individual states in the counter example.
		/// </summary>
		private static readonly string[] _splitCharacter = { "," };

		/// <summary>
		///   The serialized counter example.
		/// </summary>
		private readonly byte[][] _counterExample;

		/// <summary>
		///   The serialized runtime model the counter example was generated for.
		/// </summary>
		private readonly byte[] _serializedModel;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the counter example was generated for.</param>
		/// <param name="serializedModel">The serialized runtime model the counter example was generated for.</param>
		/// <param name="counterExample">The serialized counter example.</param>
		internal CounterExample(RuntimeModel model, byte[] serializedModel, byte[][] counterExample)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(serializedModel, nameof(serializedModel));
			Requires.NotNull(counterExample, nameof(counterExample));

			Model = model;
			_serializedModel = serializedModel;
			_counterExample = counterExample;
		}

		/// <summary>
		///   Gets the model the counter example was generated for.
		/// </summary>
		public RuntimeModel Model { get; }

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
				Model.Deserialize((byte*)pointer);

			return Model;
		}

		/// <summary>
		///   Executs the <paramref name="action" /> for each step of the counter example.
		/// </summary>
		/// <param name="action">The action that should be executed on the deserialized model state.</param>
		public void ForEachStep(Action<RuntimeModel> action)
		{
			Requires.NotNull(action, nameof(action));

			for (var i = 0; i < StepCount; ++i)
				action(DeserializeState(i));
		}

		/// <summary>
		///   Executs the <paramref name="action" /> for each step of the counter example.
		/// </summary>
		/// <param name="action">The action that should be executed on the serialized model state.</param>
		internal void ForEachStep(Action<byte[]> action)
		{
			Requires.NotNull(action, nameof(action));
			Requires.That(_counterExample != null, "No counter example has been loaded.");

			for (var i = 0; i < StepCount; ++i)
				action(_counterExample[i]);
		}

		/// <summary>
		///   Saves the counter example to the <paramref name="file" />.
		/// </summary>
		/// <param name="file">The file the counter example should be saved to.</param>
		public void Save(string file)
		{
			Requires.NotNullOrWhitespace(file, nameof(file));
			Requires.That(file.EndsWith(FileExtension), nameof(file), "Invalid file extension.");

			using (var writer = new BinaryWriter(File.OpenWrite(file), Encoding.UTF8))
			{
				writer.Write(FileHeader);
				writer.Write(_serializedModel.Length);
				writer.Write(_serializedModel);

				var formatter = new BinaryFormatter();
				var memoryStream = new MemoryStream();
				formatter.Serialize(memoryStream, Model.StateVectorLayout.ToArray());

				var metadata = memoryStream.ToArray();
				writer.Write(metadata.Length);
				writer.Write(metadata);

				writer.Write(StepCount);
				writer.Write(Model.StateVectorSize);

				foreach (var slot in _counterExample.SelectMany(step => step))
					writer.Write(slot);
			}
		}

		/// <summary>
		///   Loads a counter example from the <paramref name="file" />.
		/// </summary>
		/// <param name="file">The path to the file the counter example should be loaded from.</param>
		public static CounterExample Load(string file)
		{
			Requires.NotNullOrWhitespace(file, nameof(file));

			using (var reader = new BinaryReader(File.OpenRead(file), Encoding.UTF8))
			{
				if (reader.ReadInt32() != FileHeader)
					throw new InvalidOperationException("The file does not contain a counter example that is compatible with this version of S#.");

				var serializedRuntimeModel = reader.ReadBytes(reader.ReadInt32());
				var model = RuntimeModelSerializer.Load(new MemoryStream(serializedRuntimeModel));
				var metadataStream = new MemoryStream(reader.ReadBytes(reader.ReadInt32()));
				var formatter = new BinaryFormatter();
				var slotMetadata = new StateVectorLayout((StateSlotMetadata[])formatter.Deserialize(metadataStream));
				var modelMetadata = model.StateVectorLayout;
				var counterExample = new byte[reader.ReadInt32()][];
				var slotCount = reader.ReadInt32();

				if (slotCount != model.StateVectorSize)
				{
					throw new InvalidOperationException(
						$"State slot count mismatch; the instantiated model requires {model.StateVectorSize} state slots, " +
						$"whereas the counter example uses {slotCount} state slots.");
				}

				if (slotMetadata.SlotCount != modelMetadata.SlotCount)
				{
					throw new InvalidOperationException(
						$"State slot metadata count mismatch; the instantiated model has {modelMetadata.SlotCount} state slot metadata entries, " +
						$"whereas the counter example has {slotMetadata.SlotCount} state slot entries.");
				}

				for (var i = 0; i < slotMetadata.SlotCount; ++i)
				{
					if (modelMetadata[i] != slotMetadata[i])
						throw new StateVectorMismatchException(slotMetadata, modelMetadata);
				}

				for (var i = 0; i < counterExample.Length; ++i)
				{
					counterExample[i] = new byte[model.StateVectorSize];
					for (var j = 0; j < model.StateVectorSize; ++j)
						counterExample[i][j] = reader.ReadByte();
				}

				return new CounterExample(model, serializedRuntimeModel, counterExample);
			}
		}

		/// <summary>
		///   Loads a LtsMin counter example from the <paramref name="file" />.
		/// </summary>
		/// <param name="serializedRuntimeModel">The serialized runtime model the counter example was generated for.</param>
		/// <param name="file">The path to the file the counter example should be loaded from.</param>
		internal static CounterExample LoadLtsMin(byte[] serializedRuntimeModel, string file)
		{
			Requires.NotNull(serializedRuntimeModel, nameof(serializedRuntimeModel));
			Requires.NotNullOrWhitespace(file, nameof(file));

			using (var csvFile = new TemporaryFile("csv"))
			{
				var printTrace = new ExternalProcess("ltsmin-printtrace.exe", $"{file} {csvFile.Path}");
				printTrace.Run();

				if (printTrace.ExitCode != 0)
				{
					var outputs = printTrace.Outputs.Select(output => output.Message).ToArray();

					// ltsmin-printtrace segfaults when the trace has length 0
					// So we have to try to detect this annoying situation and create an empty trace file instead
					if (outputs.Any(output => output.Contains("length of trace is 0")))
						File.WriteAllText(csvFile.Path, "");
					else
						throw new InvalidOperationException($"Failed to read LtsMin counter example:\n{String.Join("\n", outputs)}");
				}

				var model = RuntimeModelSerializer.Load(new MemoryStream(serializedRuntimeModel));
				return new CounterExample(model, serializedRuntimeModel, ParseCsv(model, File.ReadAllLines(csvFile.Path)).ToArray());
			}
		}

		/// <summary>
		///   Parses the comma-separated values in the <paramref name="lines" /> into state vectors.
		/// </summary>
		/// <param name="model">The model the counter example was generated for.</param>
		/// <param name="lines">The lines that should be parsed.</param>
		private static unsafe IEnumerable<byte[]> ParseCsv(RuntimeModel model, string[] lines)
		{
			if (lines.Length == 0)
				return new[] { new byte[model.StateVectorSize] };

			var counterExample = new List<byte[]>();
			var stateNames = lines[0].Split(_splitCharacter, StringSplitOptions.RemoveEmptyEntries);
			var firstValue = Array.FindIndex(stateNames, s => s.Contains(RuntimeModel.ConstructionStateName));

			foreach (var serializedState in lines.Skip(1))
			{
				var stateVectors = serializedState.Split(_splitCharacter, StringSplitOptions.RemoveEmptyEntries);
				var values = stateVectors.Skip(firstValue).Take(model.StateVectorSize).ToArray();

				var byteState = new byte[model.StateVectorSize];
				fixed (byte* state = byteState)
				{
					for (var i = 0; i < model.StateVectorSize / 4; ++i)
						((int*)state)[i] = Int32.Parse(values[i]);
				}

				counterExample.Add(byteState);
			}

			return counterExample;
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (disposing)
				Model.SafeDispose();
		}

		/// <summary>
		///   Raised when the state vector layout of the counter example does not match the layout of the instantiated model.
		/// </summary>
		public class StateVectorMismatchException : Exception
		{
			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			/// <param name="counterExampleMetadata">The state vector layout expected by the counter example.</param>
			/// <param name="modelMetadata">The state vector layout expected by the model.</param>
			internal StateVectorMismatchException(StateVectorLayout counterExampleMetadata, StateVectorLayout modelMetadata)
				: base("Mismatch detected between the layout of the state vector as expected by the counter example and the " +
					   "actual layout of the state vector used by the instantiated model.")
			{
				CounterExampleMetadata = counterExampleMetadata;
				ModelMetadata = modelMetadata;
			}

			/// <summary>
			///   Gets the state vector layout expected by the counter example.
			/// </summary>
			public StateVectorLayout CounterExampleMetadata { get; }

			/// <summary>
			///   Gets the state vector layout expected by the model.
			/// </summary>
			public StateVectorLayout ModelMetadata { get; }
		}
	}
}