using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.Analysis.FormulaVisitors
{
	internal class IsFormulaReturningProbabilityVisitor : FormulaVisitor
	{
		/// <summary>
		///   Indicates whether the formula is returning a probability.
		/// </summary>
		public bool IsReturningProbability { get; private set; }

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitUnaryFormula(UnaryFormula formula)
		{
			IsReturningProbability = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitBinaryFormula(BinaryFormula formula)
		{
			IsReturningProbability = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitAtomarPropositionFormula(AtomarPropositionFormula formula)
		{
			IsReturningProbability = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitExecutableStateFormula(ExecutableStateFormula formula)
		{
			IsReturningProbability = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitRewardFormula(RewardFormula formula)
		{
			IsReturningProbability = false;
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitProbabilisticFormula(ProbabilitisticFormula formula)
		{
			if (formula is CalculateProbabilityToReachStateFormula)
			{
				IsReturningProbability = true;
			}
			else if (formula is ProbabilityToReachStateFormula)
			{
				IsReturningProbability = false;
			}
			else
			{
				throw new Exception("Not supported, yet");
			}
		}
	}
}
