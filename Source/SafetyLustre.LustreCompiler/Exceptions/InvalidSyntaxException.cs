using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.LustreCompiler.Exceptions
{
    class InvalidSyntaxException : Exception
    {
        public InvalidSyntaxException(string error, IToken token) :
            base($"Invaild Synatx: {error} (Line {token.Line}: Character {token.Column}).")
        { }

    }
}
