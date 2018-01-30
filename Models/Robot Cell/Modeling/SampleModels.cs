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
    using System.Diagnostics;
    using System.Linq;
    using Analysis;
    using Controllers;
	using Odp;
    using Odp.Reconfiguration;

    using static ModelBuilderHelper;
	using FastConfigurationFinder = Controllers.Reconfiguration.FastConfigurationFinder;


	internal static class SampleModels
    {
        private static readonly Func<ModelBuilder, ModelBuilder>[] _defaultConfigurations =
            { Ictss1, Ictss2, Ictss3, Ictss4, Ictss5, Ictss6, Ictss7 };

        private static readonly Func<ModelBuilder, ModelBuilder>[] _performanceEvaluationConfigurations =
            { FewAgentsHighRedundancy , ManyAgentsLowRedundancy, ManyAgentsHighRedundancy, MediumSizePerformanceMeasurementModel };

        public static Model DefaultInstance<T>(IConfigurationFinder finder, AnalysisMode mode = AnalysisMode.AllFaults)
            where T : IController
        {
            return DefaultSetup<T>(finder, mode, false).Invoke(new ModelBuilder(nameof(Ictss1)).Ictss1()).Build();
        }

        public static IEnumerable<Model> CreateDefaultConfigurations<T>(IConfigurationFinder finder, AnalysisMode mode, bool verify = false)
            where T : IController
        {
            return CreateConfigurations(b => b, DefaultSetup<T>(finder, mode, verify));
        }

        public static IEnumerable<Model> CreateDefaultConfigurationsWithoutPlant<T>(IConfigurationFinder finder, AnalysisMode mode, bool verify = false)
            where T : IController
        {
            return CreateConfigurations(b => b.DisablePlants(), DefaultSetup<T>(finder, mode, verify));
        }

        public static IEnumerable<Model> CreateCoalitionConfigurations(bool verify = false)
        {
            return CreateConfigurations(
                b => b.DisablePlants(),
                b => b.UseCoalitionFormation(new FastConfigurationFinder()).EnableControllerVerification(verify).DisableIntolerableFaults()
            );
        }

        public static IEnumerable<Model> CreateCentralConfigurations(bool verify = false)
        {
            return CreateConfigurations(
                b => b.DisablePlants(),
                b => b.ChooseController<CentralizedController>(new FastConfigurationFinder()).CentralReconfiguration().EnableControllerVerification(verify).DisableIntolerableFaults());
                
        }

        public static IEnumerable<Model> CreatePerformanceEvaluationConfigurationsCentralized()
        {
            return CreateConfigurations(
                _performanceEvaluationConfigurations,
                b => b.DisablePlants(),
                b => b.ChooseController<CentralizedController>(new FastConfigurationFinder()).UseControllerReconfigurationAgents().EnablePerformanceMeasurement().DisableIntolerableFaults()
            );
        }

        public static IEnumerable<Model> CreatePerformanceEvaluationConfigurationsCoalition()
        {
            return CreateConfigurations(
                _performanceEvaluationConfigurations,
                b => b.DisablePlants(),
                b => b.UseCoalitionFormation(new FastConfigurationFinder()).EnablePerformanceMeasurement().DisableIntolerableFaults()
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

        private static Func<ModelBuilder, ModelBuilder> DefaultSetup<T>(IConfigurationFinder finder, AnalysisMode mode, bool verify) where T : IController
        {
            return builder => ChooseAnalysisMode(builder.ChooseController<T>(finder)
                                                        .EnableControllerVerification(verify)
                                                        .CentralReconfiguration(), mode);
        }

        public static Model PerformanceMeasurement1<T>(IConfigurationFinder finder, AnalysisMode mode = AnalysisMode.AllFaults, bool verify = false)
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

                    .ChooseController<T>(finder)
                    .EnablePerformanceMeasurement()
                    .EnableControllerVerification(verify)
                    .CentralReconfiguration()
                , mode).Build();
        }

        public static Model MediumSizePerformanceMeasurementModel()
        {
            return new ModelBuilder(nameof(MediumSizePerformanceMeasurementModel)).MediumSizePerformanceMeasurementModel().Build();
        }

        public static ModelBuilder FewAgentsHighRedundancy(this ModelBuilder builder)
        {
            return builder.DefineTask(1000, Produce, Drill, Insert, Tighten, Consume) // 5 capabilities in task

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
                .AddCart(Route(3, 4), Route(4, 5), Route(1, 5), Route(5, 3));
        }

        public static ModelBuilder ManyAgentsLowRedundancy(this ModelBuilder builder)
        {
            const int capCount = 5;
            const int sysSize = 15;
            const int ioCount = 15;
            const int numberOfCarts = 5;
            const int numWorkpieces = 1000;
            return GenerateSystem(builder, capCount, sysSize, ioCount, numberOfCarts, numWorkpieces, new Random(42));
        }

        private static ModelBuilder GenerateSystem(this ModelBuilder builder, int capCount, int sysSize, int ioCount, int numberOfCarts, int numWorkpieces, Random rnd)
        {
            var tsg = new TestSystemGenerator();
            var generatedSystem = tsg.Generate(sysSize, capCount, ioCount, rnd);
            Debug.Assert(capCount <= Enum.GetNames(typeof(ProductionAction)).Length && Enum.GetNames(typeof(ProductionAction)).Length >= sysSize);

            var currentCapa = generatedSystem.Item2;
            var enumerator = Enum.GetValues(typeof(ProductionAction)).Cast<ProductionAction>().GetEnumerator();
            var dummyToProcessCapability = new Dictionary<DummyCapability, ProcessCapability>();
            foreach (var dummyCapability in currentCapa)
            {
                enumerator.MoveNext();
                dummyToProcessCapability.Add(dummyCapability, new ProcessCapability(enumerator.Current));
            }

            // Produce needs to be added at the beginning and Consume at the end to complete the generated task
            var newTask = new ICapability[generatedSystem.Item2.Count + 2];
            var i = 0;
            newTask[i++] = Produce;
            foreach (var taskItem in generatedSystem.Item2)
            {
                newTask[i++] = dummyToProcessCapability[taskItem];
            }
            newTask[i++] = Consume;
            
            builder.DefineTask(numWorkpieces, newTask);
            foreach (var dummyAgent in generatedSystem.Item1)
            {
                var currentCapabilities = new ICapability[dummyAgent.GetCapabilities().Count + 2];
                var j = 0;
                currentCapabilities[j++] = Produce;
                foreach (var capability in dummyAgent.GetCapabilities())
                {
                    currentCapabilities[j++] = dummyToProcessCapability[capability];
                }
                currentCapabilities[j++] = Consume;

                builder.AddRobot(currentCapabilities);
            }

            var neededConnections = new HashSet<Tuple<int, int>>();
            var agents = generatedSystem.Item1.ToArray();
            for (var index0 = 0; index0 < agents.Length; index0++)
            {
                for (var index1 = 0; index1 < agents.Length; index1++)
                {
                    if (agents[index0].GetOutputs().Contains(agents[index1]))
                    {
                        neededConnections.Add(new Tuple<int, int>(index0, index1));
                    }
                }
            }
            
            for (var j = 0; j < numberOfCarts; j++)
            {
                builder.AddCart(neededConnections.ToArray());
            }

            //Debug.Assert(IsReconfigurationPossible(builder.Build().Tasks.First(), builder.Build().RobotAgents.ToArray()));
            return builder;
        }

        private static bool IsReconfigurationPossible(ITask task, RobotAgent[] robotsAgents)
        {
            var matrix = GetConnectionMatrix(robotsAgents);

            var isReconfPossible = task.RequiredCapabilities.All(capability => robotsAgents.Any(agent => agent.AvailableCapabilities.Contains(capability)));
            if (!isReconfPossible)
                return false;

            var candidates = robotsAgents.Where(agent => agent.AvailableCapabilities.Contains(task.RequiredCapabilities.First())).ToArray();

            for (var i = 0; i < task.RequiredCapabilities.Length - 1 && isReconfPossible; i++)
            {
                candidates = candidates.SelectMany(r => matrix[r])
                                       .Where(r => r.AvailableCapabilities.Contains(task.RequiredCapabilities[i + 1]))
                                       .ToArray();
                isReconfPossible &= candidates.Length > 0;
                Debug.WriteLine(i);
            }

            return isReconfPossible;
        }

        private static Dictionary<RobotAgent, List<RobotAgent>> GetConnectionMatrix(IEnumerable<RobotAgent> robotAgents)
        {
            var matrix = new Dictionary<RobotAgent, List<RobotAgent>>();

            foreach (var robot in robotAgents)
            {
                var list = new List<RobotAgent>(robotAgents.Where(r => IsConnected(robot, r, new HashSet<RobotAgent>())));
                matrix.Add(robot, list);
            }

            return matrix;
        }

        private static bool IsConnected(RobotAgent source, RobotAgent target, HashSet<RobotAgent> seenRobots)
        {
            if (source == target)
                return true;

            if (!seenRobots.Add(source))
                return false;

            foreach (var output in source.Outputs)
            {
                foreach (var output2 in output.Outputs)
                {
                    if (output2 == target)
                        return true;

                    if (IsConnected((RobotAgent)output2, target, seenRobots))
                        return true;
                }
            }

            return false;
        }
        

        public static ModelBuilder ManyAgentsHighRedundancy(this ModelBuilder builder)
        {
            const int capCount = 20;
            const int sysSize = 20;
            const int ioCount = 15;
            const int numberOfCarts = 15;
            const int numWorkpieces = 1000;
            return GenerateSystem(builder, capCount, sysSize, ioCount, numberOfCarts, numWorkpieces, new Random(42));
        }

        public static ModelBuilder MediumSizePerformanceMeasurementModel(this ModelBuilder builder)
        {
            const int capCount = 5;
            const int sysSize = 10;
            const int ioCount = 10;
            const int numberOfCarts = 3;
            const int numWorkpieces = 1000;
            return GenerateSystem(builder, capCount, sysSize, ioCount, numberOfCarts, numWorkpieces, new Random(42));
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
