namespace SafetySharp.CaseStudies.Visualizations
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using CaseStudies.RobotCell.Modeling;
    using CaseStudies.RobotCell.Modeling.Controllers;
    using Odp;
    using System.Windows;

    /// <summary>
    /// Interaktionslogik für RobotControl.xaml
    /// </summary>
    public partial class RobotControl
    {
        private RobotAgent _robotAgent;
        private readonly RobotCell _container;
        public RobotControl(RobotAgent robotAgent)
        {
            InitializeComponent();
            Update(robotAgent);
        }

        public void Update(RobotAgent robotAgent) {
            _robotAgent = robotAgent;
            state.Text = RobotCell.GetState(robotAgent);

            //IEnumerable<ICapability> enumerator = _robotAgent.AvailableCapabilities;
            List<ICapability> capList = _robotAgent.AvailableCapabilities.ToList();
            List<string> stringCapList = new List<string>();

            foreach (var cap in capList)
            {
                if (cap.GetType() == typeof(ProduceCapability)) {
                    ProduceCapability prod = (ProduceCapability)cap;
                    stringCapList.Add(prod.CapabilityType.ToString());

                    //Console.WriteLine("\n<Produce Capability> : " + prod.CapabilityType);
                }

                if (cap.GetType() == typeof(ProcessCapability)) {
                    ProcessCapability proc = (ProcessCapability)cap;
                    stringCapList.Add(proc.ProductionAction.ToString());

                    //Console.WriteLine("\n<Process Capability> : " + proc.CapabilityType);
                    //Console.WriteLine("<PC: Production Action> : " + proc.ProductionAction);
                }

                if (cap.GetType() == typeof(ConsumeCapability)) {
                    ConsumeCapability cons = (ConsumeCapability)cap;
                    stringCapList.Add(cons.CapabilityType.ToString());

                    //Console.WriteLine("\n<Consume Capability> : " + cons.CapabilityType + "\n");
                }
            }

            //IEnumerable<string> num = stringCapList;
            availableCapabilityList.ItemsSource = stringCapList;

            InvalidateArrange();
            InvalidateVisual();
            UpdateLayout();
        }

        private void OnBrokenFault(object sender, RoutedEventArgs e) {
            //to-do
            //maybe with RobotCell in Constructor
        }

        private void OnSwitchFault(object sender, RoutedEventArgs e) {
            //to-do
        }

        private void OnSwitchToWrongToolFault(object sender, RoutedEventArgs e) {
            //to-do
        }

        private void OnResourceTransportFault(object sender, RoutedEventArgs e) {
            //to-do
        }
    }
}
