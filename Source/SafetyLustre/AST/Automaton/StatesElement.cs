using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.AST.Automaton
{
    class StatesElement : Element
    {
        public List<StateElement> Children { get; set; }

        public StatesElement()
        {
            Children = new List<StateElement>();
        }
    }
}
