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

namespace SafetySharp.CaseStudies.SmallModels.DeadReckoning
{
	using System;
	using System.Collections.Generic;
	using Analysis;
	using Bayesian;
	using ISSE.SafetyChecking;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ISSE.SafetyChecking.Modeling;
	using ModelChecking;
	using NUnit.Framework;
	using System.Linq;

	public class EvaluationTests
	{
        [Test]
        public void ManuallyCalculateProbabilities()
        {
            var model = new DeadReckoningModel();
            Func<bool> hazard = () => model.Component.Hazard;

            var config = BayesianLearningConfiguration.Default;
			var tolerance = 0.000000001;
	        var stepBounds = 10; //230 for railroad crossing

			var allVars = new List<RandomVariable>();
	        var randomVariableFactory = new RandomVariableFactory(model);
	        var rvFaultF = randomVariableFactory.FromFault(model.Component.FF);
			var rvFaultC = randomVariableFactory.FromFault(model.Component.FC);
			var rvFaultS = randomVariableFactory.FromFault(model.Component.FS);
			var mcs = new MinimalCriticalSet(new HashSet<Fault>() { model.Component.FC, model.Component.FS });
	        var rvMcs = new McsRandomVariable(mcs, new[] { rvFaultC, rvFaultS }, "mcs_FC_FS");
	        var rvHazard = randomVariableFactory.FromState(hazard, "H");
			allVars.AddRange(new RandomVariable[] { rvFaultF, rvFaultC, rvFaultS, rvMcs, rvHazard});

			var probCalculator = new OnDemandProbabilityDistributionCalculator(model, allVars, stepBounds, tolerance, config);


	        var result = probCalculator.CalculateProbability(new RandomVariable[] { rvHazard }, new RandomVariable[] { });
			Console.Out.WriteLine($"Probability of {rvHazard}+: {result}");
			Console.Out.WriteLine();
			GC.Collect();

			result = probCalculator.CalculateProbability(new RandomVariable[] { rvHazard }, new RandomVariable[] { rvFaultF });
			Console.Out.WriteLine($"Probability of {rvHazard}+,{rvFaultF}- : {result}");
			Console.Out.WriteLine();
			GC.Collect();

			probCalculator.WriteProbsToConsole();
		}

        [Test]
        public void CalculateHazardProbability()
        {
            var tc = SafetySharpModelChecker.TraversalConfiguration;
            tc.WriteGraphvizModels = true;
            tc.MomentOfIndependentFaultActivation = MomentOfIndependentFaultActivation.OnFirstMethodWithoutUndo;
            SafetySharpModelChecker.TraversalConfiguration = tc;

            var model = new DeadReckoningModel();
            var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Component.Hazard, 20);

            Console.WriteLine($"Probability of hazard in model: {result}");
        }

        [Test]
        public void CalculateDcca()
        {
            var model = new DeadReckoningModel();

            var analysis = new SafetySharpSafetyAnalysis
            {
                Backend = SafetyAnalysisBackend.FaultOptimizedOnTheFly,
                Heuristics = { new MaximalSafeSetHeuristic(model.Faults) }
            };
            var result = analysis.ComputeMinimalCriticalSets(model, model.Component.Hazard);

            var orderResult = SafetySharpOrderAnalysis.ComputeOrderRelationships(result);
            Console.WriteLine(orderResult);
        }
    }
}