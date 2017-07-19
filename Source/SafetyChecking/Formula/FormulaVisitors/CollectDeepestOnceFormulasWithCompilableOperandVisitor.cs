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

namespace ISSE.SafetyChecking.Formula
{
	using System.Collections.Generic;

	/// <summary>
	///   Determines whether a <see cref="Formula" /> is a formula that can be evaluted in a single state.
	/// </summary>
	public class CollectDeepestOnceFormulasWithCompilableOperandVisitor : CollectStateFormulasVisitor
	{
		public HashSet<Formula> DeepestOnceFormulasWithCompilableOperand { get; } = new HashSet<Formula>();

		public override IEnumerable<Formula> CollectedStateFormulas => DeepestOnceFormulasWithCompilableOperand;

		public bool IsCompilable; //information propagated from children to parents

		public bool NestedInsideOnce; //information propagated from children to parents

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitUnaryFormula(UnaryFormula formula)
		{
			switch (formula.Operator)
			{
				case UnaryOperator.Not:
					Visit(formula.Operand);
					break;
				case UnaryOperator.Once:
					Visit(formula.Operand);
					if (!NestedInsideOnce && IsCompilable)
					{
						DeepestOnceFormulasWithCompilableOperand.Add(formula);
					}
					if (NestedInsideOnce)
					{
						IsCompilable = false;
					}
					NestedInsideOnce = true;
					break;
				default:
					// formula itself is not normalizable, so add child if possible
					Visit(formula.Operand);
					IsCompilable = false;
					break;
			}
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitBinaryFormula(BinaryFormula formula)
		{
			var leftUsedNestedOnce = false;
			var rightUsedNestedOnce = false;
			switch (formula.Operator)
			{
				case BinaryOperator.And:
				case BinaryOperator.Or:
				case BinaryOperator.Implication:
				case BinaryOperator.Equivalence:
					Visit(formula.LeftOperand);
					var leftIsCompilable = IsCompilable;
					leftUsedNestedOnce = NestedInsideOnce;
					Visit(formula.RightOperand);
					var rightIsCompilable = IsCompilable;
					rightUsedNestedOnce = NestedInsideOnce;

					if (leftIsCompilable && rightIsCompilable)
					{
						IsCompilable = true;
					}
					else
					{
						IsCompilable = false;
					}
					NestedInsideOnce = leftUsedNestedOnce || rightUsedNestedOnce;
					break;
				case BinaryOperator.Until:
					Visit(formula.LeftOperand);
					leftUsedNestedOnce = NestedInsideOnce;
					Visit(formula.RightOperand);
					rightUsedNestedOnce = NestedInsideOnce;
					IsCompilable = false;
					NestedInsideOnce = leftUsedNestedOnce || rightUsedNestedOnce;
					break;
			}
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitAtomarPropositionFormula(AtomarPropositionFormula formula)
		{
			IsCompilable = true;
			NestedInsideOnce = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitBoundedUnaryFormula(BoundedUnaryFormula formula)
		{
			Visit(formula.Operand);
			IsCompilable = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitBoundedBinaryFormula(BoundedBinaryFormula formula)
		{
			Visit(formula.LeftOperand);
			var leftUsedNestedOnce = NestedInsideOnce;
			Visit(formula.RightOperand);
			var rightUsedNestedOnce = NestedInsideOnce;
			IsCompilable = false;
			NestedInsideOnce = leftUsedNestedOnce || rightUsedNestedOnce;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitRewardFormula(RewardFormula formula)
		{
			IsCompilable = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitProbabilisticFormula(ProbabilitisticFormula formula)
		{
			Visit(formula.Operand);
			IsCompilable = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitNewTopLevelFormula(Formula formula)
		{
			formula.Visit(this);
		}
	}

}