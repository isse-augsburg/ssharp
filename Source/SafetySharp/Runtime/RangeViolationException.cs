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

namespace SafetySharp.Runtime
{
	using System;
	using System.Reflection;
	using Modeling;

	/// <summary>
	///   Raised when the value of a <see cref="Component" /> field exceeds its allowed range and the field's
	///   <see cref="OverflowBehavior" /> is set to <see cref="OverflowBehavior.Error" />.
	/// </summary>
	public sealed class RangeViolationException : Exception
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="obj">The object the exception should be raised for.</param>
		/// <param name="field">The field the exception should be raised for.</param>
		/// <param name="range">The allowed range of the field.</param>
		internal RangeViolationException(object obj, FieldInfo field, RangeAttribute range)
		{
			Object = obj;
			Field = field;
			Range = range;
		}

		/// <summary>
		///   Gets a message that describes the current exception.
		/// </summary>
		/// <returns>
		///   The error message that explains the reason for the exception, or an empty string ("").
		/// </returns>
		public override string Message =>
			$"The value '{FieldValue}' of field '{Field.FieldType.FullName} {Field.DeclaringType.FullName}.{Field.Name}' " +
			$"lies outside of the allowed range [{Range.LowerBound};{Range.UpperBound}].";

		/// <summary>
		///   Gets the object the exception was raised for.
		/// </summary>
		public object Object { get; }

		/// <summary>
		///   Gets the field the exception was raised for.
		/// </summary>
		public FieldInfo Field { get; }

		/// <summary>
		///   Gets the range metadata of the <see cref="Object" />'s <see cref="Field" />.
		/// </summary>
		public RangeAttribute Range { get; }

		/// <summary>
		///   Gets the <see cref="Object" />'s <see cref="Field" />'s value the exception was raised for.
		/// </summary>
		public object FieldValue => Field.GetValue(Object);
	}
}