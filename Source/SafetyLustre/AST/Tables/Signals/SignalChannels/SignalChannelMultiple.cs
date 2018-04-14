using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.AST.Tables.Signals.SignalChannels
{
    class SignalChannelMultiple : SignalChannel
    {
        public string VarIndex { get; set; }
        public string CombFunIndex { get; set; }
    }
}
