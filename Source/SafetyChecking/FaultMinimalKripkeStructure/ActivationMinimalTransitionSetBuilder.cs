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

namespace ISSE.SafetyChecking.FaultMinimalKripkeStructure
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;
	using ExecutableModel;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using Utilities;

	/// <summary>
	///   Creates an activation-minimal set of <see cref="CandidateTransition" /> instances.
	/// </summary>
	internal sealed unsafe class ActivationMinimalTransitionSetBuilder<TExecutableModel> : DisposableObject where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private const int ProbeThreshold = 1000;
		private readonly long _capacity;
		private readonly FaultSetInfo* _faults;
		private readonly MemoryBuffer _faultsBuffer = new MemoryBuffer();
		private readonly Func<bool>[] _formulas;
		private readonly MemoryBuffer _hashedStateBuffer = new MemoryBuffer();
		private readonly byte* _hashedStateMemory;
		private readonly int* _lookup;
		private readonly MemoryBuffer _lookupBuffer = new MemoryBuffer();
		private readonly int _stateVectorSize;
		private readonly List<uint> _successors;
		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly CandidateTransition* _transitions;
		private int _computedCount;
		private int _count;
		private int _nextFaultIndex;

		/// <summary>
		///   A storage where temporal states can be saved to.
		/// </summary>
		private readonly TemporaryStateStorage _temporalStateStorage;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="temporalStateStorage">A storage where temporal states can be saved to.</param>
		/// <param name="capacity">The maximum number of successors that can be cached.</param>
		/// <param name="formulas">The formulas that should be checked for all successor states.</param>
		public ActivationMinimalTransitionSetBuilder(TemporaryStateStorage temporalStateStorage, long capacity, params Func<bool>[] formulas)
		{
			Requires.NotNull(temporalStateStorage, nameof(temporalStateStorage));
			Requires.NotNull(formulas, nameof(formulas));
			Requires.That(formulas.Length < 32, "At most 32 formulas are supported.");
			Requires.That(capacity <= (1 << 30), nameof(capacity), $"Maximum supported capacity is {1 << 30}.");

			_temporalStateStorage = temporalStateStorage;
			_stateVectorSize = temporalStateStorage.AnalysisModelStateVectorSize;
			_formulas = formulas;

			_transitionBuffer.Resize(capacity * sizeof(CandidateTransition), zeroMemory: false);
			_transitions = (CandidateTransition*)_transitionBuffer.Pointer;

			_lookupBuffer.Resize(capacity * sizeof(int), zeroMemory: false);
			_faultsBuffer.Resize(capacity * sizeof(FaultSetInfo), zeroMemory: false);
			_hashedStateBuffer.Resize(capacity * _stateVectorSize, zeroMemory: false);

			_successors = new List<uint>();
			_capacity = capacity;

			_lookup = (int*)_lookupBuffer.Pointer;
			_faults = (FaultSetInfo*)_faultsBuffer.Pointer;
			_hashedStateMemory = _hashedStateBuffer.Pointer;

			for (var i = 0; i < capacity; ++i)
				_lookup[i] = -1;
		}

		/// <summary>
		///   Adds a transition to the <paramref name="model" />'s current state.
		/// </summary>
		/// <param name="model">The model the transition should be added for.</param>
		public void Add(ExecutableModel<TExecutableModel> model)
		{
			if (_count >= _capacity)
				throw new OutOfMemoryException("Unable to store an additional transition. Try increasing the successor state capacity.");

			++_computedCount;

			// 1. Serialize the model's computed state; that is the successor state of the transition's source state
			//    modulo any changes resulting from notifications of fault activations
			var successorState = _temporalStateStorage.GetFreeTemporalSpaceAddress();
			var activatedFaults = FaultSet.FromActivatedFaults(model.NondeterministicFaults);
			model.Serialize(successorState);

			// 2. Make sure the transition we're about to add is activation-minimal
			if (!Add(successorState, activatedFaults))
				return;

			// 3. Execute fault activation notifications and serialize the updated state if necessary
			if (model.NotifyFaultActivations())
				model.Serialize(successorState);

			// 4. Store the transition
			_transitions[_count] = new CandidateTransition
			{
				TargetStatePointer = successorState,
				Formulas = new StateFormulaSet(_formulas),
				ActivatedFaults = activatedFaults,
				Flags = TransitionFlags.IsValidFlag,
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
			_computedCount = 0;

			foreach (var state in _successors)
				_lookup[state] = -1;

			_successors.Clear();
			_nextFaultIndex = 0;
		}

		/// <summary>
		///   Adds the <paramref name="successorState" /> to the transition set if neccessary, reached using the
		///   <paramref name="activatedFaults" />.
		/// </summary>
		/// <param name="successorState">The successor state that should be added.</param>
		/// <param name="activatedFaults">The faults activated by the transition to reach the state.</param>
		private bool Add(byte* successorState, FaultSet activatedFaults)
		{
			var hash = MemoryBuffer.Hash(successorState, _stateVectorSize, 0);
			for (var i = 1; i < ProbeThreshold; ++i)
			{
				var stateHash = MemoryBuffer.Hash((byte*)&hash, sizeof(int), i * 8345723) % _capacity;
				var faultIndex = _lookup[stateHash];

				// If we don't know the state yet, set everything up and add the transition
				if (faultIndex == -1)
				{
					_successors.Add((uint)stateHash);
					AddFaultMetadata(stateHash, -1);
					MemoryBuffer.Copy(successorState, _hashedStateMemory + stateHash * _stateVectorSize, _stateVectorSize);

					return true;
				}

				// If there is a hash conflict, try again
				if (!MemoryBuffer.AreEqual(successorState, _hashedStateMemory + stateHash * _stateVectorSize, _stateVectorSize))
					continue;

				// The transition has an already-known target state; it might have to be added or invalidate previously found transitions
				return UpdateTransitions(stateHash, activatedFaults, faultIndex);
			}

			throw new OutOfMemoryException(
				"Failed to find an empty hash table slot within a reasonable amount of time. Try increasing the successor state capacity.");
		}

		/// <summary>
		///   Adds the current transition if it is activation-minimal. Previously found transition might have to be removed if they are
		///   no longer activation-minimal.
		/// </summary>
		private bool UpdateTransitions(long stateHash, FaultSet activatedFaults, int faultIndex)
		{
			bool addTransition;
			bool addFaults;
			bool cleanupTransitions;

			ClassifyActivatedFaults(activatedFaults, faultIndex, out addTransition, out addFaults, out cleanupTransitions);

			if (cleanupTransitions)
				CleanupTransitions(activatedFaults, faultIndex, stateHash);

			if (addFaults)
				AddFaultMetadata(stateHash, _lookup[stateHash]);

			return addTransition;
		}

		/// <summary>
		///   Removes all transitions that are no longer activation minimal due to the current transition.
		/// </summary>
		private void CleanupTransitions(FaultSet activatedFaults, int faultIndex, long stateHash)
		{
			var current = faultIndex;
			var nextPointer = &_lookup[stateHash];

			while (current != -1)
			{
				var faultSet = &_faults[current];

				// Remove the fault set and the corresponding transition if it is a proper subset of the activated faults
				if (activatedFaults.IsSubsetOf(faultSet->Transition->ActivatedFaults) && activatedFaults != faultSet->Transition->ActivatedFaults)
				{
					faultSet->Transition->Flags = TransitionFlags.RemoveValid(faultSet->Transition->Flags);
					*nextPointer = faultSet->NextSet;
				}

				if (nextPointer != &_lookup[stateHash])
					nextPointer = &faultSet->NextSet;

				current = faultSet->NextSet;
			}
		}

		/// <summary>
		///   Classifies how the transition set must be updated.
		/// </summary>
		private void ClassifyActivatedFaults(FaultSet activatedFaults, int faultIndex, out bool addTransition, out bool addFaults,
											 out bool cleanupTransitions)
		{
			addFaults = false;
			cleanupTransitions = false;

			// Basic invariant of the fault list: it contains only sets of activation-minimal faults
			while (faultIndex != -1)
			{
				var faultSet = &_faults[faultIndex];
				faultIndex = faultSet->NextSet;

				// If the fault set is a subset of the activated faults, the current transition is not activation-minimal;
				// we can therefore safely ignore the transition; due to the list invariant, none of the remaining
				// fault sets in the list can be a superset of the activated faults because then the current fault set
				// would also be a subset of that other fault set, violating the invariant
				if (faultSet->Transition->ActivatedFaults.IsSubsetOf(activatedFaults))
				{
					addTransition = faultSet->Transition->ActivatedFaults == activatedFaults;
					return;
				}

				// If at least one of the previously added transitions that we assumed to be activation-minimal is 
				// in fact not activation-minimal, we have to clean up the transition set
				if (activatedFaults.IsSubsetOf(faultSet->Transition->ActivatedFaults))
					cleanupTransitions = true;
			}

			// If we get here, we must add the faults and the transition
			addTransition = true;
			addFaults = true;
		}

		/// <summary>
		///   Adds the fault metadata of the current transition.
		/// </summary>
		private void AddFaultMetadata(long stateHash, int nextSet)
		{
			if (_nextFaultIndex >= _capacity)
				throw new OutOfMemoryException("Unable to store an additional transition. Try increasing the successor state capacity.");

			_faults[_nextFaultIndex] = new FaultSetInfo
			{
				NextSet = nextSet,
				Transition = &_transitions[_count]
			};

			_lookup[stateHash] = _nextFaultIndex;
			_nextFaultIndex++;
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
			_hashedStateBuffer.SafeDispose();
			_faultsBuffer.SafeDispose();
			_lookupBuffer.SafeDispose();
		}

		/// <summary>
		///   Creates a <see cref="TransitionCollection" /> instance for all transitions contained in the set.
		/// </summary>
		public TransitionCollection ToCollection()
		{
			return new TransitionCollection((Transition*)_transitions, _count, _computedCount, sizeof(CandidateTransition));
		}
	}

	/// <summary>
	///   Represents an element of a linked list of activated faults.
	/// </summary>
	internal unsafe struct FaultSetInfo
	{
		public int NextSet;
		public CandidateTransition* Transition;
	}
}