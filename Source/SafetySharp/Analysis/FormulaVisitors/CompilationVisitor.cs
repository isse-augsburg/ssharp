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

namespace SafetySharp.Analysis.FormulaVisitors
{
	using System;
	using System.Linq.Expressions;
	using Utilities;

	/// <summary>
	///   Compiles a <see cref="Formula" /> if it does not contain any temporal operators.
	/// </summary>
	internal class CompilationVisitor : FormulaVisitor
	{
		/// <summary>
		///   The expression that is being generated.
		/// </summary>
		private Expression _expression;

		/// <summary>
		///   Compiles the <paramref name="formula" />.
		/// </summary>
		/// <param name="formula">The formula that should be compiled.</param>
		public static Func<bool> Compile(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));

			var visitor = new CompilationVisitor();
			visitor.Visit(formula);

			return Expression.Lambda<Func<bool>>(visitor._expression).Compile();
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitUnaryFormula(UnaryFormula formula)
		{
			switch (formula.Operator)
			{
				case UnaryOperator.Not:
					Visit(formula.Operand);
					_expression = Expression.Not(_expression);
					break;
				default:
					throw new InvalidOperationException("Only state formulas can be evaluated.");
			}
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitBinaryFormula(BinaryFormula formula)
		{
			Visit(formula.LeftOperand);
			var left = _expression;

			Visit(formula.RightOperand);
			var right = _expression;

			switch (formula.Operator)
			{
				case BinaryOperator.And:
					_expression = Expression.AndAlso(left, right);
					break;
				case BinaryOperator.Or:
					_expression = Expression.OrElse(left, right);
					break;
				case BinaryOperator.Implication:
					_expression = Expression.OrElse(Expression.Not(left), right);
					break;
				case BinaryOperator.Equivalence:
					var leftLocal = Expression.Parameter(typeof(bool), "left");
					var rightLocal = Expression.Parameter(typeof(bool), "right");
					var bothHold = Expression.AndAlso(leftLocal, rightLocal);
					var neitherHolds = Expression.AndAlso(Expression.Not(leftLocal), Expression.Not(rightLocal));

					_expression = Expression.Block(
						new[] { leftLocal, rightLocal },
						Expression.Assign(leftLocal, left),
						Expression.Assign(rightLocal, right),
						Expression.OrElse(bothHold, neitherHolds));
					break;
				case BinaryOperator.Until:
					throw new InvalidOperationException("Only state formulas can be evaluated.");
			}
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitStateFormula(StateFormula formula)
		{
			_expression = Expression.Invoke(Expression.Constant(formula.Expression));
		}
	}
}