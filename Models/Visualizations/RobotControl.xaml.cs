namespace SafetySharp.CaseStudies.Visualizations
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using CaseStudies.RobotCell.Modeling;
    using CaseStudies.RobotCell.Modeling.Controllers;
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

            //Not sure if this will stay...causes an error in the xaml 
            //availableCapabilityList.ItemsSource = _robotAgent.AvailableCapabilities;

            //IEnumerable<ICapability> enumerator = _robotAgent.AvailableCapabilities;
            List<ICapability> capList = _robotAgent.AvailableCapabilities.ToList();
            List<string> stringCapList = new List<string>();

            //for (int i = 0; i < capList.Count; i++) {
            //    Console.WriteLine("<Listenelement " + i + "> " + capList.ElementAt(i));
            //    Type type = capList.ElementAt(i).GetType();

            //    string show = "", str = "", str1 = "", str2 = "";

            //    if (type == typeof(ProcessCapability))
            //    {
            //        str = "<PROCESS CAPABILITY>"+type.Name +"    ";
            //        ProcessCapability procCap = CastToProcessCapability(type);
            //        //availableCapabilityList.ItemsSource = "Produce";

            //        Console.WriteLine("\n"+"<PRODUCTION ACTION> " + procCap.ProductionAction+ "\n");
                    
            //    }
            //    else if (type == typeof(ProduceCapability))
            //    {
            //        str1 = "<PRODUCE CAPABILITY>" + type.Name + "    ";
            //        ProduceCapability prodCap = CastToProduceCapability(type);
            //    }
            //    else if (type == typeof(ConsumeCapability))
            //    {
            //        str2 = "<CONSUME CAPABILITY>" + type.Name + "    ";
            //    }
            //    show = str + str1 + str2;
            //    Console.WriteLine(show);

            //    IEnumerable<ICapability> num = capList;
            //    availableCapabilityList.ItemsSource = num;
            //}

            foreach (var cap in capList)
            {
                if (cap.GetType() == typeof(ProduceCapability)) {
                    ProduceCapability prod = (ProduceCapability) cap;
                    stringCapList.Add(prod.CapabilityType.ToString());

                    Console.WriteLine("\n<Produce Capability> : " + prod.CapabilityType);
                }

                if (cap.GetType() == typeof(ProcessCapability)) {
                    ProcessCapability proc = (ProcessCapability) cap;
                    stringCapList.Add(proc.ProductionAction.ToString());

                    Console.WriteLine("\n<Process Capability> : " + proc.CapabilityType);
                    Console.WriteLine("<PC: Production Action> : " + proc.ProductionAction);
                }

                if (cap.GetType() == typeof(ConsumeCapability)) {
                    ConsumeCapability cons = (ConsumeCapability) cap;
                    stringCapList.Add(cons.CapabilityType.ToString());

                    Console.WriteLine("\n<Consume Capability> : " + cons.CapabilityType + "\n");
                }
            }

            IEnumerable<string> num = stringCapList;
            availableCapabilityList.ItemsSource = num;

            InvalidateArrange();
            InvalidateVisual();
            UpdateLayout();
        }

        public ProduceCapability CastToProduceCapability(object obj) {
            return (ProduceCapability)obj;
        }

        public ProcessCapability CastToProcessCapability(object obj)
        {
            return (ProcessCapability)obj;
        }
    }
}
