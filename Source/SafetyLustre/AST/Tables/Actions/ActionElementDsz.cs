using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.AST.Tables.Actions
{
    /// <summary>
    /// Decrement and skip on zero
    /// </summary>
    class ActionElementDsz : ActionElement
    {
        public string VariableIndex { get; set; }
    }
}
