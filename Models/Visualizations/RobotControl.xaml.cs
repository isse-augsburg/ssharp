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
                if (cap.GetType() == typeof(ProduceCapability) || cap.GetType() == typeof(ConsumeCapability)) {
                    stringCapList.Add(cap.CapabilityType.ToString());
                }

                if (cap.GetType() == typeof(ProcessCapability)) {
                    ProcessCapability proc = (ProcessCapability)cap;
                    stringCapList.Add(proc.ProductionAction.ToString());                    
                }
            }
            
            availableCapabilityList.ItemsSource = stringCapList;

            if (_robotAgent.RoleExecutor.Role == null)
                Console.WriteLine("<ROLE IS NULL!!!>");
            else {
                Console.WriteLine("<Current Role>: " + _robotAgent.RoleExecutor.Role);
            }
            

            //int i = 0;
            //foreach (var role in _robotAgent.AllocatedRoles) {
            //    foreach (var cap in role.CapabilitiesToApply) {
            //        Console.WriteLine("Rolle "+i+": " + cap.CapabilityType.ToString());
            //    }
            //    i++;
            //}
            
            InvalidateArrange();
            InvalidateVisual();
            UpdateLayout();
        }

        private void OnBrokenFault(object sender, RoutedEventArgs e) {
            //to-do
            //maybe with RobotCell in Constructor
            Console.WriteLine("<BROKEN FAULT OCCURED!>");
        }

        private void OnSwitchFault(object sender, RoutedEventArgs e) {
            //to-do
            Console.WriteLine("<SWITCH FAULT OCCURED>");
        }

        private void OnSwitchToWrongToolFault(object sender, RoutedEventArgs e) {
            //to-do
            Console.WriteLine("<SWITCH TO WRONG TOOL FAULT OCCURRED>");
        }

        private void OnResourceTransportFault(object sender, RoutedEventArgs e) {
            //to-do
            Console.WriteLine("<RESOURCE TRANSPORT FAULT OCCURRED>");
        }

        public RobotAgent GetRobotAgent() {
            return _robotAgent;
        }
    }
}
