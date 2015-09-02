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

namespace SafetySharp.Modeling.Formulas
{
	using System;
	using Utilities;

	/// <summary>
	///   Represents a state formula, i.e., a Boolean expression that is evaluated in a single system state.
	/// </summary>
	internal sealed class StateFormula : Formula
	{
		/// <summary>
		///   Initializes a new instance of the <see cref="StateFormula" /> class.
		/// </summary>
		/// <param name="expression">The expression that represents the state formula.</param>
		internal StateFormula(Func<bool> expression)
		{
			Requires.NotNull(expression, nameof(expression));

			Expression = expression;
			Label = $"StateFormula" + Guid.NewGuid().ToString().Replace("-", String.Empty);
		}

		/// <summary>
		///   Gets the expression that represents the state formula.
		/// </summary>
		public Func<bool> Expression { get; }

		/// <summary>
		///   Gets the state label that a model checker can use to determine whether the state formula holds.
		/// </summary>
		public string Label { get; }

		/// <summary>
		///   Gets a value indicating whether the formula is a valid linear temporal logic formula.
		/// </summary>
		public override bool IsLinearFormula => true;

		/// <summary>
		///   Returns a string that represents the current object.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}
	}
}