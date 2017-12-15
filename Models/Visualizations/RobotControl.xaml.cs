namespace SafetySharp.CaseStudies.Visualizations
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using CaseStudies.RobotCell.Modeling;
    using CaseStudies.RobotCell.Modeling.Controllers;
    using Odp;
    using System.Windows;
    using Modeling;
    using Infrastructure;

    /// <summary>
    /// Interaktionslogik für RobotControl.xaml
    /// </summary>
    public partial class RobotControl
    {
        private RobotAgent _robotAgent;
        private readonly RobotCell _container;
        public RobotControl(RobotAgent robotAgent, RobotCell containter)
        {
            InitializeComponent();
            _container = containter;
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

            _robotAgent.Broken.Activation = BrokenFault.IsChecked.ToOccurrenceKind();
            _robotAgent.ResourceTransportFault.Activation = ResourceTransportFault.IsChecked.ToOccurrenceKind();
            _robotAgent.DrillBroken.Activation = DrillBrokenFault.IsChecked.ToOccurrenceKind();
            _robotAgent.InsertBroken.Activation = InsertBrokenFault.IsChecked.ToOccurrenceKind();
            _robotAgent.TightenBroken.Activation = TightenBrokenFault.IsChecked.ToOccurrenceKind();
            _robotAgent.PolishBroken.Activation = PolishBrokenFault.IsChecked.ToOccurrenceKind();

            InvalidateArrange();
            InvalidateVisual();
            UpdateLayout();
        }

        //to-do: check the faults for working correctly

        private void OnBrokenFault(object sender, RoutedEventArgs e) {
            //maybe without RobotCell in Constructor
            Console.WriteLine("<BROKEN FAULT OCCURED!>");

            _robotAgent.Broken.ToggleActivationMode();
        }
        
        private void OnResourceTransportFault(object sender, RoutedEventArgs e) {
            Console.WriteLine("<RESOURCE TRANSPORT FAULT OCCURRED>");

            _robotAgent.ResourceTransportFault.ToggleActivationMode();
        }

        private void OnDrillBrokenFault(object sender, RoutedEventArgs e) {
            Console.WriteLine("<DRILL BROKEN FAULT OCCURRED>");

            _robotAgent.DrillBroken.ToggleActivationMode();
        }

        private void OnInsertBrokenFault(object sender, RoutedEventArgs e) {
            Console.WriteLine("<INSERT BROKEN FAULT OCCURRED>");

            _robotAgent.DrillBroken.ToggleActivationMode();
        }

        private void OnTightenBrokenFault(object sender, RoutedEventArgs e) {
            Console.WriteLine("<TIGHTEN BROKEN FAULT OCCURRED>");

            _robotAgent.TightenBroken.ToggleActivationMode();
        }

        private void OnPolishBrokenFault(object sender, RoutedEventArgs e) {
            Console.WriteLine("<POLISH BROKEN FAULT OCCURRED>");

            _robotAgent.PolishBroken.ToggleActivationMode();
        }

        public RobotAgent GetRobotAgent() {
            return _robotAgent;
        }
    }
}
