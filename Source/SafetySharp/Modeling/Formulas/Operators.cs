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
	using System.Diagnostics.CodeAnalysis;
	using Utilities;

	/// <summary>
	///   Provides factory methods for the construction of CTL* formulas.
	/// </summary>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public static class Operators
	{
		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'all paths' operator to <paramref name="operand" />.
		/// </summary>
		/// <param name="operand">The operand the 'all paths' operator should be applied to.</param>
		public static Formula A(Formula operand)
		{
			Requires.NotNull(operand, nameof(operand));
			return new UnaryFormula(operand, UnaryOperator.All);
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'exists path' operator to <paramref name="operand" />.
		/// </summary>
		/// <param name="operand">The operand the 'exists path' operator should be applied to.</param>
		public static Formula E(Formula operand)
		{
			Requires.NotNull(operand, nameof(operand));
			return new UnaryFormula(operand, UnaryOperator.Exists);
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'next' operator to <paramref name="operand" />.
		/// </summary>
		/// <param name="operand">The operand the 'next' operator should be applied to.</param>
		public static Formula X(Formula operand)
		{
			Requires.NotNull(operand, nameof(operand));
			return new UnaryFormula(operand, UnaryOperator.Next);
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'finally' operator to <paramref name="operand" />.
		/// </summary>
		/// <param name="operand">The operand the 'finally' operator should be applied to.</param>
		public static Formula F(Formula operand)
		{
			Requires.NotNull(operand, nameof(operand));
			return new UnaryFormula(operand, UnaryOperator.Finally);
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'globally' operator to <paramref name="operand" />.
		/// </summary>
		/// <param name="operand">The operand the 'globally' operator should be applied to.</param>
		public static Formula G(Formula operand)
		{
			Requires.NotNull(operand, nameof(operand));
			return new UnaryFormula(operand, UnaryOperator.Globally);
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'until' operator to <paramref name="leftOperand" /> and
		///   <paramref name="rightOperand" />.
		/// </summary>
		/// <param name="leftOperand">The operand on the left-hand side of the 'until' operator.</param>
		/// <param name="rightOperand">The operand on the right-hand side of the 'until' operator.</param>
		public static Formula U(Formula leftOperand, Formula rightOperand)
		{
			Requires.NotNull(leftOperand, nameof(leftOperand));
			Requires.NotNull(rightOperand, nameof(rightOperand));

			return new BinaryFormula(leftOperand, BinaryOperator.Until, rightOperand);
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'next' operator to <paramref name="operand" /> for all paths.
		/// </summary>
		/// <param name="operand">The operand the 'next' operator should be applied to.</param>
		public static Formula AX(Formula operand)
		{
			return A(X(operand));
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'next' operator to <paramref name="operand" /> for any path.
		/// </summary>
		/// <param name="operand">The operand the 'next' operator should be applied to.</param>
		public static Formula EX(Formula operand)
		{
			return E(X(operand));
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'finally' operator to <paramref name="operand" /> for all paths.
		/// </summary>
		/// <param name="operand">The operand the 'finally' operator should be applied to.</param>
		public static Formula AF(Formula operand)
		{
			return A(F(operand));
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'finally' operator to <paramref name="operand" /> for any path.
		/// </summary>
		/// <param name="operand">The operand the 'finally' operator should be applied to.</param>
		public static Formula EF(Formula operand)
		{
			return E(F(operand));
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'globally' operator to <paramref name="operand" /> for all paths.
		/// </summary>
		/// <param name="operand">The operand the 'globally' operator should be applied to.</param>
		public static Formula AG(Formula operand)
		{
			return A(G(operand));
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'globally' operator to <paramref name="operand" /> for any path.
		/// </summary>
		/// <param name="operand">The operand the 'globally' operator should be applied to.</param>
		public static Formula EG(Formula operand)
		{
			return E(G(operand));
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'until' operator to <paramref name="leftOperand" /> and
		///   <paramref name="rightOperand" /> for all paths.
		/// </summary>
		/// <param name="leftOperand">The operand on the left-hand side of the 'until' operator.</param>
		/// <param name="rightOperand">The operand on the right-hand side of the 'until' operator.</param>
		public static Formula AU(Formula leftOperand, Formula rightOperand)
		{
			return A(U(leftOperand, rightOperand));
		}

		/// <summary>
		///   Returns a <see cref="Formula" /> that applies the 'until' operator to <paramref name="leftOperand" /> and
		///   <paramref name="rightOperand" /> for any path.
		/// </summary>
		/// <param name="leftOperand">The operand on the left-hand side of the 'until' operator.</param>
		/// <param name="rightOperand">The operand on the right-hand side of the 'until' operator.</param>
		public static Formula EU(Formula leftOperand, Formula rightOperand)
		{
			return E(U(leftOperand, rightOperand));
		}
	}
}