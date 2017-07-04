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
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using Modeling;
	using Modeling.Controllers;
	using Modeling.Controllers.Reconfiguration;
	using Modeling.Plants;
	using NUnit.Framework;
	using Odp.Reconfiguration;
	using RDotNet;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using FastController = Modeling.Controllers.Reconfiguration.FastController;

    public class SimulationTests
	{

        internal class ProfileBasedSimulator<T> where T : IPerformanceMeasurementController
        {
            private Model model { get; set; }
            Tuple<Fault, ReliabilityAttribute, IComponent>[] faults;
            private readonly Simulator Simulator;
            public int Throughput { get; set; } = 0;

            public ProfileBasedSimulator(Model model)
            {
                Simulator = new Simulator(model);//ModellessSimulator(model.Components);
                this.model = (Model)Simulator.Model;
                CollectFaults();
            }

            private void CollectFaults()
            {
                var faultInfo = new HashSet<Tuple<Fault, ReliabilityAttribute, IComponent>>();
                model.VisitPostOrder(component =>
                {
                    var faultFields =
                        from faultField in component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                        where typeof(Fault).IsAssignableFrom(faultField.FieldType) && faultField.GetCustomAttribute<ReliabilityAttribute>() != null
                        select Tuple.Create((Fault)faultField.GetValue(component), faultField.GetCustomAttribute<ReliabilityAttribute>(), component);

                    foreach (var info in faultFields)
                        faultInfo.Add(info);
                });
                faults = faultInfo.ToArray();
            }

            public void Simulate(int numberOfSteps)
            {
                var rd = new Random();
                double currentRDFail;
                double currentDistValueFail;
                double currentRDRepair;
                double currentDistValueRepair;
                using (var sw = new StreamWriter(@"C:\Users\Eberhardinger\Documents\testRD.csv", true))
                {
                    sw.WriteLine("currentRDFail; currentDistValueFail; currentRDRepair; currentDistValueRepair; failed; repaired");
                }
                for (var x = 0; x < numberOfSteps; x++)
                {
                    foreach (var fault in this.faults)
                    {
                        
//                        using (StreamWriter sw = new StreamWriter(@"C:\Users\Eberhardinger\Documents\testRD.csv", true))
//                        {
//                            sw.WriteLine(rn);
//                        }
                        /*currentRDFail = rd.NextDouble();
                        currentDistValueFail = fault.Item2.DistributionValueToFail();
                        currentRDRepair = rd.NextDouble();
                        currentDistValueRepair = fault.Item2.DistributionValueToRepair();
                        var failed = currentRDFail <= currentDistValueFail;
                        var repaired = currentRDRepair <= currentDistValueRepair;*/
                        if (fault.Item2?.MTTF > 0 && !fault.Item1.IsActivated && rd.NextDouble() <= fault.Item2.DistributionValueToFail())
//                        if (fault.Item2?.MTTF > 0 && !fault.Item1.IsActivated && currentRDFail <= currentDistValueFail)
                        {
                            fault.Item1.ForceActivation();
                            Console.WriteLine("Activation of: " + fault.Item1.Name + " at time " + x);
                            fault.Item2.ResetDistributionToFail();
                        }
                        else {
                            if (fault.Item2?.MTTR > 0 && fault.Item1.IsActivated && rd.NextDouble() <= fault.Item2.DistributionValueToRepair())
//                            if (fault.Item2?.MTTR > 0 && fault.Item1.IsActivated && currentRDRepair <= currentDistValueRepair)
                            {
                                fault.Item1.SuppressActivation();
                                Debug.Assert(fault.Item3 is Agent);
                                (fault.Item3 as Agent)?.Restore(fault.Item1);
                                Console.WriteLine("Deactivation of: " + fault.Item1.Name + " at time " + x);
                                fault.Item2.ResetDistributionToRepair();
                            }
                        }
                        /*using (var sw = new StreamWriter(@"C:\Users\Eberhardinger\Documents\testRD.csv", true))
                        {
                            sw.WriteLine(currentRDFail + "; " + currentDistValueFail + "; " + currentRDRepair + "; " + currentDistValueRepair + "; " + failed + "; " + repaired);
                        }*/
                    }
                    Simulator.SimulateStep();
                    Throughput = model.Workpieces.Select(w => w.IsComplete).Count();
                }
                CreateStats(Throughput, (T)model.Controller);

            }

            private void CreateStats(int throughput, T modelController)
            {
                Debug.Assert(Throughput!=0);
                Debug.Assert(modelController.CollectedTimeValues != null);
                
                REngine engine;
                string fileName;

                //init the R engine            
                REngine.SetEnvironmentVariables();
                engine = REngine.GetInstance();
                engine.Initialize();

                //prepare data
                var timeValueData = modelController.CollectedTimeValues;
                var reconfTimeOfAgents = timeValueData.Values.SelectMany(t => t.Select(a => (double)a.Item2.Ticks)).ToArray();
                var productionTimeOfAgents = timeValueData.Values.SelectMany(t => t.Select(a => (double)a.Item1.Ticks)).ToArray();
                var measurePoints = timeValueData.Values.SelectMany(t => t.Select(a => (double)a.Item3)).ToArray();
                NumericVector measurePointsVector = engine.CreateNumericVector(reconfTimeOfAgents);
                NumericVector reconfTimeOfAgentsNumericVector = engine.CreateNumericVector(reconfTimeOfAgents);
                NumericVector productionTimeOfAgentsNumericVector = engine.CreateNumericVector(productionTimeOfAgents);
                engine.SetSymbol("reconfTimeOfAgents", reconfTimeOfAgentsNumericVector);
                engine.SetSymbol("productionTimeOfAgents", productionTimeOfAgentsNumericVector);
                engine.SetSymbol("measurePoints", measurePointsVector);
                IntegerVector throughputVector = engine.CreateIntegerVector(new int[] { throughput });
                engine.SetSymbol("throughput", throughputVector);
                engine.SetSymbol("maxThroughput", engine.CreateIntegerVector(new int[] { 10 }));
                engine.SetSymbol("w", engine.CreateNumericVector(new double[] { 0.5 }));

                //prepare data
                fileName = "C:\\Users\\Eberhardinger\\Desktop\\myplot.pdf";

                //calculate
                engine.Evaluate("perfomranceValueVector <- productionTimeOfAgents/reconfTimeOfAgents");
                engine.Evaluate("overallPerformanceTimeValue <- mean(perfomranceValueVector)");
                var overallPerformanceTimeValue = engine.GetSymbol("overallPerformanceTimeValue");
                engine.Evaluate("relativeCostValue <- throughput/maxThroughput");
                engine.Evaluate("overallPerformanceValue <- overallPerformanceTimeValue + w * relativeCostValue");

                CharacterVector fileNameVector = engine.CreateCharacterVector(new[] { fileName });
                engine.SetSymbol("fileName", fileNameVector);

//                engine.Evaluate("reg <- lm(perfomranceValueVector~measurePoints)");
                engine.Evaluate("cairo_pdf(filename=fileName, width=6, height=6, bg='transparent')");
                engine.Evaluate("plot(perfomranceValueVector~measurePoints)");
//                engine.Evaluate("abline(reg)");
                engine.Evaluate("dev.off()");

                //clean up
                engine.Dispose();

            }

       
            private bool ReonfPossibleAfterFault(Fault item1)
            {

                throw new NotImplementedException();
            }

        }


        [Test]
		public void Simulate()
		{
			var model = SampleModels.PerformanceMeasurement1<PerformanceMeasurementController<FastController>>();
			model.Faults.SuppressActivations();

			var simulator = new Simulator(model);
            /*simulator.SimulateStep();*/
            PrintTrace(simulator, model, steps: 120);
		}

        [Test]
        public void SimulateProfileBased()
        {
            var model = SampleModels.PerformanceMeasurement1<PerformanceMeasurementController<FastController>>();
            model.Faults.SuppressActivations();
            var profileBasedSimulator = new ProfileBasedSimulator<PerformanceMeasurementController<FastController>>(model);
            profileBasedSimulator.Simulate(numberOfSteps: 1000);
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