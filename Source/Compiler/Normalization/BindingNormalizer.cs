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
	using System.Linq;
	using CompilerServices;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Modeling;
	using Roslyn.Symbols;
	using Roslyn.Syntax;
	using Utilities;

	/// <summary>
	///   Normalizes all calls of <see cref="Component.Bind(string,string)" /> and <see cref="Component.Bind{T}(string,string)" />.
	/// 
	///   For instance:
	///   <code>
	///    		Bind(nameof(c.X), nameof(Y));
	///    		// becomes (for some matching delegate type D):
	///  		Bind((D)c.X, (D)Y);
	/// 
	///         Bind{D}(nameof(c.X), nameof(Y));
	///    		// becomes:
	///  		Bind((D)c.X, (D)Y);
	///   	</code>
	/// </summary>
	public sealed class BindingNormalizer : SyntaxNormalizer
	{
		/// <summary>
		///   Normalizes the <paramref name="statement" />.
		/// </summary>
		public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax statement)
		{
			if (!statement.Expression.IsKind(SyntaxKind.InvocationExpression))
				return statement;

			var expression = (InvocationExpressionSyntax)statement.Expression;
			var methodSymbol = expression.GetReferencedSymbol<IMethodSymbol>(SemanticModel);
			if (!methodSymbol.IsBindMethod(SemanticModel))
				return statement;

			var requiredPortReferenceExpression = (InvocationExpressionSyntax)expression.ArgumentList.Arguments[0].Expression;
			var providedPortReferenceExpression = (InvocationExpressionSyntax)expression.ArgumentList.Arguments[1].Expression;

			var requiredPorts = requiredPortReferenceExpression.ResolvePortReferences(SemanticModel);
			var providedPorts = providedPortReferenceExpression.ResolvePortReferences(SemanticModel);

			requiredPorts.RemoveWhere(symbol => !symbol.IsRequiredPort(SemanticModel));
			providedPorts.RemoveWhere(symbol => !symbol.IsProvidedPort(SemanticModel));

			if (methodSymbol.Arity == 1)
			{
				var delegateType = (INamedTypeSymbol)methodSymbol.TypeArguments[0];
				MethodSymbolFilter.Filter(requiredPorts, delegateType);
				MethodSymbolFilter.Filter(providedPorts, delegateType);
			}
			else
				MethodSymbolFilter.Filter(requiredPorts, providedPorts);

			// The analyzer guarantees that there are exactly one port in each set now, but just to be sure...
			Assert.That(requiredPorts.Count == 1, "Expected exactly one required port at this point.");
			Assert.That(providedPorts.Count == 1, "Expected exactly one provided port at this point.");
			Assert.That(requiredPorts.Single().IsRequiredPort(SemanticModel), "Expected a required port at this point.");
			Assert.That(providedPorts.Single().IsProvidedPort(SemanticModel), "Expected a provided port at this point.");

			var requiredPortExpression = requiredPortReferenceExpression.ArgumentList.Arguments[0].Expression;
			var providedPortExpression = providedPortReferenceExpression.ArgumentList.Arguments[0].Expression;

			var requiredPortReference = CreatePortReference(requiredPorts.Single(), requiredPortExpression);
			var providedPortReference = CreatePortReference(providedPorts.Single(), providedPortExpression);

			var binderType = Syntax.TypeExpression(SemanticModel.GetTypeSymbol(typeof(PortBinding)));
			var binderInstance = Syntax.ObjectCreationExpression(binderType, requiredPortReference, providedPortReference);
			var bindMemberAccess = Syntax.MemberAccessExpression(binderInstance, nameof(PortBinding.Bind));
			var invocation = (ExpressionSyntax)Syntax.InvocationExpression(bindMemberAccess);

			return statement.WithExpression(invocation).NormalizeWhitespace().WithTrivia(statement).EnsureLineCount(statement);
		}

		/// <summary>
		///   Gets the expression that creates the <see cref="PortReference" /> at runtime.
		/// </summary>
		private ExpressionSyntax CreatePortReference(IMethodSymbol methodSymbol, SyntaxNode portExpression)
		{
			var componentArg = CreatePortTargetExpression(portExpression);
			var declaringTypeArg = Syntax.TypeOfExpression(Syntax.TypeExpression(methodSymbol.ContainingType));
			var argumentTypesArg = GetParameterTypeArray(methodSymbol);
			var returnTypeArg = SyntaxFactory.TypeOfExpression((TypeSyntax)Syntax.TypeExpression(methodSymbol.ReturnType));
			var nameArg = Syntax.LiteralExpression(methodSymbol.Name);
			var isVirtualArg = CreateIsVirtualExpression(portExpression);

			var portReferenceType = SyntaxFactory.ParseTypeName(typeof(PortReference).GetGlobalName());
			return (ExpressionSyntax)Syntax.ObjectCreationExpression(portReferenceType, componentArg, declaringTypeArg,
				nameArg, argumentTypesArg, returnTypeArg, isVirtualArg);
		}

		/// <summary>
		///   Creates the expression that refers to the port target.
		/// </summary>
		private SyntaxNode CreatePortTargetExpression(SyntaxNode portExpression)
		{
			var nestedMemberAccess = portExpression.RemoveParentheses() as MemberAccessExpressionSyntax;
			if (nestedMemberAccess == null)
				return Syntax.ThisExpression();

			if (nestedMemberAccess.Expression.IsKind(SyntaxKind.BaseExpression))
				return Syntax.ThisExpression();

			return nestedMemberAccess.Expression;
		}

		/// <summary>
		///   Creates the expression that indicates whether the port is invoked virtually or non-virtually.
		/// </summary>
		private SyntaxNode CreateIsVirtualExpression(SyntaxNode portExpression)
		{
			return Syntax.LiteralExpression(
				!portExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression) ||
				!((MemberAccessExpressionSyntax)portExpression).Expression.IsKind(SyntaxKind.BaseExpression));
		}

		/// <summary>
		///   Gets the parameter type array that can be used to retrieve the <paramref name="methodSymbol" /> via reflection.
		/// </summary>
		private SyntaxNode GetParameterTypeArray(IMethodSymbol methodSymbol)
		{
			var typeExpressions = methodSymbol.Parameters.Select(p =>
			{
				var typeofExpression = SyntaxFactory.TypeOfExpression((TypeSyntax)Syntax.TypeExpression(p.Type));
				if (p.RefKind == RefKind.None)
					return typeofExpression;

				var makeRefType = Syntax.MemberAccessExpression(typeofExpression, "MakeByRefType");
				return (ExpressionSyntax)Syntax.InvocationExpression(makeRefType);
			});

			var arguments = SyntaxFactory.SeparatedList(typeExpressions);
			var initialize = SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, arguments);
			var arrayType = Syntax.ArrayTypeExpression(SyntaxFactory.ParseTypeName(typeof(Type).FullName));
			return SyntaxFactory.ArrayCreationExpression((ArrayTypeSyntax)arrayType, initialize);
		}
	}
}