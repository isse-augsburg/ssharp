using System.Diagnostics;
using SafetySharp.Modeling;

namespace ProductionCell
{
    class Capability : Component
    {
        public readonly Type Type;

        public Capability(Type type)
        {
            this.Type = type;
        }

        void Apply(Workpiece rs)
        {
            rs.Add(this);
        }

        public static bool operator ==(Capability left, Capability right)
        {
            return left.Type == right.Type;
        }

        public static bool operator !=(Capability left, Capability right)
        {
            return left.Type != right.Type;
        }

        public override bool Equals(object obj)
        {
            return this == (Capability)obj;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public override string ToString()
        {
            if (Type == Type.None) return "";
            return Type.ToString();
        }
    }
}