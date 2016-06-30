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
	///   Represents a CTL* formula.
	/// </summary>
	public abstract class Formula
	{
		/// <summary>
		///   Converts the <paramref name="expression" /> to an instance of <see cref="Formula" />.
		/// </summary>
		/// <param name="expression">The expression that should be converted.</param>
		public static implicit operator Formula(bool expression)
		{
			Requires.CompilationTransformation();
			return null;
		}

		/// <summary>
		///   Converts the <paramref name="formula" /> to a <see cref="bool" /> value.
		/// </summary>
		public static bool operator true(Formula formula)
		{
			return false;
		}

		/// <summary>
		///   Converts the <paramref name="formula" /> to a <see cref="bool" /> value.
		/// </summary>
		public static bool operator false(Formula formula)
		{
			return false;
		}

		/// <summary>
		///   Executes the <paramref name="visitor" /> for this formula.
		/// </summary>
		/// <param name="visitor">The visitor that should be executed.</param>
		internal abstract void Visit(FormulaVisitor visitor);

		/// <summary>
		///   Evaluates the formula if it does not contain any temporal operators.
		/// </summary>
		internal bool Evaluate()
		{
			return Compile()();
		}

		/// <summary>
		///   Compiles the formula if it does not contain any temporal operators.
		/// </summary>
		public Func<bool> Compile()
		{
			return CompilationVisitor.Compile(this);
		}

		/// <summary>
		///   Returns a string that represents the current object.
		/// </summary>
		public override string ToString()
		{
			var visitor = new ToStringVisitor();
			visitor.Visit(this);
			return visitor.FormulaString;
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'not' operator to the <paramref name="formula" />.
		/// </summary>
		public static Formula operator !(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));
			return new UnaryFormula(formula, UnaryOperator.Not);
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'conjunction' operator to <paramref name="left" /> and
		///   <paramref name="right" />.
		/// </summary>
		public static Formula operator &(Formula left, Formula right)
		{
			Requires.NotNull(left, nameof(left));
			Requires.NotNull(right, nameof(right));

			return new BinaryFormula(left, BinaryOperator.And, right);
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'disjunction' operator to <paramref name="left" /> and
		///   <paramref name="right" />.
		/// </summary>
		public static Formula operator |(Formula left, Formula right)
		{
			Requires.NotNull(left, nameof(left));
			Requires.NotNull(right, nameof(right));

			return new BinaryFormula(left, BinaryOperator.Or, right);
		}
	}
}