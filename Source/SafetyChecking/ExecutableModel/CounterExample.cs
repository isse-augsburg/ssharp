// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace ISSE.SafetyChecking.ExecutableModel
{
	using System;
	using Utilities;

	/// <summary>
	///   Represents a model checking counter example.
	/// </summary>
	public sealed class CounterExample<TExecutableModel> : DisposableObject where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		/// <summary>
		///   The file extension used by counter example files.
		/// </summary>
		public const string FileExtension = ".ssharp";

		/// <summary>
		///   The first few bytes that indicate that a file is a valid S# counter example file.
		/// </summary>
		public const int FileHeader = 0x3FE0DD04;

		public int[][] ReplayInfo { get; }
		public byte[][] States { get; }

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="runtimeModel">The runtime model the counter example was generated for.</param>
		/// <param name="states">The serialized counter example.</param>
		/// <param name="replayInfo">The replay information of the counter example.</param>
		/// <param name="endsWithException">Indicates whether the counter example ends with an exception.</param>
		public CounterExample(TExecutableModel runtimeModel, byte[][] states, int[][] replayInfo, bool endsWithException)
		{
			Requires.NotNull(runtimeModel, nameof(runtimeModel));
			Requires.NotNull(states, nameof(states));
			Requires.NotNull(replayInfo, nameof(replayInfo));
			Requires.That(replayInfo.Length == states.Length - 1, "Invalid replay info.");

			RuntimeModel = runtimeModel;
			EndsWithException = endsWithException;

			States = states;
			ReplayInfo = replayInfo;
		}

		/// <summary>
		///   Indicates whether the counter example ends with an exception.
		/// </summary>
		public bool EndsWithException { get; }

		/// <summary>
		///   Gets the runtime model the counter example was generated for.
		/// </summary>
		public TExecutableModel RuntimeModel { get; }

		/// <summary>
		///   Gets the model the counter example was generated for.
		/// </summary>
		//public ModelBase Model => RuntimeModel.Model;

		/// <summary>
		///   Gets the number of steps the counter example consists of.
		/// </summary>
		public int StepCount
		{
			get
			{
				Requires.That(States != null, "No counter example has been loaded.");
				return States.Length - 1;
			}
		}

		/// <summary>
		///   Gets the serialized states of the counter example.
		/// </summary>
		internal byte[] GetState(int position) => States[position + 1];
		
		/// <summary>
		///   Deserializes the state at the <paramref name="position" /> of the counter example.
		/// </summary>
		/// <param name="position">The position of the state within the counter example that should be deserialized.</param>
		public unsafe ExecutableModel<TExecutableModel> DeserializeState(int position)
		{
			Requires.That(States != null, "No counter example has been loaded.");
			Requires.InRange(position, nameof(position), 0, StepCount);

			using (var pointer = PinnedPointer.Create(GetState(position)))
				RuntimeModel.Deserialize((byte*)pointer);

			return RuntimeModel;
		}

		/// <summary>
		///   Replays the transition of the counter example with the zero-baesd <paramref name="transitionIndex" />.
		/// </summary>
		/// <param name="choiceResolver">The choice resolver that should be used to resolve nondeterministic choices.</param>
		/// <param name="transitionIndex">The zero-based index of the transition that should be replayed.</param>
		internal unsafe void Replay(ChoiceResolver choiceResolver, int transitionIndex)
		{
			if (StepCount == 0)
				return;

			choiceResolver.Clear();
			choiceResolver.PrepareNextState();
			choiceResolver.SetChoices(ReplayInfo[transitionIndex]);

			fixed (byte* state = States[transitionIndex])
				RuntimeModel.Deserialize(state);
			
			foreach (var fault in RuntimeModel.NondeterministicFaults)
				fault.Reset();

			if (transitionIndex == 0)
				RuntimeModel.ExecuteInitialStep();
			else
				RuntimeModel.ExecuteStep();

			RuntimeModel.NotifyFaultActivations();
		}

		/// <summary>
		///   Executs the <paramref name="action" /> for each step of the counter example.
		/// </summary>
		/// <param name="action">The action that should be executed on the serialized model state.</param>
		internal void ForEachStep(Action<byte[]> action)
		{
			Requires.NotNull(action, nameof(action));
			Requires.That(States != null, "No counter example has been loaded.");

			for (var i = 1; i < StepCount + 1; ++i)
				action(States[i]);
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

		/// <summary>
		///   Saves the counter example to the <paramref name="file" />.
		/// </summary>
		/// <param name="file">The file the counter example should be saved to.</param>
		public void Save(string file)
		{
			RuntimeModel.CounterExampleSerialization.Save(this,file);
		}
	}
}