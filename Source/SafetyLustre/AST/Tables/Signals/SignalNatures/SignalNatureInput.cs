using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.AST.Tables.Signals.SignalNatures
{
    class SignalNatureInput : SignalNature
    {
        public string Name { get; set; }
        public string PresentAction { get; set; }
    }
}
