using System.Linq;

namespace ProductionCell
{
    class DumpObserverController : ObserverController
    {
        public override void Reconfigure()
        {
//            var availableCaps = Agents.SelectMany(agent => agent.AvailableCapabilites).Distinct();
//            bool reconfPossible = !CurrentTasks.RequiresCapabilities.Except(availableCaps).Any();
//            if (reconfPossible)
//            {
//                foreach (var agent in Agents)
//                {
//                    RolePool.AddRange(agent.AllocatedRoles);
//                    agent.AllocatedRoles.Clear();
//                    foreach (var requiredCapbility in CurrentTasks.RequiresCapabilities)
//                    {
//                        if (agent.AvailableCapabilites.Contains(requiredCapbility))
//                        {
//                            OdpRole roleToAllocate = RolePool.First();
//                            RolePool.Remove(roleToAllocate);
//                            roleToAllocate.CapabilitiesToApply.Clear();
//                            roleToAllocate.CapabilitiesToApply.Add(agent.AvailableCapabilites.Single(c => c.Equals(requiredCapbility)));
//
//                            agent.AllocatedRoles.Add(roleToAllocate);
//                            return;
//                        }
//                    }
//                }
//            }
//            else
//            {
//                foreach (var agent in Agents)
//                {
//                    agent.AllocatedRoles.Clear();
//                }
//                //throw new NotSupportedException();
//            }
        }
    }
}