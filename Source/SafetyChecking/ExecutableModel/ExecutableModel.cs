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

namespace ISSE.SafetyChecking.ExecutableModel
{
	using System;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Runtime.CompilerServices;
	using AnalysisModel;
	using ExecutedModel;
	using Formula;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Represents a runtime model that can be used for model checking or simulation.
	/// </summary>
	public abstract unsafe class ExecutableModel<TExecutableModel> : DisposableObject where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		/// <summary>
		///   Deserializes a state of the model.
		/// </summary>
		protected SerializationDelegate _deserialize;

		/// <summary>
		///   The faults contained in the model.
		/// </summary>
		public Fault[] Faults { get; protected set; }

		/// <summary>
		///   Restricts the ranges of the model's state variables.
		/// </summary>
		protected Action _restrictRanges;

		/// <summary>
		///   Serializes a state of the model.
		/// </summary>
		protected SerializationDelegate _serialize;

		/// <summary>
		///   The number of bytes reserved at the beginning of each state vector by the model checker.
		/// </summary>
		protected int StateHeaderBytes { get; }

		/// <summary>
		///   The state constraints which describe allowed states. When any stateConstraint returns false the state is deleted.
		/// </summary>
		public Func<bool>[] StateConstraints { protected set; get; }

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="serializedData">The serialized data describing the model.</param>
		/// <param name="stateHeaderBytes">
		///   The number of bytes that should be reserved at the beginning of each state vector for the model checker tool.
		/// </param>
		internal ExecutableModel(int stateHeaderBytes = 0)
		{
			Requires.That(stateHeaderBytes % 4 == 0, nameof(stateHeaderBytes), "Expected a multiple of 4.");			
			StateHeaderBytes = stateHeaderBytes;
		}

		protected void CheckConsistencyAfterInitialization()
		{
			FaultSet.CheckFaultCount(Faults.Length);
			StateFormulaSet.CheckFormulaCount(AtomarPropositionFormulas.Length);
		}

		protected void InitializeConstructionState()
		{
			ConstructionState = new byte[StateVectorSize];
			fixed (byte* state = ConstructionState)
			{
				Serialize(state);
				_restrictRanges();
			}
		}
		
		/// <summary>
		///   Gets the construction state of the model.
		/// </summary>
		public byte[] ConstructionState { get; private set; }
		

		/// <summary>
		///   Gets the buffer the model was deserialized from.
		/// </summary>
		public byte[] SerializedModel { get; set; }
		
		/// <summary>
		///   The formulas that are checked on the model.
		/// </summary>
		public Formula[] Formulas { get; protected set; }

		/// <summary>
		///   Gets the size of the state vector in bytes. The size is always a multiple of 4.
		/// </summary>
		public abstract int StateVectorSize { get; }
		
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
		public abstract AtomarPropositionFormula[] AtomarPropositionFormulas { get; }

		/// <summary>
		///   Updates the activation states of the model's faults.
		/// </summary>
		/// <param name="getActivation">The callback that should be used to determine a fault's activation state.</param>
		internal void ChangeFaultActivations(Func<Fault, Activation> getActivation)
		{
			foreach (var fault in Faults)
				fault.Activation = getActivation(fault);

			UpdateFaultSets();
		}

		/// <summary>
		///   Updates the fault sets in accordance with the fault's actual activation states.
		/// </summary>
		internal void UpdateFaultSets()
		{
			NondeterministicFaults = Faults.Where(fault => fault.Activation == Activation.Nondeterministic).ToArray();
			ActivationSensitiveFaults = NondeterministicFaults.Where(fault => fault.RequiresActivationNotification).ToArray();
		}

		/// <summary>
		///   Copies the fault activation states of this instance to <paramref name="target" />.
		/// </summary>
		protected void CopyFaultActivationStates(TExecutableModel target)
		{
			for (var i = 0; i < Faults.Length; ++i)
				target.Faults[i].Activation = Faults[i].Activation;

			target.UpdateFaultSets();
		}

		/// <summary>
		///   Deserializes the model's state from <paramref name="serializedState" />.
		/// </summary>
		/// <param name="serializedState">The state of the model that should be deserialized.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Deserialize(byte* serializedState)
		{
			_deserialize(serializedState + StateHeaderBytes);
		}

		/// <summary>
		///   Serializes the model's state to <paramref name="serializedState" />.
		/// </summary>
		/// <param name="serializedState">The memory region the model's state should be serialized into.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Serialize(byte* serializedState)
		{
			_serialize(serializedState + StateHeaderBytes);
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
		}

		/// <summary>
		///   Computes an initial state of the model.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public abstract void ExecuteInitialStep();

		/// <summary>
		///   Updates the state of the model by executing a single step.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public abstract void ExecuteStep();

		/// <summary>
		///   Creates a counter example from the <paramref name="path" />.
		/// </summary>
		/// <param name="createModel">The factory function that can be used to create new instances of this model.</param>
		/// <param name="path">
		///   The path the counter example should be generated from. A value of <c>null</c> indicates that no
		///   transitions could be generated for the model.
		/// </param>
		/// <param name="endsWithException">Indicates whether the counter example ends with an exception.</param>
		public CounterExample<TExecutableModel> CreateCounterExample(CoupledExecutableModelCreator<TExecutableModel> createModel, byte[][] path, bool endsWithException)
		{
			Requires.NotNull(createModel, nameof(createModel));

			// We have to create new model instances to generate and initialize the counter example, otherwise hidden
			// state variables might prevent us from doing so if they somehow influence the state
			var replayModel = createModel.Create(StateHeaderBytes);
			var counterExampleModel = createModel.Create(StateHeaderBytes);
			var choiceResolver = new NondeterministicChoiceResolver();

			replayModel.SetChoiceResolver(choiceResolver);
			
			CopyFaultActivationStates(replayModel);
			CopyFaultActivationStates(counterExampleModel);

			// Prepend the construction state to the path; if the path is null, at least one further state must be added
			// to enable counter example debugging.
			// Also, get the replay information, i.e., the nondeterministic choices that were made on the path; if the path is null,
			// we still have to get the choices that caused the problem.

			if (path == null)
				path = new[] { new byte[StateVectorSize] };

			path = new[] { ConstructionState }.Concat(path).ToArray();
			var replayInfo = replayModel.GenerateReplayInformation(choiceResolver, path, endsWithException);
			return new CounterExample<TExecutableModel>(counterExampleModel, path, replayInfo, endsWithException);
		}

		internal abstract void SetChoiceResolver(ChoiceResolver choiceResolver);

		/// <summary>
		///   Generates the replay information for the <paramref name="trace" />.
		/// </summary>
		/// <param name="choiceResolver">The choice resolver that should be used to resolve nondeterministic choices.</param>
		/// <param name="trace">The trace the replay information should be generated for.</param>
		/// <param name="endsWithException">Indicates whether the trace ends with an exception being thrown.</param>
		private int[][] GenerateReplayInformation(ChoiceResolver choiceResolver, byte[][] trace, bool endsWithException)
		{
			var info = new int[trace.Length - 1][];
			var targetState = stackalloc byte[StateVectorSize];

			// We have to generate the replay info for all transitions
			for (var i = 0; i < trace.Length - 1; ++i)
			{
				choiceResolver.Clear();
				choiceResolver.PrepareNextState();

				// Try all transitions until we find the one that leads to the desired state
				while (true)
				{
					try
					{
						if (!choiceResolver.PrepareNextPath())
							break;

						fixed (byte* sourceState = trace[i])
						Deserialize(sourceState);

						if (i == 0)
							ExecuteInitialStep();
						else
							ExecuteStep();

						if (endsWithException && i == trace.Length - 2)
							continue;
					}
					catch (Exception)
					{
						Requires.That(endsWithException, "Unexpected exception.");
						Requires.That(i == trace.Length - 2, "Unexpected exception.");

						info[i] = choiceResolver.GetChoices().ToArray();
						break;
					}

					NotifyFaultActivations();
					Serialize(targetState);

					// Compare the target states; if they match, we've found the correct transition
					var areEqual = true;
					for (var j = StateHeaderBytes; j < StateVectorSize; ++j)
						areEqual &= targetState[j] == trace[i + 1][j];

					if (!areEqual)
						continue;

					info[i] = choiceResolver.GetChoices().ToArray();
					break;
				}

				Requires.That(info[i] != null, $"Unable to generate replay information for step {i + 1} of {trace.Length}.");
			}

			return info;
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

		public abstract CounterExampleSerialization<TExecutableModel> CounterExampleSerialization { get; }
		

		public abstract Expression CreateExecutableExpressionFromAtomarPropositionFormula(AtomarPropositionFormula formula);
		
		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
		}
	}
}