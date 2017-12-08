using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.RobotCell.Modeling
{
    using System.Diagnostics;
    using Controllers;
    using Odp;

    class AgentCapabilityGenerator
    {
        private readonly int _capsPerAgent;
        private readonly List<DummyCapability> _task;
        private readonly HashSet<DummyAgent> _agents;

        public AgentCapabilityGenerator(int capsPerAgent, HashSet<DummyAgent> agents, List<DummyCapability> task)
        {
            //Debug.Assert(agents.Count() == (task.Count()));

            _capsPerAgent = capsPerAgent;
            _task = task;
            _agents = agents;
        }

        public HashSet<DummyAgent> Generate()
        {
            var capsToUsage = new Dictionary<DummyCapability, int>();
            var capToAgent = new Dictionary<DummyCapability, DummyAgent>();
            foreach (var cap in _task)
                capsToUsage.Add(cap, 0);

            foreach (var anAgent in _agents)
            {
                anAgent.GetCapabilities().Clear();
                var cap = _task[anAgent.GetId() % _task.Count];
                anAgent.AddCapability(cap);
                capsToUsage[cap] = capsToUsage[cap] + 1;
//                capsToUsage.Add(cap, capsToUsage[cap] + 1);
                if (capsToUsage[cap] >= _capsPerAgent)
                    capsToUsage.Remove(cap);

                capToAgent[cap] = anAgent;
                anAgent.SetAgentTypeId(anAgent.GetId());
            }

            var remainingCaps = new HashSet<DummyCapability>(_task);
            if (_capsPerAgent > 0)
            {
                var agentsSorted = new List<DummyAgent>(_agents);
                agentsSorted.Sort((agent, dummyAgent) => (agent.GetId() < dummyAgent.GetId())
                    ? -1
                    : (agent.GetId() > dummyAgent.GetId())
                        ? 1
                        : 0);

				var rnd = new Random();
				while (true)
                {
                    var agentsToRemove = new HashSet<DummyAgent>();

                    var anAgent = agentsSorted[0];

                    while (anAgent.GetCapCount() < _capsPerAgent)
                    {
                        var remainingCapsLocal = new List<DummyCapability>(remainingCaps);
                        remainingCapsLocal.RemoveAll(capability => anAgent.GetCapabilities().Contains(capability));
						var remainingTaskCaps = _task.Except(anAgent.GetCapabilities()).ToList();

						DummyCapability cap;
						if (remainingCapsLocal.Count == 0)
						{
							var index = rnd.Next(0, remainingTaskCaps.Count - 1);
							cap = remainingTaskCaps[index];
						}
						else
						{
							var rdm = rnd.Next(0, remainingCapsLocal.Count - 1);
							cap = remainingCapsLocal[rdm];
						}

						anAgent.AddCapability(cap);
                    }

                    foreach (var cap in anAgent.GetCapabilities())
                    {
                        var capAgent = capToAgent[cap];
                        if (capAgent.Equals(anAgent) == false)
                        {
                            capAgent.GetCapabilities().Clear();
                            foreach (var c in anAgent.GetCapabilities())
                                capAgent.GetCapabilities().Add(c);
                            capAgent.SetAgentTypeId(anAgent.GetAgentTypeId());
                            agentsToRemove.Add(capAgent);
                        }
                    }

                    agentsToRemove.Add(anAgent);
                    foreach (var dummyCapability in anAgent.GetCapabilities())
                    {
                        remainingCaps.Remove(dummyCapability);
                    }
                    agentsSorted.RemoveAll(agent => agentsToRemove.Contains(agent));

                    if (agentsSorted.Count==0)
                        break;
                }
            }

            return _agents;
        }
    }
}