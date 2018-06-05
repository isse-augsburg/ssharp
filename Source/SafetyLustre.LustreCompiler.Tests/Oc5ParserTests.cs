// The MIT License (MIT)
// 
// Copyright (c) 2014-2018, Institute for Software & Systems Engineering
// Copyright (c) 2018, Pascal Pfeil
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Antlr4.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace SafetyLustre.LustreCompiler.Tests
{
    [TestClass]
    public class Oc5ParserTests
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
        public void TestParseConstant(string value)
        {
            Arrange(value);

            var context = parser.constant();

            Assert.AreEqual((parser.InputStream as CommonTokenStream)?.Get(0).Type, Oc5Lexer.LIST_INDEX);
            Assert.AreEqual((parser.InputStream as CommonTokenStream)?.Get(1).Type, Oc5Lexer.IDENTIFIER);
            Assert.IsNotNull(context.index());
            AssertParsingOk();
        }

        [DataTestMethod]
        [DataRow(@"constants: 1 0: abc 5 end:", 1)]
        [DataRow(@"constants: 2 0: PI $3 1: BUF_SIZE $1 end:", 2)]
        public void TestParseConstants(string value, int constantCount)
        {
            Arrange(value);

            var context = parser.constants();

            Assert.AreEqual((parser.InputStream as CommonTokenStream)?.Get(0).Type, Oc5Lexer.CONSTANTS);
            Assert.AreEqual((parser.InputStream as CommonTokenStream)?.Get(1).Type, Oc5Lexer.NUMBER);
            Assert.AreEqual(context.constant().Length, constantCount);
            Assert.AreEqual((parser.InputStream as CommonTokenStream)?.GetTokens().Reverse().Skip(1).First().Type, Oc5Lexer.ENDTABLE);
            AssertParsingOk();
        }
    }
}
