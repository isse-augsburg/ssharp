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
	///   Determines whether a <see cref="Formula" /> is a formula that can be evaluated in a single state in a
	///   model normalized using a re-traversal (LtmcFromRetraverseModelGenerator). This means that using the unary Operator "Once" is okay,
	///   as long as no other Once is nested inside.
	/// </summary>
	public class CollectMaximalNormalizableFormulasVisitor : CollectStateFormulasVisitor
	{
		public HashSet<Formula> MaximalNormalizableFormulas { get; } = new HashSet<Formula>();

		public override IEnumerable<Formula> CollectedStateFormulas => MaximalNormalizableFormulas;

		public bool IsNormalizable; //information propagated from children to parents

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
					if (NestedInsideOnce)
					{
						// formula itself is not normalizable, because Once was used inside.
						// so add child if possible
						if (IsNormalizable)
						{
							MaximalNormalizableFormulas.Add(formula.Operand);
						}
						IsNormalizable = false;
					}
					NestedInsideOnce = true;
					break;
				default:
					// formula itself is not normalizable, so add child if possible
					Visit(formula.Operand);
					if (IsNormalizable)
					{
						MaximalNormalizableFormulas.Add(formula.Operand);
					}
					IsNormalizable = false;
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
					var leftIsCompilable = IsNormalizable;
					leftUsedNestedOnce = NestedInsideOnce;
					Visit(formula.RightOperand);
					var rightIsCompilable = IsNormalizable;
					rightUsedNestedOnce = NestedInsideOnce;

					if (leftIsCompilable && rightIsCompilable)
					{
						IsNormalizable = true;
					}
					else
					{
						if (leftIsCompilable)
						{
							MaximalNormalizableFormulas.Add(formula.LeftOperand);
						}
						if (rightIsCompilable)
						{
							MaximalNormalizableFormulas.Add(formula.RightOperand);
						}
						IsNormalizable = false;
					}
					NestedInsideOnce = leftUsedNestedOnce || rightUsedNestedOnce;
					break;
				case BinaryOperator.Until:
					Visit(formula.LeftOperand);
					leftUsedNestedOnce = NestedInsideOnce;
					if (IsNormalizable)
					{
						MaximalNormalizableFormulas.Add(formula.LeftOperand);
					}
					Visit(formula.RightOperand);
					rightUsedNestedOnce = NestedInsideOnce;
					if (IsNormalizable)
					{
						MaximalNormalizableFormulas.Add(formula.RightOperand);
					}
					IsNormalizable = false;
					NestedInsideOnce = leftUsedNestedOnce || rightUsedNestedOnce;
					break;
			}
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitAtomarPropositionFormula(AtomarPropositionFormula formula)
		{
			IsNormalizable = true;
			NestedInsideOnce = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitBoundedUnaryFormula(BoundedUnaryFormula formula)
		{
			Visit(formula.Operand);
			if (IsNormalizable)
			{
				MaximalNormalizableFormulas.Add(formula.Operand);
			}
			IsNormalizable = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitBoundedBinaryFormula(BoundedBinaryFormula formula)
		{
			Visit(formula.LeftOperand);
			var leftUsedNestedOnce = NestedInsideOnce;
			if (IsNormalizable)
			{
				MaximalNormalizableFormulas.Add(formula.LeftOperand);
			}
			Visit(formula.RightOperand);
			var rightUsedNestedOnce = NestedInsideOnce;
			if (IsNormalizable)
			{
				MaximalNormalizableFormulas.Add(formula.RightOperand);
			}
			IsNormalizable = false;
			NestedInsideOnce = leftUsedNestedOnce || rightUsedNestedOnce;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitRewardFormula(RewardFormula formula)
		{
			IsNormalizable = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitProbabilisticFormula(ProbabilitisticFormula formula)
		{
			Visit(formula.Operand);
			if (IsNormalizable)
			{
				MaximalNormalizableFormulas.Add(formula.Operand);
			}
			IsNormalizable = false;
		}
		
		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitNewTopLevelFormula(Formula formula)
		{
			formula.Visit(this);
			if (IsNormalizable)
			{
				MaximalNormalizableFormulas.Add(formula);
			}
		}
	}
}