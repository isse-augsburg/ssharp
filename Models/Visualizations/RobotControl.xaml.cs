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
            
            //Set the capabilities, the robot is able to execute
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
            
            //Display the capability, the robot is currently executing 
            RoleExecutor executor = _robotAgent.RoleExecutor;

            availableCapabilityList.SelectedItem = null;
            if (executor.IsExecuting && _robotAgent.HasResource)
            {
                if (executor.ExecutionState.Count() > 0)
                {
                    var cap = executor.ExecutionState.Last();

                    if (capList.Contains(cap))
                    {
                        if (cap.GetType() == typeof(ProduceCapability) || cap.GetType() == typeof(ConsumeCapability))
                        {
                            //currentRole.Text = cap.CapabilityType.ToString();
                            availableCapabilityList.SelectedItem = cap.CapabilityType.ToString();
                            _container.AddDoneCapability(cap.CapabilityType.ToString());
                        }
                        else if (cap.GetType() == typeof(ProcessCapability))
                        {
                            ProcessCapability procCap = (ProcessCapability)cap;
                            //currentRole.Text = procCap.ProductionAction.ToString();
                            availableCapabilityList.SelectedItem = procCap.ProductionAction.ToString();
                            _container.AddDoneCapability(procCap.ProductionAction.ToString());
                        }
                        else
                            availableCapabilityList.SelectedItem = null;
                            //currentRole.Text = "";
                    }
                }
            }            
               
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

        //to-do: check the faults for working correctly; functions are invoked

        private void OnBrokenFault(object sender, RoutedEventArgs e) {
            //maybe without RobotCell in Constructor
            Console.WriteLine("<BROKEN FAULT OCCURED!>");

            //_robotAgent.Broken.ToggleActivationMode();
            _robotAgent.Broken.ForceActivation();
        }
        
        private void OnResourceTransportFault(object sender, RoutedEventArgs e) {
            Console.WriteLine("<RESOURCE TRANSPORT FAULT OCCURRED>");

            _robotAgent.ResourceTransportFault.ToggleActivationMode();
        }

        private void OnDrillBrokenFault(object sender, RoutedEventArgs e) {
            Console.WriteLine("<DRILL BROKEN FAULT OCCURRED>");

            _robotAgent.DrillBroken.ForceActivation();
        }

        private void OnInsertBrokenFault(object sender, RoutedEventArgs e) {
            Console.WriteLine("<INSERT BROKEN FAULT OCCURRED>");

            _robotAgent.InsertBroken.Activation = Activation.Forced;
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
