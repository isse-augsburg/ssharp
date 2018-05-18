using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SafetyLustre.Oc5Compiler
{
    internal static class PredefinedObjects
    {
        #region Types

        internal enum Types
        {
            _boolean, _integer, _string, _float, _double
        }

        internal static Type GetType(int typeIndex)
        {
            switch ((Types)typeIndex)
            {
                case Types._boolean:
                    return typeof(bool);
                case Types._integer:
                    return typeof(int);
                case Types._string:
                    return typeof(string);
                case Types._float:
                    return typeof(float);
                case Types._double:
                    return typeof(double);
                default:
                    throw new ArgumentException($"No predefined type with index {typeIndex}!");
            }
        }

        #endregion

        #region Constants

        internal enum Constants
        {
            _false, _true
        }

        internal static ConstantExpression GetConstantExpression(int constantIndex)
        {
            switch ((Constants)constantIndex)
            {
                case Constants._true:
                    return Expression.Constant(true);
                case Constants._false:
                    return Expression.Constant(false);
                default:
                    throw new ArgumentException($"No predefined constant with index {constantIndex}!");
            }
        }

        internal static ConstantExpression GetConstantExpressionOfType(int typeIndex)
        {
            switch ((Types)typeIndex)
            {
                case Types._boolean:
                    return Expression.Constant(default(bool));
                case Types._integer:
                    return Expression.Constant(default(int));
                case Types._string:
                    return Expression.Constant(default(string));
                case Types._float:
                    return Expression.Constant(default(float));
                case Types._double:
                    return Expression.Constant(default(double));
                default:
                    throw new ArgumentException($"No predefined type with index {typeIndex}!");
            }
        }

        #endregion

        #region Variables

        internal static Expression GetVariableExpression(int typeIndex, Oc5ModelState oc5State, ParameterExpression oc5StateParameterExpression)
        {
            var type = (Types)typeIndex;
            switch (type)
            {
                case Types._boolean:
                    oc5State.Bools.Add(default(bool));
                    var boolIndex = oc5State.Bools.Count - 1;
                    oc5State.Mappings.Add(new PositionInOc5State { Type = type, IndexInOc5StateList = boolIndex });
                    var boolsExpression = Expression.Property(oc5StateParameterExpression, "Bools");
                    return Expression.Property(boolsExpression, "Item", Expression.Constant(boolIndex));
                case Types._integer:
                    oc5State.Ints.Add(default(int));
                    var intIndex = oc5State.Ints.Count - 1;
                    oc5State.Mappings.Add(new PositionInOc5State { Type = type, IndexInOc5StateList = intIndex });
                    var intsExpression = Expression.Property(oc5StateParameterExpression, "Ints");
                    return Expression.Property(intsExpression, "Item", Expression.Constant(intIndex));
                case Types._string:
                    oc5State.Strings.Add(default(string));
                    var stringIndex = oc5State.Strings.Count - 1;
                    oc5State.Mappings.Add(new PositionInOc5State { Type = type, IndexInOc5StateList = stringIndex });
                    var stringsExpression = Expression.Property(oc5StateParameterExpression, "Strings");
                    return Expression.Property(stringsExpression, "Item", Expression.Constant(stringIndex));
                case Types._float:
                    oc5State.Floats.Add(default(float));
                    var floatIndex = oc5State.Floats.Count - 1;
                    oc5State.Mappings.Add(new PositionInOc5State { Type = type, IndexInOc5StateList = floatIndex });
                    var floatsExpression = Expression.Property(oc5StateParameterExpression, "Floats");
                    return Expression.Property(floatsExpression, "Item", Expression.Constant(floatIndex));
                case Types._double:
                    oc5State.Doubles.Add(default(double));
                    var doubleIndex = oc5State.Doubles.Count - 1;
                    oc5State.Mappings.Add(new PositionInOc5State { Type = type, IndexInOc5StateList = doubleIndex });
                    var doublesExpression = Expression.Property(oc5StateParameterExpression, "Doubles");
                    return Expression.Property(doublesExpression, "Item", Expression.Constant(doubleIndex));
                default:
                    throw new ArgumentException($"No predefined type with index {typeIndex}!");
            }
        }

        #endregion

        #region Functions

        internal enum Functions
        {
            _eq__boolean,
            _ne__boolean,
            _cond__boolean,
            _or__boolean,
            _and__boolean,
            _not__boolean,

            _eq__integer,
            _ne__integer,
            _cond__integer,
            _lt__integer,
            _le__integer,
            _gt__integer,
            _ge__integer,
            _plus__integer,
            _minus__inetger,
            _times__integer,
            _mod__integer,
            _div_inetger,
            _uminus__integer,
            _logor__integer,
            _lognot__integer_binary,
            _lognot__integer_unary,

            _eq__string,
            _ne__string,
            _cond_string,

            _eq__float,
            _ne__float,
            _cond__float,
            _lt__float,
            _le__float,
            _gt__float,
            _ge__float,
            _plus__float,
            _minus__float,
            _times__float,
            _div__float,
            _uminus__float,
            _float,

            _eq__double,
            _ne__double,
            _cond__double,
            _lt__double,
            _le__double,
            _gt__double,
            _ge__double,
            _plus__double,
            _minus__double,
            _times__double,
            _div__double,
            _uminus__double
        }

        private static HashSet<Functions> UnaryFunctions = new HashSet<Functions> { Functions._not__boolean, Functions._lognot__integer_unary, Functions._float, Functions._uminus__integer, Functions._uminus__float, Functions._uminus__double };

        private static HashSet<Functions> TernaryFunctions = new HashSet<Functions> { Functions._cond__boolean, Functions._cond__integer, Functions._cond_string, Functions._cond__float, Functions._cond__double, };

        internal static Expression GetFunctionExpression(int functionIndex, params Expression[] expressions)
        {
            if (!Enum.IsDefined(typeof(Functions), functionIndex))
                throw new ArgumentException($"No predefined function with index { functionIndex }!");

            var func = (Functions)functionIndex;

            if (UnaryFunctions.Contains(func))
            {
                if (expressions.Count() != 1)
                    throw new InvalidOperationException($"{func} needs exactly 1 expression!");
                return GetUnaryFunction(func, expressions[0]);
            }
            else if (TernaryFunctions.Contains(func))
            {
                if (expressions.Count() != 3)
                    throw new InvalidOperationException($"{func} needs exactly 3 expressions!");
                return GetTernaryFunction(func, expressions[0], expressions[1], expressions[2]);
            }
            else
            {
                if (expressions.Count() != 2)
                    throw new InvalidOperationException($"{func} needs exactly 2 expressions!");
                return GetBinaryFunction(func, expressions[0], expressions[1]);
            }
        }

        private static Expression GetUnaryFunction(Functions func, Expression expression)
        {
            switch (func)
            {
                case Functions._not__boolean:
                case Functions._lognot__integer_unary:
                    return Expression.Not(expression);
                case Functions._float:
                    return Expression.Convert(expression, typeof(float));
                case Functions._uminus__integer:
                case Functions._uminus__float:
                case Functions._uminus__double:
                    return Expression.Multiply(expression, Expression.Constant(-1));
                default:
                    throw new InvalidOperationException($"'{func}' is not a unary expression!");
            }
        }

        private static ConditionalExpression GetTernaryFunction(Functions func, Expression test, Expression ifTrue, Expression ifFalse)
        {
            switch (func)
            {
                case Functions._cond__boolean:
                case Functions._cond__integer:
                case Functions._cond_string:
                case Functions._cond__float:
                case Functions._cond__double:
                    return Expression.Condition(test, ifTrue, ifFalse);
                default:
                    throw new InvalidOperationException($"'{func}' is not a ternary expression!");
            }
        }

        private static BinaryExpression GetBinaryFunction(Functions func, Expression left, Expression right)
        {
            switch (func)
            {
                case Functions._eq__boolean:
                case Functions._eq__integer:
                case Functions._eq__string:
                case Functions._eq__float:
                case Functions._eq__double:
                    return Expression.Equal(left, right);
                case Functions._ne__boolean:
                case Functions._ne__integer:
                case Functions._ne__string:
                case Functions._ne__float:
                case Functions._ne__double:
                    return Expression.NotEqual(left, right);
                case Functions._or__boolean:
                case Functions._logor__integer:
                    return Expression.Or(left, right);
                case Functions._and__boolean:
                    return Expression.And(left, right);
                case Functions._lt__integer:
                case Functions._lt__float:
                case Functions._lt__double:
                    return Expression.LessThan(left, right);
                case Functions._le__integer:
                case Functions._le__float:
                case Functions._le__double:
                    return Expression.LessThanOrEqual(left, right);
                case Functions._gt__integer:
                case Functions._gt__float:
                case Functions._gt__double:
                    return Expression.GreaterThan(left, right);
                case Functions._ge__integer:
                case Functions._ge__float:
                case Functions._ge__double:
                    return Expression.GreaterThanOrEqual(left, right);
                case Functions._plus__integer:
                case Functions._plus__float:
                case Functions._plus__double:
                    return Expression.Add(left, right);
                case Functions._minus__inetger:
                case Functions._minus__float:
                case Functions._minus__double:
                    return Expression.Subtract(left, right);
                case Functions._times__integer:
                case Functions._times__float:
                case Functions._times__double:
                    return Expression.Multiply(left, right);
                case Functions._mod__integer:
                    return Expression.Modulo(left, right);
                case Functions._div_inetger:
                case Functions._div__float:
                case Functions._div__double:
                    return Expression.Divide(left, right);
                case Functions._lognot__integer_binary:
                    return Expression.And(Expression.Not(left), Expression.Not(right));
                default:
                    throw new InvalidOperationException($"'{func}' is not a binary expression!");
            }
        }

        #endregion

        #region Actions

        internal enum Actions
        {
            present, @if, dsz, call, output
        }

        internal static Expression GetAction(int actionIndex, Expression expression)
        {
            switch ((Actions)actionIndex)
            {
                case Actions.present:
                    return GetPresentAction(expression);
                case Actions.@if:
                    return GetIfAction(expression);
                case Actions.dsz:
                    return GetDszAction(expression);
                case Actions.call:
                    throw new NotImplementedException();
                case Actions.output:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentException($"No predefined action with index { actionIndex }!");
            }
        }

        internal static Expression GetPresentAction(Expression expression)
        {
            //var nullExpression = Expression.Constant(null, expression.Type);
            //return Expression.NotEqual(expression, nullExpression);
            //TODO actually evaluate if signal is present
            return Expression.Constant(true);
        }

        internal static Expression GetIfAction(Expression expression)
        {
            return Expression.IsTrue(expression);
        }

        internal static Expression GetDszAction(Expression expression)
        {
            var decrement = Expression.Decrement(expression);
            var zeroExpression = Expression.Constant(0);
            return Expression.LessThanOrEqual(decrement, zeroExpression);
        }

        #endregion

        #region Procedures

        internal enum Procedures
        {
            _assign__boolean,
            _assign__integer,
            _assign__string,
            _assign__float,
            _assign__double
        }
        internal static Expression GetProcedure(int procedureIndex, Expression left, Expression right)
        {
            switch ((Procedures)procedureIndex)
            {
                case Procedures._assign__boolean:
                case Procedures._assign__integer:
                case Procedures._assign__string:
                case Procedures._assign__float:
                case Procedures._assign__double:
                    return Expression.Assign(left, right);
                default:
                    throw new ArgumentException($"No predefined procedure with index { procedureIndex }!");
            }
        }

        #endregion
    }
}
