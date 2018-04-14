using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.AST.Tables.Variables
{
    class VariableElement : Element
    {
        public string TypeIndex { get; set; }
        public string InitialValue { get; set; }
    }
}
