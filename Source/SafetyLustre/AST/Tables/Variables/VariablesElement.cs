using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.AST.Tables.Variables
{
    class VariablesElement : Element
    {
        public List<VariableElement> Children { get; set; }

        public VariablesElement()
        {
            Children = new List<VariableElement>();
        }
    }
}
