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

    /// <summary>
    /// Interaktionslogik für RobotCell.xaml
    /// </summary>
    public partial class RobotCell
    {
        private Model _model;
        public RobotCell()
        {
            InitializeComponent();
            SimulationControls.ModelStateChanged += (o, e) => UpdateModelState();
            SimulationControls.Reset += (o, e) => OnModelStateReset();
            SimulationControls.SetModel(new Model());

            UpdateModelState();

            SimulationControls.MaxSpeed = 64;
            SimulationControls.ChangeSpeed(1);
        }



        private void OnModelStateReset()
        {
            _model = (Model)SimulationControls.Model;

            if (SimulationControls.Simulator.IsReplay)
                return;
        }

        private void UpdateModelState() {

            //to be implemented

        }
    }
}
