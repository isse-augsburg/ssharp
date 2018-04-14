using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.AST.Tables.Signals.SignalNatures
{
    class SignalNatureOutput : SignalNature
    {
        public string Name { get; set; }
        public string OutAction { get; set; }
    }
}
