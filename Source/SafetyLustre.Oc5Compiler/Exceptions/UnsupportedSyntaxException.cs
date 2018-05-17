using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.Oc5Compiler.Exceptions
{
    class UnsupportedSyntaxException : Exception
    {
        public UnsupportedSyntaxException(string unsupportedSyntax, IToken token) :
            base(unsupportedSyntax + $" currently not supported (Line {token.Line}: Character {token.Column}).")
        { }
    }
}
