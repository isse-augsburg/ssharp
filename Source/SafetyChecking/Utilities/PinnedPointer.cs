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
	///   Provides access to a pointer to a pinned object.
	/// </summary>
	internal struct PinnedPointer : IDisposable
	{
		/// <summary>
		///   The handle of the pinned object.
		/// </summary>
		private GCHandle _handle;

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			if (_handle.IsAllocated)
				_handle.Free();
		}

		/// <summary>
		///   Initializes a new instance, allowing access to a pointer to <paramref name="obj" />.
		/// </summary>
		internal static PinnedPointer Create<T>(T obj)
		{
			return new PinnedPointer { _handle = GCHandle.Alloc(obj, GCHandleType.Pinned) };
		}

		/// <summary>
		///   Converts the pinned pointer to a <c>void*</c>.
		/// </summary>
		/// <param name="pointer">The pinned pointer that should be converted.</param>
		public static unsafe implicit operator void*(PinnedPointer pointer)
		{
			return pointer._handle.AddrOfPinnedObject().ToPointer();
		}
	}
}