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

        private readonly Dictionary<uint, RobotControl> _robots = new Dictionary<uint, RobotControl>();
        //private readonly Dictionary<uint, CartControl> _carts = new Dictionary<uint, CartControl>();
        public RobotCell()
        {
            InitializeComponent();
            SimulationControls.ModelStateChanged += (o, e) => UpdateModelState();
            SimulationControls.Reset += (o, e) => OnModelStateReset();
            //SimulationControls.SetModel(new Model());

            //Model = SampleModels.Ictss6<FastController>(AnalysisMode.AllFaults);

            UpdateModelState();

            //SimulationControls.MaxSpeed = 64;
            //SimulationControls.ChangeSpeed(1);
        }

        public Model Model {
            get { return _model; }
            set {
                _model = value;
                InitializeVisualization();
                SimulationControls.SetModel(value);
            }
        }

        private void InitializeVisualization()
        {
            throw new NotImplementedException();
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
