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
	internal sealed unsafe class RuntimeModel : DisposableObject
	{
		/// <summary>
		///   The unique name of the construction state.
		/// </summary>
		internal const string ConstructionStateName = "constructionState259C2EE0D9884B92989DF442BA268E8E";

		/// <summary>
		///   The <see cref="Runtime.ChoiceResolver" /> used by the model.
		/// </summary>
		private readonly ChoiceResolver _choiceResolver;

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

			Requires.That(serializedData.Model != null, "Expected a valid model instance.");
			Requires.NotNull(buffer, nameof(buffer));
			Requires.NotNull(rootComponents, nameof(rootComponents));
			Requires.NotNull(objectTable, nameof(objectTable));
			Requires.NotNull(formulas, nameof(formulas));
			Requires.That(stateHeaderBytes % 4 == 0, nameof(stateHeaderBytes), "Expected a multiple of 4.");

			Model = serializedData.Model;
			SerializedModel = buffer;
			RootComponents = rootComponents;
			Faults = objectTable.OfType<Fault>().Where(fault => fault.Activation == Activation.Nondeterministic).ToArray();
			ActivationSensitiveFaults = Faults.Where(fault => fault.RequiresActivationNotification).ToArray();
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

			_deserialize = StateVectorLayout.CreateDeserializer(_serializedObjects);
			_serialize = StateVectorLayout.CreateSerializer(_serializedObjects);
			_stateHeaderBytes = stateHeaderBytes;

			PortBinding.BindAll(objectTable);
			_choiceResolver = new ChoiceResolver(objectTable);

			ConstructionState = new byte[StateVectorSize];
			fixed (byte* state = ConstructionState)
				Serialize(state);

			FaultSet.CheckFaultCount(Faults.Length);
			StateFormulaSet.CheckFormulaCount(StateFormulas.Length);
		}

		/// <summary>
		///   Gets a copy of the original model the runtime model was generated from.
		/// </summary>
		internal ModelBase Model { get; }

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
		///   Gets the faults contained in the model that can be activated nondeterministically and that must be notified about their
		///   activation.
		/// </summary>
		internal Fault[] ActivationSensitiveFaults { get; }

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
		///   Computes an initial state of the model.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ExecuteInitialStep()
		{
			foreach (var fault in Faults)
				fault.Reset();

			foreach (var obj in _serializedObjects.OfType<IInitializable>())
				obj.Initialize();
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
		///   Computes the initial states of the model, storing the computed <paramref name="transitions" />.
		/// </summary>
		/// <param name="transitions">The set the computed transitions should be stored in.</param>
		internal void ComputeInitialStates(TransitionSet transitions)
		{
			_choiceResolver.PrepareNextState();

			fixed (byte* state = ConstructionState)
			{
				while (_choiceResolver.PrepareNextPath())
				{
					Deserialize(state);
					ExecuteInitialStep();
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
			_choiceResolver.PrepareNextState();

			while (_choiceResolver.PrepareNextPath())
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
				_choiceResolver.Clear();
				_choiceResolver.PrepareNextState();

				// Try all transitions until we find the one that leads to the desired state
				while (_choiceResolver.PrepareNextPath())
				{
					fixed (byte* sourceState = trace[i])
						Deserialize(sourceState);

					try
					{
						if (i == 0)
							ExecuteInitialStep();
						else
							ExecuteStep();
					}
					catch (Exception)
					{
						Requires.That(endsWithException, "Unexpected exception.");
						Requires.That(i == trace.Length - 2, "Unexpected exception.");

						info[i] = _choiceResolver.GetChoices().ToArray();
						break;
					}

					NotifyFaultActivations();
					Serialize(targetState);

					// Compare the target states; if they match, we've found the correct transition
					var areEqual = true;
					for (var j = 0; j < StateVectorSize; ++j)
						areEqual &= targetState[j] == trace[i + 1][j];

					if (!areEqual)
						continue;

					info[i] = _choiceResolver.GetChoices().ToArray();
					break;
				}

				Requires.That(info[i] != null, $"Unable to generate replay information for step {i} of {trace.Length}.");
			}

			return info;
		}

		/// <summary>
		///   Replays the model step starting at the serialized <paramref name="state" /> using the given
		///   <paramref name="replayInformation" />.
		/// </summary>
		/// <param name="state">The serialized state that the replay starts from.</param>
		/// <param name="replayInformation">The replay information required to compute the target state.</param>
		/// <param name="initializationStep">Indicates whether the initialization step should be replayed.</param>
		internal void Replay(byte* state, int[] replayInformation, bool initializationStep)
		{
			Requires.NotNull(replayInformation, nameof(replayInformation));

			_choiceResolver.Clear();
			_choiceResolver.PrepareNextState();
			_choiceResolver.SetChoices(replayInformation);

			Deserialize(state);

			if (initializationStep)
				ExecuteInitialStep();
			else
				ExecuteStep();

			NotifyFaultActivations();
		}

		/// <summary>
		///   Notifies all activated faults of their activation. Returns <c>false</c> to indicate that no notifications were necessary.
		/// </summary>
		internal bool NotifyFaultActivations()
		{
			if (ActivationSensitiveFaults.Length == 0)
				return false;

			var notificationsSent = false;
			foreach (var fault in ActivationSensitiveFaults)
			{
				if (fault.IsActivated)
				{
					fault.OnActivated();
					notificationsSent = true;
				}
			}

			return notificationsSent;
		}

		/// <summary>
		///   Gets the choices that were made to generate the last transitions.
		/// </summary>
		internal int[] GetLastChoices()
		{
			return _choiceResolver.GetChoices().ToArray();
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_choiceResolver.SafeDispose();
			Objects.OfType<IDisposable>().SafeDisposeAll();
		}
	}
}