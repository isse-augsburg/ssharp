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

namespace ProductionCell
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	internal class Model : ModelBase
	{
		[Root(RootKind.Controller)]
		public Robot AwesomeRobot;

		[Root(RootKind.Controller)]
		public Cart FirstCart = new Cart() { RouteBlocked = { Name = "RouteCart1" } };

		[Root(RootKind.Controller)]
		public Robot PatheticRobot;

		[Root(RootKind.Controller)]
		public Cart SecondCart = new Cart() { RouteBlocked = { Name = "RouteCart2" } };

		[Root(RootKind.Controller)]
		public Robot YetAnotherAwesomeRobot;

		public Model()
		{
//            ObserverController = new DumpObserverController();
			ObserverController = new MiniZincObserverController();

			AwesomeRobot = new Robot("awesome", new List<Capability> { new Capability(Type.Tighten), new Capability(Type.Drill) });
			AwesomeRobot.AllocatedRoles = new List<OdpRole>(5);

			YetAnotherAwesomeRobot = new Robot("yetAnotherAwesome",
				new List<Capability> { new Capability(Type.Drill),  new Capability(Type.Insert) });
			YetAnotherAwesomeRobot.AllocatedRoles = new List<OdpRole>(5);

			PatheticRobot = new Robot("pathetic", new List<Capability> { new Capability(Type.Tighten), new Capability(Type.Insert) });
			PatheticRobot.AllocatedRoles = new List<OdpRole>(1);

			FirstCart.AvailableCapabilites = new List<Capability> { new Capability(Type.None) };
			FirstCart.AllocatedRoles = new List<OdpRole>(1);
			// FirstCart.Inputs = new List<Agent> { AwesomeRobot, PatheticRobot };
			// FirstCart.Output = new List<Agent> { YetAnotherAwesomeRobot, PatheticRobot };

			SecondCart.AvailableCapabilites = new List<Capability> { new Capability(Type.None) };
			SecondCart.AllocatedRoles = new List<OdpRole>(1);
			//SecondCart.Inputs = new List<Agent> { YetAnotherAwesomeRobot, PatheticRobot };
			//SecondCart.Output = new List<Agent> { AwesomeRobot, PatheticRobot };

			Connect(AwesomeRobot, FirstCart);
			Connect(FirstCart, YetAnotherAwesomeRobot);
			Connect(YetAnotherAwesomeRobot, SecondCart);
			Connect(SecondCart, PatheticRobot);

			ObserverController.Agents = new List<Agent>() { AwesomeRobot, YetAnotherAwesomeRobot, PatheticRobot, FirstCart, SecondCart };

			var currentTask = new Task(new List<Capability> { new Capability(Type.Drill), new Capability(Type.Insert), new Capability(Type.Tighten) });

			ObserverController.CurrentTask = currentTask;

			Console.WriteLine("Init:");
			ObserverController.Reconfigure();
		}

		[Root(RootKind.Controller)]
		public ObserverController ObserverController { get; }

		private void Connect(Agent from, Agent to)
		{
			from.Outputs.Add(to);
			to.Inputs.Add(from);
		}

		[Test]
		public void Simulation()
		{
			var simulator = new Simulator(this);
			simulator.Model.Faults.SuppressActivations();

			//((Model)simulator.Model).AwesomeRobot.AllToolsFault.Activation = Activation.Forced;
			//((Model)simulator.Model).YetAnotherAwesomeRobot.AllToolsFault.Activation = Activation.Forced;
			((Model)simulator.Model).FirstCart.RouteBlocked.Activation = Activation.Forced;
		//	((Model)simulator.Model).SecondCart.RouteBlocked.Activation = Activation.Forced;

			simulator.FastForward(10);

			Assert.IsTrue(((Model)simulator.Model).ObserverController.Unsatisfiable);
		}

		[Test]
		public void Test()
		{
			var modelChecker = new SSharpChecker();

			Formula invariant = MakeInv();
			modelChecker.Configuration.StateCapacity = 20000;
			modelChecker.Configuration.CpuCount = 1;
			var result = modelChecker.CheckInvariant(this, invariant);
			Assert.IsTrue(result.FormulaHolds);
//            var result = modelChecker.CheckInvariant(this, true);
		}

		[Test]
		public void Dcca()
		{
			var modelChecker = new SafetyAnalysis();

			Formula invariant = MakeInv();
			modelChecker.Configuration.StateCapacity = 20000;
			modelChecker.Configuration.CpuCount = 1;
			var result = modelChecker.ComputeMinimalCriticalSets(this, ObserverController.Unsatisfiable);

			Console.WriteLine(result);
			//            var result = modelChecker.CheckInvariant(this, true);
		}

		private bool MakeInv()
		{
			var invariant =
				!AwesomeRobot.AllocatedRoles.SelectMany(role => role.CapabilitiesToApply).Distinct().Except(AwesomeRobot.AvailableCapabilites).Any();
			var connection = ObserverController.CurrentTask;
			var availableCaps = ObserverController.Agents.SelectMany(agent => agent.AvailableCapabilites).Distinct();
			var reconfPossible = !ObserverController.CurrentTask.RequiresCapabilities.Except(availableCaps).Any();
			invariant = invariant &&
						(reconfPossible
							? ObserverController.Agents.SelectMany(agent => agent.AllocatedRoles).Any()
							: !ObserverController.Agents.SelectMany(agent => agent.AllocatedRoles).Any());
			return invariant;
		}
	}
}