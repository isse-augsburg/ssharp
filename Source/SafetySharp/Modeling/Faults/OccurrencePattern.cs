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

namespace SafetySharp.Modeling.Faults
{
	/// <summary>
	///   Represents a base class for all fault occurrence patterns.
	/// </summary>
	public abstract class OccurrencePattern
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		protected OccurrencePattern()
		{
		}

		/// <summary>
		///   Gets or sets a value indicating whether the fault is currently occurring.
		/// </summary>
		protected internal bool IsOccurring { get; set; }

		/// <summary>
		///   Gets the nondeterministic choice that should be used to determine whether the associated fault occurs.
		/// </summary>
		protected Choice Choice { get; } = new Choice();

		/// <summary>
		///   Updates the internal state of the occurrence pattern.
		/// </summary>
		public abstract void UpdateOccurrenceState();
	}
}