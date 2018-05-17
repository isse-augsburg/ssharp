using Antlr4.Runtime;
using SafetyLustre.Oc5Compiler.Oc5Objects;
using SafetyLustre.Oc5Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

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

        public void Tick<T1, T2>(T1 input1, T2 input2)
        {
            int i = -1;
            foreach (var signal in Oc5Model.Signals.OfType<SingleInputSignal>())
            {
                i++;
                var index = signal.VarIndex;
            }

            var state = Oc5Model.States[Oc5State.CurrentState];

            switch (Oc5Model.Variables.Count)
            {
                case 4:
                    var arg1 = false;
                    var arg2 = input1;
                    var arg3 = input2;
                    var arg4 = 0;
                    Oc5State.CurrentState = CompileAndInvoke(state, Oc5Model.Variables, ref arg1, ref arg2, ref arg3, ref arg4);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        #region Delegates

        delegate int FuncRef<T1>(ref T1 arg1);
        delegate int FuncRef<T1, T2>(ref T1 arg1, ref T2 arg2);
        delegate int FuncRef<T1, T2, T3>(ref T1 arg1, ref T2 arg2, ref T3 arg3);
        delegate int FuncRef<T1, T2, T3, T4>(ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4);
        delegate int FuncRef<T1, T2, T3, T4, T5>(ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5);
        delegate int FuncRef<T1, T2, T3, T4, T5, T6>(ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6);
        delegate int FuncRef<T1, T2, T3, T4, T5, T6, T7>(ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7);
        delegate int FuncRef<T1, T2, T3, T4, T5, T6, T7, T8>(ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8);
        delegate int FuncRef<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9);
        delegate int FuncRef<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10);
        delegate int FuncRef<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10, ref T11 arg11);
        delegate int FuncRef<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10, ref T11 arg11, ref T12 arg12);
        delegate int FuncRef<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10, ref T11 arg11, ref T12 arg12, ref T13 arg13);
        delegate int FuncRef<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10, ref T11 arg11, ref T12 arg12, ref T13 arg13, ref T14 arg14);
        delegate int FuncRef<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10, ref T11 arg11, ref T12 arg12, ref T13 arg13, ref T14 arg14, ref T15 arg15);
        delegate int FuncRef<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10, ref T11 arg11, ref T12 arg12, ref T13 arg13, ref T14 arg14, ref T15 arg15, ref T16 arg16);

        #endregion

        #region CompileAndInvoke Methods

        private int CompileAndInvoke<T1>(Expression expression, IEnumerable<ParameterExpression> parameters, ref T1 arg1)
        {
            return Expression.Lambda<FuncRef<T1>>(expression, parameters)
                    .Compile().Invoke(ref arg1);
        }
        private int CompileAndInvoke<T1, T2>(Expression expression, IEnumerable<ParameterExpression> parameters, ref T1 arg1, ref T2 arg2)
        {
            return Expression.Lambda<FuncRef<T1, T2>>(expression, parameters)
                    .Compile().Invoke(ref arg1, ref arg2);
        }

        private int CompileAndInvoke<T1, T2, T3>(Expression expression, IEnumerable<ParameterExpression> parameters, ref T1 arg1, ref T2 arg2, ref T3 arg3)
        {
            return Expression.Lambda<FuncRef<T1, T2, T3>>(expression, parameters)
                    .Compile().Invoke(ref arg1, ref arg2, ref arg3);
        }

        private int CompileAndInvoke<T1, T2, T3, T4>(Expression expression, IEnumerable<ParameterExpression> parameters, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4)
        {
            return Expression.Lambda<FuncRef<T1, T2, T3, T4>>(expression, parameters)
                    .Compile().Invoke(ref arg1, ref arg2, ref arg3, ref arg4);
        }

        #endregion
    }
}
