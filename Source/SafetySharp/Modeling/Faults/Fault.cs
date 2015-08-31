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
	///   Represents a base class for all faults affecting the behavior of <see cref="Component" />s, where the actual type of the
	///   affected <see cref="Component" /> instance is irrelevant.
	/// </summary>
	public abstract class Fault
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		protected Fault()
		{
		}

		/// <summary>
		///   Gets the fault's occurrence pattern.
		/// </summary>
		internal OccurrencePattern OccurrencePattern { get; private set; }

		/// <summary>
		///   Gets the <see cref="Component" /> instance affected by the fault.
		/// </summary>
		protected internal Component Component { get; internal set; }

		/// <summary>
		///   Gets or sets a value indicating whether the fault is currently occurring.
		/// </summary>
		internal bool IsOccurring => OccurrencePattern.IsOccurring;

		/// <summary>
		///   Gets or sets a value indicating whether the fault is ignored for a simulation or model checking run.
		/// </summary>
		internal bool IsIgnored { get; set; }

		/// <summary>
		///   Updates the internal state of the fault.
		/// </summary>
		public virtual void UpdateFaultState()
		{
		}
	}
}