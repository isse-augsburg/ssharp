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
	/// <summary>
	///   Determines whether a <see cref="Formula" /> is a formula that can be evaluted in a single state.
	/// </summary>
	internal class IsStateFormulaVisitor : FormulaVisitor
	{
		/// <summary>
		///   Indicates whether the visited formula contains any invalid operators.
		/// </summary>
		private bool _containsInvalidOperators;

		/// <summary>
		///   Gets a value indicating whether the formula is a LTL formula.
		/// </summary>
		public bool IsStateFormula => !_containsInvalidOperators;

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitUnaryFormula(UnaryFormula formula)
		{
			if (formula.Operator == UnaryOperator.Not)
				Visit(formula.Operand);
			else
				_containsInvalidOperators = true;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitBinaryFormula(BinaryFormula formula)
		{
			if (formula.Operator == BinaryOperator.Until)
				_containsInvalidOperators = true;
			else
			{
				Visit(formula.LeftOperand);

				if (IsStateFormula)
					Visit(formula.RightOperand);
			}
		}
		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitAtomarPropositionFormula(AtomarPropositionFormula formula)
		{
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitRewardFormula(RewardFormula formula)
		{
			_containsInvalidOperators = true;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitProbabilisticFormula(ProbabilitisticFormula formula)
		{
			_containsInvalidOperators = true;
		}
	}
}