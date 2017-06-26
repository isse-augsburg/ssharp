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

namespace Tests.SimpleExecutableModel.Analysis.ProbabilisticNondeterministic
{
	using System;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized;
	using ISSE.SafetyChecking.Modeling;
	using Shouldly;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	

	public class SimpleExample2a : AnalysisTest
	{
		public SimpleExample2a(ITestOutputHelper output = null) : base(output)
		{
		}

		[Fact]
		public void Check()
		{
			var m = new SharedModels.SimpleExample2a();
			Probability minProbabilityOfFinal0;
			Probability minProbabilityOfFinal1;
			Probability minProbabilityOfFinal2;
			Probability maxProbabilityOfFinal0;
			Probability maxProbabilityOfFinal1;
			Probability maxProbabilityOfFinal2;

			var final0Formula = new BoundedUnaryFormula(new SimpleStateInRangeFormula(0), UnaryOperator.Finally, 4);
			var final1Formula = new BoundedUnaryFormula(new SimpleStateInRangeFormula(1), UnaryOperator.Finally, 4);
			var final2Formula = new BoundedUnaryFormula(new SimpleStateInRangeFormula(2), UnaryOperator.Finally, 4);

			var nmdpGenerator = new SimpleNmdpFromExecutableModelGenerator(m);
			nmdpGenerator.Configuration.WriteGraphvizModels = true;
			nmdpGenerator.Configuration.DefaultTraceOutput = Output.TextWriterAdapter();
			nmdpGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			nmdpGenerator.AddFormulaToCheck(final0Formula);
			nmdpGenerator.AddFormulaToCheck(final1Formula);
			nmdpGenerator.AddFormulaToCheck(final2Formula);
			var nmdp = nmdpGenerator.GenerateMarkovDecisionProcess();
			var typeOfModelChecker = typeof(BuiltinNmdpModelChecker);
			var modelChecker = (NmdpModelChecker)Activator.CreateInstance(typeOfModelChecker, nmdp, Output.TextWriterAdapter());
			using (modelChecker)
			{
				minProbabilityOfFinal0 = modelChecker.CalculateMinimalProbability(final0Formula);
				minProbabilityOfFinal1 = modelChecker.CalculateMinimalProbability(final1Formula);
				minProbabilityOfFinal2 = modelChecker.CalculateMinimalProbability(final2Formula);
				maxProbabilityOfFinal0 = modelChecker.CalculateMaximalProbability(final0Formula);
				maxProbabilityOfFinal1 = modelChecker.CalculateMaximalProbability(final1Formula);
				maxProbabilityOfFinal2 = modelChecker.CalculateMaximalProbability(final2Formula);
			}

			minProbabilityOfFinal0.Is(1.0, 0.000001).ShouldBe(true);
			minProbabilityOfFinal1.Is(0.0, 0.000001).ShouldBe(true);
			minProbabilityOfFinal2.Is(0.0, 0.000001).ShouldBe(true);
			maxProbabilityOfFinal0.Is(1.0, 0.000001).ShouldBe(true);
			var maxProbabilityOf1And2Calculated = 1.0 - Math.Pow(0.6, 4);
			maxProbabilityOfFinal1.Is(maxProbabilityOf1And2Calculated, 0.000001).ShouldBe(true);
			maxProbabilityOfFinal2.Is(maxProbabilityOf1And2Calculated, 0.000001).ShouldBe(true);
		}
	}
}