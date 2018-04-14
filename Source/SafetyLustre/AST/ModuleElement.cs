using SafetyLustre.AST.Automaton;
using SafetyLustre.AST.Tables;

namespace SafetyLustre.AST
{
    class ModuleElement : Element
    {
        public string Name { get; set; }
        public TablesElement Tables { get; set; }
        public AutomatonElement Automaton { get; set; }
    }
}
