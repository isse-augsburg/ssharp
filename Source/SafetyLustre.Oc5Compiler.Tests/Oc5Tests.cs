using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Antlr4.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static SafetyLustre.Oc5Compiler.Oc5Parser;

namespace SafetyLustre.Oc5Compiler.Tests
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

            Assert.AreEqual((parser.InputStream as CommonTokenStream)?.Get(0).Type, Oc5Lexer.LIST_INDEX);
            Assert.AreEqual((parser.InputStream as CommonTokenStream)?.Get(1).Type, Oc5Lexer.IDENTIFIER);
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

            Assert.AreEqual((parser.InputStream as CommonTokenStream)?.Get(0).Type, Oc5Lexer.CONSTANTS);
            Assert.AreEqual((parser.InputStream as CommonTokenStream)?.Get(1).Type, Oc5Lexer.NUMBER);
            Assert.AreEqual(context.constant().Length, constantCount);
            Assert.AreEqual((parser.InputStream as CommonTokenStream)?.GetTokens().Reverse().Skip(1).First().Type, Oc5Lexer.ENDTABLE);
            AssertParsingOk();
        }

        [TestMethod]
        public void TestListVariableAccess()
        {
            var valuation = new List<int>() { 1, 3, 5, 7, 9 };
            var index = 3;
            Console.WriteLine("-----------");
            Console.WriteLine(String.Join("\t", valuation));
            Console.WriteLine("-----------");
            var valuationExpression = Expression.Constant(valuation);
            //ref: https://stackoverflow.com/questions/31924907/accessing-elements-of-types-with-indexers-using-expression-trees/31925006#31925006
            //ref: https://docs.microsoft.com/de-de/dotnet/api/system.collections.generic.list-1.item?view=netframework-4.7#System_Collections_Generic_List_1_Item_System_Int32
            var valuationPropertyExpression = Expression.Property(valuationExpression, "Item", Expression.Constant(index));
            var ex = Expression.Assign(valuationPropertyExpression, Expression.Constant(99999));
            Expression.Lambda<Func<int>>(ex).Compile().Invoke();
            Console.WriteLine(String.Join("\t", valuation));
        }

        [TestMethod]
        public void TestArrayVariableAccess()
        {
            var valuation = new List<int>() { 1, 3, 5, 7, 9 }.ToArray();
            var index = 3;
            Console.WriteLine("-----------");
            Console.WriteLine(String.Join("\t", valuation));
            Console.WriteLine("-----------");
            var valuationExpression = Expression.Constant(valuation);
            var arrayIndexExpression = Expression.ArrayAccess(valuationExpression, Expression.Constant(index));
            var ex = Expression.Assign(arrayIndexExpression, Expression.Constant(99999));
            Expression.Lambda<Func<int>>(ex).Compile().Invoke();
            Console.WriteLine(String.Join("\t", valuation));
        }

        delegate void ActionRef<T>(ref T obj);
        delegate void ActionRef<T1, T2>(ref T1 arg1, ref T2 arg2);

        [TestMethod]
        public void TestSingleVariableAccess()
        {
            int value = 3;
            Console.WriteLine("-----------");
            Console.WriteLine(value);
            Console.WriteLine("-----------");
            var valuationExpression = Expression.Parameter(typeof(int).MakeByRefType());
            var ex = Expression.Assign(valuationExpression, Expression.Constant(99999));

            Expression.Lambda<ActionRef<int>>(ex, valuationExpression).Compile().Invoke(ref value);
            Console.WriteLine(value);
        }

        [TestMethod]
        public void TestMultiVariableAccess()
        {
            int value = 3;
            double value2 = 2;

            Console.WriteLine("-----------");
            Console.WriteLine($"{value}, {value2}");
            Console.WriteLine("-----------");

            var param1 = Expression.Parameter(typeof(int).MakeByRefType());
            var param2 = Expression.Parameter(typeof(double).MakeByRefType());

            var ex1 = Expression.Assign(param1, Expression.Constant(99999));
            var ex2 = Expression.Assign(param2, Expression.Constant(99999.0));
            var exBlock = Expression.Block(ex1, ex2);

            Func(ex1,
                param1,
                ref value);

            Console.WriteLine($"{value}, {value2}");
            Console.WriteLine("-----------");

            Func(exBlock,
                new List<ParameterExpression> { param1, param2 },
                ref value, ref value2);

            Console.WriteLine($"{value}, {value2}");
        }

        private void Func<T>(Expression expression, ParameterExpression parameter, ref T obj)
        {
            Expression.Lambda<ActionRef<T>>(expression, parameter)
                    .Compile().Invoke(ref obj);
        }

        private void Func<T1, T2>(Expression expression, IEnumerable<ParameterExpression> parameters, ref T1 arg1, ref T2 arg2)
        {
            Expression.Lambda<ActionRef<T1, T2>>(expression, parameters)
                .Compile().Invoke(ref arg1, ref arg2);
        }

        internal class MyClass
        {
            public int MyProperty { get; set; }
        }

        [TestMethod]
        public void TestObjectVariableAccess()
        {
            var myObj = new MyClass { MyProperty = 4 };
            Console.WriteLine("-----------");
            Console.WriteLine(myObj.MyProperty);
            Console.WriteLine("-----------");
            var valuationExpression = Expression.Property(
                Expression.Constant(myObj),
                "MyProperty"
            );
            var ex = Expression.Assign(valuationExpression, Expression.Constant(99999));

            Expression.Lambda<Func<int>>(ex).Compile().Invoke();
            Console.WriteLine(myObj.MyProperty);
        }
    }
}
