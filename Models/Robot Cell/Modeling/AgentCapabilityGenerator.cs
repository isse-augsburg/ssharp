// The MIT License (MIT)
//
// Copyright (c) 2014-2018, Institute for Software & Systems Engineering
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace SafetySharp.CaseStudies.RobotCell.Modeling
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	internal class AgentCapabilityGenerator
    {
        private readonly int _capsPerAgent;
        private readonly List<DummyCapability> _task;
        private readonly HashSet<DummyAgent> _agents;
		private readonly Random _rnd;

        public AgentCapabilityGenerator(int capsPerAgent, HashSet<DummyAgent> agents, List<DummyCapability> task, Random rnd)
        {
            Debug.Assert(agents.Count == task.Count);

            _capsPerAgent = capsPerAgent;
            _task = task;
            _agents = agents;
			_rnd = rnd;
		}

        public HashSet<DummyAgent> Generate()
		{
			var capsToUsage = _task.ToDictionary(cap => cap, cap => 0);
			var capToAgent = new Dictionary<DummyCapability, DummyAgent>();

			// Assign each capability to at least one agent (# capabilities == # agents)
			foreach (var anAgent in _agents)
			{
				var cap = _task[anAgent.GetId()];

				anAgent.GetCapabilities().Clear();
                anAgent.AddCapability(cap);
				anAgent.SetAgentTypeId(anAgent.GetId());

				capsToUsage[cap]++;
                if (capsToUsage[cap] >= _capsPerAgent)
                    capsToUsage.Remove(cap);

                capToAgent.Add(cap, anAgent);
            }

			// Assign further capabilities to agents.
			if (_capsPerAgent > 0)
                FillCapabilityRequirements(capToAgent);

			var avg = _agents.Select(a => a.GetCapabilities().Count).Average();
			var capAvg = _task.Select(cap => _agents.Count(ag => ag.GetCapabilities().Contains(cap))).Average();
			Console.WriteLine($"Avg: {avg} -- capsPerAgent: {_capsPerAgent} -- agentsPerCap AVG: {capAvg}");


            return _agents;
        }

		private void FillCapabilityRequirements(Dictionary<DummyCapability, DummyAgent> capToAgent)
		{
			var remainingCaps = new HashSet<DummyCapability>(_task);
			var agentsSorted = _agents.OrderBy(agent => agent.GetId()).ToList();

			while (agentsSorted.Count > 0)
			{
				var anAgent = agentsSorted[0];
				agentsSorted.RemoveAt(0);

				// Assign the agent the prescribed number of capabilities, while avoiding assigning the same capability twice to the same agent.
				var availableCapabilities = remainingCaps.Where(cap => !anAgent.GetCapabilities().Contains(cap)).ToList();
				while (anAgent.GetCapCount() < _capsPerAgent && availableCapabilities.Count > 0)
				{
					var rdm = _rnd.Next(0, availableCapabilities.Count - 1);
					var cap = availableCapabilities[rdm];

					anAgent.AddCapability(cap);
					availableCapabilities.RemoveAt(rdm);
				}

				foreach (var cap in anAgent.GetCapabilities())
				{
					var capAgent = capToAgent[cap];
					if (!capAgent.Equals(anAgent)) // If anAgent has a capability for which it is not the "assigned agent":
					{
						// Let the "assigned agent" capAgent have the same capabilities (and type id) as anAgent.
						capAgent.GetCapabilities().Clear();
						capAgent.GetCapabilities().UnionWith(anAgent.GetCapabilities());
						capAgent.SetAgentTypeId(anAgent.GetAgentTypeId());

						// It is not necessary to assign further capabilities to capAgent, as it has just received all it needs.
						agentsSorted.Remove(capAgent);
					}
				}

				// Do not assign the capabilities assigned to anAgent to any more agents. (why?)
				remainingCaps.ExceptWith(anAgent.GetCapabilities());
			}
		}
    }
}