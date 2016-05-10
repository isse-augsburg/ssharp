using System.Collections.Generic;
using SafetySharp.Modeling;

namespace ProductionCell
{
    class OdpRole : Component
    {
        public List<Capability> CapabilitiesToApply { get; set; }
        public Condition PostCondition { get; set; }
        public Condition PreCondition { get; set; } 
    }
}