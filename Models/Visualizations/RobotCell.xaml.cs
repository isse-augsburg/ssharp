using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;

namespace SafetySharp.CaseStudies.Visualizations
{
    using CaseStudies.RobotCell.Modeling;
    using CaseStudies.RobotCell.Modeling.Controllers;
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
        private readonly Dictionary<uint, CartControl> _carts = new Dictionary<uint, CartControl>();
        private readonly List<WorkpieceControl> _workpieces = new List<WorkpieceControl>();
        private List<string> _task;

        public RobotCell()
        {
            /// <summary>
            /// Ictss6 manually, because of problems with the SampleModels-file
            /// </summary>
            _builder = new ModelBuilder("model builder");
            _builder.DefineTask(5, ModelBuilderHelper.Produce, ModelBuilderHelper.Drill, ModelBuilderHelper.Insert, ModelBuilderHelper.Tighten, ModelBuilderHelper.Consume);

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

            //_builder.CreateWorkpiece( ModelBuilderHelper.Insert, ModelBuilderHelper.Drill, ModelBuilderHelper.Tighten);

            _builder.ChooseController<FastController>();
            _builder.CentralReconfiguration();

            Model = _builder.Build();

            //Initialize/Update task
            _task = UpdateTask();
            PrintTask();

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
                //CreateRow(1);
                CreateRow(3);
                CreateRow(2);
            }
            for (int i = 0; i < gridWidth; i++) {
                //CreateColumn(1);
                CreateColumn(3);
                CreateColumn(2);
            }

            //Create and place robots
            for (int i = 0; i < robotCount; i++) {
                var robot = new RobotControl(_model.RobotAgents[i], this);
                _robots.Add(_model.RobotAgents[i].Id, robot);
                visualizationArea.Children.Add(robot);

                var row = 2 * (i / gridWidth);
                var col = 2 * (i % gridWidth);

                //Console.Out.WriteLine("<WIDTH> " + gridWidth + "<ROW> " + row + "<COLUMN>" + col);

                Grid.SetRow(robot, row);
                Grid.SetColumn(robot, col);
            }

            //Create and place carts
            for (int i = 0; i < _model.CartAgents.Count; i++)
            {
                var agent = _model.CartAgents[i];
                var cart = new CartControl(agent, this);
                _carts.Add(agent.Id, cart);
                visualizationArea.Children.Add(cart);

                var robot = _model.RobotAgents.First(r => agent.Cart.IsPositionedAt(r.Robot));
                int row, column;
                GetFreePosition(robot, out row, out column);
                
                row += 2;
                column += 2;

                Grid.SetRow(cart, row);
                Grid.SetColumn(cart, column);
            }

            //Create resources
            for (int i = 0; i < _model.Resources.Count; i++) {
                var agent = _model.Resources[i];
                var resource = new WorkpieceControl();
                _workpieces.Add(resource);
            }

            //Display task 
            _task = UpdateTask();
            DisplayTask();

            //Place Workpiece/Resource
            PlaceWorkpiece();

            //Display the current role of a robot
            DisplayRole();
        }

        private void DisplayRole() {
            foreach (var robot in _robots) {
                var agent = robot.Value.GetRobotAgent();
                Console.WriteLine("'ROLE HAS VALUE' IS: "+agent.RoleExecutor.Role.HasValue);
            }
        }

        private void PlaceWorkpiece() {
            Rectangle rect = new Rectangle();
            rect.Width = 25; rect.Height = 25;
            visualizationArea.Children.Add(rect);
            rect.Visibility = Visibility.Hidden;
            SolidColorBrush brush = new SolidColorBrush(Colors.Orange);
            rect.Stroke = brush;
            rect.StrokeThickness = 2;

            //WorkpieceControl resource = new WorkpieceControl();
            //visualizationArea.Children.Add(resource);
            //resource.Visibility = Visibility.Visible;

            foreach (var robot in _robots)
            {
                var agent = robot.Value.GetRobotAgent();
                if (agent.HasResource) {
                    Console.WriteLine("<Robot with ID '"+ agent.Id +"' has the resource>");

                    //var rowRob = Grid.GetRow(robot.Value);
                    //var colRob = Grid.GetColumn(robot.Value) + 1;
                    //var rowRect = Grid.GetRow(rect); 
                    //var colRect = Grid.GetColumn(rect);

                    //if (colRob != colRect || rowRob != rowRect) {
                    //    rect.Visibility = Visibility.Collapsed;
                    //    //visualizationArea.Children.Remove(rect);
                        
                    //    Console.WriteLine("\n\ncolRob != colRect is "+ (colRob != colRect));
                    //    Console.WriteLine("rowRob != rowRect is " + (rowRob != rowRect) +"\n\n");

                    //    Grid.SetRow(rect, Grid.GetRow(robot.Value));
                    //    Grid.SetColumn(rect, Grid.GetColumn(robot.Value) + 1);
                    //    rect.Visibility = Visibility.Visible;
                    //}
                    Grid.SetRow(rect, Grid.GetRow(robot.Value));
                    Grid.SetColumn(rect, Grid.GetColumn(robot.Value) + 1);
                    rect.Visibility = Visibility.Visible;
                    
                }
            }
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

            foreach (var robot in _robots) {
                robot.Value.Update(robot.Value.GetRobotAgent());
            }
        }

        private void UpdateModelState()
        {
            foreach (var robot in _model.RobotAgents) {
                _robots[robot.Id].Update(robot);
            }

            foreach (var cart in _model.CartAgents) {
                _carts[cart.Id].Update(cart);
            }

            PlaceWorkpiece();

            _task = UpdateTask();
            PrintTask();

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

        private List<string> UpdateTask()
        {
            //Task task = _model.Tasks.First();
            Task task = (Task)_model.Resources.First().Task;
            ICapability[] capabilities = task.RequiredCapabilities;
            List<string> stringCapabilities = new List<string>();

            foreach (var cap in capabilities)
            {
                if (cap.GetType() == typeof(ProduceCapability) || cap.GetType() == typeof(ConsumeCapability))
                {
                    stringCapabilities.Add(cap.CapabilityType.ToString());
                }

                if (cap.GetType() == typeof(ProcessCapability))
                {
                    var proc = (ProcessCapability)cap;
                    stringCapabilities.Add(proc.ProductionAction.ToString());
                }
            }

            return stringCapabilities;
        }

        private void PrintTask()
        {
            string str = "Task: ";
            for (int i = 0; i < _task.Count; i++)
            {
                if (i < _task.Count - 1)
                    str += _task.ElementAt(i).ToString() + ", ";
                else
                    str += _task.ElementAt(i).ToString();
            }
            Console.WriteLine(str);
        }

        private void DisplayTask() {
            ListBox lbox = new ListBox();
            lbox.Visibility = Visibility.Visible;
            //lbox.FontSize = 14;
            lbox.Items.Add("Current Task:");  //Maybe: Drawing the item myself and changing the background color

            //if (_task != null)
            foreach (var cap in _task)
            {
                lbox.Items.Add(cap);
            }

            var row = visualizationArea.RowDefinitions.Count - 2;
            var col = visualizationArea.ColumnDefinitions.Count - 2;

            if (IsFreePosition(row, col)) {
                visualizationArea.Children.Add(lbox);
                Grid.SetColumn(lbox, col);
                Grid.SetRow(lbox, row);
            }
        }
    }
}
