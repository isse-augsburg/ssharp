using SafetyLustre.AST.Tables.Actions;
using SafetyLustre.AST.Tables.Constants;
using SafetyLustre.AST.Tables.Signals;
using SafetyLustre.AST.Tables.Variables;

namespace SafetyLustre.AST.Tables
{
    class TablesElement
    {
        //instances:
        //types:
        //constants:
        public ConstantsElement Constants { get; set; }
        //functions:
        //procedures:
        //signals:
        public SignalsElement Signals { get; set; }
        //implications:
        //exclusions:
        //variables:
        public VariablesElement Variables { get; set; }
        //tasks:
        //execs:
        //actions:
        public ActionsElement Actions { get; set; }
        //halts:
    }
}
