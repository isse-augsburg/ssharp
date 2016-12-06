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
	using System.Globalization;
	using System.Text;
	using Utilities;

	/// <summary>
	///   Transforms a linear temporal logic formula to a LtsMin LTL formula.
	/// </summary>
	internal class MrmcTransformer : FormulaVisitor
	{
		/// <summary>
		///   The string builder that is used to construct the transformed formula.
		/// </summary>
		private readonly StringBuilder _builder = new StringBuilder();

		/// <summary>
		///   Gets the transformed LTL formula.
		/// </summary>
		public string TransformedFormula => _builder.ToString();

		/// <summary>
		///   Visits the <paramref name="formula" />.
		/// </summary>
		public override void VisitUnaryFormula(UnaryFormula formula)
		{
			_builder.Append("(");

			switch (formula.Operator)
			{
				case UnaryOperator.Not:
					_builder.Append(" ! ");
					break;
				default:
					Assert.NotReached($"Unknown or unsupported unary operator '{formula.Operator}'.");
					break;
			}

			Visit(formula.Operand);
			_builder.Append(")");
		}

		/// <summary>
		///   Visits the <paramref name="formula" />.
		/// </summary>
		public override void VisitBinaryFormula(BinaryFormula formula)
		{
			_builder.Append("(");
			Visit(formula.LeftOperand);

			switch (formula.Operator)
			{
				case BinaryOperator.And:
					_builder.Append(" && ");
					break;
				case BinaryOperator.Or:
					_builder.Append(" || ");
					break;
				case BinaryOperator.Implication:
					_builder.Append(" -> ");
					break;
				case BinaryOperator.Equivalence:
					_builder.Append(" <-> ");
					break;
				default:
					Assert.NotReached($"Unknown or unsupported binary operator '{formula.Operator}'.");
					break;
			}

			Visit(formula.RightOperand);
			_builder.Append(")");
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitExecutableStateFormula(ExecutableStateFormula formula)
		{
			_builder.Append(formula.Label);
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitRewardFormula(RewardFormula formula)
		{
			if (formula is CalculateLongRunExpectedRewardFormula)
			{
				var longRunExpected = (CalculateLongRunExpectedRewardFormula)formula;
				_builder.Append($"E [ {0.0}, {1.0}] [ ");
				//_builder.Append(formula.RewardRetriever.Label);
				_builder.Append(" tt ");
				_builder.Append(" ]");
			}
			else if (formula is CalculateExpectedAccumulatedRewardFormula)
			{
				throw new Exception("Not supported, yet");
			}
			else if (formula is ExpectedAccumulatedRewardFormula)
			{
				throw new Exception("Not supported, yet");
			}
			else if (formula is LongRunExpectedRewardFormula)
			{
				var longRunExpected = (LongRunExpectedRewardFormula) formula;
				var lowerBound = longRunExpected.LowerBound.ToString(CultureInfo.InvariantCulture);
				var upperBound = longRunExpected.UpperBound.ToString(CultureInfo.InvariantCulture);
				_builder.Append($"E [ {lowerBound}, {upperBound}] [ ");
				//_builder.Append(formula.RewardRetriever.Label);
				_builder.Append(" tt ");
				_builder.Append(" ]");
			}
			else
			{
				throw new Exception("Not supported, yet");
			}
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitProbabilisticFormula(ProbabilitisticFormula formula)
		{
			if (formula is CalculateProbabilityToReachStateFormula)
			{
				_builder.Append("P { > 0 } [ tt U ");
				Visit(formula.Operand);
				_builder.Append(" ]");
			}
			else if (formula is ProbabilityToReachStateFormula)
			{
				throw new Exception("Not supported, yet");
			}
			else
			{
				throw new Exception("Not supported, yet");
			}
		}
	}
}