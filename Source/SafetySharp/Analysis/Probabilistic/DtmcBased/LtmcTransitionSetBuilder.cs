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

namespace SafetySharp.Analysis.ModelChecking.Transitions
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;
	using Runtime;
	using Utilities;

	/// <summary>
	///   Creates an activation-minimal set of <see cref="CandidateTransition"/> instances.
	/// </summary>
	internal sealed unsafe class LtmcTransitionSetBuilder : DisposableObject
	{
		private readonly Func<bool>[] _formulas;
		private readonly int _stateVectorSize;
		private readonly MemoryBuffer _targetStateBuffer = new MemoryBuffer();
		private readonly byte* _targetStateMemory;
		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly LtmcTransition* _transitions;
		private int _count;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the successors are computed for.</param>
		/// <param name="capacity">The maximum number of successors that can be cached.</param>
		/// <param name="formulas">The formulas that should be checked for all successor states.</param>
		public LtmcTransitionSetBuilder(ExecutableModel model, long capacity, params Func<bool>[] formulas)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formulas, nameof(formulas));
			Requires.That(formulas.Length < 32, "At most 32 formulas are supported.");
			Requires.That(capacity <= (1 << 30), nameof(capacity), $"Maximum supported capacity is {1 << 30}.");

			_stateVectorSize = model.StateVectorSize;
			_formulas = formulas;

			_transitionBuffer.Resize(capacity * sizeof(LtmcTransition), zeroMemory: false);
			_transitions = (LtmcTransition*)_transitionBuffer.Pointer;

			_targetStateBuffer.Resize(capacity * model.StateVectorSize, zeroMemory: true);
			_targetStateMemory = _targetStateBuffer.Pointer;
		}

		/// <summary>
		///   Adds a transition to the <paramref name="model" />'s current state.
		/// </summary>
		/// <param name="model">The model the transition should be added for.</param>
		/// <param name="probability">The probability of the transition.</param>
		public void Add(ExecutableModel model, double probability)
		{
			// 1. Notify all fault activations, so that the correct activation is set in the run time model
			//    (Needed to persist persistent faults)
			model.NotifyFaultActivations();
			
			// 2. Serialize the model's computed state; that is the successor state of the transition's source state
			//    _including_ any changes resulting from notifications of fault activations
			var successorState = _targetStateMemory + _stateVectorSize * _count;
			model.Serialize(successorState);

			// 3. Store the transition
			var activatedFaults = FaultSet.FromActivatedFaults(model.NondeterministicFaults);
			_transitions[_count] = new LtmcTransition
			{
				TargetState = successorState,
				Formulas = new StateFormulaSet(_formulas),
				ActivatedFaults = activatedFaults,
				IsValid = true,
				Probability = probability
			};
			++_count;
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
			_targetStateBuffer.SafeDispose();
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