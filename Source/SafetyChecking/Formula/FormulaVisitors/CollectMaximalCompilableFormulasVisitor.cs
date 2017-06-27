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
	using System;
	using System.Collections.Generic;
	using System.Linq.Expressions;

	/// <summary>
	///   Determines whether a <see cref="Formula" /> is a formula that can be evaluted in a single state.
	/// </summary>
	public class CollectMaximalCompilableFormulasVisitor : FormulaVisitor
	{
		public HashSet<Formula> MaximalCompilableFormulas { get; } = new HashSet<Formula>();

		public IEnumerable<Formula> CollectedStateFormulas => MaximalCompilableFormulas;

		public bool IsCompilable; //information propagated from children to parents

		public bool AddWhenPossible; //information propagated from parents to children

		private void AddIfNecessary(Formula formula)
		{
			if (AddWhenPossible && IsCompilable)
				MaximalCompilableFormulas.Add(formula);
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitUnaryFormula(UnaryFormula formula)
		{
			switch (formula.Operator)
			{
				case UnaryOperator.Not:
					var addThis = AddWhenPossible;
					AddWhenPossible = false;
					Visit(formula.Operand);
					AddWhenPossible = addThis;
					//Value of IsCompilable is kept
					AddIfNecessary(formula);
					break;
				default:
					// formula itself is not compilable, so add child if possible
					AddWhenPossible = true;
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
			switch (formula.Operator)
			{
				case BinaryOperator.And:
				case BinaryOperator.Or:
				case BinaryOperator.Implication:
				case BinaryOperator.Equivalence:
					var addThis = AddWhenPossible;
					AddWhenPossible = false;
					Visit(formula.LeftOperand);
					var leftIsCompilable = IsCompilable;
					AddWhenPossible = false;
					Visit(formula.RightOperand);
					var rightIsCompilable = IsCompilable;
					AddWhenPossible = addThis;
					if (leftIsCompilable && rightIsCompilable)
					{
						IsCompilable = true;
						AddIfNecessary(formula);
					}
					else if (!leftIsCompilable && rightIsCompilable)
					{
						IsCompilable = true;
						AddIfNecessary(formula.RightOperand);
						IsCompilable = false;
					}
					else if (leftIsCompilable && !rightIsCompilable)
					{
						IsCompilable = true;
						AddIfNecessary(formula.LeftOperand);
						IsCompilable = false;
					}
					else
					{
						IsCompilable = false;
					}
					break;
				case BinaryOperator.Until:
					AddWhenPossible = true;
					Visit(formula.LeftOperand);
					Visit(formula.RightOperand);
					IsCompilable = false;
					break;
			}
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitAtomarPropositionFormula(AtomarPropositionFormula formula)
		{
			IsCompilable = true;
			AddIfNecessary(formula);
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitBoundedUnaryFormula(BoundedUnaryFormula formula)
		{
			AddWhenPossible = true;
			Visit(formula.Operand);
			IsCompilable = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitBoundedBinaryFormula(BoundedBinaryFormula formula)
		{
			AddWhenPossible = true;
			Visit(formula.LeftOperand);
			Visit(formula.RightOperand);
			IsCompilable = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitRewardFormula(RewardFormula formula)
		{
			AddWhenPossible = true;
			IsCompilable = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitProbabilisticFormula(ProbabilitisticFormula formula)
		{
			AddWhenPossible = true;
			Visit(formula.Operand);
			IsCompilable = false;
		}
		
		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public void VisitNewTopLevelFormula(Formula formula)
		{
			AddWhenPossible = true;
			formula.Visit(this);
		}
	}
}