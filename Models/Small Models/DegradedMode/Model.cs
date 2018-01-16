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

namespace SafetySharp.CaseStudies.SmallModels.DegradedMode
{
	using global::System;
	using Analysis;
	using global::System.IO;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ISSE.SafetyChecking.Modeling;
	using ISSE.SafetyChecking.Utilities;
	using ModelChecking;
	using Modeling;
	using NUnit.Framework;

	public abstract class SignalDetector : Component
	{
		public abstract bool PerformSelfCheck(); //return true on success

		public abstract int MeasureSignal();
	}

	public class UnreliablePreciseSignalDetectorWithSelfCheck : SignalDetector
	{
		public override bool PerformSelfCheck()
		{
			if (MeasureSignal() > 3)
			{
				return false;
			}
			return true;
		}

		public override int MeasureSignal()
		{
			return ChooseWithUniformDistribution(1, 2, 3);
		}

		public Fault F1 = new TransientFault() {ProbabilityOfOccurrence = new Probability(0.003)};

		[FaultEffect(Fault = nameof(F1)), Priority(0)]
		public class F1Effect : UnreliablePreciseSignalDetectorWithSelfCheck
		{
			public override int MeasureSignal()
			{
				return 4;
			}
		}
	}

	public class ReliableUnpreciseSignalDetector : SignalDetector
	{
		public override bool PerformSelfCheck()
		{
			return true;
		}

		public override int MeasureSignal()
		{
			return ChooseWithUniformDistribution(1, 2);
		}
	}

	public class System : Component
	{
		public UnreliablePreciseSignalDetectorWithSelfCheck SignalDetector1 { get; } = new UnreliablePreciseSignalDetectorWithSelfCheck();
		
		public ReliableUnpreciseSignalDetector SignalDetector2 { get; } = new ReliableUnpreciseSignalDetector();

		[Modeling.Range(0, 10, OverflowBehavior.Clamp)]
		public int TimeStep = 0;

		public SignalDetector ActiveSignalDetector;

		//[Hidden]
		public bool HazardActive;

		public override void Update()
		{
			HazardActive = false;
			if (TimeStep == 0)
			{
				if (SignalDetector1.PerformSelfCheck())
					ActiveSignalDetector = SignalDetector1;
				else
					ActiveSignalDetector = SignalDetector2;
			}

			if (ActiveSignalDetector.MeasureSignal() == 4)
				HazardActive = true;
			TimeStep++;
		}
	}


	public sealed class DegradedModeModel : ModelBase
	{
		[Root(RootKind.Controller)]
		public System System { get; } = new System();
	}


	public class ExampleAnalysis
	{
		[Test]
		public void CalculateHazardProbabilityWhenF1AlwaysOccurs()
		{
			var model = new DegradedModeModel();
			model.System.SignalDetector1.F1.ProbabilityOfOccurrence = new Probability(1.0);
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.System.HazardActive, 50);
			Console.Write($"Probability of hazard: {result}");
		}

		[Test]
		public void CalculateHazardProbabilityWhenF1NeverOccurs()
		{
			var model = new DegradedModeModel();
			model.System.SignalDetector1.F1.ProbabilityOfOccurrence = new Probability(0.0);
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.System.HazardActive, 50);
			Console.Write($"Probability of hazard: {result}");
		}
		
		[Test]
		public void CalculateHazardProbabilityGraph()
		{
			var model = new DegradedModeModel();

			var minValue = 0.0;
			var maxValue = 1.0;
			var steps = 250;
			var stepSize = (maxValue - minValue) / (steps - 1);

			var sourceValues = new double[steps];
			var resultValues = new double[steps];

			for (var i = 0; i < steps; i++)
			{
				var currentValue = i * stepSize;
				sourceValues[i] = currentValue;
				model.System.SignalDetector1.F1.ProbabilityOfOccurrence = new Probability(currentValue);
				var currentResult = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.System.HazardActive, 50);
				GC.Collect();
				resultValues[i] = currentResult.Value;
			}

			var fileWriter = new StreamWriter("PrecisenessTradeoff/graph.csv",append:false);
			var csvWriter = new CsvWriter(fileWriter);
			csvWriter.AddEntry("Step");
			for (var i = 0; i < steps; i++)
			{
				csvWriter.AddEntry(i);
			}
			csvWriter.NewLine();

			csvWriter.AddEntry("Pr(F1)");
			for (var i = 0; i < steps; i++)
			{
				csvWriter.AddEntry(sourceValues[i]);
			}
			csvWriter.NewLine();

			csvWriter.AddEntry("Pr(Hazard)");
			for (var i = 0; i < steps; i++)
			{
				csvWriter.AddEntry(resultValues[i]);
			}
			csvWriter.NewLine();
			fileWriter.Close();
		}


		[Test]
		public void CalculateDcca()
		{
			var model = new DegradedModeModel();
			
			var analysis = new SafetySharpSafetyAnalysis { Backend = SafetyAnalysisBackend.FaultOptimizedOnTheFly, Heuristics = { new MaximalSafeSetHeuristic(model.Faults) } };
			var result = analysis.ComputeMinimalCriticalSets(model, model.System.HazardActive);
			//result.SaveCounterExamples("counter examples/height control/dcca/collision/original");

			var orderResult = SafetySharpOrderAnalysis.ComputeOrderRelationships(result);
			Console.WriteLine(orderResult);
		}
	}
}
