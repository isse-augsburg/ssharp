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
		private const int FileHeader = 0x3FE0DD04;

		private readonly int[][] _replayInfo;
		private readonly byte[][] _states;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="runtimeModel">The runtime model the counter example was generated for.</param>
		/// <param name="states">The serialized counter example.</param>
		/// <param name="replayInfo">The replay information of the counter example.</param>
		/// <param name="endsWithException">Indicates whether the counter example ends with an exception.</param>
		internal CounterExample(RuntimeModel runtimeModel, byte[][] states, int[][] replayInfo, bool endsWithException)
		{
			Requires.NotNull(runtimeModel, nameof(runtimeModel));
			Requires.NotNull(states, nameof(states));
			Requires.NotNull(replayInfo, nameof(replayInfo));
			Requires.That(replayInfo.Length == states.Length - 1, "Invalid replay info.");

			RuntimeModel = runtimeModel;
			EndsWithException = endsWithException;

			_states = states;
			_replayInfo = replayInfo;
		}

		/// <summary>
		///   Indicates whether the counter example ends with an exception.
		/// </summary>
		public bool EndsWithException { get; }

		/// <summary>
		///   Gets the runtime model the counter example was generated for.
		/// </summary>
		internal RuntimeModel RuntimeModel { get; }

		/// <summary>
		///   Gets the model the counter example was generated for.
		/// </summary>
		public ModelBase Model => RuntimeModel.Model;

		/// <summary>
		///   Gets the number of steps the counter example consists of.
		/// </summary>
		public int StepCount
		{
			get
			{
				Requires.That(_states != null, "No counter example has been loaded.");
				return _states.Length - 1;
			}
		}

		/// <summary>
		///   Gets the serialized states of the counter example.
		/// </summary>
		internal byte[] GetState(int position) => _states[position + 1];

		/// <summary>
		///   Deserializes the state at the <paramref name="position" /> of the counter example.
		/// </summary>
		/// <param name="position">The position of the state within the counter example that should be deserialized.</param>
		public unsafe ModelBase DeserializeState(int position)
		{
			Requires.That(_states != null, "No counter example has been loaded.");
			Requires.InRange(position, nameof(position), 0, StepCount);

			using (var pointer = PinnedPointer.Create(GetState(position)))
				RuntimeModel.Deserialize((byte*)pointer);

			return Model;
		}

		/// <summary>
		///   Gets the replay information for the state identified by the zero-based <paramref name="stateIndex" />.
		/// </summary>
		/// <param name="stateIndex">The index of the state the replay information should be returned for.</param>
		internal int[] GetReplayInformation(int stateIndex)
		{
			Requires.InRange(stateIndex, nameof(stateIndex), 0, _states.Length - 1);
			return _replayInfo[stateIndex + 1];
		}

		/// <summary>
		///   Replays the computation of the initial state.
		/// </summary>
		internal unsafe void ReplayInitialState()
		{
			var initialState = _states[0];
			fixed (byte* state = &initialState[0])
				RuntimeModel.Replay(state, _replayInfo[0], initializationStep: true);
		}

		/// <summary>
		///   Executs the <paramref name="action" /> for each step of the counter example.
		/// </summary>
		/// <param name="action">The action that should be executed on the serialized model state.</param>
		internal void ForEachStep(Action<byte[]> action)
		{
			Requires.NotNull(action, nameof(action));
			Requires.That(_states != null, "No counter example has been loaded.");

			for (var i = 1; i < StepCount + 1; ++i)
				action(_states[i]);
		}

		/// <summary>
		///   Saves the counter example to the <paramref name="file" />.
		/// </summary>
		/// <param name="file">The file the counter example should be saved to.</param>
		public void Save(string file)
		{
			Requires.NotNullOrWhitespace(file, nameof(file));

			if (!file.EndsWith(FileExtension))
				file += FileExtension;

			var directory = Path.GetDirectoryName(file);
			if (!String.IsNullOrWhiteSpace(directory))
				Directory.CreateDirectory(directory);

			using (var writer = new BinaryWriter(File.OpenWrite(file), Encoding.UTF8))
			{
				writer.Write(FileHeader);
				writer.Write(EndsWithException);
				writer.Write(RuntimeModel.SerializedModel.Length);
				writer.Write(RuntimeModel.SerializedModel);

				foreach (var fault in RuntimeModel.Objects.OfType<Fault>())
					writer.Write((int)fault.Activation);

				var formatter = new BinaryFormatter();
				var memoryStream = new MemoryStream();
				formatter.Serialize(memoryStream, RuntimeModel.StateVectorLayout.ToArray());

				var metadata = memoryStream.ToArray();
				writer.Write(metadata.Length);
				writer.Write(metadata);

				writer.Write(StepCount + 1);
				writer.Write(RuntimeModel.StateVectorSize);

				foreach (var slot in _states.SelectMany(step => step))
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

				var endsWithException = reader.ReadBoolean();
				var serializedRuntimeModel = reader.ReadBytes(reader.ReadInt32());
				var modelData = RuntimeModelSerializer.LoadSerializedData(serializedRuntimeModel);

				foreach (var fault in modelData.ObjectTable.OfType<Fault>())
					fault.Activation = (Activation)reader.ReadInt32();

				var runtimeModel = new RuntimeModel(modelData);
				runtimeModel.UpdateFaultSets();

				var metadataStream = new MemoryStream(reader.ReadBytes(reader.ReadInt32()));
				var formatter = new BinaryFormatter();
				var slotMetadata = new StateVectorLayout((StateSlotMetadata[])formatter.Deserialize(metadataStream));
				var modelMetadata = runtimeModel.StateVectorLayout;

				var counterExample = new byte[reader.ReadInt32()][];
				var slotCount = reader.ReadInt32();

				if (slotCount != runtimeModel.StateVectorSize)
				{
					throw new InvalidOperationException(
						$"State slot count mismatch; the instantiated model requires {runtimeModel.StateVectorSize} state slots, " +
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
					counterExample[i] = new byte[runtimeModel.StateVectorSize];
					for (var j = 0; j < runtimeModel.StateVectorSize; ++j)
						counterExample[i][j] = reader.ReadByte();
				}

				var replayInfo = new int[reader.ReadInt32()][];
				for (var i = 0; i < replayInfo.Length; ++i)
				{
					replayInfo[i] = new int[reader.ReadInt32()];
					for (var j = 0; j < replayInfo[i].Length; ++j)
						replayInfo[i][j] = reader.ReadInt32();
				}

				return new CounterExample(runtimeModel, counterExample, replayInfo, endsWithException);
			}
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (disposing)
				RuntimeModel.SafeDispose();
		}
	}
}