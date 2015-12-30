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

namespace SafetySharp.Analysis
{
	using System;
	using FormulaVisitors;
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
		/// <param name="label">
		///   The name that should be used for the state label of the formula. If <c>null</c>, a unique name is generated.
		/// </param>
		internal StateFormula(Func<bool> expression, string label = null)
		{
			Requires.NotNull(expression, nameof(expression));

			Expression = expression;
			Label = label ?? "StateFormula" + Guid.NewGuid().ToString().Replace("-", String.Empty);
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
		///   Executes the <paramref name="visitor" /> for this formula.
		/// </summary>
		/// <param name="visitor">The visitor that should be executed.</param>
		internal override void Visit(FormulaVisitor visitor)
		{
			visitor.VisitStateFormula(this);
		}
	}
}