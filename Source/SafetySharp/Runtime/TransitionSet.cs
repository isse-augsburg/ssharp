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
	using System.Collections;
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;
	using Analysis;
	using JetBrains.Annotations;
	using Modeling;
	using Serialization;
	using Utilities;

	/// <summary>
	///   Caches an activation-minimal set of transitions.
	/// </summary>
	internal sealed unsafe class TransitionSet : DisposableObject
	{
		private const int ProbeThreshold = 1000;
		private readonly int _capacity;
		private readonly TargetStateGroupElement* _targetStateGroupElements;
		private readonly MemoryBuffer _targetStateGroupElementsBuffer = new MemoryBuffer();
		private readonly Func<bool>[] _formulas;
		private readonly RewardRetriever[] _rewards;
		private readonly MemoryBuffer _hashedStateBuffer = new MemoryBuffer();
		private readonly byte* _hashedStateMemory;
		private readonly int* _lookup;
		private readonly MemoryBuffer _lookupBuffer = new MemoryBuffer();
		private readonly int _stateVectorSize;
		private readonly List<uint> _stateHashesOfTargetStateGroups;

		private readonly MemoryBuffer _tempStateBuffer = new MemoryBuffer();
		private readonly byte* _tempStateMemoryNotified;
		private readonly byte* _tempStateMemoryUnnotified;
		private readonly MemoryBuffer _targetStateBuffer = new MemoryBuffer();
		private readonly byte* _targetStateMemory;

		private readonly TransitionEnumerator Enumerator;

		private int _nextTargetStateBufferIndex;
		private int _nextTargetStateGroupIndex;

		public TransitionMinimizationMode TransitionMinimizationMode { get; set; } = TransitionMinimizationMode.RemoveNonActivationMinimalTransitions;
		
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the successors are computed for.</param>
		/// <param name="capacity">The maximum number of successors that can be cached.</param>
		/// <param name="formulas">The formulas that should be checked for all successor states.</param>
		public TransitionSet(RuntimeModel model, int capacity, Func<bool>[] formulas, RewardRetriever[] rewards)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formulas, nameof(formulas));
			Requires.That(formulas.Length < 32, "At most 32 formulas are supported.");
			Requires.That(capacity <= (1 << 30), nameof(capacity), $"Maximum supported capacity is {1 << 30}.");

			_stateVectorSize = model.StateVectorSize;
			_formulas = formulas;
			_rewards = rewards;


			_tempStateBuffer.Resize(2 * model.StateVectorSize, zeroMemory: true);
			_tempStateMemoryNotified = _tempStateBuffer.Pointer;
			_tempStateMemoryUnnotified = _tempStateBuffer.Pointer + _stateVectorSize;
			_targetStateBuffer.Resize(capacity * model.StateVectorSize, zeroMemory: true);
			_targetStateMemory = _targetStateBuffer.Pointer;

			_lookupBuffer.Resize(capacity * sizeof(int), zeroMemory: false);
			_targetStateGroupElementsBuffer.Resize(capacity * sizeof(TargetStateGroupElement), zeroMemory: false);
			_hashedStateBuffer.Resize(capacity * _stateVectorSize, zeroMemory: true);

			_stateHashesOfTargetStateGroups = new List<uint>(capacity);
			_capacity = capacity;

			_lookup = (int*)_lookupBuffer.Pointer;
			_targetStateGroupElements = (TargetStateGroupElement*)_targetStateGroupElementsBuffer.Pointer;
			_hashedStateMemory = _hashedStateBuffer.Pointer;

			Enumerator = new TransitionEnumerator(this);

			for (var i = 0; i < capacity; ++i)
				_lookup[i] = -1;
		}
		
		public TransitionSet(RuntimeModel model, int capacity)
			: this(model,capacity,new Func<bool>[] { }, new RewardRetriever[] {} )
		{
		}

		/// <summary>
		///   Gets the total number of computed transitions.
		/// </summary>
		public int ComputedTransitionCount { get; private set; }

		/// <summary>
		///   Adds a transition to the <paramref name="model" />'s current state.
		/// </summary>
		/// <param name="model">The model the transition should be added for.</param>
		public void Add(RuntimeModel model)
		{
			++ComputedTransitionCount;

			// 1. Serialize the model's computed state; that is the successor state of the transition's source state
			//    modulo any changes resulting from notifications of fault activations
			var activatedFaults = FaultSet.FromActivatedFaults(model.Faults);
			model.Serialize(_tempStateMemoryUnnotified);
			
			// 2. Execute fault activation notifications, serialize the updated state if necessary, and store the transition
			if (model.NotifyFaultActivations())
				model.Serialize(_tempStateMemoryNotified);
			else
				MemoryBuffer.Copy(_tempStateMemoryUnnotified, _tempStateMemoryNotified, _stateVectorSize);

			// 3. Store Transition
			StoreTransition(activatedFaults,model.GetProbability());
		}

		/// <summary>
		///   Clears the cache, removing all cached states.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			ComputedTransitionCount = 0;

			foreach (var state in _stateHashesOfTargetStateGroups)
				_lookup[state] = -1;

			_stateHashesOfTargetStateGroups.Clear();
			_nextTargetStateGroupIndex = 0;
			_nextTargetStateBufferIndex = 0;
		}

		/// <summary>
		///   Adds the current transition to the transition set if necessary, reached using the
		///   <paramref name="activatedFaults" />.
		/// </summary>
		/// <param name="activatedFaults">The faults activated by the transition to reach the state.</param>
		private void StoreTransition(FaultSet activatedFaults,Probability probability)
		{
			var targetStateGroup = (TransitionMinimizationMode == TransitionMinimizationMode.RemoveNonActivationMinimalTransitions) ? _tempStateMemoryUnnotified : _tempStateMemoryNotified;

			var hash = MemoryBuffer.Hash(targetStateGroup, _stateVectorSize, 0);
			for (var i = 1; i < ProbeThreshold; ++i)
			{
				var stateHash = MemoryBuffer.Hash((byte*)&hash, sizeof(int), i * 8345723) % _capacity;
				var targetStateGroupIndex = _lookup[stateHash];

				// If we don't know the state yet, set everything up and add the transition
				if (targetStateGroupIndex == -1)
				{
					_stateHashesOfTargetStateGroups.Add((uint)stateHash);
					AddTargetStateGroupElement(stateHash, activatedFaults, probability, - 1);
					MemoryBuffer.Copy(targetStateGroup, _hashedStateMemory + stateHash * _stateVectorSize, _stateVectorSize);

					return;
				}

				// If there is a hash conflict, try again
				if (!MemoryBuffer.AreEqual(targetStateGroup, _hashedStateMemory + stateHash * _stateVectorSize, _stateVectorSize))
					continue;

				// The transition has an already-known target state; it might have to be added or invalidate previously found transitions
				UpdateTargetStateGroup(stateHash, activatedFaults, probability, targetStateGroupIndex);
				return;
			}

			throw new OutOfMemoryException(
				"Failed to find an empty hash table slot within a reasonable amount of time. Try increasing the successor state capacity.");
		}

		/// <summary>
		///   Adds the current transition if it is activation-minimal. Previously found transition might have to be removed if they are
		///   no longer activation-minimal.
		/// </summary>
		private void UpdateTargetStateGroup(long stateHash, FaultSet activatedFaults, Probability probability,int targetStateGroupIndex)
		{
			bool addTransition;
			bool addFaults;
			bool cleanupTransitions;

			if (TransitionMinimizationMode == TransitionMinimizationMode.RemoveNonActivationMinimalTransitions)
			{
				ClassifyActivatedFaults(activatedFaults, targetStateGroupIndex, out addTransition, out addFaults, out cleanupTransitions);

				if (cleanupTransitions)
					CleanupTargetStateGroup(activatedFaults, targetStateGroupIndex, stateHash);

				if (addFaults || addTransition)
					AddTargetStateGroupElement(stateHash, activatedFaults, probability, _lookup[stateHash]);
			}
			else
			{
				AddTargetStateGroupElement(stateHash, activatedFaults, probability, _lookup[stateHash]);
			}
		}

		/// <summary>
		///   Removes all transitions that are no longer activation minimal due to the current transition.
		/// </summary>
		private void CleanupTargetStateGroup(FaultSet activatedFaults, int targetStateGroupIndex, long stateHash)
		{
			var current = targetStateGroupIndex;
			var nextPointer = &_lookup[stateHash];

			while (current != -1)
			{
				var faultSet = &_targetStateGroupElements[current];

				// Remove the fault set and the corresponding transition if it is a proper subset of the activated faults
				if (activatedFaults.IsSubsetOf(faultSet->ActivatedFaults) && activatedFaults != faultSet->ActivatedFaults)
				{
					faultSet->IsValid = false;
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
				var faultSet = &_targetStateGroupElements[faultIndex];
				faultIndex = faultSet->NextSet;

				// If the fault set is a subset of the activated faults, the current transition is not activation-minimal;
				// we can therefore safely ignore the transition; due to the list invariant, none of the remaining
				// fault sets in the list can be a superset of the activated faults because then the current fault set
				// would also be a subset of that other fault set, violating the invariant
				if (faultSet->ActivatedFaults.IsSubsetOf(activatedFaults))
				{
					addTransition = faultSet->ActivatedFaults == activatedFaults;
					return;
				}

				// If at least one of the previously added transitions that we assumed to be activation-minimal is 
				// in fact not activation-minimal, we have to clean up the transition set
				if (activatedFaults.IsSubsetOf(faultSet->ActivatedFaults))
					cleanupTransitions = true;
			}

			// If we get here, we must add the faults and the transition
			addTransition = true;
			addFaults = true;
		}

		/// <summary>
		///   Adds the fault metadata of the current transition.
		/// </summary>
		private void AddTargetStateGroupElement(long stateHash, FaultSet activatedFaults, Probability probability, int nextSet)
		{
			if (_nextTargetStateBufferIndex >= _capacity)
				throw new OutOfMemoryException("Out of memory. Try increasing the successor state capacity.");

			var targetState = _targetStateMemory + _nextTargetStateBufferIndex * _stateVectorSize;
			MemoryBuffer.Copy(_tempStateMemoryNotified, targetState, _stateVectorSize); // copy the real target state where all fault notifications have been enabled

			if (_nextTargetStateGroupIndex >= _capacity)
				throw new OutOfMemoryException("Out of memory. Try increasing the successor state capacity.");

			_targetStateGroupElements[_nextTargetStateGroupIndex] = new TargetStateGroupElement
			{
				ActivatedFaults = activatedFaults,
				Formulas = new StateFormulaSet(_formulas),
				Reward0 = _rewards.Length >= 1 ? _rewards[0].Retriever() : default(Reward),
				Reward1 = _rewards.Length >= 2 ? _rewards[1].Retriever() : default(Reward),
				NextSet = nextSet,
				TargetState = targetState,
				Probability = probability,
				IsValid = true,
			};

			_lookup[stateHash] = _nextTargetStateGroupIndex;
			_nextTargetStateGroupIndex++;
			_nextTargetStateBufferIndex++;
		}

		// Note: We only have one single TransitionEnumerator
		internal TransitionEnumerator GetResetedEnumerator()
		{
			Enumerator.Reset();
			return Enumerator;
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;
			
			_tempStateBuffer.SafeDispose();
			_targetStateBuffer.SafeDispose();
			_targetStateGroupElementsBuffer.SafeDispose();
			_lookupBuffer.SafeDispose();
		}

		/// <summary>
		///   Represents a transition.
		/// </summary>
		/// TODO: Maybe use FieldOffsets https://msdn.microsoft.com/de-de/library/system.runtime.interopservices.fieldoffsetattribute(v=vs.110).aspx
		internal struct Transition
		{
			/// <summary>
			///   The transition's target state.
			/// </summary>
			public byte* TargetState;

			/// <summary>
			///   The state formulas holding in the target successorState.
			/// </summary>
			public StateFormulaSet Formulas;

			public Reward Reward0;
			public Reward Reward1;

			public bool IsValid;

			public Probability Probability;
		}

		/// <summary>
		///   Represents an element of a linked list of activated faults.
		/// </summary>
		internal struct TargetStateGroupElement
		{
			public FaultSet ActivatedFaults;
			public StateFormulaSet Formulas;

			public Reward Reward0;
			public Reward Reward1;

			public int NextSet;
			//public Transition* Transition;

			public bool IsValid;

			public Probability Probability;

			public byte* TargetState; //The target state of this fault with activated
		}
		
		internal class TransitionEnumerator : IEnumerator<Transition>
		{
			private TransitionSet _transitionSet;
			
			private TargetStateGroupElement? currentElement;

			private int _targetStateGroupHashIterator;

			public TransitionEnumerator(TransitionSet transitionSet)
			{
				_transitionSet = transitionSet;
				Reset();
			}

			/// <summary>
			/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
			/// </summary>
			public void Dispose()
			{
			}

			private bool MoveToNextStateGroup()
			{
				if (_transitionSet._stateHashesOfTargetStateGroups.Count <= _targetStateGroupHashIterator)
				{
					currentElement = null;
					return false;
				}
				var stateGroupHash = _transitionSet._stateHashesOfTargetStateGroups[_targetStateGroupHashIterator++];
				var nextPointer = _transitionSet._lookup[stateGroupHash];
				if (nextPointer == -1)
				{
					currentElement = null;
					return false;
				}
				currentElement = _transitionSet._targetStateGroupElements[nextPointer];
				return true;
			}

			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns>
			/// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
			/// </returns>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			public bool MoveNext()
			{
				if (currentElement == null)
				{
					return MoveToNextStateGroup();
				}
				var nextPointer = currentElement.Value.NextSet;

				if (nextPointer == -1)
					return MoveToNextStateGroup();
				currentElement = _transitionSet._targetStateGroupElements[nextPointer];
				return true;
			}

			/// <summary>
			/// Sets the enumerator to its initial position, which is before the first element in the collection.
			/// </summary>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			public void Reset()
			{
				_targetStateGroupHashIterator = 0;
				currentElement=null;
			}

			/// <summary>
			/// Gets the element in the collection at the current position of the enumerator.
			/// </summary>
			/// <returns>
			/// The element in the collection at the current position of the enumerator.
			/// </returns>
			public Transition Current
			{
				get
				{
					var currentTargetStateGroupElement = currentElement.Value;
					return new Transition
					{
						TargetState = currentTargetStateGroupElement.TargetState,
						Formulas = currentTargetStateGroupElement.Formulas,
						Reward0=currentTargetStateGroupElement.Reward0,
						Reward1=currentTargetStateGroupElement.Reward1,
						Probability = currentTargetStateGroupElement.Probability,
						IsValid =  currentTargetStateGroupElement.IsValid
					};
				}
			}

			/// <summary>
			/// Gets the current element in the collection.
			/// </summary>
			/// <returns>
			/// The current element in the collection.
			/// </returns>
			object IEnumerator.Current
			{
				get { return Current; }
			}
		}
	}
}