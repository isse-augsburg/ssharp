// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace SafetySharp.Runtime.Serialization
{
	using System;

	/// <summary>
	///   Raised when the state vector layout of the counter example does not match the layout of the instantiated model.
	/// </summary>
	public class StateVectorMismatchException : Exception
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="counterExampleMetadata">The state vector layout expected by the counter example.</param>
		/// <param name="modelMetadata">The state vector layout expected by the model.</param>
		internal StateVectorMismatchException(StateVectorLayout counterExampleMetadata, StateVectorLayout modelMetadata)
			: base("Mismatch detected between the layout of the state vector as expected by the counter example and the " +
				   "actual layout of the state vector used by the instantiated model.")
		{
			CounterExampleMetadata = counterExampleMetadata;
			ModelMetadata = modelMetadata;
		}

		/// <summary>
		///   Gets the state vector layout expected by the counter example.
		/// </summary>
		public StateVectorLayout CounterExampleMetadata { get; }

		/// <summary>
		///   Gets the state vector layout expected by the model.
		/// </summary>
		public StateVectorLayout ModelMetadata { get; }
	}
}