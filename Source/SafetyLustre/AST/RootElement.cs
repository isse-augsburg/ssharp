using SafetyLustre.AST.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.AST
{
    class RootElement : Element
    {
        public VersionElement Version { get; set; }
        public ModuleElement Module { get; set; }
    }
}
