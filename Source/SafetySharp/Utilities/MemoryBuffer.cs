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

namespace SafetySharp.Utilities
{
	using System;
	using System.Runtime.InteropServices;

	/// <summary>
	///   Represents a memory buffer.
	/// </summary>
	internal sealed unsafe class MemoryBuffer : DisposableObject
	{
		/// <summary>
		///   The garbage collector handle that pins the memory to a fixed location.
		/// </summary>
		private GCHandle _handle;

		/// <summary>
		///   The underlying memory.
		/// </summary>
		private byte[] _memory;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public MemoryBuffer()
		{
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="sizeInBytes">The size of the buffer in bytes.</param>
		public MemoryBuffer(int sizeInBytes)
		{
			Resize(sizeInBytes);
		}

		/// <summary>
		///   Gets the size of the memory buffer in bytes.
		/// </summary>
		public int SizeInBytes { get; private set; }

		/// <summary>
		///   Gets a pointer to the underlying memory of the buffer.
		/// </summary>
		public void* Pointer { get; private set; }

		/// <summary>
		///   Resizes the buffer so that it can contain at least <paramref name="sizeInBytes" /> bytes.
		/// </summary>
		public void Resize(int sizeInBytes)
		{
			Requires.That(sizeInBytes >= 0, nameof(sizeInBytes), $"Cannot allocate {sizeInBytes} bytes.");

			// We don't resize if less space is requested
			if (sizeInBytes <= SizeInBytes)
				return;

			var oldBuffer = _memory;
			var newBuffer = new byte[sizeInBytes];

			if (oldBuffer != null)
				Array.Copy(oldBuffer, newBuffer, SizeInBytes);

			if (_handle.IsAllocated)
				_handle.Free();

			_memory = newBuffer;
			_handle = GCHandle.Alloc(newBuffer, GCHandleType.Pinned);

			Pointer = _handle.AddrOfPinnedObject().ToPointer();
			SizeInBytes = sizeInBytes;
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (_handle.IsAllocated)
				_handle.Free();
		}
	}
}