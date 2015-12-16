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

namespace SafetySharp.Analysis.FormulaVisitors
{
	using System;

	/// <summary>
	///   Determines whether a <see cref="Formula" /> is a LTL formula.
	/// </summary>
	internal class EvaluationVisitor : FormulaVisitor
	{
		/// <summary>
		///   Indicates whether the visited formula holds.
		/// </summary>
		private bool _holds;

		/// <summary>
		///   Gets a value indicating whether the formula holds.
		/// </summary>
		public bool Result => _holds;

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitUnaryFormula(UnaryFormula formula)
		{
			switch (formula.Operator)
			{
				case UnaryOperator.Not:
					Visit(formula.Operand);
					_holds = !_holds;
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

			switch (formula.Operator)
			{
				case BinaryOperator.And:
					if (_holds)
						Visit(formula.RightOperand);
					break;
				case BinaryOperator.Or:
					if (!_holds)
						Visit(formula.RightOperand);
					break;
				case BinaryOperator.Implication:
					if (_holds)
						Visit(formula.RightOperand);
					else
						_holds = true;
					break;
				case BinaryOperator.Equivalence:
					var leftHolds = _holds;
					Visit(formula.RightOperand);
					_holds = (leftHolds && _holds) || (!leftHolds && !_holds);
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
			_holds = formula.Expression();
		}
	}
}