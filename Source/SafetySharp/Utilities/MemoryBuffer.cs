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
		///   The number of additional bytes to allocate in front of and behind the buffer to check for out of bounds writes.
		/// </summary>
		private const int CheckBytes = 128;

		/// <summary>
		///   The value that is used check for out of bounds writes.
		/// </summary>
		private const int CheckValue = 0xAF;

		/// <summary>
		///   Gets the size of the memory buffer in bytes.
		/// </summary>
		public long SizeInBytes { get; private set; }

		/// <summary>
		///   Gets a pointer to the underlying memory of the buffer.
		/// </summary>
		public byte* Pointer { get; private set; }

		/// <summary>
		///   Resizes the buffer so that it can contain at least <paramref name="sizeInBytes" /> bytes.
		/// </summary>
		/// <param name="sizeInBytes">The buffer's new size in bytes.</param>
		/// <param name="zeroMemory">Indicates whether the buffer's contents should be initialized to zero.</param>
		public void Resize(long sizeInBytes, bool zeroMemory)
		{
			Requires.That(sizeInBytes >= 0, nameof(sizeInBytes), $"Cannot allocate {sizeInBytes} bytes.");

			// We don't resize if less space is requested
			if (sizeInBytes <= SizeInBytes)
				return;

			var allocatedBytes = sizeInBytes + 2 * CheckBytes;
			var oldBuffer = Pointer;
			var newBuffer = (byte*)Marshal.AllocHGlobal(new IntPtr(allocatedBytes)).ToPointer();
			GC.AddMemoryPressure(allocatedBytes);

			if (zeroMemory)
				ZeroMemory(new IntPtr(newBuffer + CheckBytes), new IntPtr(sizeInBytes));

			if (oldBuffer != null)
				Buffer.MemoryCopy(oldBuffer, newBuffer + CheckBytes, sizeInBytes, SizeInBytes);

			OnDisposing(true);
			Pointer = newBuffer + CheckBytes;
			SizeInBytes = sizeInBytes;

			for (var i = 0; i < CheckBytes; ++i)
			{
				*(newBuffer + i) = CheckValue;
				*(Pointer + sizeInBytes + i) = CheckValue;
			}
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			CheckBounds();

			if (Pointer == null)
				return;

			Marshal.FreeHGlobal(new IntPtr(Pointer - CheckBytes));
			GC.RemoveMemoryPressure(SizeInBytes + 2 * CheckBytes);

			Pointer = null;
		}

		/// <summary>
		///   Checks whether any writes have been out of bounds.
		/// </summary>
		internal void CheckBounds()
		{
			if (Pointer == null)
				return;

			for (var i = 0; i < CheckBytes; ++i)
			{
				if (*(Pointer - i - 1) != CheckValue || *(Pointer + SizeInBytes + i) != CheckValue)
					throw new InvalidOperationException("Out of bounds write detected.");
			}
		}

		[DllImport("Kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
		private static extern void ZeroMemory(IntPtr memory, IntPtr size);
	}
}