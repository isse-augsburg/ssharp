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

namespace ISSE.SafetyChecking.Utilities
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
			byte* newBuffer;

			try
			{
				newBuffer = (byte*)Marshal.AllocHGlobal(new IntPtr(allocatedBytes)).ToPointer();
				GC.AddMemoryPressure(allocatedBytes);
			}
			catch (OutOfMemoryException)
			{
				throw new InvalidOperationException(
					$"Unable to allocate {allocatedBytes:n0} bytes. Try optimizing state vector sizes or decrease the state capacity.");
			}

			if (zeroMemory)
				ZeroMemory(new IntPtr(newBuffer + CheckBytes), new IntPtr(sizeInBytes));

			if (oldBuffer != null)
				Copy(oldBuffer, newBuffer + CheckBytes, SizeInBytes);

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
		///   Clears the data stored in the buffer, overwriting everything with zeroes.
		/// </summary>
		public void Clear()
		{
			ZeroMemory(new IntPtr(Pointer), new IntPtr(SizeInBytes));
		}

		/// <summary>
		///   Compares the two buffers <paramref name="buffer1" /> and <paramref name="buffer2" />, returning <c>true</c> when the
		///   buffers are equivalent.
		/// </summary>
		/// <param name="buffer1">The first buffer of memory to compare.</param>
		/// <param name="buffer2">The second buffer of memory to compare.</param>
		/// <param name="sizeInBytes">The size of the buffers in bytes.</param>
		public static bool AreEqual(byte* buffer1, byte* buffer2, int sizeInBytes)
		{
			if (buffer1 == buffer2)
				return true;

			for (var i = sizeInBytes / 8; i > 0; --i)
			{
				if (*(long*)buffer1 != *(long*)buffer2)
					return false;

				buffer1 += 8;
				buffer2 += 8;
			}

			for (var i = sizeInBytes % 8; i > 0; --i)
			{
				if (*buffer1 != *buffer2)
					return false;

				buffer1 += 1;
				buffer2 += 1;
			}

			return true;
		}

		/// <summary>
		///   Copies <paramref name="sizeInBytes" />-many bytes from <paramref name="source" /> to <see cref="destination" />.
		/// </summary>
		/// <param name="source">The first buffer of memory to compare.</param>
		/// <param name="destination">The second buffer of memory to compare.</param>
		/// <param name="sizeInBytes">The size of the buffers in bytes.</param>
		public static void Copy(byte* source, byte* destination, long sizeInBytes)
		{
			for (var i = sizeInBytes / 8; i > 0; --i)
			{
				*(long*)destination = *(long*)source;

				source += 8;
				destination += 8;
			}

			var remaining = sizeInBytes % 8;
			if (remaining >= 4)
			{
				*(int*)destination = *(int*)source;

				source += 4;
				destination += 4;
				remaining -= 4;
			}

			for (var i = 0; i < remaining; ++i)
			{
				*destination = *source;

				source += 1;
				destination += 1;
			}
		}

		/// <summary>
		///   Hashes the <paramref name="buffer" />.
		/// </summary>
		/// <param name="buffer">The buffer of memory that should be hashed.</param>
		/// <param name="sizeInBytes">The size of the buffer in bytes.</param>
		/// <param name="seed">The seed value for the hash.</param>
		/// <remarks>See also https://en.wikipedia.org/wiki/MurmurHash (MurmurHash3 implementation)</remarks>
		public static uint Hash(byte* buffer, int sizeInBytes, int seed)
		{
			const uint c1 = 0xcc9e2d51;
			const uint c2 = 0x1b873593;
			const int r1 = 15;
			const int r2 = 13;
			const uint m = 5;
			const uint n = 0xe6546b64;

			var hash = (uint)seed;
			var numBlocks = sizeInBytes / 4;
			var blocks = (uint*)buffer;

			for (var i = 0; i < numBlocks; i++)
			{
				var k = blocks[i];
				k *= c1;
				k = (k << r1) | (k >> (32 - r1));
				k *= c2;

				hash ^= k;
				hash = ((hash << r2) | (hash >> (32 - r2))) * m + n;
			}

			var tail = buffer + numBlocks * 4;
			var k1 = 0u;

			switch (sizeInBytes & 3)
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

			hash ^= (uint)sizeInBytes;
			hash ^= hash >> 16;
			hash *= (0x85ebca6b);
			hash ^= hash >> 13;
			hash *= (0xc2b2ae35);
			hash ^= hash >> 16;

			return hash;
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