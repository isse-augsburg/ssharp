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
	using Utilities;

	/// <summary>
	///   Represents a CTL* formula.
	/// </summary>
	public abstract class Formula
	{
		/// <summary>
		///   Gets a value indicating whether the formula is a valid linear temporal logic formula.
		/// </summary>
		public abstract bool IsLinearFormula { get; }

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
		///   Returns a <see cref="Formula" /> that applies the implication operator to this instance (the antecedent) and
		///   <paramref name="formula" /> (the succedent).
		/// </summary>
		/// <param name="formula">The formula representing the succedent of the implication.</param>
		public Formula Implies(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));
			return new BinaryFormula(this, BinaryOperator.Implication, formula);
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the equivalence operator to this instance and
		///   <paramref name="formula" />.
		/// </summary>
		/// <param name="formula">The formula that should be equivalent.</param>
		public Formula EquivalentTo(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));
			return new BinaryFormula(this, BinaryOperator.Equivalence, formula);
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