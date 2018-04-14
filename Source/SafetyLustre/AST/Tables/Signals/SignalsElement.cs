using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.AST.Tables.Signals
{
    class SignalsElement : Element
    {
        public List<SignalElement> Children { get; set; }

        public SignalsElement()
        {
            Children = new List<SignalElement>();
        }
    }
}
