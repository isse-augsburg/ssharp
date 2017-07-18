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

namespace Tests.SimpleExecutableModel.Analysis.Invariants.NotViolated
{
	using System;
	using ISSE.SafetyChecking;
	using ISSE.SafetyChecking.AnalysisModelTraverser;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using ISSE.SafetyChecking.Simulator;
	using ISSE.SafetyChecking.Utilities;
	using Shouldly;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	
	public class NestedSideEffectFreeMethodsWithFaults : AnalysisTest
	{
		public NestedSideEffectFreeMethodsWithFaults(ITestOutputHelper output = null) : base(output)
		{
		}

		[Fact]
		public void Check()
		{
			var m = new Model();
			var formulaNotTwo = new UnaryFormula(Model.StateIsTwo,UnaryOperator.Not);
			var checker = new SimpleQualitativeChecker(m, formulaNotTwo)
			{
				Configuration = AnalysisConfiguration.Default
			};
			checker.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			checker.OutputWritten += output => Output.Log(output);

			var result = checker.CheckInvariant(formulaNotTwo);
			result.FormulaHolds.ShouldBe(true);
		}

		public class Model : SimpleModelBase
		{
			public override Fault[] Faults { get; } = new Fault[]
			{
				new TransientFault { Identifier = 0, ProbabilityOfOccurrence = new Probability(0.5) },
				new TransientFault { Identifier = 1, ProbabilityOfOccurrence = new Probability(0.5) }
			};

			public override bool[] LocalBools { get; } = new bool[0];
			public override int[] LocalInts { get; } = new int[0];


			private Fault F1 => Faults[0];
			private Fault F2 => Faults[1];

			public override void SetInitialState()
			{
				State = 0;
			}

			public bool BaseMethod()
			{
				return Choice.Choose(true, false);
			}

			public bool InnerPossibleFaultMethod()
			{
				var myConstantResult = false;
				var baseResult = BaseMethod();
				if (baseResult == myConstantResult)
					return baseResult;

				//If F1.IsActivated has no effect, it is not even activated.
				F1.TryActivate();
				if (!F1.IsActivated)
					return baseResult;
				return myConstantResult;
			}

			public bool OuterPossibleFaultMethod()
			{
				var myConstantResult = true;
				var baseResult = InnerPossibleFaultMethod();
				if (baseResult == myConstantResult)
					return baseResult;

				//If F2.IsActivated has no effect, it is not even activated.
				F2.TryActivate();
				if (!F2.IsActivated)
					return baseResult;
				return myConstantResult;
			}

			public override void Update()
			{
				if (State != 0)
					return;
				if (OuterPossibleFaultMethod())
					State = 100;
				else
					State = 200;
			}

			public static Formula StateIsTwo = new SimpleStateInRangeFormula(2);
		}
	}
}