namespace SafetySharp.CaseStudies.RobotCell.Modeling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class DummyAgent
    {
        private HashSet<DummyAgent> inputs = new HashSet<DummyAgent>();
        private HashSet<DummyAgent> outputs = new HashSet<DummyAgent>();
        private HashSet<DummyCapability> caps = new HashSet<DummyCapability>();
        private int id = -1;
        private int agentTypeId = -1;

        public DummyAgent(int id)
        {
            this.id = id;
            this.agentTypeId = id;
        }

        public int GetId()
        {
            return id;
        }

        public void AddInput(DummyAgent anAgent)
        {
            this.inputs.Add(anAgent);
        }

        public void AddOutput(DummyAgent anAgent)
        {
            this.outputs.Add(anAgent);
        }

        public HashSet<DummyAgent> GetInputs()
        {
            return inputs;
        }

        public HashSet<DummyAgent> GetOutputs()
        {
            return outputs;
        }

        public HashSet<DummyCapability> GetCapabilities()
        {
            return caps;
        }

        public void AddCapability(DummyCapability aCap)
        {
            this.caps.Add(aCap);
        }

        public int GetCapCount()
        {
            return this.caps.Count();
        }

        public int GetIndegree()
        {
            return this.inputs.Count();
        }

        public int GetOutdegree()
        {
            return this.outputs.Count();
        }

        public int GetAgentTypeId()
        {
            return agentTypeId;
        }

        public void SetAgentTypeId(int typeId)
        {
            this.agentTypeId = typeId;
        }

        public String getName()
        {
            if (this.id == Int32.MaxValue)
                return "DrainStorage";
            else if (this.id == -2)
                return "SourceStorage";
            else if (this.id >= 0)
                return "Robo" + (this.id + 1);

            return "N/A";
        }


        public Object clone()
        {
            DummyAgent clone = new DummyAgent(this.id)
            {
                agentTypeId = this.agentTypeId,
                outputs = new HashSet<DummyAgent>(this.outputs),
                inputs = new HashSet<DummyAgent>(this.inputs),
                caps = new HashSet<DummyCapability>(this.caps)
            };

            return clone;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + id;
            return result;
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            DummyAgent other = (DummyAgent)obj;
            if (id != other.id)
                return false;
            return true;
        }

    }
}