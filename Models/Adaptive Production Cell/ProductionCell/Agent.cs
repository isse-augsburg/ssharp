using System;
using System.Collections.Generic;
using SafetySharp.Modeling;

namespace ProductionCell
{
    class Agent : Component
    {
        public List<Capability> AvailableCapabilites { get; set; }
        public List<OdpRole> AllocatedRoles { get; set; }
        public Resource Resource { get; set; }
        public List<Agent> Inputs { get; set; }
        public List<Agent> Output { get; set; }
        public int Id { get; set; }
        public bool IsCart { get; set; }
    
    }
}