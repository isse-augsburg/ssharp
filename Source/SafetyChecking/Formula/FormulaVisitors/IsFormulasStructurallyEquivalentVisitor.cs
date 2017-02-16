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


namespace SafetySharp.Analysis.FormulaVisitors
{
	using System;
	using System.Linq.Expressions;
	using Runtime;
	using Utilities;

	/// <summary>
	///   Determines whether a <see cref="Formula" /> is structurally equivalent to a given <see cref="Formula" />.
	/// </summary>
	internal class IsFormulasStructurallyEquivalentVisitor : FormulaVisitor
	{
		/// <summary>
		///   Compares <paramref name="formula" /> with <paramref name="referenceFormula" />.
		/// </summary>
		public static bool Compare(Formula referenceFormula, Formula formula)
		{
			Requires.NotNull(referenceFormula, nameof(referenceFormula));
			Requires.NotNull(formula, nameof(formula));

			var visitor = new IsFormulasStructurallyEquivalentVisitor(referenceFormula);
			visitor.Visit(formula);
			return visitor.IsEqual;
		}

		private Formula _referenceFormula;

		public bool IsEqual { get; private set; }

		private IsFormulasStructurallyEquivalentVisitor(Formula referenceFormula)
		{
			_referenceFormula = referenceFormula;
			IsEqual = true;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitUnaryFormula(UnaryFormula formula)
		{
			var referenceFormula = _referenceFormula as UnaryFormula;
			if (referenceFormula == null)
			{
				IsEqual = false;
			}
			else
			{
				if (formula.Operator != referenceFormula.Operator)
				{
					IsEqual = false;
				}
				else
				{
					_referenceFormula = referenceFormula.Operand;
					Visit(formula.Operand);
				}
			}
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitBinaryFormula(BinaryFormula formula)
		{
			var referenceFormula = _referenceFormula as BinaryFormula;
			if (referenceFormula == null)
			{
				IsEqual = false;
			}
			else
			{
				if (formula.Operator != referenceFormula.Operator)
				{
					IsEqual = false;
				}
				else
				{
					_referenceFormula = referenceFormula.LeftOperand;
					Visit(formula.LeftOperand);
					if (!IsEqual)
						return;
					_referenceFormula = referenceFormula.RightOperand;
					Visit(formula.RightOperand);
				}
			}
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitAtomarPropositionFormula(AtomarPropositionFormula formula)
		{
			var referenceFormula = _referenceFormula as AtomarPropositionFormula;
			if (referenceFormula == null)
			{
				IsEqual = false;
			}
			else
			{
				if (formula.Label != referenceFormula.Label)
				{
					IsEqual = false;
				}
			}
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitRewardFormula(RewardFormula formula)
		{
			var referenceFormula = _referenceFormula as RewardFormula;
			if (referenceFormula == null)
			{
				IsEqual = false;
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitProbabilisticFormula(ProbabilitisticFormula formula)
		{
			var referenceFormula = _referenceFormula as ProbabilitisticFormula;
			if (referenceFormula == null)
			{
				IsEqual = false;
			}
			else
			{
				if (referenceFormula.Comparator != formula.Comparator || referenceFormula.CompareToValue != formula.CompareToValue)
				{
					IsEqual = false;
				}
				else
				{
					_referenceFormula = referenceFormula.Operand;
					Visit(formula.Operand);
				}
			}
		}
	}
}
