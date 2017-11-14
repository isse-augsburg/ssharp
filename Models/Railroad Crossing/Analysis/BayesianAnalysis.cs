namespace SafetySharp.CaseStudies.RailroadCrossing.Analysis
{
    using System;
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
            model.Channel.MessageDropped.ProbabilityOfOccurrence = new Probability(0.0001);
            model.CrossingController.Motor.BarrierMotorStuck.ProbabilityOfOccurrence = new Probability(0.001);
            model.CrossingController.Sensor.BarrierSensorFailure.ProbabilityOfOccurrence = new Probability(0.00003);
            model.CrossingController.TrainSensor.ErroneousTrainDetection.ProbabilityOfOccurrence = new Probability(0.0002);
            model.TrainController.Brakes.BrakesFailure.ProbabilityOfOccurrence = new Probability(0.00002);
            model.TrainController.Odometer.OdometerPositionOffset.ProbabilityOfOccurrence = new Probability(0.02);
            model.TrainController.Odometer.OdometerSpeedOffset.ProbabilityOfOccurrence = new Probability(0.02);


            Func<bool> hazard = () => (model.Barrier.Angle != 0)
                               &&
                               (model.Train.Position <= Model.CrossingPosition &&
                                model.Train.Position + model.Train.Speed > Model.CrossingPosition);

            var bayesianCreator = new BayesianNetworkCreator(model, 50);
            var config = BayesianNetworkCreator.Config;
            config.MaxConditionSize = 1;
            BayesianNetworkCreator.Config = config;
            bayesianCreator.LearnConstraintBasedBayesianNetwork(hazard);
        }

        [Test]
        public void ScoreBased()
        {
            var model = new Model();
            model.Channel.MessageDropped.ProbabilityOfOccurrence = new Probability(0.0001);
            model.CrossingController.Motor.BarrierMotorStuck.ProbabilityOfOccurrence = new Probability(0.001);
            model.CrossingController.Sensor.BarrierSensorFailure.ProbabilityOfOccurrence = new Probability(0.00003);
            model.CrossingController.TrainSensor.ErroneousTrainDetection.ProbabilityOfOccurrence = new Probability(0.0002);
            model.TrainController.Brakes.BrakesFailure.ProbabilityOfOccurrence = new Probability(0.00002);
            model.TrainController.Odometer.OdometerPositionOffset.ProbabilityOfOccurrence = new Probability(0.02);
            model.TrainController.Odometer.OdometerSpeedOffset.ProbabilityOfOccurrence = new Probability(0.02);


            Func<bool> hazard = () => (model.Barrier.Angle != 0)
                               &&
                               (model.Train.Position <= Model.CrossingPosition &&
                                model.Train.Position + model.Train.Speed > Model.CrossingPosition);

            var bayesianCreator = new BayesianNetworkCreator(model, 50);
            var config = BayesianNetworkCreator.Config;
            config.UseRealProbabilitiesForSimulation = false;
            BayesianNetworkCreator.Config = config;
            bayesianCreator.LearnScoreBasedBayesianNetwork(20000000, hazard);
        }
    }
}