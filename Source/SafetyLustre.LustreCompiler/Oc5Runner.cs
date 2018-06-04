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

        /// <summary>
        /// Creates an instance of Oc5Runner
        /// </summary>
        /// <param name="oc5Source">Oc5 source code to parse and run.</param>
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
