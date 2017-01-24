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

namespace Tests
{
	using System;
	using SafetySharp.Analysis;
	using SafetySharp.Analysis.ModelChecking.Probabilistic;
	using SafetySharp.ModelChecking;
	using Xunit;

	public partial class DccaTests
	{
		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Dcca")]
		public void FaultRemovalDcca(string test, string file)
		{
			ExecuteDynamicTests(file, SafetyAnalysisBackend.FaultOptimizedOnTheFly);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Dcca")]
		public void StateGraphDcca(string test, string file)
		{
			ExecuteDynamicTests(file, SafetyAnalysisBackend.FaultOptimizedStateGraph);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Heuristics")]
		public void Heuristics(string test, string file)
		{
			ExecuteDynamicTests(file, SafetyAnalysisBackend.FaultOptimizedOnTheFly);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Ordering")]
		public void Ordering(string test, string file)
		{
			ExecuteDynamicTests(file, SafetyAnalysisBackend.FaultOptimizedOnTheFly);
		}
	}

	public partial class InvariantTests
	{
		private bool _useCheckInvariantsInsteadOfCheckInvariant = false;

		private readonly AnalysisTestsVariant _analysisTestVariant = new AnalysisTestsWithQualitative();

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/CounterExamples")]
		public void CounterExamples(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant, _useCheckInvariantsInsteadOfCheckInvariant);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/NotViolated")]
		public void NotViolated(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant, _useCheckInvariantsInsteadOfCheckInvariant);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/Violated")]
		public void Violated(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant, _useCheckInvariantsInsteadOfCheckInvariant);
		}
	}

	public partial class InvariantWithIndexTests
	{
		private bool _useCheckInvariantsInsteadOfCheckInvariant = false;

		private readonly AnalysisTestsVariant _analysisTestVariant = new AnalysisTestsWithQualitativeWithIndex();

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/CounterExamples")]
		public void CounterExamples(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant, _useCheckInvariantsInsteadOfCheckInvariant);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/NotViolated")]
		public void NotViolated(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant, _useCheckInvariantsInsteadOfCheckInvariant);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/Violated")]
		public void Violated(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant, _useCheckInvariantsInsteadOfCheckInvariant);
		}
	}

	public partial class StateConstraintTests
	{
		private bool _useCheckInvariantsInsteadOfCheckInvariant = false;

		private readonly AnalysisTestsVariant _analysisTestVariant = new AnalysisTestsWithQualitative();

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/StateConstraints")]
		public void StateConstraints(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant, _useCheckInvariantsInsteadOfCheckInvariant);
		}
	}

	public partial class StateGraphInvariantTests
	{
		private bool _useCheckInvariantsInsteadOfCheckInvariant = true;

		private readonly AnalysisTestsVariant _analysisTestVariant = new AnalysisTestsWithQualitative();

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/CounterExamples")]
		public void CounterExamples(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant, _useCheckInvariantsInsteadOfCheckInvariant);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/NotViolated")]
		public void NotViolated(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant, _useCheckInvariantsInsteadOfCheckInvariant);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/Violated")]
		public void Violated(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant, _useCheckInvariantsInsteadOfCheckInvariant);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/MultipleInvariants")]
		public void MultipleInvariants(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant, _useCheckInvariantsInsteadOfCheckInvariant);
		}
	}

	public partial class LtsMinInvariantTests
	{
		private readonly AnalysisTestsVariant _analysisTestVariant = new AnalysisTestsWithLtsMin();

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/NotViolated")]
		public void NotViolated(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/Violated")]
		public void Violated(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant);
		}
	}

	public partial class LtlTests
	{
		private readonly AnalysisTestsVariant _analysisTestVariant = new AnalysisTestsWithLtsMin();

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Ltl/Violated")]
		public void Violated(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Ltl/NotViolated")]
		public void NotViolated(string test, string file)
		{
			ExecuteDynamicTests(file, _analysisTestVariant);
		}
	}

	public partial class ProbabilisticTests
	{
		[Theory(Skip = "Requires external tools"), MemberData("AllProbabilisticModelCheckerTests", "Analysis/Probabilistic")]
		public void ProbabilisticMrmc(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(ExternalDtmcModelCheckerMrmc));
		}

		[Theory, MemberData("AllProbabilisticModelCheckerTests", "Analysis/Probabilistic")]
		public void ProbabilisticBuiltin(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(BuiltinDtmcModelChecker));
		}
	}
}