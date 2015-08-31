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

namespace SafetySharp.Modeling
{
	/// <summary>
	///   Controls the semantics of the assignment operator when the assigned value lies outside the variable's range.
	/// </summary>
	public enum OverflowBehavior
	{
		/// <summary>
		///   Indicates that an exception is thrown when a value outside the range of the variable is assigned during
		///   simulation. During model-checking, however, the precise behavior of this overflow behavior is undefined.
		/// </summary>
		Error,

		/// <summary>
		///   Indicates the assigned value is clamped to the variable's minimum or maximum value.
		/// </summary>
		Clamp,

		/// <summary>
		///   Indicates the assigned value wraps around if it underflows or overflows the variable's range.
		/// </summary>
		WrapAround
	}
}