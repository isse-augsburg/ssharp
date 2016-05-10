using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SafetySharp.Analysis;
using SafetySharp.Modeling;

namespace ProductionCell
{
	using System;

	class Model : ModelBase
    {
        [Root(Role.System)]
        public ObserverController ObserverController { get; }

		[Root(Role.Environment)]
		public Robot AwesomeRobot;

		[Root(Role.Environment)]
		public Robot YetAnotherAwesomeRobot;

		[Root(Role.Environment)]
		public Robot PatheticRobot;
        [Root(Role.Environment)]
        public Agent FirstCart = new Agent();
        [Root(Role.Environment)]
        public Agent SecondCart = new Agent();


        public Model()
        {
//            ObserverController = new DumpObserverController();
            ObserverController = new MiniZincObserverController();

			AwesomeRobot = new Robot("awesome", new List<Capability> { new Capability(Type.T) , new Capability(Type.D) });
            AwesomeRobot.AllocatedRoles = new List<OdpRole>(5);

	        YetAnotherAwesomeRobot = new Robot("yetAnotherAwesome",
		        new List<Capability> { new Capability(Type.D), new Capability(Type.I), new Capability(Type.T) });
			YetAnotherAwesomeRobot.AllocatedRoles = new List<OdpRole>(5);

			PatheticRobot = new Robot("pathetic", new List<Capability> { new Capability(Type.I), new Capability(Type.I) });
			PatheticRobot.AllocatedRoles = new List<OdpRole>(1);

			FirstCart.AvailableCapabilites = new List<Capability> { new Capability(Type.None) };
            FirstCart.AllocatedRoles = new List<OdpRole>(1);
			// FirstCart.Inputs = new List<Agent> { AwesomeRobot, PatheticRobot };
			// FirstCart.Output = new List<Agent> { YetAnotherAwesomeRobot, PatheticRobot };

			SecondCart.AvailableCapabilites = new List<Capability> { new Capability(Type.None) };
            SecondCart.AllocatedRoles = new List<OdpRole>(1);
			//SecondCart.Inputs = new List<Agent> { YetAnotherAwesomeRobot, PatheticRobot };
			//SecondCart.Output = new List<Agent> { AwesomeRobot, PatheticRobot };

			ObserverController.Agents = new List<Agent>() { AwesomeRobot, PatheticRobot, YetAnotherAwesomeRobot, FirstCart, SecondCart };

            var currentTask = new Task(new List<Capability> { new Capability(Type.D), new Capability(Type.I), new Capability(Type.T) });
        
            ObserverController.CurrentTask = currentTask;

	        Console.WriteLine("Init:");
            ObserverController.Reconfigure();

        }

		[Test]
		public void Simulation()
		{
			var simulator = new Simulator(this);
			simulator.Model.Faults.SuppressActivations();

			((Model)simulator.Model).AwesomeRobot.AllToolsFault.Activation = Activation.Forced;
			((Model)simulator.Model).YetAnotherAwesomeRobot.AllToolsFault.Activation = Activation.Forced;


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
            var invariant = !AwesomeRobot.AllocatedRoles.SelectMany(role => role.CapabilitiesToApply).Distinct().Except(AwesomeRobot.AvailableCapabilites).Any();
            var connection = ObserverController.CurrentTask;
            var availableCaps = ObserverController.Agents.SelectMany(agent => agent.AvailableCapabilites).Distinct();
            var reconfPossible = !ObserverController.CurrentTask.RequiresCapabilities.Except(availableCaps).Any();
            invariant = invariant && (reconfPossible ? ObserverController.Agents.SelectMany(agent => agent.AllocatedRoles).Any() : !ObserverController.Agents.SelectMany(agent => agent.AllocatedRoles).Any());
            return invariant;
        }
    }
}