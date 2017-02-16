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

namespace ISSE.SafetyChecking.StateGraphModel
{
	using System.Runtime.CompilerServices;
	using Utilities;
	using AnalysisModel;

	/// <summary>
	///   Creates a set of <see cref="CandidateTransition" /> instances.
	/// </summary>
	internal sealed unsafe class StateGraphTransitionSetBuilder : DisposableObject
	{
		private readonly int _stateVectorSize;
		private readonly MemoryBuffer _targetStateBuffer = new MemoryBuffer();
		private readonly byte* _targetStateMemory;
		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly CandidateTransition* _transitions;
		private int _count;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="stateVectorSize">The size of the state vector in bytes.</param>
		/// <param name="capacity">The maximum number of successors that can be cached.</param>
		public StateGraphTransitionSetBuilder(int stateVectorSize, long capacity)
		{
			Requires.That(capacity <= (1 << 30), nameof(capacity), $"Maximum supported capacity is {1 << 30}.");

			_stateVectorSize = stateVectorSize;

			_transitionBuffer.Resize(capacity * sizeof(CandidateTransition), zeroMemory: false);
			_transitions = (CandidateTransition*)_transitionBuffer.Pointer;

			_targetStateBuffer.Resize(capacity * _stateVectorSize, zeroMemory: false);
			_targetStateMemory = _targetStateBuffer.Pointer;
		}

		/// <summary>
		///   Adds a transition to the <paramref name="successorState" /> to the set.
		/// </summary>
		/// <param name="successorState">The successor state of the transition that should be added.</param>
		/// <param name="activatedFaults">The faults activated by the transition that should be added.</param>
		/// <param name="formulas">The formulas holding in the successor state of the transition that should be added.</param>
		public void Add(byte* successorState, FaultSet activatedFaults, StateFormulaSet formulas)
		{
			var targetState = _targetStateMemory + _count * _stateVectorSize;
			MemoryBuffer.Copy(successorState, targetState, _stateVectorSize);

			_transitions[_count] = new CandidateTransition
			{
				TargetState = targetState,
				Formulas = formulas,
				ActivatedFaults = activatedFaults,
				IsValid = true,
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
			return new TransitionCollection((Transition*)_transitions, _count, _count, sizeof(CandidateTransition));
		}
	}
}