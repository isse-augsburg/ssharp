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

namespace SafetySharp.CompilerServices
{
	using System.Runtime.CompilerServices;
	using Modeling;

	/// <summary>
	///   Allows the compiler to check whether a fault is activated while avoiding activation of faults that are known to have no
	///   effect.
	/// </summary>
	public static class FaultHelper
	{
		/// <summary>
		///   Tries to activate the <paramref name="fault" />, if possible, returning <c>true</c> to indicate that the fault is indeed
		///   activated.
		/// </summary>
		/// <param name="fault">The fault that should be activated.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ActivateFault(Fault fault)
		{
			fault.TryActivate();
			return fault.IsActivated;
		}
	}
}