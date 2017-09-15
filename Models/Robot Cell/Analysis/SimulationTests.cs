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
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Reflection;

	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	using Modeling;
	using Modeling.Controllers;
	using Modeling.Controllers.Reconfiguration;
	using Modeling.Plants;

	using NUnit.Framework;
	using RDotNet;

    public class SimulationTests
	{
	    [Test]
	    public void TempTestSystemGeneratorTest()
	    {
	        var tsg = new TestSystemGenerator();
            var result = tsg.Generate(100, 10, 40);
	        ;
	    }

	    [Test, TestCaseSource(nameof(PerformanceMeasurementConfigurations))]
        public void Simulate(Model model)
        {
            model.Faults.SuppressActivations();
            var simulator = new Simulator(model);
            PrintTrace(simulator, model, steps: 100);
		}

        [Test, TestCaseSource(nameof(PerformanceMeasurementConfigurations))]
        public void SimulateProfileBased(Model model)
        {
            model.Faults.SuppressActivations();
            var profileBasedSimulator = new ProfileBasedSimulator(model);
            profileBasedSimulator.Simulate(numberOfSteps: 1000);
        }

	    private static IEnumerable PerformanceMeasurementConfigurations()
	    {
		    return SampleModels.CreatePerformanceEvaluationConfigurationsCentralized()
							   .Select(model => new TestCaseData(model).SetName(model.Name + " (Centralized)"))
							   .Concat(SampleModels.CreatePerformanceEvaluationConfigurationsCoalition()
												   .Select(model => new TestCaseData(model).SetName(model.Name + " (Coalition)")));
	    }

        private static void PrintTrace(Simulator simulator, Model model, int steps)
		{
			
			for (var i = 0; i < steps; ++i)
			{
				WriteLine($"=================  Step: {i}  =====================================");

				if (model.ReconfigurationMonitor.ReconfigurationFailure)
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