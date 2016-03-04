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

namespace SafetySharp.Runtime
{
	using System;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using Analysis;
	using CompilerServices;
	using Modeling;
	using Serialization;
	using Utilities;

	/// <summary>
	///   Represents a runtime model that can be used for model checking or simulation.
	/// </summary>
	public sealed unsafe class RuntimeModel : DisposableObject
	{
		/// <summary>
		///   The unique name of the construction state.
		/// </summary>
		internal const string ConstructionStateName = "constructionState259C2EE0D9884B92989DF442BA268E8E";

		/// <summary>
		///   Deserializes a state of the model.
		/// </summary>
		private readonly SerializationDelegate _deserialize;

		/// <summary>
		///   Serializes a state of the model.
		/// </summary>
		private readonly SerializationDelegate _serialize;

		/// <summary>
		///   The objects referenced by the model that participate in state serialization.
		/// </summary>
		private readonly ObjectTable _serializedObjects;

		/// <summary>
		///   The number of bytes reserved at the beginning of each state vector by the model checker tool.
		/// </summary>
		private readonly int _stateHeaderBytes;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="serializedData">The serialized data describing the model.</param>
		/// <param name="stateHeaderBytes">
		///   The number of bytes that should be reserved at the beginning of each state vector for the model checker tool.
		/// </param>
		internal RuntimeModel(SerializedRuntimeModel serializedData, int stateHeaderBytes = 0)
		{
			var buffer = serializedData.Buffer;
			var rootComponents = serializedData.RootComponents;
			var objectTable = serializedData.ObjectTable;
			var formulas = serializedData.Formulas;

			Requires.NotNull(buffer, nameof(buffer));
			Requires.NotNull(rootComponents, nameof(rootComponents));
			Requires.NotNull(objectTable, nameof(objectTable));
			Requires.NotNull(formulas, nameof(formulas));
			Requires.That(stateHeaderBytes % 4 == 0, nameof(stateHeaderBytes), "Expected a multiple of 4.");

			SerializedModel = buffer;
			RootComponents = rootComponents;
			Faults = objectTable.OfType<Fault>().Where(fault => fault.Activation == Activation.Nondeterministic).ToArray();
			StateFormulas = objectTable.OfType<StateFormula>().ToArray();
			Formulas = formulas;

			// Create a local object table just for the objects referenced by the model; only these objects
			// have to be serialized and deserialized. The local object table does not contain, for instance,
			// the closure types of the state formulas
			var objects = SerializationRegistry.Default.GetReferencedObjects(rootComponents, SerializationMode.Optimized);
			var deterministicFaults = objectTable.OfType<Fault>().Where(fault => fault.Activation != Activation.Nondeterministic);

			objects = objects.Except(deterministicFaults, ReferenceEqualityComparer<object>.Default);
			_serializedObjects = new ObjectTable(objects);
			Objects = objectTable;

			StateVectorLayout = SerializationRegistry.Default.GetStateVectorLayout(_serializedObjects, SerializationMode.Optimized);

			_deserialize = StateVectorLayout.CreateDeserializer();
			_serialize = StateVectorLayout.CreateSerializer();
			_stateHeaderBytes = stateHeaderBytes;

			PortBinding.BindAll(objectTable);
			ChoiceResolver = new ChoiceResolver(objectTable);

			ConstructionState = new byte[StateVectorSize];
			fixed (byte* state = ConstructionState)
				Serialize(state);

			Requires.That(Faults.Length < 32, "More than 32 faults are not supported.");
			Requires.That(StateFormulas.Length < 32, "More than 32 state formulas are not supported.");
		}

		/// <summary>
		///   Gets the <see cref="Runtime.ChoiceResolver" /> used by the model.
		/// </summary>
		internal ChoiceResolver ChoiceResolver { get; }

		/// <summary>
		///   Gets the construction state of the model.
		/// </summary>
		internal byte[] ConstructionState { get; }

		/// <summary>
		///   Gets the objects referenced by the model.
		/// </summary>
		internal ObjectTable Objects { get; }

		/// <summary>
		///   Gets the buffer the model was deserialized from.
		/// </summary>
		internal byte[] SerializedModel { get; }

		/// <summary>
		///   Gets the model's <see cref="StateVectorLayout" />.
		/// </summary>
		internal StateVectorLayout StateVectorLayout { get; }

		/// <summary>
		///   The formulas that are checked on the model.
		/// </summary>
		public Formula[] Formulas { get; }

		/// <summary>
		///   Gets the size of the state vector in bytes. The size is always a multiple of 4.
		/// </summary>
		internal int StateVectorSize => StateVectorLayout.SizeInBytes + _stateHeaderBytes;

		/// <summary>
		///   Gets the root components of the model.
		/// </summary>
		public Component[] RootComponents { get; }

		/// <summary>
		///   Gets the faults contained in the model that can be activated nondeterministically.
		/// </summary>
		public Fault[] Faults { get; }

		/// <summary>
		///   Gets the state formulas of the model.
		/// </summary>
		internal StateFormula[] StateFormulas { get; }

		/// <summary>
		///   Deserializes the model's state from <paramref name="serializedState" />.
		/// </summary>
		/// <param name="serializedState">The state of the model that should be deserialized.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Deserialize(byte* serializedState)
		{
			_deserialize(serializedState + _stateHeaderBytes);
		}

		/// <summary>
		///   Serializes the model's state to <paramref name="serializedState" />.
		/// </summary>
		/// <param name="serializedState">The memory region the model's state should be serialized into.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Serialize(byte* serializedState)
		{
			_serialize(serializedState + _stateHeaderBytes);
		}

		/// <summary>
		///   Resets the model to one of its initial states.
		/// </summary>
		internal void Reset()
		{
			fixed (byte* state = ConstructionState)
			{
				Deserialize(state);

				foreach (var obj in _serializedObjects.OfType<IInitializable>())
					obj.Initialize();
			}
		}

		/// <summary>
		///   Updates the state of the model by executing a single step.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ExecuteStep()
		{
			foreach (var fault in Faults)
				fault.Reset();

			foreach (var component in RootComponents)
				component.Update();
		}

		/// <summary>
		///   Checks whether the state formula identified by the zero-based <paramref name="formulaIndex" /> holds for the model's
		///   current state.
		/// </summary>
		/// <param name="formulaIndex">The zero-based index of the formula that should be checked.</param>
		public bool CheckStateFormula(int formulaIndex)
		{
			return StateFormulas[formulaIndex].Expression();
		}

		/// <summary>
		///   Computes the initial states of the model, storing the computed <paramref name="transitions" />.
		/// </summary>
		/// <param name="transitions">The set the computed transitions should be stored in.</param>
		internal void ComputeInitialStates(TransitionSet transitions)
		{
			ChoiceResolver.PrepareNextState();

			fixed (byte* state = ConstructionState)
			{
				while (ChoiceResolver.PrepareNextPath())
				{
					Deserialize(state);

					foreach (var obj in _serializedObjects.OfType<IInitializable>())
						obj.Initialize();

					transitions.Add(this);
				}
			}
		}

		/// <summary>
		///   Computes the successor states for <paramref name="sourceState" />, storing the computed <paramref name="transitions" />.
		/// </summary>
		/// <param name="transitions">The set the computed transitions should be stored in.</param>
		/// <param name="sourceState">The source state the next states should be computed for.</param>
		internal void ComputeSuccessorStates(TransitionSet transitions, byte* sourceState)
		{
			ChoiceResolver.PrepareNextState();

			while (ChoiceResolver.PrepareNextPath())
			{
				Deserialize(sourceState);
				ExecuteStep();
				transitions.Add(this);
			}
		}

		/// <summary>
		///   Generates the replay information for the <paramref name="trace" />.
		/// </summary>
		/// <param name="trace">The trace the replay information should be generated for.</param>
		/// <param name="endsWithException">Indicates whether the trace ends with an exception being thrown.</param>
		internal int[][] GenerateReplayInformation(byte[][] trace, bool endsWithException)
		{
			var info = new int[trace.Length - 1][];
			var targetState = stackalloc byte[StateVectorSize];

			// We have to generate the replay info for all transitions
			for (var i = 0; i < trace.Length - 1; ++i)
			{
				ChoiceResolver.Clear();
				ChoiceResolver.PrepareNextState();

				// Try all transitions until we find the one that leads to the desired state
				while (ChoiceResolver.PrepareNextPath())
				{
					fixed (byte* sourceState = trace[i])
						Deserialize(sourceState);

					try
					{
						ExecuteStep();
					}
					catch (Exception)
					{
						Requires.That(endsWithException, "Unexpected exception.");
						Requires.That(i == trace.Length - 2, "Unexpected exception.");

						info[i] = ChoiceResolver.GetChoices().ToArray();
						break;
					}

					Serialize(targetState);

					// Compare the target states; if they match, we've found the correct transition
					var areEqual = true;
					for (var j = 0; j < StateVectorSize; ++j)
						areEqual &= targetState[j] == trace[i + 1][j];

					if (!areEqual)
						continue;

					info[i] = ChoiceResolver.GetChoices().ToArray();
					break;
				}

				Requires.That(info[i] != null, "Unable to generate replay information.");
			}

			return info;
		}

		/// <summary>
		///   Replays the model step starting at the serialized <paramref name="state" /> using the given
		///   <paramref name="replayInformation" />.
		/// </summary>
		/// <param name="state">The serialized state that the replay starts from.</param>
		/// <param name="replayInformation">The replay information required to compute the target state.</param>
		internal void Replay(byte* state, int[] replayInformation)
		{
			Requires.NotNull(replayInformation, nameof(replayInformation));

			ChoiceResolver.Clear();
			ChoiceResolver.PrepareNextState();
			ChoiceResolver.SetChoices(replayInformation);

			Deserialize(state);
			ExecuteStep();
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			ChoiceResolver.SafeDispose();
			Objects.OfType<IDisposable>().SafeDisposeAll();
		}
	}
}