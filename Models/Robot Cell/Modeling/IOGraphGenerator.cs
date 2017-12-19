using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.RobotCell.Modeling
{
    class IOGraphGenerator
    {
        private int _inOutDegree;
        private readonly HashSet<DummyAgent> _agents;
        private int _ioProbability = 25;
		private readonly Random _rnd;

        public IOGraphGenerator(int inOutDegree, HashSet<DummyAgent> agents, Random rnd)
        {
            this._inOutDegree = inOutDegree;
            this._agents = this.GetFreshAgentSetWithoutIOs(agents);
			_rnd = rnd;
		}

        private HashSet<DummyAgent> GetFreshAgentSetWithoutIOs(HashSet<DummyAgent> setToClone)
        {
            // clone agents
            var cloneSet = new HashSet<DummyAgent>();
            foreach (var anAgent in setToClone)
            {
                var clone = (DummyAgent)anAgent.clone();
                anAgent.GetOutputs().Clear();
                anAgent.GetInputs().Clear();
                cloneSet.Add(clone);
            }

            return cloneSet;
        }

        public HashSet<DummyAgent> Generate()
        {
            var agentTypeIdToInAgentTypeIds = new Dictionary<int, HashSet<int>>();
            var agentTypeIdToOutAgentTypeIds = new Dictionary<int, HashSet<int>>();
            var agentIdToAgent = new Dictionary<int, DummyAgent>();
            var agentTypeIdToAgents = new Dictionary<int, HashSet<DummyAgent>>();

            // build agent id to agent and agent type id to agents map
            foreach (var anAgent in _agents)
            {
                // agent id to agent map
                agentIdToAgent.Add(anAgent.GetId(), anAgent);

                // agent type id to agents map
                if (agentTypeIdToAgents.ContainsKey(anAgent.GetAgentTypeId()) == false)
                {
                    var tempSet = new HashSet<DummyAgent> { anAgent };
                    agentTypeIdToAgents.Add(anAgent.GetAgentTypeId(), tempSet);
                }
                else
                {
                    agentTypeIdToAgents[anAgent.GetAgentTypeId()].Add(anAgent);
                }
            }

            // build IOs for initial resource-flow (without Source- and DrainStorage) and agent type id to agent type ids map
            var agentsSorted = new List<DummyAgent>(_agents);
            /*if (o1.getId() < o2.getId())
                return -1;
            else if (o1.getId() > o2.getId())
                return 1;

            return 0;*/
            agentsSorted.Sort((agent, dummyAgent) => (agent.GetId() < dummyAgent.GetId())
                ? -1
                : (agent.GetId() > dummyAgent.GetId())
                    ? 1
                    : 0);
            foreach (var anAgent in agentsSorted)
            {
                var id = anAgent.GetId();

                if (id < _agents.Count - 1)
                {
                    var outAgent = agentIdToAgent[id + 1];
                    outAgent.AddInput(anAgent);
                    anAgent.AddOutput(outAgent);

                    // agent type id to out-agent type ids map
                    if (agentTypeIdToOutAgentTypeIds.ContainsKey(anAgent.GetAgentTypeId()) == false)
                    {
                        var tempSet = new HashSet<int> { outAgent.GetAgentTypeId() };
                        agentTypeIdToOutAgentTypeIds.Add(anAgent.GetAgentTypeId(), tempSet);
                    }
                    else
                    {
                        agentTypeIdToOutAgentTypeIds[anAgent.GetAgentTypeId()].Add(outAgent.GetAgentTypeId());
                    }
                    // agent type id to in-agent type ids map
                    if (agentTypeIdToInAgentTypeIds.ContainsKey(outAgent.GetAgentTypeId()) == false)
                    {
                        var tempSet = new HashSet<int>();
                        tempSet.Add(anAgent.GetAgentTypeId());
                        agentTypeIdToInAgentTypeIds.Add(outAgent.GetAgentTypeId(), tempSet);
                    }
                    else
                    {
                        agentTypeIdToInAgentTypeIds[outAgent.GetAgentTypeId()].Add(anAgent.GetAgentTypeId());
                    }
                }
            }

            // create links to other agents
            if (_inOutDegree > 1)
            {
                var shuffledAgents = new List<DummyAgent>(_agents);
                Shuffle<DummyAgent>(shuffledAgents);

                // create ios to agents of the type the type of anAgent is communicating with.
                foreach (var anAgent in shuffledAgents)
                {
                    var agentTypeId = anAgent.GetAgentTypeId();

                    foreach (var tempAgentTypeId in agentTypeIdToOutAgentTypeIds[agentTypeId])
                    {
                        if (anAgent.GetOutdegree() >= _inOutDegree)
                            break;

                        foreach (var tempAgent in agentTypeIdToAgents[tempAgentTypeId])
                        {
                            if (anAgent.GetOutdegree() >= _inOutDegree)
                                break;

                            // anAgent must not have a link to itself
                            if (anAgent.Equals(tempAgent) == false)
                            {
                                var rdm = _rnd.Next(1, 100);
                                if (rdm <= _ioProbability && tempAgent.GetIndegree() < _inOutDegree)
                                {
                                    anAgent.AddOutput(tempAgent);
                                    tempAgent.AddInput(anAgent);
                                }
                            }
                        }
                    }

                    foreach (var tempAgentTypeId in agentTypeIdToInAgentTypeIds[agentTypeId])
                    {
                        if (anAgent.GetIndegree() >= _inOutDegree)
                            break;

                        foreach (var tempAgent in agentTypeIdToAgents[tempAgentTypeId])
                        {
                            if (anAgent.GetIndegree() >= _inOutDegree)
                                break;

                            // anAgent must not have a link to itself
                            if (anAgent.Equals(tempAgent) == false)
                            {
                                var rdm = _rnd.Next(1, 100);
                                if (rdm <= _ioProbability && tempAgent.GetOutdegree() < _inOutDegree)
                                {
                                    anAgent.AddInput(tempAgent);
                                    tempAgent.AddOutput(anAgent);
                                }
                            }
                        }
                    }
                }

                // create ios to random agents (fill ios up, as good as possible)
                Shuffle<DummyAgent>(shuffledAgents);
                foreach (var anAgent in shuffledAgents)
                {
                    var remainingOutAgents = new List<DummyAgent>(_agents);
                    remainingOutAgents.RemoveAll(agent => anAgent.GetOutputs().Contains(agent));
                    Shuffle<DummyAgent>(remainingOutAgents);

                    foreach (var tempAgent in remainingOutAgents)
                    {
                        if (anAgent.GetOutdegree() >= _inOutDegree)
                            break;

                        if (tempAgent.Equals(anAgent) == false && tempAgent.GetIndegree() < _inOutDegree)
                        {
                            anAgent.AddOutput(tempAgent);
                            tempAgent.AddInput(anAgent);
                        }
                    }

                    var remainingInAgents = new List<DummyAgent>(_agents);
                    remainingInAgents.RemoveAll(agent => anAgent.GetInputs().Contains(agent));
                    Shuffle<DummyAgent>(remainingInAgents);

                    foreach (var tempAgent in remainingInAgents)
                    {
                        if (anAgent.GetIndegree() >= _inOutDegree)
                            break;

                        if (tempAgent.Equals(anAgent) == false && tempAgent.GetOutdegree() < _inOutDegree)
                        {
                            anAgent.AddInput(tempAgent);
                            tempAgent.AddOutput(anAgent);
                        }
                    }
                }
            }

            return _agents;
        }

        private void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                int k = (_rnd.Next(0, n) % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}