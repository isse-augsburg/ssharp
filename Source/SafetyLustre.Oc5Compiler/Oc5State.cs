using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.Oc5Compiler
{
    class Oc5State
    {
        public List<bool> Bools { get; set; } = new List<bool>();
        public List<int> Ints { get; set; } = new List<int>();
        public List<string> Strings { get; set; } = new List<string>();
        public List<float> Floats { get; set; } = new List<float>();
        public List<double> Doubles { get; set; } = new List<double>();
        /// <summary>
        /// This List maps the variable index in the oc5 file (e.g. 0:)
        /// to a type and the index in the <see cref="Bools"/>, <see cref="Ints"/>,
        /// <see cref="Strings"/>, <see cref="Floats"/> or <see cref="Doubles"/> list.
        /// </summary>
        public List<PositionInOc5State> Mappings { get; set; } = new List<PositionInOc5State>();
        /// <summary>
        /// Represents the cureent oc5 state the model is in.
        /// </summary>
        public int CurrentState { get; set; }
    }

    struct PositionInOc5State
    {
        public PredefinedObjects.Types Type { get; set; }
        public int IndexInOc5StateList { get; set; }
    }
}
