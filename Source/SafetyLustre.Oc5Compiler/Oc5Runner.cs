using Antlr4.Runtime;
using SafetyLustre.Oc5Compiler.Visitors;
using System;

namespace SafetyLustre.Oc5Compiler
{
    public class Oc5Runner
    {
        public string Oc5Source { get; set; }
        private Oc5Model Oc5Model { get; set; }
        private Oc5State Oc5State { get; set; }

        /// <summary>
        /// Creates an instance of Oc5Runner
        /// </summary>
        /// <param name="oc5Source">Oc5 source code as string.</param>
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
            Oc5State = visitor.Oc5State;
            Console.WriteLine();
        }

        public void Tick(params object[] inputs)
        {
            //int i = -1;
            //foreach (var signal in Oc5Model.Signals.OfType<SingleInputSignal>())
            //{
            //    i++;
            //    var index = signal.VarIndex;
            //}

            //HACK
            Oc5State.Ints[0] = 1;
            Oc5State.Ints[1] = 2;

            var result = Oc5Model.States[Oc5State.CurrentState].Invoke(Oc5State);

            Console.WriteLine($"result: {result}");
            Console.WriteLine($"output: {Oc5State.Ints[2]}");
            Console.ReadKey();
        }
    }
}
