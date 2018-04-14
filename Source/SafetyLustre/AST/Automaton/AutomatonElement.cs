using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.AST.Automaton
{
    class AutomatonElement : Element
    {
        public StatesElement States { get; set; }
        public StartpointElement Startpoint { get; set; }
        public CallsElement Calls{ get; set; }
    }
}
