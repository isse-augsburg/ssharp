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
		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/CounterExamples")]
		public void CounterExamples(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(SafetySharpQualitativeChecker), false);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/NotViolated")]
		public void NotViolated(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(SafetySharpQualitativeChecker), false);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/Violated")]
		public void Violated(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(SafetySharpQualitativeChecker), false);
		}
	}

	public partial class StateConstraintTests
	{
		[Theory, MemberData(nameof(DiscoverTests), "Analysis/StateConstraints")]
		public void StateConstraints(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(SafetySharpQualitativeChecker), false);
		}
	}

	public partial class StateGraphInvariantTests
	{
		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/CounterExamples")]
		public void CounterExamples(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(SafetySharpQualitativeChecker), true);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/NotViolated")]
		public void NotViolated(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(SafetySharpQualitativeChecker), true);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/Violated")]
		public void Violated(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(SafetySharpQualitativeChecker), true);
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/MultipleInvariants")]
		public void MultipleInvariants(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(SafetySharpQualitativeChecker), true);
		}
	}

	public partial class LtsMinInvariantTests
	{
		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/NotViolated")]
		public void NotViolated(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(LtsMin));
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Invariants/Violated")]
		public void Violated(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(LtsMin));
		}
	}

	public partial class LtlTests
	{
		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Ltl/Violated")]
		public void Violated(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(LtsMin));
		}

		[Theory, MemberData(nameof(DiscoverTests), "Analysis/Ltl/NotViolated")]
		public void NotViolated(string test, string file)
		{
			ExecuteDynamicTests(file, typeof(LtsMin));
		}
	}

	public partial class ProbabilisticTests
	{
		[Theory, MemberData("AllProbabilisticModelCheckerTests", "Analysis/Probabilistic")]
		public void Probabilistic(Type modelCheckerType, string test, string file)
		{
			ExecuteDynamicTests(file, modelCheckerType);
		}
	}
}