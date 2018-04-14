using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.AST.Tables.Actions
{
    class ActionsElement : Element
    {
        public List<ActionElement> Children { get; set; }

        public ActionsElement()
        {
            Children = new List<ActionElement>();
        }
    }
}
