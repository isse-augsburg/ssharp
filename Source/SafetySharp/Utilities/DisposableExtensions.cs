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
	using System.Collections.Generic;
	using System.Diagnostics;

	/// <summary>
	///   Provides extension methods for disposing classes implementing the <see cref="IDisposable" /> interface and collections
	///   containing disposable items.
	/// </summary>
	internal static class DisposableExtensions
	{
		/// <summary>
		///   Disposes all <paramref name="items" />.
		/// </summary>
		/// <param name="items">The items that should be disposed.</param>
		[DebuggerHidden]
		public static void SafeDisposeAll<T>(this IEnumerable<T> items)
			where T : class, IDisposable
		{
			if (items == null)
				return;

			foreach (var obj in items)
				obj.SafeDispose();
		}

		/// <summary>
		///   Disposes the object if it is not null.
		/// </summary>
		/// <param name="obj">The object that should be disposed.</param>
		[DebuggerHidden]
		public static void SafeDispose<T>(this T obj)
			where T : class, IDisposable
		{
			var disposableObject = obj as DisposableObject;
			if (disposableObject != null && disposableObject.IsDisposed)
				return;

			obj?.Dispose();
		}
	}
}