using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SafetySharp.Modeling;
using SafetySharp.Analysis;

namespace SafetySharp.CaseStudies.RobotCell.Analysis
{
    using Modeling;
    using Modeling.Controllers;
    using Modeling.Controllers.Reconfiguration;
    using Modeling.Plants;
    using Odp;

    public class GUISimulationTest
    {
        private ModelBuilder _builder;
        private Simulator simulator;

        public Model CreateModel() {
            _builder = new ModelBuilder("model builder");
            _builder.DefineTask(5, ModelBuilderHelper.Produce, ModelBuilderHelper.Drill, ModelBuilderHelper.Insert, ModelBuilderHelper.Tighten, ModelBuilderHelper.Consume);

            _builder.AddRobot(ModelBuilderHelper.Produce, ModelBuilderHelper.Insert);
            _builder.AddRobot(ModelBuilderHelper.Insert);
            _builder.AddRobot(ModelBuilderHelper.Drill, ModelBuilderHelper.Tighten);
            _builder.AddRobot(ModelBuilderHelper.Tighten);
            _builder.AddRobot(ModelBuilderHelper.Drill, ModelBuilderHelper.Consume);

            _builder.AddCart(ModelBuilderHelper.Route(0, 1), ModelBuilderHelper.Route(0, 2));
            _builder.AddCart(ModelBuilderHelper.Route(1, 3), ModelBuilderHelper.Route(1, 4), ModelBuilderHelper.Route(1, 2));

            _builder.ChooseController<FastController>();
            _builder.CentralReconfiguration();

            return _builder.Build();
        }

        public void PrintRobotStates(List<RobotAgent> robots)
        {
            foreach (var robot in robots)
            {
                Console.WriteLine("\nRoboter mit ID: " + robot.Id + " und Name: " + robot.Name + " hat die Capabilities: " + GetICapabilitiesString(robot.AvailableCapabilities.ToList()));
                Console.WriteLine("Roboter mit ID: " + robot.Id + " und Name: " + robot.Name + " hasResource is " + robot.HasResource);
                Console.WriteLine("Roboter mit ID: " + robot.Id + " und Name: " + robot.Name + " isBroken is  " + robot.Broken.IsActivated);
                //if (robot.RoleExecutor.ExecutionState.Count() > 0)
                //    Console.WriteLine("Roboter mit ID: " + robot.Id + " und Name: " + robot.Name + " has executed the following capabilities: " + GetExecutionState(robot.RoleExecutor));
                //else
                //    Console.WriteLine("Roboter mit ID: " + robot.Id + " und Name: " + robot.Name + " has  not executed any capabilities yet");
                if (robot.AllocatedRoles.Count() > 0)
                    Console.WriteLine("Roboter mit ID: " + robot.Id + " und Name: " + robot.Name + " kann seine erste allocated Rolle ausführen is " + robot.CanExecute(robot.AllocatedRoles.First()));
                else
                    Console.WriteLine("Roboter mit ID: " + robot.Id + " und Name: " + robot.Name + " hat keine Rolle in AllocatedRoles");
                
            }
        }

        public void PrintCartStates(List<CartAgent> carts)
        {
            foreach (var cart in carts)
            {
                Console.WriteLine("\nCart mit ID: " + cart.Id + " und Name: " + cart.Name + " hasResource is " + cart.HasResource);
                Console.WriteLine("Cart mit ID: " + cart.Id + " und Name: " + cart.Name + " kann Empfangen von: " + GetCartInput(cart));
                Console.WriteLine("Cart mit ID: " + cart.Id + " und Name: " + cart.Name + " kann Senden an: " + GetCartOutput(cart));
                Console.WriteLine("Cart mit ID: " + cart.Id + " und Name: " + cart.Name + " isBroken is " + cart.Broken.IsActivated);
            }
        }

        public void PrintResource(Modeling.Controllers.Resource resource)
        {
            Console.WriteLine("\nDie Resource wurde komplett verarbeitet: " + resource.IsComplete);
        }

        public string GetICapabilitiesString(List<ICapability> capabilities)
        {
            string capString = "";
            foreach (var cap in capabilities)
            {
                if (cap.GetType() == typeof(ProcessCapability))
                {
                    ProcessCapability proc = (ProcessCapability)cap;
                    capString += proc.ProductionAction + ", ";
                }
                else
                    capString += cap.CapabilityType + ", ";
            }
            if (capString.Length > 2)
                return capString.Remove(capString.Length - 2, 2);

            return capString;
        }

        public string GetCartInput(CartAgent cart)
        {
            string inputString = "";
            foreach (var agent in cart.Inputs)
            {
                inputString += agent.Id + ", ";
            }

            if (inputString.Length > 2)
                return inputString.Remove(inputString.Length - 2, 2);
            return inputString;
        }

        public string GetCartOutput(CartAgent cart)
        {
            string outputString = "";
            foreach (var agent in cart.Outputs)
            {
                outputString += agent.Id + ", ";
            }

            if (outputString.Length > 2)
                return outputString.Remove(outputString.Length - 2, 2);
            return outputString;
        }

        public string GetExecutionState(List<ICapability> capList/*RoleExecutor executor*/)
        {
            string stateString = "";
            //List<ICapability> capList = executor.ExecutionState.ToList();
            foreach (var cap in capList)
            {
                if (cap.GetType() == typeof(ProcessCapability))
                {
                    ProcessCapability proc = (ProcessCapability)cap;
                    stateString += proc.ProductionAction + ", ";
                } else
                    stateString += cap.CapabilityType + ", ";
            }

            Console.WriteLine("LÄNGE: "+stateString.Length);
            if (stateString.Length > 2)
                return stateString.Remove(-2, 2);
            return stateString;
        }

        [Test]
        public void SimulateNoBrokenFault() {
            var model = CreateModel();
            model.Faults.SuppressActivations();

            PrintRobotStates(model.RobotAgents);
            PrintCartStates(model.CartAgents);

            var simulator = new Simulator(model);
            for (int i = 0; i < 100; i++)
            {
                model = (Model)simulator.Model;
                simulator.SimulateStep();
                PrintRobotStates(model.RobotAgents);
                PrintCartStates(model.CartAgents);
                PrintResource(model.Resources.First());
            }

            foreach (var robot in ((Model)simulator.Model).RobotAgents) {
                Assert.IsFalse(robot.Broken.IsActivated);
            }
        }

        [Test]
        public void SimulateBrokenFault() {
            var model = CreateModel();
            model.Faults.SuppressActivations();
            model.RobotAgents.ElementAt(2).Broken.ToggleActivationMode();

            PrintRobotStates(model.RobotAgents);
            PrintCartStates(model.CartAgents);

            var simulator = new Simulator(model);
            for (int i = 0; i < 100; i++)
            {
                model = (Model)simulator.Model;
                simulator.SimulateStep();
                PrintRobotStates(model.RobotAgents);
                PrintCartStates(model.CartAgents);
                PrintResource(model.Resources.First());
            }

            Assert.True(model.RobotAgents.ElementAt(2).Broken.IsActivated);
        }

        [Test]
        public void SimulateDrillBrokenFault()
        {
            var model = CreateModel();
            model.Faults.SuppressActivations();
            model.RobotAgents.ElementAt(2).DrillBroken.ToggleActivationMode();

            PrintRobotStates(model.RobotAgents);
            PrintCartStates(model.CartAgents);

            var simulator = new Simulator(model);
            for (int i = 0; i < 100; i++)
            {
                model = (Model)simulator.Model;
                simulator.SimulateStep();
                PrintRobotStates(model.RobotAgents);
                PrintCartStates(model.CartAgents);
                PrintResource(model.Resources.First());
                //For some reason there are no capabilities in the execution state
                Console.WriteLine("\nRobot.RoleExecutor has the following state: " + GetExecutionState(model.Resources.First().State.ToList()));
            }

            Assert.True(model.RobotAgents.ElementAt(2).DrillBroken.IsActivated);
        }

        /// <summary>
        /// For later tests concerning the carts
        /// </summary>
        [Test]
        public void SimulateBrokenCart() {
            var model = CreateModel();
            model.Faults.SuppressActivations();
            model.CartAgents.First().Broken.ToggleActivationMode();

            PrintRobotStates(model.RobotAgents);
            PrintCartStates(model.CartAgents);

            var simulator = new Simulator(model);
            for (int i = 0; i < 100; i++)
            {
                simulator.SimulateStep();
                PrintRobotStates(((Model)simulator.Model).RobotAgents);
                PrintCartStates(((Model)simulator.Model).CartAgents);
                PrintResource(((Model)simulator.Model).Resources.First());
            }

            Assert.True(model.CartAgents.First().Broken.IsActivated);
        }
    }
}
