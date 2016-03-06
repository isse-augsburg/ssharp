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
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;
	using Utilities;

	/// <summary>
	///   Caches an activation-minimal set of transitions.
	/// </summary>
	internal sealed unsafe class TransitionSet : DisposableObject
	{
		private readonly ActivationMap _activationMap;
		private readonly Func<bool>[] _formulas;
		private readonly MemoryBuffer _stateBuffer = new MemoryBuffer();
		private readonly byte* _stateMemory;
		private readonly int _stateVectorSize;
		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly Transition* _transitions;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the successors are computed for.</param>
		/// <param name="capacity">The maximum number of successors that can be cached.</param>
		/// <param name="formulas">The formulas that should be checked for all successor states.</param>
		public TransitionSet(RuntimeModel model, int capacity, params Func<bool>[] formulas)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formulas, nameof(formulas));
			Requires.That(formulas.Length < 32, "At most 32 formulas are supported.");

			_stateVectorSize = model.StateVectorSize;
			_formulas = formulas;
			_activationMap = new ActivationMap(_stateVectorSize, capacity);

			_transitionBuffer.Resize(capacity * sizeof(Transition), zeroMemory: false);
			_transitions = (Transition*)_transitionBuffer.Pointer;

			_stateBuffer.Resize(capacity * model.StateVectorSize, zeroMemory: true);
			_stateMemory = _stateBuffer.Pointer;
		}

		/// <summary>
		///   Gets the number of activation-minimal transitions.
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		///   Gets the total number of computed transitions.
		/// </summary>
		public int ComputedTransitionCount { get; private set; }

		/// <summary>
		///   Gets the transition stored at the <paramref name="index" />.
		/// </summary>
		public Transition* this[int index] => &_transitions[index];

		/// <summary>
		///   Adds a transition to the <paramref name="model" />'s current state.
		/// </summary>
		/// <param name="model">The model the transition should be added for.</param>
		public void Add(RuntimeModel model)
		{
			++ComputedTransitionCount;

			// 1. Serialize the model's computed state; that is the successor successorState of the transition's source successorState
			//    modulo any changes resulting from notifications of fault activations
			var successorState = _stateMemory + _stateVectorSize * Count;
			var activatedFaults = GetActivatedFaults(model);
			model.Serialize(successorState);

			// 2. Make sure the transition we're about to add is activation-minimal
			if (!_activationMap.Add(successorState, activatedFaults))
				return;

			// 3. Execute fault activation notifications, serialize the updated state if necessary, and store the transition
			if (model.NotifyFaultActivations())
				model.Serialize(successorState);

			_transitions[Count++] = new Transition
			{
				TargetState = successorState,
				ActivatedFaults = activatedFaults,
				Formulas = EvaluateFormulas()
			};
		}

		/// <summary>
		///   Clears the cache, removing all cached states.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			Count = 0;
			ComputedTransitionCount = 0;

			_activationMap.Clear();
		}

		/// <summary>
		///   Gets the faults that were activated by the transition. The returned bit mask has a bit n set if fault n
		///   was activated.
		/// </summary>
		private static int GetActivatedFaults(RuntimeModel model)
		{
			var faults = model.Faults;
			var mask = 0;

			for (var i = 0; i < faults.Length; ++i)
				mask |= faults[i].IsActivated ? 1 << i : 0;

			return mask;
		}

		/// <summary>
		///   Evaluates all of the model's state formulas. The returned bit mask has a bit n set if formula n holds.
		/// </summary>
		private int EvaluateFormulas()
		{
			var mask = 0;

			for (var i = 0; i < _formulas.Length; ++i)
				mask |= _formulas[i]() ? 1 << i : 0;

			return mask;
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
			_stateBuffer.SafeDispose();
			_activationMap.SafeDispose();
		}

		/// <summary>
		///   Represents a transition.
		/// </summary>
		internal struct Transition
		{
			/// <summary>
			///   The transition's target state.
			/// </summary>
			public byte* TargetState;

			/// <summary>
			///   The faults activated by the transition.
			/// </summary>
			public int ActivatedFaults;

			/// <summary>
			///   The state formulas holding in the target successorState.
			/// </summary>
			public int Formulas;
		}

		/// <summary>
		///   Represents a lookup table that maps each known successor state to the fault sets that can be activated to reach it.
		/// </summary>
		private class ActivationMap : DisposableObject
		{
			private const int ProbeThreshold = 1000;
			private readonly int _capacity;
			private readonly FaultSet* _faults;
			private readonly MemoryBuffer _faultsBuffer = new MemoryBuffer();
			private readonly int* _lookup;
			private readonly MemoryBuffer _lookupBuffer = new MemoryBuffer();
			private readonly MemoryBuffer _stateBuffer = new MemoryBuffer();
			private readonly byte* _stateMemory;
			private readonly int _stateVectorSize;
			private readonly List<uint> _successors;
			private int _nextFaultIndex;

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			/// <param name="stateVectorSize">The size in bytes of each state.</param>
			/// <param name="capacity">The number of successor states and fault activations that can be stored.</param>
			public ActivationMap(int stateVectorSize, int capacity)
			{
				Requires.That(capacity <= (1 << 30), nameof(capacity), $"Maximum supported capacity is {1 << 30}.");

				_lookupBuffer.Resize(capacity * sizeof(int), zeroMemory: false);
				_faultsBuffer.Resize(capacity * sizeof(FaultSet), zeroMemory: false);
				_stateBuffer.Resize(capacity * stateVectorSize, zeroMemory: true);

				_successors = new List<uint>(capacity);
				_capacity = capacity;

				_stateVectorSize = stateVectorSize;
				_lookup = (int*)_lookupBuffer.Pointer;
				_faults = (FaultSet*)_faultsBuffer.Pointer;
				_stateMemory = _stateBuffer.Pointer;

				for (var i = 0; i < capacity; ++i)
					_lookup[i] = -1;
			}

			/// <summary>
			///   Adds the <paramref name="successorState" /> to the map, reached using the <paramref name="activatedFaults" />. Returns
			///   <c>false</c> if the transition can be ignored, indicating that it is not activation-minimal.
			/// </summary>
			/// <param name="successorState">The successor state that should be added.</param>
			/// <param name="activatedFaults">The faults activated by the transition to reach the state.</param>
			public bool Add(byte* successorState, int activatedFaults)
			{
				var hash = MemoryBuffer.Hash(successorState, _stateVectorSize, 0);
				for (var i = 1; i < ProbeThreshold; ++i)
				{
					var hashedIndex = MemoryBuffer.Hash((byte*)&hash, sizeof(int), i * 8345723) % _capacity;
					var faultIndex = _lookup[hashedIndex];

					// If we don't know the state yet, set everything up
					if (faultIndex == -1)
					{
						_lookup[hashedIndex] = _nextFaultIndex;
						_faults[_nextFaultIndex] = new FaultSet { ActivatedFaults = activatedFaults, NextSet = -1 };
						_successors.Add((uint)hashedIndex);
						Buffer.MemoryCopy(successorState, _stateMemory + hashedIndex * _stateVectorSize, _stateVectorSize, _stateVectorSize);

						_nextFaultIndex++;
						return true;
					}

					// If there is a hash conflict, try again
					if (!MemoryBuffer.AreEqual(successorState, _stateMemory + hashedIndex * _stateVectorSize, _stateVectorSize))
						continue;

					// If we know the state already, check whether the new transition is activation-minimal
					while (faultIndex != -1)
					{
						var faultSet = &_faults[faultIndex];
						faultIndex = faultSet->NextSet;

						// If the fault set is a subset of the activated faults, the current transition is not activation-minimal
						if ((faultSet->ActivatedFaults & activatedFaults) == faultSet->ActivatedFaults)
							return false;
					}

					// If we reach this point, we have to add the transition
					_faults[_nextFaultIndex] = new FaultSet { ActivatedFaults = activatedFaults, NextSet = _lookup[hashedIndex] };
					_lookup[hashedIndex] = _nextFaultIndex;
					_nextFaultIndex++;

					return true;
				}

				throw new OutOfMemoryException(
					"Failed to find an empty hash table slot within a reasonable amount of time. Try increasing the state capacity.");
			}

			/// <summary>
			///   Clears the map.
			/// </summary>
			public void Clear()
			{
				foreach (var state in _successors)
					_lookup[state] = -1;

				_successors.Clear();
				_nextFaultIndex = 0;
			}

			/// <summary>
			///   Disposes the object, releasing all managed and unmanaged resources.
			/// </summary>
			/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
			protected override void OnDisposing(bool disposing)
			{
				if (!disposing)
					return;

				_faultsBuffer.SafeDispose();
				_lookupBuffer.SafeDispose();
			}

			/// <summary>
			///   Represents an element of a linked list of activated faults.
			/// </summary>
			private struct FaultSet
			{
				public int ActivatedFaults;
				public int NextSet;
			}
		}
	}
}