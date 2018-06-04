using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.LustreCompiler
{
    public class Oc5ErrorListener : BaseErrorListener
    {
        public String OffendingSymbol { get; private set; }
        public StringWriter Writer { get; private set; }

        public Oc5ErrorListener(StringWriter writer)
        {
            Writer = writer;
        }

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            //base.SyntaxError(recognizer, offendingSymbol, line, charPositionInLine, msg, e);

            Writer.WriteLine(msg);

            OffendingSymbol = offendingSymbol.Text;
        }
    }
}
