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
	///   Represents a stack that uses a <see cref="MemoryBuffer" /> for its underlying storage.
	/// </summary>
	internal sealed unsafe class ChoiceStack : DisposableObject
	{
		/// <summary>
		///   The underlying memory of the stack.
		/// </summary>
		private readonly MemoryBuffer _memoryBuffer;

		/// <summary>
		///   The pointer to the stack's memory.
		/// </summary>
		private int* _buffer;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="initialCapacity">The initial capacity of the stack.</param>
		public ChoiceStack(int initialCapacity)
		{
			_memoryBuffer = new MemoryBuffer(initialCapacity * sizeof(int));
			_buffer = (int*)_memoryBuffer.Pointer;
		}

		/// <summary>
		///   Gets the number of elements on the stack.
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		///   Gets the element at <paramref name="index" /> from the stack.
		/// </summary>
		public int this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { return _buffer[index]; }
		}

		/// <summary>
		///   Returns the element at the top of the stack without removing it.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Peek()
		{
			return _buffer[Count - 1];
		}

		/// <summary>
		///   Removes the top element from the stack.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Remove()
		{
			return _buffer[--Count];
		}

		/// <summary>
		///   Pushes <paramref name="value" /> onto the stack.
		/// </summary>
		/// <param name="value">The value that should be pushed.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Push(int value)
		{
			if (_memoryBuffer.SizeInBytes <= Count * sizeof(int))
			{
				_memoryBuffer.Resize(_memoryBuffer.SizeInBytes * 2);
				_buffer = (int*)_memoryBuffer.Pointer;
			}

			_buffer[Count++] = value;
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (disposing)
				_memoryBuffer.SafeDispose();
		}
	}
}