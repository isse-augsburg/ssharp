namespace SafetySharp.CaseStudies.SmallModels.SimpleBayesianExample
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
            var model = new SimpleBayesianExampleModel();
            Func<bool> hazard = () => model.Component.Hazard;
            Func<bool> state = () => model.Component.NoDataAvailable || model.Component.SubsystemError;
            var states = new Dictionary<string, Func<bool>> { /*["State"] = state*/ };

            var config = BayesianLearningConfiguration.Default;
            var bayesianCreator = new BayesianNetworkCreator(model, 10, config);
            var result = bayesianCreator.LearnScoreBasedBayesianNetwork(@"C:\SafetySharpSimulation\", 100000, hazard, states);
        }

        [Test]
        public void TestConstraintBasedLearning()
        {
            var model = new SimpleBayesianExampleModel();
            Func<bool> hazard = () => model.Component.Hazard;
            Func<bool> state = () => true;
            var states = new Dictionary<string, Func<bool>> { /*["State"] = state*/ };
            

            var config = BayesianLearningConfiguration.Default;
            var bayesianNetworkCreator = new BayesianNetworkCreator(model, 10, config);
            var result = bayesianNetworkCreator.LearnConstraintBasedBayesianNetwork(hazard, states, new[] { model.Component.FL, model.Component.FS, model.Component.FV });
        }

        [Test]
        public void SerializeAndDeserializeBayesianNetwork()
        {
            const string filePath = "network.json";
            var model = new SimpleBayesianExampleModel();
            Func<bool> hazard = () => model.Component.Hazard;

            var config = BayesianLearningConfiguration.Default;
            config.BayesianNetworkSerializationPath = filePath;
            var bayesianNetworkCreator = new BayesianNetworkCreator(model, 10, config);
            var result = bayesianNetworkCreator.LearnConstraintBasedBayesianNetwork(hazard, null, new[] { model.Component.FL, model.Component.FS, model.Component.FV });

            bayesianNetworkCreator = new BayesianNetworkCreator(model, 10);
            var network = bayesianNetworkCreator.FromJson(filePath, hazard);
        }

        [Test]
        public void CalculateBayesianNetworkProbabilities()
        { 
            const string filePath = "network.json";
            var model = new SimpleBayesianExampleModel();
            Func<bool> hazard = () => model.Component.Hazard;
            var bayesianNetworkCreator = new BayesianNetworkCreator(model, 10);
            var network = bayesianNetworkCreator.FromJson(filePath, hazard);

            var calculator = new BayesianNetworkProbabilityDistributionCalculator(network, 0.0000000001);
            var result = calculator.CalculateConditionalProbabilityDistribution(new[] { "FS" }, new[] { "FV", "FL" });
            Console.Out.WriteLine(string.Join("\n", result));
        }

        [Test]
        public void CalculateHazardProbability()
        {
            var tc = SafetySharpModelChecker.TraversalConfiguration;
            tc.WriteGraphvizModels = true;
            tc.MomentOfIndependentFaultActivation = MomentOfIndependentFaultActivation.OnFirstMethodWithoutUndo;
            SafetySharpModelChecker.TraversalConfiguration = tc;

            var model = new SimpleBayesianExampleModel();
            var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Component.Hazard, 20);

            Console.WriteLine($"Probability of hazard in model: {result}");
        }

        [Test]
        public void CalculateFaultProbabilities()
        {
            var tc = SafetySharpModelChecker.TraversalConfiguration;
            tc.WriteGraphvizModels = true;
            tc.MomentOfIndependentFaultActivation = MomentOfIndependentFaultActivation.OnFirstMethodWithoutUndo;
            SafetySharpModelChecker.TraversalConfiguration = tc;

            var model = new SimpleBayesianExampleModel();

            var isFlActivated = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Component.FL.IsActivated, 20);
            var isFvActivated = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Component.FV.IsActivated, 20);
            var isFsActivated = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Component.FS.IsActivated, 20);


            Console.WriteLine($"Probability that Fault1 is activated: {isFlActivated}");
            Console.WriteLine($"Probability that Fault2 is activated: {isFvActivated}");
            Console.WriteLine($"Probability that Fault2 is activated: {isFsActivated}");
        }

        [Test]
        public void CalculateDcca()
        {
            var model = new SimpleBayesianExampleModel();

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