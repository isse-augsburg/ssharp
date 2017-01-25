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

namespace SafetySharp.CaseStudies.RobotCell.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Reflection;
	using Modeling;
	using Modeling.Controllers.Reconfiguration;
	using Modeling.Plants;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	public class SimulationTests
	{

        internal class ProfileBasedSimulator
        {
            private Model model { get; set; }
            Tuple<Fault, ReliabilityAttribute>[] faults;
            private Simulator Simulator;

            public ProfileBasedSimulator(Model model)
            {
                Simulator = new Simulator(model);
                this.model = (Model)Simulator.Model;
                CollectFaults();
            }

            private void CollectFaults()
            {
                var faultInfo = new HashSet<Tuple<Fault, ReliabilityAttribute>>();
                model.VisitPostOrder(component =>
                {
                    var faultFields =
                        from faultField in component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                        where typeof(Fault).IsAssignableFrom(faultField.FieldType)
                        select Tuple.Create((Fault)faultField.GetValue(component), faultField.GetCustomAttribute<ReliabilityAttribute>());

                    foreach (var info in faultFields)
                        faultInfo.Add(info);
                });

                faults = faultInfo.ToArray();
            }

            public void Simulate(int numberOfSteps)
            {
                var rd = new Random();
                for (var x = 0; x < numberOfSteps; x++)
                {
                    foreach (var fault in this.faults)
                    {
                        if (fault.Item2?.Lambda > 0 && !fault.Item1.IsActivated && rd.NextDouble() <= 1 - Math.Exp(-1 * fault.Item2.Lambda * x))
                        {
                            fault.Item1.ForceActivation();
                        }
                    }
                    Simulator.SimulateStep();

                }
               
            }


        }


        [Test]
		public void Simulate()
		{
			var model = SampleModels.DefaultInstance<PerformanceMeasurementController<FastController>>();
			model.Faults.SuppressActivations();

			var simulator = new Simulator(model);
			PrintTrace(simulator, steps: 120);
		}

        [Test]
        public void SimulateProfileBased()
        {
            var model = SampleModels.DefaultInstance<PerformanceMeasurementController<FastController>>();
            model.Faults.SuppressActivations();
            var profileBasedSimulator = new ProfileBasedSimulator(model);
            profileBasedSimulator.Simulate(numberOfSteps: 10000);
        }

        public static void PrintTrace(Simulator simulator, int steps)
		{
			var model = (Model)simulator.Model;

			for (var i = 0; i < steps; ++i)
			{
				WriteLine($"=================  Step: {i}  =====================================");

				if (model.Controller.ReconfigurationFailure)
					WriteLine("Reconfiguration failed.");
				else
				{
					foreach (var robot in model.RobotAgents)
						WriteLine(robot);

					foreach (var cart in model.CartAgents)
						WriteLine(cart);

					foreach (var workpiece in model.Workpieces)
						WriteLine(workpiece);

					foreach (var robot in model.Robots)
						WriteLine(robot);

					foreach (var cart in model.Carts)
						WriteLine(cart);
				}

				simulator.SimulateStep();
			}
		}

		private static void WriteLine(object line)
		{
			Debug.WriteLine(line.ToString());
#if !DEBUG
			Console.WriteLine(line.ToString());
#endif
		}
        
    }
}