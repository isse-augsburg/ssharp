using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.RobotCell.Modeling
{
    public class TestSystemGenerator
    {

        public Tuple<HashSet<DummyAgent>, List<DummyCapability>> Generate(int sysSize, int capCount, int ioCount, Random rnd)
        {
            // generate agents
            var agentSet = new HashSet<DummyAgent>();
            for (var i = 0; i < sysSize; i++)
                agentSet.Add(new DummyAgent(i));

            // generate task
            var task = new List<DummyCapability>();
            for (var i = 0; i < sysSize; i++)
            {
                var p1 = "";
                var p0 = ((char)('A' + (i % 26))).ToString();

                if (i >= 26)
                {
                    p1 = ((char)('A' + (int)(i / 26) - 1)).ToString();
                }

                task.Add(new DummyCapability(p1 + p0));
            }

            // Will hold the result
            HashSet<DummyAgent> agents = null;

            // generate capabilities
            var agentCapGenerator = new AgentCapabilityGenerator(capCount, agentSet, task, rnd);

            agents = agentCapGenerator.Generate();

            
            // generate ios
            var iogGenerator = new IOGraphGenerator(ioCount, agents, rnd);
            agents = iogGenerator.Generate();

            return new Tuple<HashSet<DummyAgent>,List<DummyCapability>>(agents, task);
        }

    }
}
