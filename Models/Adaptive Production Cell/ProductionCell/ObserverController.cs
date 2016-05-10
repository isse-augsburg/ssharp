using System.Collections.Generic;
using SafetySharp.Modeling;

namespace ProductionCell
{
    abstract class ObserverController : Component
    {
        public List<Agent> Agents { get; set; }
        public Task CurrentTasks { get; set; }
        protected List<OdpRole> RolePool = new List<OdpRole>(10); 

        public abstract void Reconfigure();
        public override void Update()
        {
            base.Update();
            Reconfigure();
        }

        protected ObserverController()
        {
            for (int i = 0; i < 10; i++)
            {
                RolePool.Add(new OdpRole() {CapabilitiesToApply =  new List<Capability>(1)});
            }
        }
    }
}