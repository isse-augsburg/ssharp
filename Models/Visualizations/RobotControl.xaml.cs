namespace SafetySharp.CaseStudies.Visualizations
{
    using CaseStudies.RobotCell.Modeling.Controllers;
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
            //availableCapabilityList.ItemsSource = _robotAgent.AvailableCapabilities;
            
            InvalidateArrange();
            InvalidateVisual();
            UpdateLayout();
        }
    }
}
