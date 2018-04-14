using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SafetyLustre.AST.Tables.Signals.SignalBools;
using SafetyLustre.AST.Tables.Signals.SignalChannels;
using SafetyLustre.AST.Tables.Signals.SignalNatures;

namespace SafetyLustre.AST.Tables.Signals
{
    class SignalElement : Element
    {
        public SignalNature Nature { get; set; }
        public SignalChannel Channel { get; set; }
        public SignalBool Bool { get; set; }
    }
}
