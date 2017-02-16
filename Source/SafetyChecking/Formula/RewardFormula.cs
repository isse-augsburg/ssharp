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

namespace SafetySharp.Analysis
{
	using System;
	using FormulaVisitors;
	using Modeling;
	using Runtime.Serialization;
	using Utilities;

	public abstract class RewardFormula : Formula
	{
		protected RewardFormula(RewardRetriever rewardRetriever)
		{
			RewardRetriever = rewardRetriever;
		}

		protected RewardFormula(Func<Reward> rewardRetriever)
		{
			RewardRetriever = rewardRetriever;
		}

		public RewardRetriever RewardRetriever { get; }

		/// <summary>
		///   Executes the <paramref name="visitor" /> for this formula.
		/// </summary>
		/// <param name="visitor">The visitor that should be executed.</param>
		internal override void Visit(FormulaVisitor visitor)
		{
			visitor.VisitRewardFormula(this);
		}
	}

	public class LongRunExpectedRewardFormula : RewardFormula
	{
		public LongRunExpectedRewardFormula(RewardRetriever rewardRetriever, double lowerBound, double upperBound)
			: base(rewardRetriever)
		{
			LowerBound = lowerBound;
			UpperBound = upperBound;
		}

		public LongRunExpectedRewardFormula(Func<Reward> rewardRetriever, double lowerBound, double upperBound)
			: base(rewardRetriever)
		{
			LowerBound = lowerBound;
			UpperBound = upperBound;
		}

		public double LowerBound { get; }
		public double UpperBound { get; }
	}

	public class ExpectedAccumulatedRewardFormula : RewardFormula
	{
		public ExpectedAccumulatedRewardFormula(RewardRetriever rewardRetriever, double lowerBound, double upperBound)
			: base(rewardRetriever)
		{
			LowerBound = lowerBound;
			UpperBound = upperBound;
		}

		public ExpectedAccumulatedRewardFormula(Func<Reward> rewardRetriever, double lowerBound, double upperBound)
			: base(rewardRetriever)
		{
			LowerBound = lowerBound;
			UpperBound = upperBound;
		}

		public double LowerBound { get; }
		public double UpperBound { get; }
	}

	public class CalculateLongRunExpectedRewardFormula : RewardFormula
	{
		public CalculateLongRunExpectedRewardFormula(RewardRetriever rewardRetriever)
			: base(rewardRetriever)
		{
		}

		public CalculateLongRunExpectedRewardFormula(Func<Reward> rewardRetriever)
			: base(rewardRetriever)
		{
		}
	}

	public class CalculateExpectedAccumulatedRewardFormula : RewardFormula
	{
		public CalculateExpectedAccumulatedRewardFormula(RewardRetriever rewardRetriever)
			: base(rewardRetriever)
		{
		}

		public CalculateExpectedAccumulatedRewardFormula(Func<Reward> rewardRetriever)
			: base(rewardRetriever)
		{
		}
	}
}