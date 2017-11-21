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
            
            //Maybe a shorter alternative, but currently triggers an exception 
            //Model = _builder.Ictss6().Build();

            //UpdateModelState();

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

            //Create grid for robots and carts
            double widthToHeightRatio = 2;
            var gridHeigth = Math.Max((int)Math.Ceiling(Math.Sqrt(robotCount/ widthToHeightRatio)), 1);
            var gridWidth = (int)(widthToHeightRatio * gridHeigth);

            //Console.Out.WriteLine("<HEIGHT> " + gridHeigth + "  <WIDTH> " + gridWidth);

            visualizationArea.RowDefinitions.Clear();
            visualizationArea.ColumnDefinitions.Clear();

            for (int i = 0; i < gridHeigth; i++) {
                CreateRow(1);
                CreateRow(3);
                CreateRow(1);
            }
            for (int i = 0; i < gridWidth; i++) {
                CreateColumn(1);
                CreateColumn(3);
                CreateColumn(1);
            }

            //Create and place robots
            for (int i = 0; i < robotCount; i++) {
                var robot = new RobotControl(_model.RobotAgents[i]);
                _robots.Add(_model.RobotAgents[i].Id, robot);
                visualizationArea.Children.Add(robot);

                var row = 3 * (i / gridWidth) + 1;
                var col = 3 * (i % gridWidth) + 1;

                Grid.SetRow(robot, row);
                Grid.SetColumn(robot, col);
            }

            ////Create and place carts
            //for (int i = 0; i < 1; i++) {

            //}

        }

        internal void GetFreePosition(RobotAgent robot, out int row, out int col) {
            var ctrl = _robots[robot.Id];
            var robotRow = Grid.GetRow(ctrl);
            var robotColumn = Grid.GetColumn(ctrl);

            var pos = new[] { robotRow - 1, robotRow, robotRow + 1 }
                .Zip(new[] { robotColumn - 1, robotColumn, robotColumn + 1 }, Tuple.Create)
                .First(coords => IsFreePosition(coords.Item1, coords.Item2));
            row = pos.Item1;
            col = pos.Item2;
        }

        private bool IsFreePosition(int row, int col) {
            return !visualizationArea.Children.Cast<UIElement>().Any(ctrl => Grid.GetRow(ctrl) == row && Grid.GetColumn(ctrl) == col);
        }

        private void OnModelStateReset()
        {
            _model = (Model)SimulationControls.Model;

            if (SimulationControls.Simulator.IsReplay)
                return;
        }

        private void UpdateModelState()
        {
            foreach (var robot in _model.RobotAgents) {
                _robots[robot.Id].Update(robot);
            }

            //Here carts

            InvalidateArrange();
            InvalidateVisual();
            UpdateLayout();

            visualizationArea.InvalidateArrange();
            visualizationArea.InvalidateVisual();
        }

        public void CreateRow(double height) {
            var row = new RowDefinition() { Height = new GridLength(height, GridUnitType.Star) };
            visualizationArea.RowDefinitions.Add(row);
        }

        public void CreateColumn(double width)
        {
            var col = new ColumnDefinition() { Width = new GridLength(width, GridUnitType.Star) };
            visualizationArea.ColumnDefinitions.Add(col);
        }

        internal static string GetState(BaseAgent agent) {
            var agentType = typeof(BaseAgent);
            var machineField = agentType.GetField("_stateMachine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var machine = machineField.GetValue(agent);

            var machineType = machine.GetType();
            var stateField = machineType.GetProperty("State");
            return stateField.GetValue(machine).ToString();
        }
    }
}
