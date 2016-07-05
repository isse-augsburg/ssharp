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

namespace SafetySharp.Runtime.Serialization
{
	using System;
	using Analysis;
	using Modeling;

	/// <summary>
	///   Represents a serialized <see cref="RuntimeModel" />.
	/// </summary>
	internal struct SerializedRuntimeModel
	{
		/// <summary>
		///   The buffer the model was deserialized from.
		/// </summary>
		public readonly byte[] Buffer;

		/// <summary>
		///   The table of objects referenced by the model.
		/// </summary>
		public readonly ObjectTable ObjectTable;

		/// <summary>
		///   A copy of the original model the runtime model was generated from.
		/// </summary>
		internal ModelBase Model;

		/// <summary>
		///   When this method evaluates to true during model checking, the current trace should not be expanded further.
		/// </summary>
		public readonly Func<bool> TerminateEarlyCondition;

		/// <summary>
		///   The formulas that are checked on the model.
		/// </summary>
		public readonly Formula[] Formulas;

		/// <param name="model">A copy of the original model the runtime model was generated from.</param>
		/// <param name="buffer">The buffer the model was deserialized from.</param>
		/// <param name="objectTable">The table of objects referenced by the model.</param>
		/// <param name="terminateEarlyCondition">When this method evaluates to true during model checking, the current trace should not be expanded further..</param>
		/// <param name="formulas">The formulas that are checked on the model.</param>
		internal SerializedRuntimeModel(ModelBase model, byte[] buffer, ObjectTable objectTable, Func<bool> terminateEarlyCondition, Formula[] formulas)
		{
			Model = model;
			Buffer = buffer;
			ObjectTable = objectTable;
			TerminateEarlyCondition = terminateEarlyCondition;
			Formulas = formulas;
		}
	}
}