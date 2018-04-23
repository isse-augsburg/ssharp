using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static SafetyLustre.ANTLR.Oc5Parser;

namespace SafetyLustre.ANTLR.Tests
{
    [TestClass]
    public class Oc5Tests
    {
        private Oc5Parser parser;
        private Oc5Lexer lexer;
        private Oc5ErrorListener errorListener;

        private void Arrange(string input)
        {
            var inputStream = new AntlrInputStream(input);
            lexer = new Oc5Lexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);
            parser = new Oc5Parser(commonTokenStream);

            StringWriter writer = new StringWriter();
            errorListener = new Oc5ErrorListener(writer);
            lexer.RemoveErrorListeners();
            //lexer.addErrorListener(errorListener);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);
        }

        private void AssertParsingOk()
        {
            //Parser is at EOF - Parser consumed all tokens
            Assert.AreEqual(parser.CurrentToken.Type, -1);

            //No Symbol caused a syntax error - OffendingSymbol is null
            Assert.IsNull(errorListener.OffendingSymbol);
        }

        [DataTestMethod]
        [DataRow("0: abc 5")]
        [DataRow("1: PI $3")]
        [DataRow("2: BUF_SIZE $1")]
        public void TestConstant(string value)
        {
            Arrange(value);

            var context = parser.constant();

            Assert.AreEqual((parser.InputStream as CommonTokenStream).Get(0).Type, Oc5Lexer.LIST_INDEX);
            Assert.AreEqual((parser.InputStream as CommonTokenStream).Get(1).Type, Oc5Lexer.IDENTIFIER);
            Assert.IsNotNull(context.index());
            AssertParsingOk();
        }

        [DataTestMethod]
        [DataRow(@"constants: 1 0: abc 5 end:", 1)]
        [DataRow(@"constants: 2 0: PI $3 1: BUF_SIZE $1 end:", 2)]
        public void TestConstants(string value, int constantCount)
        {
            Arrange(value);

            var context = parser.constants();

            Assert.AreEqual((parser.InputStream as CommonTokenStream).Get(0).Type, Oc5Lexer.CONSTANTS);
            Assert.AreEqual((parser.InputStream as CommonTokenStream).Get(1).Type, Oc5Lexer.NUMBER);
            Assert.AreEqual(context.constant().Length, constantCount);
            Assert.AreEqual((parser.InputStream as CommonTokenStream).GetTokens().Reverse().Skip(1).First().Type, Oc5Lexer.ENDTABLE);
            AssertParsingOk();
        }
    }
}
