using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers
{
    using Odp;

    public class SimpleProcessCapability : Capability<SimpleProcessCapability>
    {

        public override CapabilityType CapabilityType => CapabilityType.Process;

        public string ProductionAction { get; }

        public SimpleProcessCapability(string productionAction)
        {
            ProductionAction = productionAction;
        }

        public static List<ICapability> ConvertToProcessCapability(SimpleProcessCapability[] simpleProcessCapabilities)
        {
            if (simpleProcessCapabilities.Length > Enum.GetNames(typeof(ProductionAction)).Length)
                throw new Exception("Number of Elements to be converted bigger than the pool to be converted in.");
            var i = 0;
            var result = new List<ICapability>();
            foreach (var productionActionValue in Enum.GetValues(typeof(ProductionAction)).Cast<ProductionAction>().Take(simpleProcessCapabilities.Length))
            {
                result.Add(new ProcessCapability(productionActionValue));
            }
            
            return result;
        }

        public override bool Equals(object obj)
        {
            var process = obj as SimpleProcessCapability;
            return ProductionAction == process?.ProductionAction;
        }


        public override int GetHashCode() => ProductionAction.GetHashCode();

      
    }
}
