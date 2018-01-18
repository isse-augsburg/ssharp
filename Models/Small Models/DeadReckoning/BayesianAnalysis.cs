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
    using ModelChecking;
    using NUnit.Framework;

    public class BayesianAnalysis
    {

        [Test]
        public void TestScoreBasedLearning()
        {
            var model = new DeadReckoningModel();
            Func<bool> hazard = () => model.Component.Hazard;
            Func<bool> state = () => model.Component.NoDataAvailable || model.Component.CalculationError;
            var states = new Dictionary<string, Func<bool>> { /*["State"] = state*/ };

            var config = BayesianLearningConfiguration.Default;
            var bayesianCreator = new BayesianNetworkCreator(model, 10, config);
            var result = bayesianCreator.LearnScoreBasedBayesianNetwork(@"C:\SafetySharpSimulation\", 100000, hazard, states);
        }

        [Test]
        public void TestConstraintBasedLearning()
        {
            var model = new DeadReckoningModel();
            Func<bool> hazard = () => model.Component.Hazard;
            Func<bool> state = () => true;
            var states = new Dictionary<string, Func<bool>> { /*["State"] = state*/ };
            

            var config = BayesianLearningConfiguration.Default;
            var bayesianNetworkCreator = new BayesianNetworkCreator(model, 10, config);
            var result = bayesianNetworkCreator.LearnConstraintBasedBayesianNetwork(hazard, states, new[] { model.Component.FF, model.Component.FS, model.Component.FC });
        }

        [Test]
        public void SerializeAndDeserializeBayesianNetwork()
        {
            const string filePath = "network.json";
            var model = new DeadReckoningModel();
            Func<bool> hazard = () => model.Component.Hazard;

            var config = BayesianLearningConfiguration.Default;
            config.BayesianNetworkSerializationPath = filePath;
            var bayesianNetworkCreator = new BayesianNetworkCreator(model, 10, config);
            var result = bayesianNetworkCreator.LearnConstraintBasedBayesianNetwork(hazard, null, new[] { model.Component.FF, model.Component.FS, model.Component.FC });

            bayesianNetworkCreator = new BayesianNetworkCreator(model, 10);
            var network = bayesianNetworkCreator.FromJson(filePath, hazard);
        }

        [Test]
        public void CalculateBayesianNetworkProbabilities()
        { 
            const string filePath = "network.json";
            var model = new DeadReckoningModel();
            Func<bool> hazard = () => model.Component.Hazard;
            var bayesianNetworkCreator = new BayesianNetworkCreator(model, 10);
            var network = bayesianNetworkCreator.FromJson(filePath, hazard);

            var calculator = new BayesianNetworkProbabilityDistributionCalculator(network, 0.0000000001);
            var result = calculator.CalculateConditionalProbabilityDistribution(new[] { "FS" }, new[] { "FC", "FF" });
            Console.Out.WriteLine(string.Join("\n", result));
        }

        [Test]
        public void CalculateHazardProbability()
        {
            var tc = SafetySharpModelChecker.TraversalConfiguration;
            tc.WriteGraphvizModels = true;
            tc.EnableStaticPruningOptimization = false;
            SafetySharpModelChecker.TraversalConfiguration = tc;

            var model = new DeadReckoningModel();
            var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Component.Hazard, 10);

            Console.WriteLine($"Probability of hazard in model: {result}");
        }

        [Test]
        public void CalculateFaultProbabilities()
        {
            var tc = SafetySharpModelChecker.TraversalConfiguration;
            tc.WriteGraphvizModels = true;
            tc.EnableStaticPruningOptimization = false;
            SafetySharpModelChecker.TraversalConfiguration = tc;

            var model = new DeadReckoningModel();

            var isFlActivated = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Component.FF.IsActivated, 20);
            var isFvActivated = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Component.FC.IsActivated, 20);
            var isFsActivated = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Component.FS.IsActivated, 20);


            Console.WriteLine($"Probability that Fault1 is activated: {isFlActivated}");
            Console.WriteLine($"Probability that Fault2 is activated: {isFvActivated}");
            Console.WriteLine($"Probability that Fault2 is activated: {isFsActivated}");
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

		[Test]
		public void CalculateRangeHazardLtmdp()
		{
			var model = new DeadReckoningModel();
			model.Component.FF.ProbabilityOfOccurrence = null;

			SafetySharpModelChecker.TraversalConfiguration.EnableStaticPruningOptimization = true;
			SafetySharpModelChecker.TraversalConfiguration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			var result = SafetySharpModelChecker.CalculateProbabilityRangeToReachStateBounded(model, model.Component.Hazard, 10);
			Console.Write($"Probability of hazard: {result}");
		}

		[Test]
		public void CalculateRangeHazardLtmdpWithoutStaticPruning()
		{
			var model = new DeadReckoningModel();
			model.Component.FF.ProbabilityOfOccurrence = null;

			SafetySharpModelChecker.TraversalConfiguration.EnableStaticPruningOptimization = false;
			SafetySharpModelChecker.TraversalConfiguration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			var result = SafetySharpModelChecker.CalculateProbabilityRangeToReachStateBounded(model, model.Component.Hazard, 10);
			SafetySharpModelChecker.TraversalConfiguration.EnableStaticPruningOptimization = true;
			Console.Write($"Probability of hazard: {result}");
		}
	}
}