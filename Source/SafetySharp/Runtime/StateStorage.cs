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
	using System.Threading;
	using Serialization;
	using Utilities;

	/// <summary>
	///   Stores the serialized states of a <see cref="RuntimeModel" />.
	/// </summary>
	/// <remarks>
	///   We store states in a contiguous array, indexed by the state's hash. The hashes are stored in a separate array,
	///   using open addressing, see Laarman, "Scalable Multi-Core Model Checking", Algorithm 2.3.
	/// </remarks>
	internal sealed unsafe class StateStorage : DisposableObject
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
		///   The number of states that can be cached.
		/// </summary>
		private readonly int _capacity;

		/// <summary>
		///   The effective length in bytes of a state vector not including the fault bytes.
		/// </summary>
		private readonly int _effectiveStateVectorSize;

		/// <summary>
		///   The number of bytes at the beginning of the state vector that are used to store fault activation states.
		/// </summary>
		private readonly int _faultBytes;

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
		private readonly byte* _stateMemory;

		/// <summary>
		///   The length in bytes of a state vector.
		/// </summary>
		private readonly int _stateVectorSize;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="layout">The layout of the state vector..</param>
		/// <param name="capacity">The capacity of the cache, i.e., the number of states that can be stored in the cache.</param>
		/// <param name="enableFaultOptimization">Indicates whether S#'s fault optimization technique should be used.</param>
		public StateStorage(StateVectorLayout layout, int capacity, bool enableFaultOptimization)
		{
			Requires.NotNull(layout, nameof(layout));
			Requires.InRange(capacity, nameof(capacity), 1024, Int32.MaxValue);

			_stateVectorSize = layout.SizeInBytes;
			_capacity = capacity;
			_faultBytes = enableFaultOptimization ? layout.FaultBytes : 0;
			_effectiveStateVectorSize = _stateVectorSize - _faultBytes;

			_stateBuffer.Resize((long)_capacity * _stateVectorSize, zeroMemory: false);
			_stateMemory = _stateBuffer.Pointer;

			// We allocate enough space so that we can align the returned pointer such that index 0 is the start of a cache line
			_hashBuffer.Resize((long)_capacity * sizeof(int) + CacheLineSize, zeroMemory: true);
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
		public byte* this[int index] => _stateMemory + (long)index * _stateVectorSize;

		/// <summary>
		///   Adds the <paramref name="state" /> to the cache if it is not already known. Returns <c>true</c> to indicate that the state
		///   has been added. This method can be called simultaneously from multiple threads.
		/// </summary>
		/// <param name="state">The state that should be added.</param>
		/// <param name="index">Returns the unique index of the state.</param>
		public bool AddState(byte* state, out int index)
		{
			// We don't have to do any out of bounds checks here

			var hash = Hash(state + _faultBytes, _effectiveStateVectorSize, 0);
			for (var i = 1; i < ProbeThreshold; ++i)
			{
				// We store 30 bit hash values as 32 bit integers, with the most significant bit #31 being set
				// indicating the 'written' state and bit #30 indicating whether writing is not yet finished
				// 'empty' is represented by 0 
				// We ignore two most significant bits of the original hash, which has no influence on the
				// correctness of the algorithm, but might result in more state comparisons
				var hashedIndex = Hash((byte*)&hash, sizeof(int), i * 8345723) % _capacity;
				var memoizedHash = hashedIndex & 0x3FFFFFFF;
				var cacheLineStart = (hashedIndex / BucketsPerCacheLine) * BucketsPerCacheLine;

				for (var j = 0; j < BucketsPerCacheLine; ++j)
				{
					var offset = (int)(cacheLineStart + (hashedIndex + j) % BucketsPerCacheLine);
					var currentValue = Volatile.Read(ref _hashMemory[offset]);

					if (currentValue == 0 && Interlocked.CompareExchange(ref _hashMemory[offset], (int)memoizedHash | (1 << 30), 0) == 0)
					{
						Buffer.MemoryCopy(state, _stateMemory + (long)offset * _stateVectorSize, _stateVectorSize, _stateVectorSize);
						Volatile.Write(ref _hashMemory[offset], (int)memoizedHash | (1 << 31));

						index = offset;
						return true;
					}

					// We have to read the hash value again as it might have been written now where it previously was not
					currentValue = Volatile.Read(ref _hashMemory[offset]);
					if ((currentValue & 0x3FFFFFFF) == memoizedHash)
					{
						while ((currentValue & 1 << 31) == 0)
							currentValue = Volatile.Read(ref _hashMemory[offset]);

						if (AreEqual(state, _stateMemory + (long)offset * _stateVectorSize))
						{
							index = offset;
							return false;
						}
					}
				}
			}

			throw new OutOfMemoryException(
				"Failed to find an empty hash table slot within a reasonable amount of time. Try increasing the state capacity.");
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

		/// <summary>
		///   Compares the two states, returning <c>true</c> when the states are equivalent.
		/// </summary>
		private bool AreEqual(byte* state1, byte* state2)
		{
			state1 += _faultBytes;
			state2 += _faultBytes;

			for (var i = _effectiveStateVectorSize / 8; i > 0; --i)
			{
				if (*(long*)state1 != *(long*)state2)
					return false;

				state1 += 8;
				state2 += 8;
			}

			for (var i = _effectiveStateVectorSize % 8; i > 0; --i)
			{
				if (*state1 != *state2)
					return false;

				state1 += 1;
				state2 += 1;
			}

			return true;
		}

		/// <summary>
		///   Hashes the <paramref name="state" /> vector.
		/// </summary>
		/// <remarks>See also https://en.wikipedia.org/wiki/MurmurHash (MurmurHash3 implementation)</remarks>
		private static uint Hash(byte* state, int length, int seed)
		{
			const uint c1 = 0xcc9e2d51;
			const uint c2 = 0x1b873593;
			const int r1 = 15;
			const int r2 = 13;
			const uint m = 5;
			const uint n = 0xe6546b64;

			var hash = (uint)seed;
			var numBlocks = length / 4;
			var blocks = (uint*)state;

			for (var i = 0; i < numBlocks; i++)
			{
				var k = blocks[i];
				k *= c1;
				k = (k << r1) | (k >> (32 - r1));
				k *= c2;

				hash ^= k;
				hash = ((hash << r2) | (hash >> (32 - r2))) * m + n;
			}

			var tail = state + numBlocks * 4;
			var k1 = 0u;

			switch (length & 3)
			{
				case 3:
					k1 ^= (uint)tail[2] << 16;
					goto case 2;
				case 2:
					k1 ^= (uint)tail[1] << 8;
					goto case 1;
				case 1:
					k1 ^= tail[0];
					k1 *= c1;
					k1 = (k1 << r1) | (k1 >> (32 - r1));
					k1 *= c2;
					hash ^= k1;
					break;
			}

			hash ^= (uint)length;
			hash ^= hash >> 16;
			hash *= (0x85ebca6b);
			hash ^= hash >> 13;
			hash *= (0xc2b2ae35);
			hash ^= hash >> 16;

			return hash;
		}
	}
}