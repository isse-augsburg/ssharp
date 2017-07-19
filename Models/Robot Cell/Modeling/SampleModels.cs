// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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

namespace SafetySharp.CaseStudies.RobotCell.Modeling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Analysis;
    using Odp.Reconfiguration;
    using SafetySharp.Modeling;
    using static ModelBuilderHelper;

    internal static class SampleModels
    {
        private static readonly Func<ModelBuilder, ModelBuilder>[] _defaultConfigurations =
            { Ictss1, Ictss2, Ictss3, Ictss4, Ictss5, Ictss6, Ictss7 };

        private static readonly Func<ModelBuilder, ModelBuilder>[] _performanceEvaluationConfigurations =
            { FewAgentsHighRedundancy, ManyAgentsLowRedundancy, ManyAgentsLowRedundancy };

        public static Model DefaultInstance<T>(AnalysisMode mode = AnalysisMode.AllFaults)
            where T : IController
        {
            return DefaultSetup<T>(mode, false).Invoke(new ModelBuilder(nameof(Ictss1)).Ictss1()).Build();
        }

        public static IEnumerable<Model> CreateDefaultConfigurations<T>(AnalysisMode mode, bool verify = false)
            where T : IController
        {
            return CreateConfigurations(b => b, DefaultSetup<T>(mode, verify));
        }

        public static IEnumerable<Model> CreateDefaultConfigurationsWithoutPlant<T>(AnalysisMode mode, bool verify = false)
            where T : IController
        {
            return CreateConfigurations(b => b.DisablePlants(), DefaultSetup<T>(mode, verify));
        }

        public static IEnumerable<Model> CreateCoalitionConfigurations(bool verify = false)
        {
            return CreateConfigurations(
                b => b.DisablePlants(),
                b => b.UseCoalitionFormation().EnableControllerVerification(verify).DisableIntolerableFaults()
            );
        }

        public static IEnumerable<Model> CreatePerformanceEvaluationConfigurationsCentralized()
        {
            return CreateConfigurations(
                _performanceEvaluationConfigurations,
                b => b.DisablePlants(),
                b => b.ChooseController<FastController>().UseControllerReconfigurationAgents().EnablePerformanceMeasurement().DisableIntolerableFaults()
            );
        }

        public static IEnumerable<Model> CreatePerformanceEvaluationConfigurationsCoalition()
        {
            return CreateConfigurations(
                _performanceEvaluationConfigurations,
                b => b.DisablePlants(),
                b => b.UseCoalitionFormation().EnablePerformanceMeasurement().DisableIntolerableFaults()
            );
        }

        private static IEnumerable<Model> CreateConfigurations(Func<ModelBuilder, ModelBuilder> preSetup,
                                                              Func<ModelBuilder, ModelBuilder> postSetup)
        {
            return CreateConfigurations(_defaultConfigurations, preSetup, postSetup);
        }

        private static IEnumerable<Model> CreateConfigurations(IEnumerable<Func<ModelBuilder, ModelBuilder>> configurations,
                                                               Func<ModelBuilder, ModelBuilder> preSetup,
                                                               Func<ModelBuilder, ModelBuilder> postSetup)
        {
            return configurations.Select(setup => postSetup(setup(preSetup(new ModelBuilder(setup.Method.Name)))).Build());
        }

        private static Func<ModelBuilder, ModelBuilder> DefaultSetup<T>(AnalysisMode mode, bool verify) where T : IController
        {
            return builder => ChooseAnalysisMode(builder.ChooseController<T>()
                                                        .EnableControllerVerification(verify)
                                                        .CentralReconfiguration(), mode);
        }

        public static Model PerformanceMeasurement1<T>(AnalysisMode mode = AnalysisMode.AllFaults, bool verify = false)
            where T : IController
        {
            return ChooseAnalysisMode(
                new ModelBuilder(nameof(PerformanceMeasurement1))
                    .DefineTask(5, Produce, Drill, Insert, Tighten, Polish, Consume)

                    .AddRobot(Produce, Drill, Insert)
                    .AddRobot(Insert, Drill, Tighten)
                    .AddRobot(Polish, Consume)
                    .AddRobot(Produce, Drill, Insert)
                    .AddRobot(Insert, Drill)
                    .AddRobot(Tighten, Polish, Tighten, Drill)
                    .AddRobot(Polish, Consume)

                    .AddCart(Route(0, 1), Route(0, 2))
                    .AddCart(Route(1, 2), Route(0, 1))
                    .AddCart(Route(2, 3))
                    .AddCart(Route(3, 4), Route(3, 5), Route(3, 6))
                    .AddCart(Route(4, 5), Route(3, 4))
                    .AddCart(Route(5, 6))

                    .ChooseController<T>()
                    .EnablePerformanceMeasurement()
                    .EnableControllerVerification(verify)
                    .CentralReconfiguration()
                , mode).Build();
        }

        public enum FaultMode { ToolFaults, CompleteFailures, IoFaults, AllFaults }

        public static Model PerformanceMeasurementModel(FaultMode mode)
        {
            var model = new ModelBuilder(nameof(PerformanceMeasurementModel) + ": " + mode).MediumSizePerformanceMeasurementModel().Build();
            switch (mode)
            {
                case FaultMode.ToolFaults:
                    model.Faults.Except(model.RobotAgents.SelectMany(r => new [] { r.DrillBroken, r.InsertBroken, r.TightenBroken, r.PolishBroken })).SuppressActivations();
                    break;
                case FaultMode.IoFaults:
                    model.Faults.Except(model.RobotAgents.Select(r => r.ResourceTransportFault)).SuppressActivations();
                    break;
                case FaultMode.CompleteFailures:
                    model.Faults.SuppressActivations(); // TODO: except complete failures, once modeled
                    break;
            }
            return model;
        }

        public static ModelBuilder FewAgentsHighRedundancy(this ModelBuilder builder)
        {
            return builder.DefineTask(100000, Produce, Drill, Insert, Tighten, Consume) // 5 capabilities in task

                // 6 robots, each capability ~ 5 times
                .AddRobot(Produce, Drill, Insert, Tighten)
                .AddRobot(Drill, Insert, Tighten, Consume)
                .AddRobot(Insert, Tighten, Consume, Produce)
                .AddRobot(Tighten, Consume, Produce, Drill)
                .AddRobot(Consume, Produce, Drill, Insert)
                .AddRobot(Produce, Drill, Insert, Consume)

                // 4 carts, many connections
                .AddCart(Route(0, 1), Route(0, 2), Route(0, 3), Route(1, 2), Route(1, 3), Route(2, 3))
                .AddCart(Route(2, 3), Route(2, 4), Route(2, 5), Route(3, 4), Route(3, 5), Route(4, 5))
                .AddCart(Route(0, 2), Route(0, 4), Route(2, 4), Route(1, 3), Route(1, 5), Route(3, 5))
                .AddCart(Route(3, 4), Route(4, 5), Route(1, 5), Route(5, 6));
        }

        public static ModelBuilder ManyAgentsLowRedundancy(this ModelBuilder builder)
        {
            // 35 robots 15 carts 15 capabilities/task
            return builder;
        }

        public static ModelBuilder ManyAgentsHighRedundancy(this ModelBuilder builder)
        {
            // 35 robots 15 carts 15 capabilities/task
            return builder;
        }

        public static ModelBuilder MediumSizePerformanceMeasurementModel(this ModelBuilder builder)
        {
            // 15 agents 6 capabilities/task
            return builder;
        }

        public static ModelBuilder Ictss1(this ModelBuilder builder)
        {
            return builder.DefineTask(5, Produce, Drill, Insert, Tighten, Polish, Consume)

                          .AddRobot(Produce, Drill, Insert)
                          .AddRobot(Insert, Drill)
                          .AddRobot(Tighten, Polish, Tighten, Drill)
                          .AddRobot(Polish, Consume)

                          .AddCart(Route(0, 1), Route(0, 2), Route(0, 3))
                          .AddCart(Route(1, 2), Route(0, 1))
                          .AddCart(Route(2, 3));
        }

        public static ModelBuilder Ictss2(this ModelBuilder builder)
        {
            return builder.DefineTask(5, Produce, Drill, Insert, Tighten, Consume)

                          .AddRobot(Produce, Insert)
                          .AddRobot(Tighten)
                          .AddRobot(Drill, Consume)

                          .AddCart(Route(0, 1), Route(0, 2))
                          .AddCart(Route(1, 2), Route(0, 1));
        }

        public static ModelBuilder Ictss3(this ModelBuilder builder)
        {
            return builder.DefineTask(5, Produce, Drill, Insert, Tighten, Consume)

                          .AddRobot(Produce, Insert, Drill, Insert)
                          .AddRobot(Insert, Tighten, Drill)
                          .AddRobot(Tighten, Insert, Consume, Drill)

                          .AddCart(Route(0, 1), Route(0, 2))
                          .AddCart(Route(1, 2), Route(0, 1));
        }

        public static ModelBuilder Ictss4(this ModelBuilder builder)
        {
            return builder.DefineTask(5, Produce, Drill, Insert, Tighten, Consume)

                          .AddRobot(Produce, Insert)
                          .AddRobot(Tighten)
                          .AddRobot(Drill, Consume)

                          .AddCart(Route(0, 1), Route(0, 2), Route(1, 2))
                          .AddCart(Route(1, 2), Route(0, 1), Route(0, 2));
        }

        public static ModelBuilder Ictss5(this ModelBuilder builder)
        {
            return builder.DefineTask(5, Produce, Drill, Insert, Tighten, Drill, Insert, Tighten, Consume)

                          .AddRobot(Produce, Insert)
                          .AddRobot(Tighten)
                          .AddRobot(Drill, Consume)

                          .AddCart(Route(0, 1), Route(0, 2))
                          .AddCart(Route(1, 2), Route(0, 1));
        }

        public static ModelBuilder Ictss6(this ModelBuilder builder)
        {
            return builder.DefineTask(5, Produce, Drill, Insert, Tighten, Consume)

                          .AddRobot(Produce, Insert)
                          .AddRobot(Insert)
                          .AddRobot(Drill, Tighten)
                          .AddRobot(Tighten)
                          .AddRobot(Drill, Consume)

                          .AddCart(Route(0, 1), Route(0, 2))
                          .AddCart(Route(1, 3), Route(1, 4), Route(1, 2));
        }

        public static ModelBuilder Ictss7(this ModelBuilder builder)
        {
            return builder.DefineTask(5, Produce, Drill, Insert, Tighten, Consume)

                          .AddRobot(Produce, Insert)
                          .AddRobot(Tighten)
                          .AddRobot(Drill, Consume)

                          .AddCart(Route(0, 1))
                          .AddCart(Route(1, 2), Route(0, 1))
                          .AddCart(Route(0, 1))
                          .AddCart(Route(1, 2));
        }

        private static ModelBuilder ChooseAnalysisMode(ModelBuilder builder, AnalysisMode mode)
        {
            switch (mode)
            {
                case AnalysisMode.IntolerableFaults:
                    return builder.IntolerableFaultsAnalysis();
                case AnalysisMode.TolerableFaults:
                    return builder.TolerableFaultsAnalysis();
                case AnalysisMode.AllFaults:
                    return builder;
                default:
                    throw new ArgumentException("Unknown analysis mode!", nameof(mode));
            }
        }
    }
}
