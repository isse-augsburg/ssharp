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
    using System.Collections.Generic;
    using Analysis;
    using Odp.Reconfiguration;
    using static ModelBuilderHelper;

    internal static class SampleModels
    {
        public static Model DefaultInstance<T>(AnalysisMode mode = AnalysisMode.AllFaults)
            where T : IController
        {
            return Ictss1<T>(mode);
        }

        public static IEnumerable<Model> CreateConfigurations<T>(AnalysisMode mode)
            where T : IController
        {
            yield return Ictss1<T>(mode);
            yield return Ictss2<T>(mode);
            yield return Ictss3<T>(mode);
            yield return Ictss4<T>(mode);
            yield return Ictss5<T>(mode);
            yield return Ictss6<T>(mode);
            yield return Ictss7<T>(mode);
        }

        public static Model Ictss1<T>(AnalysisMode mode) where T : IController
        {
            return ChooseAnalysisMode(
                new ModelBuilder(nameof(Ictss1))
                    .DefineTask(5, Produce, Drill, Insert, Tighten, Polish, Consume)

                    .AddRobot(Produce, Drill, Insert)
                    .AddRobot(Insert, Drill)
                    .AddRobot(Tighten, Polish, Tighten, Drill)
                    .AddRobot(Polish, Consume)

                    .AddCart(Route(0, 1), Route(0, 2), Route(0, 3))
                    .AddCart(Route(1, 2), Route(0, 1))
                    .AddCart(Route(2, 3))

                    .ChooseController<T>()
                    .CentralReconfiguration()
                , mode);
        }

        public static Model Ictss2<T>(AnalysisMode mode) where T : IController
        {
            return ChooseAnalysisMode(
                new ModelBuilder(nameof(Ictss2))
                    .DefineTask(5, Produce, Drill, Insert, Tighten, Consume)

                    .AddRobot(Produce, Insert)
                    .AddRobot(Tighten)
                    .AddRobot(Drill, Consume)

                    .AddCart(Route(0, 1), Route(0, 2))
                    .AddCart(Route(1, 2), Route(0, 1))

                    .ChooseController<T>()
                    .CentralReconfiguration()
                , mode);
        }

        public static Model Ictss3<T>(AnalysisMode mode) where T : IController
        {
            return ChooseAnalysisMode(
                new ModelBuilder(nameof(Ictss3))
                    .DefineTask(5, Produce, Drill, Insert, Tighten, Consume)

                    .AddRobot(Produce, Insert, Drill, Insert)
                    .AddRobot(Insert, Tighten, Drill)
                    .AddRobot(Tighten, Insert, Consume, Drill)

                    .AddCart(Route(0, 1), Route(0, 2))
                    .AddCart(Route(1, 2), Route(0, 1))

                    .ChooseController<T>()
                    .CentralReconfiguration()
                , mode);
        }

        public static Model Ictss4<T>(AnalysisMode mode) where T : IController
        {
            return ChooseAnalysisMode(
                new ModelBuilder(nameof(Ictss4))
                    .DefineTask(5, Produce, Drill, Insert, Tighten, Consume)

                    .AddRobot(Produce, Insert)
                    .AddRobot(Tighten)
                    .AddRobot(Drill, Consume)

                    .AddCart(Route(0, 1), Route(0, 2), Route(1, 2))
                    .AddCart(Route(1, 2), Route(0, 1), Route(0, 2))

                    .ChooseController<T>()
                    .CentralReconfiguration()
                , mode);
        }

        public static Model Ictss5<T>(AnalysisMode mode) where T : IController
        {
            return ChooseAnalysisMode(
                new ModelBuilder(nameof(Ictss5))
                    .DefineTask(5, Produce, Drill, Insert, Tighten, Drill, Insert, Tighten, Consume)

                    .AddRobot(Produce, Insert)
                    .AddRobot(Tighten)
                    .AddRobot(Drill, Consume)

                    .AddCart(Route(0, 1), Route(0, 2))
                    .AddCart(Route(1, 2), Route(0, 1))

                    .ChooseController<T>()
                    .CentralReconfiguration()
                , mode);
        }

        public static Model Ictss6<T>(AnalysisMode mode) where T : IController
        {
            return ChooseAnalysisMode(
                new ModelBuilder(nameof(Ictss6))
                    .DefineTask(5, Produce, Drill, Insert, Tighten, Consume)

                    .AddRobot(Produce, Insert)
                    .AddRobot(Insert)
                    .AddRobot(Drill, Tighten)
                    .AddRobot(Tighten)
                    .AddRobot(Drill, Consume)

                    .AddCart(Route(0, 1), Route(0, 2))
                    .AddCart(Route(1, 3), Route(1, 4), Route(1, 2))

                    .ChooseController<T>()
                    .CentralReconfiguration()
                , mode);
        }

        public static Model Ictss7<T>(AnalysisMode mode) where T : IController
        {
            return ChooseAnalysisMode(
                new ModelBuilder(nameof(Ictss7))
                    .DefineTask(5, Produce, Drill, Insert, Tighten, Consume)

                    .AddRobot(Produce, Insert)
                    .AddRobot(Tighten)
                    .AddRobot(Drill, Consume)

                    .AddCart(Route(0, 1))
                    .AddCart(Route(1, 2), Route(0, 1))
                    .AddCart(Route(0, 1))
                    .AddCart(Route(1, 2))

                    .ChooseController<T>()
                    .CentralReconfiguration()
                , mode);
        }

        private static Model ChooseAnalysisMode(ModelBuilder builder, AnalysisMode mode)
        {
            switch (mode)
            {
                case AnalysisMode.IntolerableFaults:
                    builder.IntolerableFaultsAnalysis();
                    break;
                case AnalysisMode.TolerableFaults:
                    builder.TolerableFaultsAnalysis();
                    break;
            }
            return builder.Build();
        }
    }
}
