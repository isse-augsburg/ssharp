using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SafetySharp.CaseStudies.Visualizations
{
    using CaseStudies.RobotCell.Analysis;
    using CaseStudies.RobotCell.Modeling;
    using CaseStudies.RobotCell.Modeling.Controllers;
    using CaseStudies.RobotCell.Modeling.Plants;
    using Odp;
    using Odp.Reconfiguration;

    /// <summary>
    /// Interaktionslogik für RobotCell.xaml
    /// </summary>
    public partial class RobotCell
    {
        private Model _model;
        private ModelBuilder _builder;

        private readonly Dictionary<uint, RobotControl> _robots = new Dictionary<uint, RobotControl>();
        //private readonly Dictionary<uint, CartControl> _carts = new Dictionary<uint, CartControl>();
        public RobotCell()
        {
            /// <summary>
            /// Ictss6 manually, because of problems with the SampleModels-file
            /// </summary>
            _builder = new ModelBuilder("model builder");
            _builder.DefineTask(5, ModelBuilderHelper.Produce, ModelBuilderHelper.Drill, ModelBuilderHelper.Produce, ModelBuilderHelper.Tighten, ModelBuilderHelper.Consume);

            InitializeComponent();
            
            SimulationControls.ModelStateChanged += (o, e) => UpdateModelState();
            SimulationControls.Reset += (o, e) => OnModelStateReset();

            _builder.AddRobot(ModelBuilderHelper.Produce, ModelBuilderHelper.Insert);
            _builder.AddRobot(ModelBuilderHelper.Insert);
            _builder.AddRobot(ModelBuilderHelper.Drill, ModelBuilderHelper.Tighten);
            _builder.AddRobot(ModelBuilderHelper.Tighten);
            _builder.AddRobot(ModelBuilderHelper.Drill, ModelBuilderHelper.Consume);
            
            _builder.AddCart(ModelBuilderHelper.Route(0, 1), ModelBuilderHelper.Route(0, 2));
            _builder.AddCart(ModelBuilderHelper.Route(1, 3), ModelBuilderHelper.Route(1, 4), ModelBuilderHelper.Route(1, 2));

            _builder.ChooseController<FastController>();
            _builder.CentralReconfiguration();

            Model = _builder.Build();

            //Mybe a shorter alternative, but currently triggers an exception 
            //Model = _builder.Ictss6().Build();

            /// <summary> 
            /// Notes and other attempts:
            /// </summary>
            //IController ac = new AbstractController();
            //AbstractController
            //IController c = FastController;

            //Model = SampleModels.PerformanceMeasurement1();

            //Model = SampleModels.Ictss6;

            UpdateModelState();

            SimulationControls.MaxSpeed = 64;
            SimulationControls.ChangeSpeed(1);
        }

        public Model Model
        {
            get { return _model; }
            set
            {
                _model = value;
                InitializeVisualization();
                SimulationControls.SetModel(value);
            }
        }

        private void InitializeVisualization()
        {
            var robotCount = _model.RobotAgents.Count;

            //...
        }

        private void OnModelStateReset()
        {
            _model = (Model)SimulationControls.Model;

            if (SimulationControls.Simulator.IsReplay)
                return;
        }

        private void UpdateModelState()
        {

            //to be implemented

        }
    }
}
