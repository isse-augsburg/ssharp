// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
// Copyright (c) 2017, Manuel Götz
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
using SafetyLustre.ANTLR;
using System;
using System.IO;

namespace SafetyLustre
{
    public class Program
    {

        static void Main(string[] args)
        {
            var oc5 = File.ReadAllText(@"C:\Users\Pascal\Source\University\ssharp\SafetyLustreTest\Examples\example1.oc");

            var inputStream = new AntlrInputStream(oc5);
            var lexer = new Oc5Lexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new Oc5Parser(tokenStream);

            var ocfile = parser.ocfile();

            //Console.WriteLine("-----------------------------------------------");
            //foreach (var token in tokenStream.GetTokens())
            //{
            //    Console.WriteLine(token.ToString() + " " + token.Type);
            //}
            //Console.WriteLine("-----------------------------------------------");
            //Console.WriteLine(ocfile.ToStringTree());
            //Console.WriteLine("-----------------------------------------------");

            var visitor = new ToStringVisitor();
            Console.WriteLine(visitor.Visit(ocfile));
            Console.ReadKey();
        }
    }
}
