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
using SafetyLustre.LustreCompiler.Visitors;
using System;

namespace SafetyLustre.LustreCompiler
{
    public class Oc5Runner
    {
        public string Oc5Source { get; set; }
        internal Oc5Model Oc5Model { get; set; }
        internal Oc5ModelState Oc5ModelState { get; set; }

        public Oc5Runner(string oc5Source)
        {
            Oc5Source = oc5Source;

            var inputStream = new AntlrInputStream(oc5Source);
            var lexer = new Oc5Lexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new Oc5Parser(tokenStream);

            var ocfileContext = parser.ocfile();

            var visitor = new CompileVisitor(ocfileContext);
            Oc5Model = visitor.Oc5Model;
            Oc5ModelState = visitor.Oc5ModelState;
        }

        public void Tick(params object[] inputs)
        {
            Oc5ModelState.AssignInputs(inputs);

            Oc5ModelState.CurrentState = Oc5Model.Oc5States[Oc5ModelState.CurrentState].Invoke(Oc5ModelState);
        }
    }
}
