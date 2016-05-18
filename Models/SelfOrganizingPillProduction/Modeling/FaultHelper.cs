using System.Linq;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    internal static class FaultHelper
    {
        internal static void PrefixFaultNames(Component component, string prefix)
        {
            var faults = (from field in component.GetType().GetFields()
                          where typeof(Fault).IsAssignableFrom(field.FieldType)
                          select field.GetValue(component)).Cast<Fault>();
            foreach (var fault in faults)
            {
                fault.Name = $"{prefix}.{fault.Name}";
            }
        }
    }
}
