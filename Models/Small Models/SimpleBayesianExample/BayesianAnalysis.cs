namespace SafetySharp.CaseStudies.SmallModels.SimpleBayesianExample
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            var bayesianCreator = new BayesianNetworkCreator(model, 10);

            var config = BayesianNetworkCreator.Config;
            config.UseDccaResultsForLearning = true;
            config.UseRealProbabilitiesForSimulation = true;
            BayesianNetworkCreator.Config = config;

            var result = bayesianCreator.LearnScoreBasedBayesianNetwork(100000, hazard, states);
        }

        [Test]
        public void TestConstraintBasedLearning()
        {
            var model = new SimpleBayesianExampleModel();
            Func<bool> hazard = () => model.Component.Hazard;
            Func<bool> state = () => model.Component.NoDataAvailable || model.Component.SubsystemError;
            var states = new Dictionary<string, Func<bool>> { /*["State"] = state*/ };
            var bayesianNetworkCreator = new BayesianNetworkCreator(model, 10);

            var config = BayesianNetworkCreator.Config;
            config.UseDccaResultsForLearning = true;
            BayesianNetworkCreator.Config = config;

            var result = bayesianNetworkCreator.LearnConstraintBasedBayesianNetwork(hazard, states, new[] { model.Component.FL, model.Component.FS, model.Component.FV });
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