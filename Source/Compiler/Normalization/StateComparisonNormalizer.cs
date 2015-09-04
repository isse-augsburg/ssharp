// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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

namespace SafetySharp.Compiler.Normalization
{
	using System;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Modeling;
	using Roslyn.Symbols;

	/// <summary>
	///   Normalizes all invocations of <see cref="StateMachine.operator==(StateMachine, IConvertible)" /> and
	///   <see cref="StateMachine.operator==(IConvertible, StateMachine)" /> to
	///   <see cref="StateMachine.operator==(StateMachine, int)" /> and
	///   <see cref="StateMachine.operator==(int, StateMachine)" />,
	///   respectively.
	/// </summary>
	public sealed class StateComparisonNormalizer : SyntaxNormalizer
	{
		/// <summary>
		///   Represents the <see cref="StateMachine" /> type.
		/// </summary>
		private INamedTypeSymbol _stateMachineType;

		/// <summary>
		///   Normalizes the syntax trees of the <see cref="Compilation" />.
		/// </summary>
		protected override Compilation Normalize()
		{
			_stateMachineType = Compilation.GetTypeSymbol(typeof(StateMachine));
			return base.Normalize();
		}

		/// <summary>
		///   Normalizes the <paramref name="expression" />.
		/// </summary>
		public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax expression)
		{
			expression = (BinaryExpressionSyntax)base.VisitBinaryExpression(expression);
			var methodSymbol = SemanticModel.GetSymbolInfo(expression).Symbol as IMethodSymbol;

			if (methodSymbol == null || methodSymbol.MethodKind != MethodKind.UserDefinedOperator)
				return expression;

			if (methodSymbol.Name != "op_Equality" && methodSymbol.Name != "op_Inequality")
				return expression;

			if (!methodSymbol.ContainingType.Equals(_stateMachineType))
				return expression;

			// Convert the left expression if it is not the state machine
			if (!methodSymbol.Parameters[0].Type.Equals(_stateMachineType))
				return expression.WithLeft(Convert(expression.Left));

			// Otherwise, we have to convert the right expression
			return expression.WithRight(Convert(expression.Right));
		}

		/// <summary>
		///   Converts the <paramref name="expression" /> to <see cref="int" />.
		/// </summary>
		private ExpressionSyntax Convert(ExpressionSyntax expression)
		{
			return (ExpressionSyntax)Syntax.CastExpression(Syntax.TypeExpression(SpecialType.System_Int32), expression);
		}
	}
}