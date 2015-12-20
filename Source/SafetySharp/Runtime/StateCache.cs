// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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
	using System.Runtime.CompilerServices;
	using Utilities;

	/// <summary>
	///   Caches a set of serialized states.
	/// </summary>
	internal sealed unsafe class StateCache : DisposableObject
	{
		/// <summary>
		///   The default initial capacity.
		/// </summary>
		private const int InitialCapacity = 64;

		/// <summary>
		///   The underlying memory of the cache.
		/// </summary>
		private readonly MemoryBuffer _buffer = new MemoryBuffer();

		/// <summary>
		///   The number of states that can be cached.
		/// </summary>
		private int _capacity;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="slotCount">The number of slots within the state vector.</param>
		/// <param name="initialCapacity">The initial capacity of the cache.</param>
		public StateCache(int slotCount, int initialCapacity = InitialCapacity)
		{
			SlotCount = slotCount;
			_capacity = initialCapacity;
		}

		/// <summary>
		///   Gets the number of slots within the state vector.
		/// </summary>
		public int SlotCount { get; }

		/// <summary>
		///   Gets a pointer to the underlying memory.
		/// </summary>
		public int* StateMemory { get; private set; }

		/// <summary>
		///   Gets the number of cached states.
		/// </summary>
		public int StateCount { get; private set; }

		/// <summary>
		///   Allocates a new state.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int* Allocate()
		{
			// Double the capacity if we are out of memory
			if (StateCount >= _capacity || StateMemory == null)
			{
				_capacity *= 2;
				_buffer.Resize(_capacity * SlotCount * sizeof(int), zeroMemory: false);

				StateMemory = (int*)_buffer.Pointer;
			}

			return StateMemory + StateCount++ * SlotCount;
		}

		/// <summary>
		///   Clears the cache, removing all cached states.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			StateCount = 0;
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (disposing)
				_buffer.SafeDispose();
		}
	}
}