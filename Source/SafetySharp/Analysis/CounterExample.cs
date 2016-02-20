// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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
	using System.IO;
	using System.Linq;
	using System.Runtime.Serialization.Formatters.Binary;
	using System.Text;
	using Modeling;
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
		private const int FileHeader = 0x3FE0DD03;

		/// <summary>
		///   The serialized counter example.
		/// </summary>
		private readonly byte[][] _counterExample;

		/// <summary>
		///   The information required to replay the counter example.
		/// </summary>
		private readonly int[][] _replayInfo;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the counter example was generated for.</param>
		/// <param name="counterExample">The serialized counter example.</param>
		/// <param name="replayInfo">The replay information of the counter example.</param>
		internal CounterExample(RuntimeModel model, byte[][] counterExample, int[][] replayInfo)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(counterExample, nameof(counterExample));
			Requires.NotNull(replayInfo, nameof(replayInfo));
			Requires.That(replayInfo.Length == counterExample.Length - 1, "Invalid replay info.");

			Model = model;
			_counterExample = counterExample;
			_replayInfo = replayInfo;
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
				writer.Write(Model.SerializedModel.Length);
				writer.Write(Model.SerializedModel);

				foreach (var fault in Model.Objects.OfType<Fault>())
					writer.Write((int)fault.Activation);

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

				writer.Write(_replayInfo.Length);
				foreach (var choices in _replayInfo)
				{
					writer.Write(choices.Length);
					foreach (var choice in choices)
						writer.Write(choice);
				}
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
				var modelData = RuntimeModelSerializer.LoadSerializedData(new MemoryStream(serializedRuntimeModel));

				foreach (var fault in modelData.ObjectTable.OfType<Fault>())
					fault.Activation = (Activation)reader.ReadInt32();

				var model = new RuntimeModel(modelData);
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

				var replayInfo = new int[reader.ReadInt32()][];
				for (var i = 0; i < replayInfo.Length; ++i)
				{
					replayInfo[i] = new int[reader.ReadInt32()];
					for (var j = 0; j < replayInfo[i].Length; ++j)
						replayInfo[i][j] = reader.ReadInt32();
				}

				return new CounterExample(model, counterExample, replayInfo);
			}
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
		///   Gets the replay information for the state identified by the zero-based <paramref name="stateIndex" />.
		/// </summary>
		/// <param name="stateIndex">The index of the state the replay information should be returned for.</param>
		internal int[] GetReplayInformation(int stateIndex)
		{
			Requires.InRange(stateIndex, nameof(stateIndex), 0, _counterExample.Length - 1);
			return _replayInfo[stateIndex];
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