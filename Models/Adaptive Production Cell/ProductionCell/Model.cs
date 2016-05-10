using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SafetySharp.Analysis;
using SafetySharp.Modeling;

namespace ProductionCell
{
    class Model : ModelBase
    {
        [Root(Role.System)]
        public ObserverController ObserverController { get; }
        [Root(Role.Environment)]
        public Agent AwesomeRobot = new Agent();
        [Root(Role.Environment)]
        public Agent YetAnotherAwesomeRobot = new Agent();
        [Root(Role.Environment)]
        public Agent PatheticRobot = new Agent();
        [Root(Role.Environment)]
        public Agent FirstCart = new Agent();
        [Root(Role.Environment)]
        public Agent SecondCart = new Agent();


        public Model()
        {
//            ObserverController = new DumpObserverController();
            ObserverController = new MiniZincObserverController();


            AwesomeRobot.IsCart = false;
            AwesomeRobot.AllocatedRoles = new List<OdpRole>(5);
            AwesomeRobot.AvailableCapabilites = new List<Capability> { new Capability(Type.D), new Capability(Type.I), new Capability(Type.T) };

            YetAnotherAwesomeRobot.IsCart = false;
            YetAnotherAwesomeRobot.AllocatedRoles = new List<OdpRole>(5);
            YetAnotherAwesomeRobot.AvailableCapabilites = new List<Capability> { new Capability(Type.D), new Capability(Type.I), new Capability(Type.T) };

            PatheticRobot.IsCart = false;
            PatheticRobot.AllocatedRoles = new List<OdpRole>(1);
            PatheticRobot.AvailableCapabilites = new List<Capability> { new Capability(Type.I) };

            FirstCart.IsCart = true;
            FirstCart.AvailableCapabilites = new List<Capability> { new Capability(Type.None) };
            FirstCart.AllocatedRoles = new List<OdpRole>(1);
            FirstCart.Inputs = new List<Agent> { AwesomeRobot, PatheticRobot };
            FirstCart.Output = new List<Agent> { YetAnotherAwesomeRobot, PatheticRobot };

            SecondCart.IsCart = true;
            SecondCart.AvailableCapabilites = new List<Capability> { new Capability(Type.None) };
            SecondCart.AllocatedRoles = new List<OdpRole>(1);
            SecondCart.Inputs = new List<Agent> { YetAnotherAwesomeRobot, PatheticRobot };
            SecondCart.Output = new List<Agent> { AwesomeRobot, PatheticRobot };

            ObserverController.Agents = new List<Agent>() { AwesomeRobot, PatheticRobot, YetAnotherAwesomeRobot, FirstCart, SecondCart };

            var currentTask = new Task() { RequiresCapabilities = new List<Capability> { new Capability(Type.D), new Capability(Type.I), new Capability(Type.T) } };
        
            ObserverController.CurrentTasks = currentTask;

            ObserverController.Reconfigure();

        }

        [Test]
        public void Test()
        {
            var modelChecker = new SSharpChecker();

            Formula invariant = MakeInv();
            modelChecker.Configuration.StateCapacity = 20000;
            var result = modelChecker.CheckInvariant(this, invariant);
            Assert.IsTrue(result.FormulaHolds);
//            var result = modelChecker.CheckInvariant(this, true);

        }

        private bool MakeInv()
        {
            var invariant = !AwesomeRobot.AllocatedRoles.SelectMany(role => role.CapabilitiesToApply).Distinct().Except(AwesomeRobot.AvailableCapabilites).Any();
            var connection = ObserverController.CurrentTasks;
            var availableCaps = ObserverController.Agents.SelectMany(agent => agent.AvailableCapabilites).Distinct();
            var reconfPossible = !ObserverController.CurrentTasks.RequiresCapabilities.Except(availableCaps).Any();
            invariant = invariant && (reconfPossible ? ObserverController.Agents.SelectMany(agent => agent.AllocatedRoles).Any() : !ObserverController.Agents.SelectMany(agent => agent.AllocatedRoles).Any());
            return invariant;
        }
    }
}