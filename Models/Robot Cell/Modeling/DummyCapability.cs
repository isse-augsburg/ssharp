namespace SafetySharp.CaseStudies.RobotCell.Modeling
{
    using System;

    public class DummyCapability
    {
        private readonly String _id;

        public DummyCapability(String id)
        {
            this._id = id;
        }

        public String PrintCap()
        {
            return this._id;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + ((_id == null) ? 0 : _id.GetHashCode());
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
            DummyCapability other = (DummyCapability)obj;
            if (_id == null)
            {
                if (other._id != null)
                    return false;
            }
            else if (!_id.Equals(other._id))
                return false;
            return true;
        }
    }
}