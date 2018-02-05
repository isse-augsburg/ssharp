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
	using System.Linq;
	using System.Reflection;

	using Modeling;
	using Modeling.Controllers;
	using Modeling.Controllers.Reconfiguration.PerformanceMeasurement;
	using Modeling.Plants;

	using Odp;
	using Odp.Reconfiguration;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	public partial class SimulationTests
	{
		internal class ProfileBasedSimulator : IDisposable
		{
			private readonly Simulator _simulator;
			private readonly Model _model;
			private readonly Tuple<Fault, ReliabilityAttribute, IComponent>[] _faults;

			private int _step;
			private int _capabilityThroughput;
			private int _resourceThroughput;
			private SimulationReport _report;

			public ProfileBasedSimulator(Model model)
			{
				RegisterListeners(model);
				model.Rebind();

				_simulator = new Simulator(model);
				_model = (Model)_simulator.Model;
				_faults = CollectFaults();
			}

			public void Reset()
			{
				_simulator.Reset();
				_step = 0;
				_capabilityThroughput = 0;
				_resourceThroughput = 0;
				_report = null;
			}

			private Tuple<Fault, ReliabilityAttribute, IComponent>[] CollectFaults()
			{
				return (from component in _model.Components
						from faultField in component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
						where typeof(Fault).IsAssignableFrom(faultField.FieldType)

						let attribute = faultField.GetCustomAttribute<ReliabilityAttribute>()
						where attribute != null

						let fault = (Fault)faultField.GetValue(component)
						where fault.Activation == Activation.Nondeterministic // Don't (de-) activate suppressed or forced faults.

						select Tuple.Create(fault, attribute, component)
				).ToArray();
			}

			private void RegisterListeners(Model model)
			{
				foreach (var resource in model.Resources)
					resource.CapabilityApplied += StaticListener.Create(() => _capabilityThroughput++);

				foreach (var component in model.Components)
				{
					var agent = component as Agent;
					if (agent == null)
						continue;

					var performanceStrategy = agent.ReconfigurationStrategy as PerformanceMeasurementReconfigurationStrategy;
					if (performanceStrategy != null)
						performanceStrategy.MeasuredReconfigurations += StaticListener.Create<IEnumerable<ReconfigurationRequest>, ulong>((requests, nanosecondsDuration) =>
						{
							if (_report == null)
								return;
							foreach (var reconfigurationRequest in requests)
								_report.AddAgentReconfiguration(_step, agent, nanosecondsDuration, reconfigurationRequest.Task);
						});

					var robotAgent = agent as RobotAgent;
					if (robotAgent != null)
						robotAgent.ResourceConsumed += StaticListener.Create(() => _resourceThroughput++);
				}

				var performanceController = model.Controller as PerformanceMeasurementController;
				if (performanceController != null)
					performanceController.MeasuredConfigurationCalculation += StaticListener.Create<ITask, ConfigurationUpdate, ulong>(
						(task, config, nanosecondsDuration) =>
						{
							_report?.AddReconfiguration(_step, task, config, nanosecondsDuration);
						});
			}

			public SimulationReport Simulate(int numberOfSteps)
			{
				return Simulate(numberOfSteps, Environment.TickCount);
			}

			public SimulationReport Simulate(int numberOfSteps, int seed)
			{
				_report = new SimulationReport(_model.Name, seed, DateTime.Now, numberOfSteps);

				Console.WriteLine("SEED: " + seed);
				var rd = new Random(seed);

				for (_step = 0; _step < numberOfSteps; _step++)
				{
					Console.WriteLine("Step " + _step);
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

				_report.ResourceThroughput = _resourceThroughput;
				_report.CapabilityThroughput = _capabilityThroughput;
				_report.SimulationEnd = DateTime.Now;

				var report = _report;
				_report = null;

				return report;
			}

			public void Dispose()
			{
				StaticListener.Clear();
				((IDisposable)_simulator).Dispose();
			}

			public class SimulationReport
			{
				public SimulationReport(string model, int seed, DateTime simulationStart, int steps)
				{
					Model = model;
					Seed = seed;
					SimulationStart = simulationStart;
					Steps = steps;
				}

				public string Model { get; }

				public int Seed { get; }

				public int ResourceThroughput { get; set; }

				public int CapabilityThroughput { get; set; }

				public DateTime SimulationStart { get; }

				public DateTime SimulationEnd { get; set; }

				public int Steps { get; }

				public LinkedList<ReconfigurationRecord> Reconfigurations { get; } = new LinkedList<ReconfigurationRecord>();

				public LinkedList<AgentReconfiguration> AgentReconfigurations { get; } = new LinkedList<AgentReconfiguration>();

				public void AddReconfiguration(int step, ITask task, ConfigurationUpdate configUpdate, ulong nanosecondsDuration)
				{
					Reconfigurations.AddLast(new ReconfigurationRecord(step, nanosecondsDuration, DateTime.Now, task, configUpdate));
				}

				public void AddAgentReconfiguration(int step, Agent agent, ulong nanosecondsDuration, ITask task)
				{
					AgentReconfigurations.AddLast(new AgentReconfiguration(agent.Id, step, nanosecondsDuration, task));
				}

				public struct ReconfigurationRecord
				{
					public ReconfigurationRecord(int step, ulong nanosecondsDuration, DateTime end, ITask task, ConfigurationUpdate configUpdate)
					{
						Step = step;
						NanosecondsDuration = nanosecondsDuration;
						End = end;
						Task = task;
						ConfigUpdate = configUpdate;
					}

					public int Step { get; }
					public ulong NanosecondsDuration { get; }
					public DateTime End { get; }
					public ITask Task { get; }
					public ConfigurationUpdate ConfigUpdate { get; }
				}

				public struct AgentReconfiguration
				{
					public AgentReconfiguration(uint agent, int step, ulong nanosecondsDuration, ITask task)
					{
						Agent = agent;
						Step = step;
						NanosecondsDuration = nanosecondsDuration;
						Task = task;
					}

					public uint Agent { get; }
					public int Step { get; }
					public ulong NanosecondsDuration { get; }
					public ITask Task { get; }
				}
			}

			private static class StaticListener
			{
				private static readonly List<Delegate> _listeners = new List<Delegate>();

				public static void Clear()
				{
					lock (_listeners)
					{
						for (var i = 0; i < _listeners.Count; ++i)
							_listeners[i] = null;
					}
				}

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

				public static Action<T1, T2, T3> Create<T1, T2, T3>(Action<T1, T2, T3> listener)
				{
					var id = AddListener(listener);
					return (a, b, c) => OnCall(id, a, b, c);
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