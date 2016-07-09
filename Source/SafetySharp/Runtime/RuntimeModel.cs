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
		///   Deserializes a state of the model.
		/// </summary>
		private readonly SerializationDelegate _deserialize;

		/// <summary>
		///   The faults contained in the model.
		/// </summary>
		private readonly Fault[] _faults;

		/// <summary>
		///   Restricts the ranges of the model's state variables.
		/// </summary>
		private readonly Action _restrictRanges;

		/// <summary>
		///   Serializes a state of the model.
		/// </summary>
		private readonly SerializationDelegate _serialize;

		/// <summary>
		///   The objects referenced by the model that participate in state serialization.
		/// </summary>
		private readonly ObjectTable _serializedObjects;

		/// <summary>
		///   The number of bytes reserved at the beginning of each state vector by the model checker.
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
			Requires.That(serializedData.Model != null, "Expected a valid model instance.");

			var buffer = serializedData.Buffer;
			var rootComponents = serializedData.Model.Roots;
			var objectTable = serializedData.ObjectTable;
			var formulas = serializedData.Formulas;

			Requires.NotNull(buffer, nameof(buffer));
			Requires.NotNull(rootComponents, nameof(rootComponents));
			Requires.NotNull(objectTable, nameof(objectTable));
			Requires.NotNull(formulas, nameof(formulas));
			Requires.That(stateHeaderBytes % 4 == 0, nameof(stateHeaderBytes), "Expected a multiple of 4.");

			Model = serializedData.Model;
			SerializedModel = buffer;
			RootComponents = rootComponents.Cast<Component>().ToArray();
			StateFormulas = objectTable.OfType<StateFormula>().ToArray();
			Formulas = formulas;

			// Create a local object table just for the objects referenced by the model; only these objects
			// have to be serialized and deserialized. The local object table does not contain, for instance,
			// the closure types of the state formulas.
			_faults = objectTable.OfType<Fault>().Where(fault => fault.IsUsed).ToArray();
			_serializedObjects = new ObjectTable(Model.ReferencedObjects);

			Objects = objectTable;
			StateVectorLayout = SerializationRegistry.Default.GetStateVectorLayout(Model, _serializedObjects, SerializationMode.Optimized);
			UpdateFaultSets();

			_deserialize = StateVectorLayout.CreateDeserializer(_serializedObjects);
			_serialize = StateVectorLayout.CreateSerializer(_serializedObjects);
			_restrictRanges = StateVectorLayout.CreateRangeRestrictor(_serializedObjects);
			_stateHeaderBytes = stateHeaderBytes;

			PortBinding.BindAll(objectTable);
			ChoiceResolver = new ChoiceResolver(objectTable);

			ConstructionState = new byte[StateVectorSize];
			fixed (byte* state = ConstructionState)
			{
				Serialize(state);
				_restrictRanges();
			}

			FaultSet.CheckFaultCount(_faults.Length);
			StateFormulaSet.CheckFormulaCount(StateFormulas.Length);
		}

		/// <summary>
		///   Gets the <see cref="Runtime.ChoiceResolver" /> used by the model.
		/// </summary>
		internal ChoiceResolver ChoiceResolver { get; }

		/// <summary>
		///   Gets a copy of the original model the runtime model was generated from.
		/// </summary>
		internal ModelBase Model { get; }

		/// <summary>
		///   Gets the construction state of the model.
		/// </summary>
		internal byte[] ConstructionState { get; }

		/// <summary>
		///   Gets all of the objects referenced by the model, including those that do not take part in state serialization.
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
		public Fault[] NondeterministicFaults { get; private set; }

		/// <summary>
		///   Gets the faults contained in the model that can be activated nondeterministically and that must be notified about their
		///   activation.
		/// </summary>
		internal Fault[] ActivationSensitiveFaults { get; private set; }

		/// <summary>
		///   Gets the state formulas of the model.
		/// </summary>
		internal StateFormula[] StateFormulas { get; }

		/// <summary>
		///   Updates the activation states of the model's faults.
		/// </summary>
		/// <param name="getActivation">The callback that should be used to determine a fault's activation state.</param>
		internal void ChangeFaultActivations(Func<Fault, Activation> getActivation)
		{
			foreach (var fault in _faults)
				fault.Activation = getActivation(fault);

			UpdateFaultSets();
		}

		/// <summary>
		///   Updates the fault sets in accordance with the fault's actual activation states.
		/// </summary>
		internal void UpdateFaultSets()
		{
			NondeterministicFaults = _faults.Where(fault => fault.Activation == Activation.Nondeterministic).ToArray();
			ActivationSensitiveFaults = NondeterministicFaults.Where(fault => fault.RequiresActivationNotification).ToArray();
		}

		/// <summary>
		///   Copies the fault activation states of this instance to <paramref name="target" />.
		/// </summary>
		private void CopyFaultActivationStates(RuntimeModel target)
		{
			for (var i = 0; i < _faults.Length; ++i)
				target._faults[i].Activation = _faults[i].Activation;

			target.UpdateFaultSets();
		}

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
				_restrictRanges();
			}

			ChoiceResolver.Clear();
		}

		/// <summary>
		///   Computes an initial state of the model.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ExecuteInitialStep()
		{
			foreach (var fault in NondeterministicFaults)
				fault.Reset();

			foreach (var obj in _serializedObjects.OfType<IInitializable>())
				obj.Initialize();

			_restrictRanges();
		}

		/// <summary>
		///   Updates the state of the model by executing a single step.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ExecuteStep()
		{
			foreach (var fault in NondeterministicFaults)
				fault.Reset();

			foreach (var component in RootComponents)
				component.Update();

			_restrictRanges();
		}

		/// <summary>
		///   Creates a counter example from the <paramref name="path" />.
		/// </summary>
		/// <param name="createModel">The factory function that can be used to create new instances of this model.</param>
		/// <param name="path">
		///   The path the counter example should be generated from. A value of <c>null</c> indicates that no
		///   transitions could be generated for the model.
		/// </param>
		/// <param name="endsWithException">Indicates whether the counter example ends with an exception.</param>
		public CounterExample CreateCounterExample(Func<RuntimeModel> createModel, byte[][] path, bool endsWithException)
		{
			Requires.NotNull(createModel, nameof(createModel));

			// We have to create new model instances to generate and initialize the counter example, otherwise hidden
			// state variables might prevent us from doing so if they somehow influence the state
			var replayModel = createModel();
			var counterExampleModel = createModel();

			CopyFaultActivationStates(replayModel);
			CopyFaultActivationStates(counterExampleModel);

			// Prepend the construction state to the path; if the path is null, at least one further state must be added
			// to enable counter example debugging.
			// Also, get the replay information, i.e., the nondeterministic choices that were made on the path; if the path is null,
			// we still have to get the choices that caused the problem.

			if (path == null)
			{
				path = new[] { ConstructionState, new byte[StateVectorSize] };
				return new CounterExample(counterExampleModel, path, new[] { GetLastChoices() }, endsWithException);
			}

			path = new[] { ConstructionState }.Concat(path).ToArray();
			var replayInfo = replayModel.GenerateReplayInformation(path, endsWithException);
			return new CounterExample(counterExampleModel, path, replayInfo, endsWithException);
		}

		/// <summary>
		///   Generates the replay information for the <paramref name="trace" />.
		/// </summary>
		/// <param name="trace">The trace the replay information should be generated for.</param>
		/// <param name="endsWithException">Indicates whether the trace ends with an exception being thrown.</param>
		private int[][] GenerateReplayInformation(byte[][] trace, bool endsWithException)
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
						if (i == 0)
							ExecuteInitialStep();
						else
							ExecuteStep();
					}
					catch (Exception)
					{
						Requires.That(endsWithException, "Unexpected exception.");
						Requires.That(i == trace.Length - 2, "Unexpected exception.");

						info[i] = ChoiceResolver.GetChoices().ToArray();
						break;
					}

					NotifyFaultActivations();
					Serialize(targetState);

					// Compare the target states; if they match, we've found the correct transition
					var areEqual = true;
					for (var j = _stateHeaderBytes; j < StateVectorSize; ++j)
						areEqual &= targetState[j] == trace[i + 1][j];

					if (!areEqual)
						continue;

					info[i] = ChoiceResolver.GetChoices().ToArray();
					break;
				}

				Requires.That(info[i] != null, $"Unable to generate replay information for step {i + 1} of {trace.Length}.");
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

			ChoiceResolver.Clear();
			ChoiceResolver.PrepareNextState();
			ChoiceResolver.SetChoices(replayInformation);

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
			return ChoiceResolver.GetChoices().ToArray();
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

		/// <summary>
		///   Creates a <see cref="RuntimeModel" /> instance from the <paramref name="model" /> and the <paramref name="formulas" />.
		/// </summary>
		/// <param name="model">The model the runtime model should be created for.</param>
		/// <param name="formulas">The formulas the model should be able to check.</param>
		internal static RuntimeModel Create(ModelBase model, params Formula[] formulas)
		{
			Requires.NotNull(formulas, nameof(formulas));

			var serializer = new RuntimeModelSerializer();
			serializer.Serialize(model, formulas);
			return serializer.Load();
		}
	}
}