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

	using Odp;
	using Odp.Reconfiguration;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	public partial class SimulationTests
	{
		internal class ProfileBasedSimulator
		{
			private readonly Simulator _simulator;
			private readonly Model _model;
			private Tuple<Fault, ReliabilityAttribute, IComponent>[] _faults;
			private int _step;

			private int _throughput;
			private readonly Dictionary<uint, List<int>> _agentReconfigurations = new Dictionary<uint, List<int>>();

			public ProfileBasedSimulator(Model model)
			{
				RegisterListeners(model);
				model.Rebind();

				_simulator = new Simulator(model);
				_model = (Model)_simulator.Model;
				CollectFaults();
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

			private void RegisterListeners(Model model)
			{
				model.VisitPostOrder(component =>
				{
					var robotAgent = component as RobotAgent;
					if (robotAgent != null)
						robotAgent.ResourceConsumed += StaticListener.Create(() => _throughput++);
				});
				model.Controller.ConfigurationsCalculated += StaticListener.Create<ITask, ConfigurationUpdate>((task, config) =>
				{
					foreach (var agent in config.InvolvedAgents)
					{
						if (!_agentReconfigurations.ContainsKey(agent.Id))
							_agentReconfigurations.Add(agent.Id, new List<int>());
						_agentReconfigurations[agent.Id].Add(_step);
					}
				});
			}

			public void Simulate(int numberOfSteps)
			{
				Simulate(numberOfSteps, seed: Environment.TickCount);
			}

			public void Simulate(int numberOfSteps, int seed)
			{
				Console.WriteLine("SEED: " + seed);
				var rd = new Random(seed);

				for (_step = 0; _step < numberOfSteps; _step++)
				{
					foreach (var fault in _faults)
					{
						if (fault.Item2?.MTTF > 0 && !fault.Item1.IsActivated && rd.NextDouble() <= fault.Item2.DistributionValueToFail())
						{
							fault.Item1.ForceActivation();
							Console.WriteLine("Activation of: " + fault.Item1.Name + " at time " + _step);
							fault.Item2.ResetDistributionToFail();
						}
						else if (fault.Item2?.MTTR > 0 && fault.Item1.IsActivated && rd.NextDouble() <= fault.Item2.DistributionValueToRepair())
						{
							fault.Item1.SuppressActivation();
							Debug.Assert(fault.Item3 is Agent);
							((Agent)fault.Item3).Restore(fault.Item1);
							Console.WriteLine("Deactivation of: " + fault.Item1.Name + " at time " + _step);
							fault.Item2.ResetDistributionToRepair();
						}
					}
					_simulator.SimulateStep();
				}

			    ExportStats((IPerformanceMeasurementController)_model.Controller);
			}

		    private void ExportStats(IPerformanceMeasurementController modelController)
		    {
				// log:
				//   seed, model
				//   throughput
				//   start and stop time
				//   total number of steps
				//   step, duration, end time, failed/success and involved agents for each reconfiguration
				//
				// interesting (derived) properties:
				//   # reconfigurations global / per agent (avg, median, max, min, ...)
				//   intervals between an agent's reconfigurations (avg, median, ...) in steps and in time
				//   size of reconf agent sets (coalitions) (avg, median, max, min ...)
				//   required time for reconfigurations (avg, median, max, min ...)
				//   throughput
				//   compare to other algorithm with same seed

				Console.WriteLine("=============================================");
				Console.WriteLine($"Throughput: {_throughput}");
				Console.WriteLine("Reconfigurations:");
				foreach (var agent in _agentReconfigurations.Keys)
					Console.WriteLine($"\tAgent #{agent}: Steps " + string.Join(", ", _agentReconfigurations[agent]));
				Console.WriteLine("=============================================");

				if (!Directory.Exists("performance-reports"))
					Directory.CreateDirectory("performance-reports");
				var reportsDirectory = Path.Combine("performance-reports", DateTime.UtcNow.Ticks.ToString());
				Directory.CreateDirectory(reportsDirectory);

				var timeValueData = modelController.CollectedTimeValues;
		        foreach (var key in timeValueData.Keys)
		        {
		            var reconfTime = timeValueData[key].Select(tuple => (int)tuple.Item2.Ticks).ToArray();
		            var productionTime = timeValueData[key].Select(tuple => (int)tuple.Item1.Ticks).ToArray();
                    using (var sw = new StreamWriter(Path.Combine(reportsDirectory, "Agent" + key + ".csv"), true))
		            {
                        sw.WriteLine("ReconfTime; ProductionTime");
		                for (var i = 0; i < reconfTime.Length; i++)
		                {
		                    sw.WriteLine(reconfTime[i] + "; " + productionTime[i]);
		                }
		            }
                }
		    }

			private static class StaticListener
			{
				private static readonly List<Delegate> _listeners = new List<Delegate>();

				public static Action Create(Action listener)
				{
					var id = AddListener(listener);
					return () => OnCall(id);
				}

				public static Action<T1, T2> Create<T1, T2>(Action<T1, T2> listener)
				{
					var id = AddListener(listener);
					return (a, b) => OnCall(id, a, b);
				}

				private static int AddListener(Delegate listener)
				{
					int id;
					lock (_listeners)
					{
						id = _listeners.Count;
						_listeners.Add(listener);
					}
					return id;
				}

				private static void OnCall(int id, params object[] parameters)
				{
					Delegate listener = null;
					lock (_listeners)
					{
						if (_listeners.Count > id)
							listener = _listeners[id];
					}
					listener?.DynamicInvoke(parameters);
				}
			}
		}
	}
}