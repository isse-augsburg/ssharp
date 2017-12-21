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

        [Test]
        public void SimulateStep() {
            var model = CreateModel();
            model.Faults.SuppressActivations();

            var simulator = new Simulator(model);
            simulator.FastForward(steps: 120);

            foreach (var robot in ((Model)simulator.Model).RobotAgents) {
                
            }
        }
    }
}
