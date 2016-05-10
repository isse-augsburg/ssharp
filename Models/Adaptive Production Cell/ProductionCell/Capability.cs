using System.Diagnostics;
using SafetySharp.Modeling;

namespace ProductionCell
{
    class Capability : Component
    {
        readonly Type _type;

        public Capability(Type type)
        {
            this._type = type;
        }

        void Apply(Workpiece rs)
        {
            rs.Add(this);
        }

        public static bool operator ==(Capability left, Capability right)
        {
            return left._type == right._type;
        }

        public static bool operator !=(Capability left, Capability right)
        {
            return left._type != right._type;
        }

        public override bool Equals(object obj)
        {
            return this == (Capability)obj;
        }

        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }

        public override string ToString()
        {
            if (_type == Type.None) return "";
            return _type.ToString();
        }
    }
}