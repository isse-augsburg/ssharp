using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.AST.Tables.Actions
{
    class ActionElementCall : ActionElement
    {
        public string ProcedureIndex { get; set; }
        public string VariableIndexList { get; set; }
        public string ExpressionList { get; set; }
    }
}
