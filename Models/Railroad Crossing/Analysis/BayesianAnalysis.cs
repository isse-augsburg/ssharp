namespace SafetySharp.CaseStudies.RailroadCrossing.Analysis
{
    using System;
    using System.Collections.Generic;
    using Bayesian;
    using ISSE.SafetyChecking;
    using ISSE.SafetyChecking.Modeling;
    using Modeling;
    using NUnit.Framework;
    using SafetySharp.Analysis;

    class BayesianAnalysis
    {

        [Test]
        public void ConstraintBased()
        {
            var model = new Model();
            model.Channel.MessageDropped.ProbabilityOfOccurrence = new Probability(0);
            model.CrossingController.Motor.BarrierMotorStuck.ProbabilityOfOccurrence = new Probability(0.001);
            model.CrossingController.Sensor.BarrierSensorFailure.ProbabilityOfOccurrence = new Probability(0.00003);
            model.CrossingController.TrainSensor.ErroneousTrainDetection.ProbabilityOfOccurrence = new Probability(0);
            model.TrainController.Brakes.BrakesFailure.ProbabilityOfOccurrence = new Probability(0.00002);
            model.TrainController.Odometer.OdometerPositionOffset.ProbabilityOfOccurrence = new Probability(0.02);
            model.TrainController.Odometer.OdometerSpeedOffset.ProbabilityOfOccurrence = new Probability(0.02);


            Func<bool> hazard = () => (model.Barrier.Angle != 0)
                               &&
                               (model.Train.Position <= Model.CrossingPosition &&
                                model.Train.Position + model.Train.Speed > Model.CrossingPosition);
            Func<bool> timerHasElapsed = () => model.CrossingController.Timer.HasElapsed;
            Func<bool> brakesActive = () => model.TrainController.Brakes.Acceleration == -1;
            var states = new Dictionary<string, Func<bool>> { /*["TimerHasElapsed"] = timerHasElapsed, ["BrakesActive"] = brakesActive */ };
            var faults = new[]
            {
                model.CrossingController.Motor.BarrierMotorStuck,
                model.CrossingController.Sensor.BarrierSensorFailure,
                model.TrainController.Brakes.BrakesFailure,
                model.TrainController.Odometer.OdometerPositionOffset,
                model.TrainController.Odometer.OdometerSpeedOffset
            };

            var config = BayesianLearningConfiguration.Default;
            config.MaxConditionSize = 0;
            var bayesianCreator = new BayesianNetworkCreator(model, 230, config);
            bayesianCreator.LearnConstraintBasedBayesianNetwork(hazard, null, faults);
        }

        [Test]
        public void ScoreBased()
        {
            var model = new Model();
            model.Channel.MessageDropped.ProbabilityOfOccurrence = new Probability(0);
            model.CrossingController.Motor.BarrierMotorStuck.ProbabilityOfOccurrence = new Probability(0.001);
            model.CrossingController.Sensor.BarrierSensorFailure.ProbabilityOfOccurrence = new Probability(0.00003);
            model.CrossingController.TrainSensor.ErroneousTrainDetection.ProbabilityOfOccurrence = new Probability(0);
            model.TrainController.Brakes.BrakesFailure.ProbabilityOfOccurrence = new Probability(0.00002);
            model.TrainController.Odometer.OdometerPositionOffset.ProbabilityOfOccurrence = new Probability(0.02);
            model.TrainController.Odometer.OdometerSpeedOffset.ProbabilityOfOccurrence = new Probability(0.02);


            Func<bool> hazard = () => (model.Barrier.Angle != 0)
                               &&
                               (model.Train.Position <= Model.CrossingPosition &&
                                model.Train.Position + model.Train.Speed > Model.CrossingPosition);
            Func<bool> timerHasElapsed = () => model.CrossingController.Timer.HasElapsed;
            Func<bool> brakesActive = () => model.TrainController.Brakes.Acceleration == -1;
            var states = new Dictionary<string, Func<bool>> { /*["TimerHasElapsed"] = timerHasElapsed, ["BrakesActive"] = brakesActive */ };
            var faults = new[]
            {
                model.CrossingController.Motor.BarrierMotorStuck,
                model.CrossingController.Sensor.BarrierSensorFailure,
                model.TrainController.Brakes.BrakesFailure,
                model.TrainController.Odometer.OdometerPositionOffset,
                model.TrainController.Odometer.OdometerSpeedOffset
            };

            var config = BayesianLearningConfiguration.Default;
            config.UseRealProbabilitiesForSimulation = true;
            var bayesianCreator = new BayesianNetworkCreator(model, 230, config);
            bayesianCreator.LearnScoreBasedBayesianNetwork(@"C:\SafetySharpSimulation\", 50000000, hazard, states, faults);
        }

        [Test]
        public void CalculateProbabilities()
        {
            const string networkPath = @"Analysis/network.json";
            var model = new Model();
            Func<bool> hazard = () => (model.Barrier.Angle != 0)
                               &&
                               (model.Train.Position <= Model.CrossingPosition &&
                                model.Train.Position + model.Train.Speed > Model.CrossingPosition);
            var faults = new[]
            {
                model.CrossingController.Motor.BarrierMotorStuck,
                model.CrossingController.Sensor.BarrierSensorFailure,
                model.TrainController.Brakes.BrakesFailure,
                model.TrainController.Odometer.OdometerPositionOffset,
                model.TrainController.Odometer.OdometerSpeedOffset
            };

            var bayesianCreator = new BayesianNetworkCreator(model, 230);
            var network = bayesianCreator.FromJson(networkPath, hazard, null, faults);
            var calculator = new BayesianNetworkProbabilityDistributionCalculator(network, 0.000000000001);

            var result = calculator.CalculateConditionalProbabilityDistribution(new[] { "H" }, new[] { "OdometerSpeedOffset" });
            Console.Out.WriteLine(string.Join("\n", result));
        }
    }
}