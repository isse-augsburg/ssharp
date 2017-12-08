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
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Modeling;

	using NUnit.Framework;

	public partial class SimulationTests
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

		[Test, TestCaseSource(nameof(PerformanceMeasurementConfigurations))]
		public void PerformanceEvaluation(Model model)
		{
			var console = Console.Out;
			Console.SetOut(TextWriter.Null);
			Debug.Listeners.Clear();

			const int simulationsPerModel = 100;
			var ctx = new Context(simulationsPerModel, model, console);

			var cores = Environment.ProcessorCount;
			console.WriteLine($"Testing with {cores} cores.");
			var workers = new EvaluationWorker[cores];

			for (var i = 0; i < cores; ++i)
			{
				workers[i] = new EvaluationWorker(ctx);
				workers[i].Start();
			}

			for (var i = 0; i < cores; ++i)
				workers[i].Wait();
		}

		private class Context
		{
			private readonly int _numRuns;
			private readonly Model _model;
			private readonly TextWriter _output;

			public Context(int numRuns, Model model, TextWriter output)
			{
				_numRuns = numRuns;
				_model = model;
				_output = output;
			}

			private int _running = 0;
			private int _successful = 0;
			private int _seed = 0;

			public bool GetTask(out ProfileBasedSimulator simulator)
			{
				lock (_model)
				{
					if (_running + _successful >= _numRuns)
					{
						simulator = null;
						return false;
					}

					_output.WriteLine($"Starting test {_seed}");
					_running++;
					simulator = new ProfileBasedSimulator(_model, _seed++);
				}
				return true;
			}

			public void Failed()
			{
				lock (_model)
				{
					_running--;
					_output.WriteLine("Test failed or aborted.");
				}
			}

			public void Success()
			{
				lock (_model)
				{
					_running--;
					_successful++;
					_output.WriteLine("Test succeeded.");
				}
			}
		}

		private class EvaluationWorker
		{
			private const int NumberOfSteps = 1000;
			private const int TimeLimitMs = 300000; // TODO: adapt for different models

			private readonly Context _ctx;
			private Thread _thread;

			public EvaluationWorker(Context ctx)
			{
				_ctx = ctx;
			}

			public void Start()
			{
				if (_thread != null)
					return;
				_thread = new Thread(Evaluate);
				_thread.Start();
			}

			public void Wait()
			{
				_thread?.Join();
			}

			private void Evaluate()
			{
				ProfileBasedSimulator simulator;
				while (_ctx.GetTask(out simulator))
				{
					var task = Task.Run(() => simulator.Simulate(NumberOfSteps));
					try
					{
						task.Wait(TimeLimitMs);
						_ctx.Success();
					}
					catch
					{
						_ctx.Failed();
					}
				}
			}
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
			System.Console.WriteLine(line.ToString());
#endif
		}
        
    }
}