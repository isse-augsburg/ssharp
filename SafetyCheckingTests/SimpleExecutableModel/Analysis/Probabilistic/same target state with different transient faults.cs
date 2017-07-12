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


namespace Tests.SimpleExecutableModel.Analysis.Probabilistic
{
	using System;
	using ISSE.SafetyChecking;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using Shouldly;
	using Xunit;
	using Xunit.Abstractions;

	public class SameTargetStateWithDifferentFaults : AnalysisTest
	{
		public SameTargetStateWithDifferentFaults(ITestOutputHelper output = null) : base(output)
		{
		}

		[Fact]
		public void Check()
		{
			var m = new Model();
			Probability probabilityOfFinal1;
			
			var finally1 = new BoundedUnaryFormula(new SimpleStateInRangeFormula(1), UnaryOperator.Finally, 1);

			var markovChainGenerator = new SimpleDtmcFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.Configuration.WriteGraphvizModels = true;
			markovChainGenerator.Configuration.DefaultTraceOutput = Output.TextWriterAdapter();
			markovChainGenerator.AddFormulaToCheck(finally1);
			var dtmc = markovChainGenerator.GenerateMarkovChain();
			var typeOfModelChecker = typeof(BuiltinDtmcModelChecker);
			var modelChecker = (DtmcModelChecker)Activator.CreateInstance(typeOfModelChecker, dtmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFinal1 = modelChecker.CalculateProbability(finally1);
			}

			probabilityOfFinal1.Is(0.325, 0.000001).ShouldBe(true);
		}

		[Fact]
		public void CheckWithFaultActivationInFormula()
		{
			var m = new Model();
			Probability probabilityOfFinal1;

			var state1WithFault1 = new BinaryFormula(new SimpleStateInRangeFormula(1), BinaryOperator.And, new FaultFormula(m.F1) );

			var finallyState1WithFault1 = new BoundedUnaryFormula(state1WithFault1, UnaryOperator.Finally,1);

			var markovChainGenerator = new SimpleDtmcFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.Configuration.MomentOfIndependentFaultActivation = MomentOfIndependentFaultActivation.AtStepBeginning;
			markovChainGenerator.Configuration.WriteGraphvizModels = true;
			markovChainGenerator.Configuration.DefaultTraceOutput = Output.TextWriterAdapter();
			markovChainGenerator.AddFormulaToCheck(finallyState1WithFault1);
			var dtmc = markovChainGenerator.GenerateMarkovChain();
			var typeOfModelChecker = typeof(BuiltinDtmcModelChecker);
			var modelChecker = (DtmcModelChecker)Activator.CreateInstance(typeOfModelChecker, dtmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFinal1 = modelChecker.CalculateProbability(finallyState1WithFault1);
			}

			probabilityOfFinal1.Is(0.275, 0.000001).ShouldBe(true);
		}

		private class Model : SimpleModelBase
		{
			public override Fault[] Faults { get; } = new Fault[]
			{
				new TransientFault { Identifier = 0, ProbabilityOfOccurrence = new Probability(0.5) },
				new TransientFault { Identifier = 1, ProbabilityOfOccurrence = new Probability(0.5) }
			};

			public override bool[] LocalBools { get; } = new bool[0];
			public override int[] LocalInts { get; } = new int[0];

			public Fault F1 => Faults[0];
			private Fault F2 => Faults[1];
			
			public virtual void Helper1()
			{
				F1.TryActivate();

				if (F1.IsActivated)
					Helper2();
				else
					State=2;
			}

			public virtual void Helper2()
			{
				F2.TryActivate();

				if (F2.IsActivated)
				{
					// way 2: Fault F1 and F2 makes this reachable
					State = 1;
				}
				else
				{
					State = 3;
				}
			}


			public override void Update()
			{
				if (State == 0)
				{
					if (Choice.Choose(
						new Option<bool>(new Probability(0.1), true),
						new Option<bool>(new Probability(0.9), false)))
					{
						// way 1
						State = 1;
					}
					else
					{
						Helper1();
					}
				}
			}
		}
	}
}
