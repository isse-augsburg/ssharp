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

namespace SafetySharp.CaseStudies.RobotCell.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Reflection;

	using Modeling;
	using Modeling.Controllers;
	using Modeling.Controllers.Reconfiguration;
	using Modeling.Plants;

	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	using RDotNet;

	public partial class SimulationTests
	{
		internal class ProfileBasedSimulator
		{
			private readonly Model _model;
			private Tuple<Fault, ReliabilityAttribute, IComponent>[] _faults;
			private readonly Simulator _simulator;
			private int _throughput;

			public ProfileBasedSimulator(Model model)
			{
				_simulator = new Simulator(model);
				_model = (Model)_simulator.Model;
				CollectFaults();
				RegisterListeners();
			}

			private void CollectFaults()
			{
				var faultInfo = new HashSet<Tuple<Fault, ReliabilityAttribute, IComponent>>();
				_model.VisitPostOrder(component =>
				{
					var faultFields =
						from faultField in component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
						where typeof(Fault).IsAssignableFrom(faultField.FieldType) && faultField.GetCustomAttribute<ReliabilityAttribute>() != null
						select Tuple.Create((Fault)faultField.GetValue(component), faultField.GetCustomAttribute<ReliabilityAttribute>(), component);

					foreach (var info in faultFields)
						faultInfo.Add(info);
				});
				_faults = faultInfo.ToArray();
			}

			private void RegisterListeners()
			{
				_model.VisitPostOrder(component =>
				{
					var robotAgent = component as RobotAgent;
					if (robotAgent != null)
						robotAgent.ResourceConsumed += () => _throughput++;
				});
			}

			public void Simulate(int numberOfSteps)
			{
				var seed = Environment.TickCount;
				Console.WriteLine("SEED: " + seed);
				var rd = new Random(seed);
//				using (var sw = new StreamWriter("testRD.csv", true))
//				{
//					sw.WriteLine("currentRDFail; currentDistValueFail; currentRDRepair; currentDistValueRepair; failed; repaired");
//				}
				for (var x = 0; x < numberOfSteps; x++)
				{
					foreach (var fault in _faults)
					{
						if (fault.Item2?.MTTF > 0 && !fault.Item1.IsActivated && rd.NextDouble() <= fault.Item2.DistributionValueToFail())
						{
							fault.Item1.ForceActivation();
							Console.WriteLine("Activation of: " + fault.Item1.Name + " at time " + x);
							fault.Item2.ResetDistributionToFail();
						}
						else if (fault.Item2?.MTTR > 0 && fault.Item1.IsActivated && rd.NextDouble() <= fault.Item2.DistributionValueToRepair())
						{
							fault.Item1.SuppressActivation();
							Debug.Assert(fault.Item3 is Agent);
							((Agent)fault.Item3).Restore(fault.Item1);
							Console.WriteLine("Deactivation of: " + fault.Item1.Name + " at time " + x);
							fault.Item2.ResetDistributionToRepair();
						}
					}
					_simulator.SimulateStep();
				}

				Console.WriteLine("THROUGHPUT: " + _throughput);
			    ExportStats(_throughput, (IPerformanceMeasurementController)_model.Controller);
			}

		    private static void ExportStats(int throughput, IPerformanceMeasurementController modelController)
		    {

		        var timeValueData = modelController.CollectedTimeValues;
		        foreach (var key in timeValueData.Keys)
		        {
		            var reconfTime = timeValueData[key].Select(tuple => (int)tuple.Item2.Ticks).ToArray();
		            var productionTime = timeValueData[key].Select(tuple => (int)tuple.Item1.Ticks).ToArray();
                    using (var sw = new StreamWriter("Agent" + key + "Export" + System.DateTime.Now.Ticks + ".csv", true))
		            {
                        sw.WriteLine("ReconfTime; ProductionTime");
		                for (var i = 0; i < reconfTime.Length; i++)
		                {
		                    sw.WriteLine(reconfTime[i] + "; " + productionTime[i]);
		                }
		            }
                }

                
		    }

			private static void CreateStats(int throughput, IPerformanceMeasurementController modelController)
			{
				Debug.Assert(throughput != 0);
				Debug.Assert(modelController.CollectedTimeValues != null);

				//init the R engine
				try
				{
					REngine.SetEnvironmentVariables();
				}
				catch (ApplicationException e)
				{
					Console.WriteLine("Cannot use R: " + e.Message);
					return;
				}

				var engine = REngine.GetInstance();
				engine.Initialize();

				//prepare data
				var timeValueData = modelController.CollectedTimeValues;
				var reconfTimeOfAgents = timeValueData.Values.SelectMany(t => t.Select(a => (double)a.Item2.Ticks)).ToArray();
				var productionTimeOfAgents = timeValueData.Values.SelectMany(t => t.Select(a => (double)a.Item1.Ticks)).ToArray();

				var measurePointsVector = engine.CreateNumericVector(reconfTimeOfAgents);
				var reconfTimeOfAgentsNumericVector = engine.CreateNumericVector(reconfTimeOfAgents);
				var productionTimeOfAgentsNumericVector = engine.CreateNumericVector(productionTimeOfAgents);
				var throughputVector = engine.CreateIntegerVector(new[] { throughput });

				engine.SetSymbol("reconfTimeOfAgents", reconfTimeOfAgentsNumericVector);
				engine.SetSymbol("productionTimeOfAgents", productionTimeOfAgentsNumericVector);
				engine.SetSymbol("measurePoints", measurePointsVector);
				engine.SetSymbol("throughput", throughputVector);
				engine.SetSymbol("maxThroughput", engine.CreateIntegerVector(new[] { 10 }));
				engine.SetSymbol("w", engine.CreateNumericVector(new[] { 0.5 }));

				//prepare data
				const string fileName = "myplot.pdf";

				//calculate
				engine.Evaluate("performanceValueVector <- productionTimeOfAgents/reconfTimeOfAgents");
				engine.Evaluate("overallPerformanceTimeValue <- mean(perfomranceValueVector)");
				engine.Evaluate("relativeCostValue <- throughput/maxThroughput");
				engine.Evaluate("overallPerformanceValue <- overallPerformanceTimeValue + w * relativeCostValue");

				var fileNameVector = engine.CreateCharacterVector(new[] { fileName });
				engine.SetSymbol("fileName", fileNameVector);

				engine.Evaluate("cairo_pdf(filename=fileName, width=6, height=6, bg='transparent')");
				engine.Evaluate("plot(performanceValueVector~measurePoints)");
				engine.Evaluate("dev.off()");

				//clean up
				engine.Dispose();
			}
		}
	}
}