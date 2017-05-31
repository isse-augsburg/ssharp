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

namespace SafetySharp.CaseStudies.SmallModels.Model2
{
	using System;
	using Analysis;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ISSE.SafetyChecking.Modeling;
	using ModelChecking;
	using Modeling;
	using NUnit.Framework;

	public class ExampleComponent : Component
	{
		public ExampleComponent()
		{
			F1 = new TransientFault();
			F1.ProbabilityOfOccurrence = new Probability(0.003);


			F2 = new TransientFault();
			F2.ProbabilityOfOccurrence = new Probability(0.0001);
		}

		public Fault F1;
		public Fault F2;
		
		public int Step = 0;

		public int Value = 0;

		public bool LoopRequestBug = false;

		public override void Update()
		{
			if (Step > 10)
				return;
			Step++;

			if (Step==1)
				Request();
			if (Step== 5 || LoopRequestBug)
				SetValueTo2();
		}

		public virtual void Request()
		{
		}

		public virtual void SetValueTo2()
		{
			Value = 2;
		}


		[FaultEffect(Fault = nameof(F1)), Priority(0)]
		public class F1Effect : ExampleComponent
		{
			public override void Request()
			{
				LoopRequestBug = true;
			}
		}

		[FaultEffect(Fault = nameof(F2)), Priority(0)]
		public class F2Effect : ExampleComponent
		{
			public override void SetValueTo2()
			{
				Value = 3;
			}
		}
	}


	public sealed class ExampleModelBase : ModelBase
	{
		[Root(RootKind.Controller)]
		public ExampleComponent ModelComponent { get; } = new ExampleComponent();
	}


	public class ExampleAnalysis
	{
		[Test]
		public void CalculateProbability()
		{
			var model = new ExampleModelBase();
			
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.ModelComponent.Value==3, 50);
			Console.Write($"Probability of hazard: {result}");
		}


		[Test]
		public void CalculateDcca()
		{
			var model = new ExampleModelBase();
			
			var analysis = new SafetySharpSafetyAnalysis { Backend = SafetyAnalysisBackend.FaultOptimizedOnTheFly, Heuristics = { new MaximalSafeSetHeuristic(model.Faults) } };
			var result = analysis.ComputeMinimalCriticalSets(model, model.ModelComponent.Value == 3);
			//result.SaveCounterExamples("counter examples/height control/dcca/collision/original");

			var orderResult = SafetySharpOrderAnalysis.ComputeOrderRelationships(result);
			Console.WriteLine(orderResult);
		}
	}
}
