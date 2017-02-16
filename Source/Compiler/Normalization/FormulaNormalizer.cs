// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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
	using CompilerServices;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Roslyn.Symbols;
	using Roslyn.Syntax;
	using ISSE.SafetyChecking.Formula;

	/// <summary>
	///   Normalizes all implicit conversions from a Boolean expression to a <see cref="Formula" /> by explicitly invoking the
	///   <see cref="ExecutableStateFormulaFactory.Create" /> method.
	/// </summary>
	public sealed class FormulaNormalizer : Normalizer
	{
		/// <summary>
		///   Represents the <see cref="ExecutableStateFormulaFactory" /> type.
		/// </summary>
		private INamedTypeSymbol _factoryType;

		/// <summary>
		///   Represents the <see cref="Formula" /> type.
		/// </summary>
		private INamedTypeSymbol _formulaType;

		/// <summary>
		///   Normalizes the syntax trees of the <see cref="Compilation" />.
		/// </summary>
		protected override Compilation Normalize()
		{
			_factoryType = Compilation.GetTypeSymbol(typeof(ExecutableStateFormulaFactory));
			_formulaType = Compilation.GetTypeSymbol<Formula>();

			return base.Normalize();
		}

		/// <summary>
		///   Normalizes the <paramref name="binaryExpression" />.
		/// </summary>
		public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax binaryExpression)
		{
			if (!IsFormulaType(binaryExpression))
				return base.VisitBinaryExpression(binaryExpression);

			var left = ReplaceImplicitConversion(binaryExpression.Left);
			var right = ReplaceImplicitConversion(binaryExpression.Right);
			return binaryExpression.Update(left, binaryExpression.OperatorToken, right);
		}

		/// <summary>
		///   Normalizes the <paramref name="assignment" />.
		/// </summary>
		public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax assignment)
		{
			if (!IsFormulaType(assignment.Left) || IsFormulaType(assignment.Right))
				return base.VisitAssignmentExpression(assignment);

			return assignment.WithRight(CreateInvocation(assignment.Right));
		}

		/// <summary>
		///   Normalizes the <paramref name="cast" />.
		/// </summary>
		public override SyntaxNode VisitCastExpression(CastExpressionSyntax cast)
		{
			if (!IsFormulaType(cast))
				return base.VisitCastExpression(cast);

			return CreateInvocation(cast.Expression);
		}

		/// <summary>
		///   Normalizes the <paramref name="initializer" />.
		/// </summary>
		public override SyntaxNode VisitEqualsValueClause(EqualsValueClauseSyntax initializer)
		{
			var typeInfo = SemanticModel.GetTypeInfo(initializer.Value);
			if (typeInfo.Type == null || typeInfo.Type.Equals(typeInfo.ConvertedType))
				return base.VisitEqualsValueClause(initializer);

			if (!IsFormulaType(typeInfo.ConvertedType))
				return base.VisitEqualsValueClause(initializer);

			return initializer.WithValue(CreateInvocation(initializer.Value));
		}

		/// <summary>
		///   Normalizes the <paramref name="statement" />.
		/// </summary>
		public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax statement)
		{
			var methodSymbol = SemanticModel.GetEnclosingSymbol(statement.SpanStart) as IMethodSymbol;
			if (methodSymbol == null)
				return base.VisitReturnStatement(statement);

			if (!IsFormulaType(methodSymbol.ReturnType))
				return base.VisitReturnStatement(statement);

			return statement.WithExpression(ReplaceImplicitConversion(statement.Expression));
		}

		/// <summary>
		///   Normalizes the <paramref name="expression" />.
		/// </summary>
		public override SyntaxNode VisitArrowExpressionClause(ArrowExpressionClauseSyntax expression)
		{
			var methodSymbol = SemanticModel.GetEnclosingSymbol(expression.SpanStart) as IMethodSymbol;
			if (methodSymbol == null)
				return base.VisitArrowExpressionClause(expression);

			if (!IsFormulaType(methodSymbol.ReturnType))
				return base.VisitArrowExpressionClause(expression);

			return expression.WithExpression(ReplaceImplicitConversion(expression.Expression));
		}

		/// <summary>
		///   Does not normalize <c>nameof</c> expressions.
		/// </summary>
		public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax expression)
		{
			if (expression.IsNameOfOperator(SemanticModel))
				return expression;

			return base.VisitInvocationExpression(expression);
		}

		/// <summary>
		///   Normalizes the <paramref name="argument" />.
		/// </summary>
		public override SyntaxNode VisitArgument(ArgumentSyntax argument)
		{
			// Special case for array accesses, as Roslyn doesn't return a symbol for the argument
			if (argument.Parent.Parent is ElementAccessExpressionSyntax)
			{
				var arrayExpression = ((ElementAccessExpressionSyntax)argument.Parent.Parent).Expression;
				var kind = SemanticModel.GetTypeInfo(arrayExpression).Type.TypeKind;
				if (kind == TypeKind.Array || kind == TypeKind.Pointer)
					return base.VisitArgument(argument);
			}

			var parameterSymbol = argument.GetParameterSymbol(SemanticModel);
			if (parameterSymbol.RefKind != RefKind.None)
				return base.VisitArgument(argument);

			var arraySymbol = parameterSymbol.Type as IArrayTypeSymbol;
			var isParamsFormula = arraySymbol != null && parameterSymbol.IsParams && IsFormulaType(arraySymbol.ElementType);

			if (!isParamsFormula && !IsFormulaType(parameterSymbol.Type))
				return base.VisitArgument(argument);

			return argument.WithExpression(ReplaceImplicitConversion(argument.Expression));
		}

		/// <summary>
		///   Checks whether <paramref name="expression" /> is implicitly converted to <see cref="Formula" />.
		///   If so, replaces the implicit conversion by an invocation of the corresponding state expression factory method.
		/// </summary>
		private ExpressionSyntax ReplaceImplicitConversion(ExpressionSyntax expression)
		{
			var expressionType = SemanticModel.GetTypeInfo(expression).Type;

			if (expressionType == null || expressionType.IsDerivedFrom(_formulaType))
				return (ExpressionSyntax)Visit(expression);

			var conversion = SemanticModel.ClassifyConversion(expression, _formulaType);
			if (conversion.IsUserDefined && conversion.IsImplicit)
				return CreateInvocation((ExpressionSyntax)Visit(expression));

			return (ExpressionSyntax)Visit(expression);
		}

		/// <summary>
		///   Creates the invocation of the factory function for the <paramref name="expression" />.
		/// </summary>
		private ExpressionSyntax CreateInvocation(ExpressionSyntax expression)
		{
			var type = Syntax.TypeExpression(_factoryType);
			var memberAccess = Syntax.MemberAccessExpression(type, Syntax.IdentifierName(nameof(ExecutableStateFormulaFactory.Create)));
			var lambdaExpression = Syntax.ValueReturningLambdaExpression(expression);
			var invocation = Syntax.InvocationExpression(memberAccess, lambdaExpression);
			return (ExpressionSyntax)invocation;
		}

		/// <summary>
		///   Classifies the type of the <paramref name="expression" />.
		/// </summary>
		private bool IsFormulaType(ExpressionSyntax expression)
		{
			return IsFormulaType(SemanticModel.GetTypeInfo(expression).Type);
		}

		/// <summary>
		///   Classifies the type of the <paramref name="expressionType" />.
		/// </summary>
		private bool IsFormulaType(ITypeSymbol expressionType)
		{
			return expressionType.Equals(_formulaType);
		}
	}
}