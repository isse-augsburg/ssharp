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
    /// Interaktionslogik für RobotControl.xaml
    /// </summary>
    public partial class RobotControl
    {
        private RobotAgent _robotAgent;
        public RobotControl(RobotAgent robotAgent)
        {
            InitializeComponent();
            Update(robotAgent);
        }

        public void Update(RobotAgent robotAgent) {
            _robotAgent = robotAgent;
            state.Text = RobotCell.GetState(robotAgent);

            //Not sure if this will stay...causes an error in the xaml file
            availableCapabilityList.ItemsSource = _robotAgent.AvailableCapabilities;
            
            InvalidateArrange();
            InvalidateVisual();
            UpdateLayout();
        }
    }
}
