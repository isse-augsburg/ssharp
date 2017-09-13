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

namespace ISSE.SafetyChecking.AnalysisModelTraverser
{
	using System;
	using System.Runtime.CompilerServices;
	using Utilities;

	public unsafe class TemporalStateStorage : DisposableObject
	{

		/// <summary>
		///   The length in bytes of a state vector required for the analysis model.
		/// </summary>
		public int AnalysisModelStateVectorSize { get; }

		/// <summary>
		///   Extra bytes in state vector for traversal parameters.
		/// </summary>
		private int _traversalModifierStateVectorSize;

		/// <summary>
		///   The length in bytes of the state vector of the analysis model with the extra bytes
		///   required for the traversal.
		/// </summary>
		private int _stateVectorSize;

		/// <summary>
		///   The length in bytes of the state vector of the analysis model with the extra bytes
		///   required for the traversal.
		/// </summary>
		public int StateVectorSize => _stateVectorSize;

		private long _temporalStates;

		private readonly MemoryBuffer _targetStateBuffer = new MemoryBuffer();

		private readonly long _capacity;

		private byte* _targetStateMemory;


		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="analysisModelStateVectorSize">The length in bytes of a state vector required for the analysis model.</param>
		/// <param name="capacity">The maximum number of successors that can be cached.</param>
		public TemporalStateStorage(int analysisModelStateVectorSize, long capacity)
		{
			Requires.That(capacity <= (1 << 30), nameof(capacity), $"Maximum supported capacity is {1 << 30}.");

			AnalysisModelStateVectorSize = analysisModelStateVectorSize;
			_capacity = capacity;

			ResizeStateBuffer();
		}

		/// <summary>
		///   Get a free temporal address to store a state.
		/// </summary>
		public byte* GetFreeTemporalSpaceAddress()
		{
			if (_temporalStates >= _capacity)
				throw new OutOfMemoryException("Unable to store an additional temporal state. Try increasing the successor state capacity.");
			
			var successorState = _targetStateMemory + _stateVectorSize * _temporalStates;
			
			++_temporalStates;

			return successorState;
		}

		public bool TryToFindState(byte *stateToFind, out byte* foundState)
		{
			for (var i = 0; i < _temporalStates; i++)
			{
				var candidateState = _targetStateMemory + i * _stateVectorSize;
				if (MemoryBuffer.AreEqual(stateToFind, candidateState, _stateVectorSize))
				{
					foundState = candidateState;
					return true;
				}
			}
			foundState = null;
			return false;
		}

		internal void ResizeStateBuffer()
		{
			_stateVectorSize = AnalysisModelStateVectorSize + _traversalModifierStateVectorSize;
			_targetStateBuffer.Resize(_capacity * _stateVectorSize, zeroMemory: true);
			_targetStateMemory = _targetStateBuffer.Pointer;
		}

		/// <summary>
		///   Clears the cache, removing all cached states.
		/// </summary>
		internal void Clear()
		{
			MemoryBuffer.ZeroMemoryWithInitblk.ClearWithZero(_targetStateMemory,_stateVectorSize * _temporalStates);
			_temporalStates = 0;
		}

		/// <summary>
		///   Resets the data structure.
		/// </summary>
		internal void Reset(int traversalModifierStateVectorSize)
		{
			_traversalModifierStateVectorSize = traversalModifierStateVectorSize;
			ResizeStateBuffer();
			Clear();
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;
			
			_targetStateBuffer.SafeDispose();
		}
	}
}
