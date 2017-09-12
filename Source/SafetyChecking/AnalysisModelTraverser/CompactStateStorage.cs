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
	using System.Threading;
	using Utilities;

	/// <summary>
	///   Stores the serialized states of an <see cref="AnalysisModel" />.
	/// </summary>
	/// <remarks>
	///   We store states in a contiguous array, indexed by a continuous variable.
	///   Therefore, we use state hashes like in <cref="SparseStateStorage" /> and one level of indirection.
	///   The hashes are stored in a separate array, using open addressing,
	///   see Laarman, "Scalable Multi-Core Model Checking", Algorithm 2.3.
	/// </remarks>
	internal sealed unsafe class CompactStateStorage : StateStorage
	{
		/// <summary>
		///   The number of attempts that are made to find an empty bucket.
		/// </summary>
		private const int ProbeThreshold = 1000;

		/// <summary>
		///   The assumed size of a cache line in bytes.
		/// </summary>
		private const int CacheLineSize = 64;

		/// <summary>
		///   The number of buckets that can be stored in a cache line.
		/// </summary>
		private const int BucketsPerCacheLine = CacheLineSize / sizeof(int);

		/// <summary>
		///   The number of states that can be cached and the number of reserved states.
		/// </summary>
		private readonly long _totalCapacity;

		/// <summary>
		///   The number of states that can be cached.
		/// </summary>
		private long _cachedStatesCapacity;

		/// <summary>
		///   The number of reserved states
		/// </summary>
		private long _reservedStatesCapacity = 0;

		/// <summary>
		///   The buffer that stores the hash table information.
		/// </summary>
		private readonly MemoryBuffer _hashBuffer = new MemoryBuffer();

		/// <summary>
		///   The memory where the hash table information is stored.
		/// </summary>
		private readonly int* _hashMemory;

		/// <summary>
		///   The buffer that stores the states.
		/// </summary>
		private readonly MemoryBuffer _stateBuffer = new MemoryBuffer();

		/// <summary>
		///   The pointer to the underlying state memory.
		/// </summary>
		private byte* _stateMemory;

		/// <summary>
		///   The buffer that stores the mapping from the hashed-based state index to the compact state index.
		/// </summary>
		private readonly MemoryBuffer _indexMapperBuffer = new MemoryBuffer();

		/// <summary>
		///   The pointer to the underlying index mapper.
		/// </summary>
		private readonly int* _indexMapperMemory;
		
		/// <summary>
		///   The length in bytes of a state vector required for the analysis model.
		/// </summary>
		private readonly int _analysisModelStateVectorSize;

		/// <summary>
		///   Extra bytes in state vector for traversal modifiers.
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
		public override int StateVectorSize => _stateVectorSize;

		/// <summary>
		///   The number of saved states (internal variable)
		/// </summary>
		private int _savedStates = 0;

		/// <summary>
		///   The number of saved states
		/// </summary>
		public int SavedStates => _savedStates;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="analysisModelStateVectorSize">The size of the state vector required for the analysis model in bytes.</param>
		/// <param name="capacity">The capacity of the cache, i.e., the number of states that can be stored in the cache.</param>
		public CompactStateStorage(int analysisModelStateVectorSize, long capacity)
		{
			Requires.InRange(capacity, nameof(capacity), 1024, Int32.MaxValue);

			_analysisModelStateVectorSize = analysisModelStateVectorSize;
			_totalCapacity = capacity;
			_cachedStatesCapacity = capacity - BucketsPerCacheLine; //offset returned by this.Add may be up to BucketsPerCacheLine-1 positions bigger than _cachedStatesCapacity

			ResizeStateBuffer();

			_indexMapperBuffer.Resize(_totalCapacity * sizeof(int), zeroMemory: false);
			_indexMapperMemory = (int*) _indexMapperBuffer.Pointer;


			// We allocate enough space so that we can align the returned pointer such that index 0 is the start of a cache line
			_hashBuffer.Resize(_totalCapacity * sizeof(int) + CacheLineSize, zeroMemory: false);
			_hashMemory = (int*)_hashBuffer.Pointer;

			if ((ulong)_hashMemory % CacheLineSize != 0)
				_hashMemory = (int*)(_hashBuffer.Pointer + (CacheLineSize - (ulong)_hashBuffer.Pointer % CacheLineSize));

			Assert.InRange((ulong)_hashMemory - (ulong)_hashBuffer.Pointer, 0ul, (ulong)CacheLineSize);
			Assert.That((ulong)_hashMemory % CacheLineSize == 0, "Invalid buffer alignment.");
		}

		/// <summary>
		///   Gets the state at the given zero-based <paramref name="index" />.
		/// </summary>
		/// <param name="index">The index of the state that should be returned.</param>
		public override byte* this[int index]
		{
			get
			{
				Assert.InRange(index, 0, _totalCapacity);
				return _stateMemory + (long)index * _stateVectorSize;
			}
		}

		/// <summary>
		///   Reserve a state index in StateStorage. Must not be called after AddState has been called.
		/// </summary>
		internal override int ReserveStateIndex()
		{
			var freshCompactIndex = InterlockedExtensions.IncrementReturnOld(ref _savedStates);
			// Use the index pointing at the last possible element in the buffers and decrease the size.
			_reservedStatesCapacity++;
			_cachedStatesCapacity--;

			// Add BucketsPerCacheLine so returnIndex does not interfere with the maximal possible index returned by AddState
			// which is _cachedStatesCapacity+BucketsPerCacheLine-1.
			// returnIndex is in range of capacityToReserve, so this is save.
			var hashBasedIndex = _cachedStatesCapacity + BucketsPerCacheLine;

			Volatile.Write(ref _indexMapperMemory[hashBasedIndex], freshCompactIndex);

			Assert.InRange(hashBasedIndex,0,Int32.MaxValue);
			Assert.InRange(hashBasedIndex, 0, _totalCapacity + BucketsPerCacheLine);
			return (int)freshCompactIndex;
		}

		/// <summary>
		///   Adds the <paramref name="state" /> to the cache if it is not already known. Returns <c>true</c> to indicate that the state
		///   has been added. This method can be called simultaneously from multiple threads.
		/// </summary>
		/// <param name="state">The state that should be added.</param>
		/// <param name="index">Returns the unique index of the state.</param>
		public override bool AddState(byte* state, out int index)
		{
			// We don't have to do any out of bounds checks here

			var hash = MemoryBuffer.Hash(state, _stateVectorSize, 0);
			for (var i = 1; i < ProbeThreshold; ++i)
			{
				// We store 30 bit hash values as 32 bit integers, with the most significant bit #31 being set
				// indicating the 'written' state and bit #30 indicating whether writing is not yet finished
				// 'empty' is represented by 0 
				// We ignore two most significant bits of the original hash, which has no influence on the
				// correctness of the algorithm, but might result in more state comparisons
				var hashedIndex = MemoryBuffer.Hash((byte*)&hash, sizeof(int), i * 8345723) % _cachedStatesCapacity;
				var memoizedHash = hashedIndex & 0x3FFFFFFF;
				var cacheLineStart = (hashedIndex / BucketsPerCacheLine) * BucketsPerCacheLine;

				for (var j = 0; j < BucketsPerCacheLine; ++j)
				{
					var offset = (int)(cacheLineStart + (hashedIndex + j) % BucketsPerCacheLine);
					var currentValue = Volatile.Read(ref _hashMemory[offset]);

					if (currentValue == 0 && Interlocked.CompareExchange(ref _hashMemory[offset], (int)memoizedHash | (1 << 30), 0) == 0)
					{
						var freshCompactIndex = InterlockedExtensions.IncrementReturnOld(ref _savedStates);
						Volatile.Write(ref _indexMapperMemory[offset], freshCompactIndex);

						MemoryBuffer.Copy(state, this[freshCompactIndex], _stateVectorSize);
						Volatile.Write(ref _hashMemory[offset], (int)memoizedHash | (1 << 31));


						index = freshCompactIndex;
						return true;
					}

					// We have to read the hash value again as it might have been written now where it previously was not
					currentValue = Volatile.Read(ref _hashMemory[offset]);
					if ((currentValue & 0x3FFFFFFF) == memoizedHash)
					{
						while ((currentValue & 1 << 31) == 0)
							currentValue = Volatile.Read(ref _hashMemory[offset]);

						var compactIndex = Volatile.Read(ref _indexMapperMemory[offset]);

						if (compactIndex!=-1 && MemoryBuffer.AreEqual(state, this[compactIndex], _stateVectorSize))
						{
							index = compactIndex;
							return false;
						}
					}
				}
			}

			throw new OutOfMemoryException(
				"Failed to find an empty hash table slot within a reasonable amount of time. Try increasing the state capacity.");
		}

		internal void ResizeStateBuffer()
		{
			_stateVectorSize = _analysisModelStateVectorSize + _traversalModifierStateVectorSize;
			_stateBuffer.Resize(_totalCapacity * _stateVectorSize, zeroMemory: false);
			_stateMemory = _stateBuffer.Pointer;
		}

		/// <summary>
		///   Clears all stored states.
		/// </summary>
		internal override void Clear(int traversalModifierStateVectorSize)
		{
			_traversalModifierStateVectorSize = traversalModifierStateVectorSize;
			ResizeStateBuffer();
			_hashBuffer.Clear();
			MemoryBuffer.SetAllBitsMemoryWithInitblk.ClearWithMinus1(_indexMapperMemory, _totalCapacity);
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (disposing)
				_stateBuffer.SafeDispose();
		}
	}
}