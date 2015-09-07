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

namespace SafetySharp.Compiler.Roslyn.Syntax
{
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="ExpressionSyntax" /> instances.
	/// </summary>
	public static class ExpressionSyntaxExtensions
	{
		/// <summary>
		///   Gets the <see cref="ITypeSymbol" /> representing the type of <paramref name="syntaxNode" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <param name="syntaxNode">The expression the type should be returned for.</param>
		/// <param name="semanticModel">The semantic model that should be used for semantic analysis.</param>
		[Pure, NotNull]
		public static ITypeSymbol GetExpressionType([NotNull] this ExpressionSyntax syntaxNode, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			var symbol = syntaxNode.GetReferencedSymbol(semanticModel);

			var parameterSymbol = symbol as IParameterSymbol;
			var localSymbol = symbol as ILocalSymbol;
			var fieldSymbol = symbol as IFieldSymbol;
			var propertySymbol = symbol as IPropertySymbol;
			var methodSymbol = symbol as IMethodSymbol;

			if (parameterSymbol != null)
				return parameterSymbol.Type;

			if (localSymbol != null)
				return localSymbol.Type;

			if (fieldSymbol != null)
				return fieldSymbol.Type;

			if (propertySymbol != null)
				return propertySymbol.Type;

			if (methodSymbol != null)
				return methodSymbol.ReturnType;

			Requires.That(false, "Failed to determine the type of the referenced symbol.");
			return null;
		}

		/// <summary>
		///   Converts the <paramref name="expression" /> into a statement body.
		/// </summary>
		/// <param name="expression">The expression the statements should be returned for.</param>
		/// <param name="returnType">The type symbol corresponding to the method's return type.</param>
		[Pure, NotNull]
		public static BlockSyntax AsStatementBody([NotNull] this ExpressionSyntax expression, [NotNull] ITypeSymbol returnType)
		{
			Requires.NotNull(expression, nameof(expression));
			Requires.NotNull(returnType, nameof(returnType));

			StatementSyntax body;

			if (returnType.SpecialType == SpecialType.System_Void)
				body = SyntaxFactory.ExpressionStatement(expression);
			else
			{
				var returnStatement = SyntaxFactory.ReturnStatement(expression);
				var returnKeyword = returnStatement.ReturnKeyword.WithTrailingSpace();

				body = returnStatement.WithReturnKeyword(returnKeyword);
			}

			var column = expression.GetLocation().GetLineSpan().StartLinePosition.Character;
			body = body.WithLeadingSpace(column).PrependLineDirective(expression.GetLineNumber());
			return SyntaxFactory.Block(body);
		}
	}
}