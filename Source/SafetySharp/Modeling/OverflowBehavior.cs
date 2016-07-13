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

namespace SafetySharp.Modeling
{
	/// <summary>
	///   Controls the overflow semantics when field values lie outside the field's allowed range of values.
	/// </summary>
	public enum OverflowBehavior
	{
		/// <summary>
		///   Indicates that an exception should be thrown when a field contains a value outside of its allowed range.
		/// </summary>
		Error,

		/// <summary>
		///   Indicates that the field value is clamped to the field's range.
		/// </summary>
		Clamp,

		/// <summary>
		///   Indicates that the field value wraps around if it underflows or overflows the field's range, i.e., if the range's upper
		///   limit is exceeded, the value is set to the lower bound and vice versa.
		/// </summary>
		WrapClamp
	}
}