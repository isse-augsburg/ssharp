using System.Collections.Generic;
using SafetySharp.Modeling;

namespace ProductionCell
{
    class OdpRole : Component
    {
		[Hidden]
	    public readonly List<Capability> CapabilitiesToApply = new List<Capability>(1);
	    //public Condition PostCondition { get; set; }
	    //public Condition PreCondition { get; set; } 
    }
}