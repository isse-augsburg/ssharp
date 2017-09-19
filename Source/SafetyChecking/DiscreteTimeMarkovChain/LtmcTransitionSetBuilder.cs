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

namespace ISSE.SafetyChecking.DiscreteTimeMarkovChain
{
	using System;
	using System.Runtime.CompilerServices;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using Utilities;
	using ExecutableModel;

	/// <summary>
	///   Creates an activation-minimal set of <see cref="CandidateTransition"/> instances.
	/// </summary>
	internal sealed unsafe class LtmcTransitionSetBuilder<TExecutableModel> : DisposableObject where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private readonly Func<bool>[] _formulas;
		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly LtmcTransition* _transitions;
		private readonly long _capacity;
		private int _count;

		/// <summary>
		///   A storage where temporal states can be saved to.
		/// </summary>
		private readonly TemporalStateStorage _temporalStateStorage;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="temporalStateStorage">A storage where temporal states can be saved to.</param>
		/// <param name="capacity">The maximum number of successors that can be cached.</param>
		/// <param name="formulas">The formulas that should be checked for all successor states.</param>
		public LtmcTransitionSetBuilder(TemporalStateStorage temporalStateStorage, long capacity, params Func<bool>[] formulas)
		{
			Requires.NotNull(temporalStateStorage, nameof(temporalStateStorage));
			Requires.NotNull(formulas, nameof(formulas));
			Requires.That(formulas.Length < 32, "At most 32 formulas are supported.");
			Requires.That(capacity <= (1 << 30), nameof(capacity), $"Maximum supported capacity is {1 << 30}.");

			_temporalStateStorage = temporalStateStorage;
			_formulas = formulas;
			_capacity = capacity;

			_transitionBuffer.Resize(capacity * sizeof(LtmcTransition), zeroMemory: false);
			_transitions = (LtmcTransition*)_transitionBuffer.Pointer;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte* AddState(byte* stateToFind)
		{
			// Try to find a matching state. If not found, then add a new one
			byte* targetState;

			if (_temporalStateStorage.TryToFindState(stateToFind, out targetState))
				return targetState;

			targetState = _temporalStateStorage.GetFreeTemporalSpaceAddress();
			MemoryBuffer.Copy(stateToFind,targetState, _temporalStateStorage.StateVectorSize);

			return targetState;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AddTransition(byte* stateToAdd, StateFormulaSet formulas, FaultSet activatedFaults, double probability)
		{
			// Try to find a matching transition. If not found, then add a new one
			var successorState = AddState(stateToAdd);

			for (var i = 0; i < _count; i++)
			{
				var candidateTransition = _transitions[i];
				if (candidateTransition.TargetStatePointer == successorState &&
					candidateTransition.Formulas == formulas &&
					candidateTransition.ActivatedFaults == activatedFaults)
				{
					candidateTransition.Probability += probability;
					_transitions[i] = candidateTransition;
					return;
				}
			}

			if (_count >= _capacity)
				throw new OutOfMemoryException("Unable to store an additional transition. Try increasing the successor state capacity.");

			_transitions[_count] = new LtmcTransition
			{
				TargetStatePointer = successorState,
				Formulas = formulas,
				ActivatedFaults = activatedFaults,
				Flags = TransitionFlags.IsValidFlag,
				Probability = probability
			};
			++_count;
		}

		/// <summary>
		///   Adds a transition to the <paramref name="model" />'s current state.
		/// </summary>
		/// <param name="model">The model the transition should be added for.</param>
		/// <param name="probability">The probability of the transition.</param>
		public void Add(ExecutableModel<TExecutableModel> model, double probability)
		{
			// 1. Notify all fault activations, so that the correct activation is set in the run time model
			//    (Needed to persist persistent faults)
			model.NotifyFaultActivations();
			
			// 2. Serialize the model's computed state; that is the successor state of the transition's source state
			//    _including_ any changes resulting from notifications of fault activations
			var temporaryState = _temporalStateStorage.ZeroedSpecialAddress1();
			model.Serialize(temporaryState);

			// 3. Store the transition
			var activatedFaults = FaultSet.FromActivatedFaults(model.NondeterministicFaults);
			AddTransition(temporaryState, new StateFormulaSet(_formulas), activatedFaults, probability);
		}

		/// <summary>
		///   Clears the cache, removing all cached states.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			_count = 0;
		}
		
		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_transitionBuffer.SafeDispose();
		}

		/// <summary>
		///   Creates a <see cref="TransitionCollection" /> instance for all transitions contained in the set.
		/// </summary>
		public TransitionCollection ToCollection()
		{
			return new TransitionCollection((Transition*)_transitions, _count, _count, sizeof(LtmcTransition));
		}
	}
}