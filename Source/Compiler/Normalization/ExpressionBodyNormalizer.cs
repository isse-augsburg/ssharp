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
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Roslyn.Symbols;
	using Roslyn.Syntax;

	/// <summary>
	///   Replaces all expression-bodied members of fault effects with regular statement-based ones.
	/// 
	///   For instance:
	///   <code>
	///     	public int X() => 1;
	///     	// becomes:
	///     	public int X() { return 1; }
	///     	
	///     	[A] bool I.X(bool b) => !b;
	///     	// becomes:
	///     	[A] bool I.X(bool b) { return !b; }
	///    	</code>
	/// </summary>
	public class ExpressionBodyNormalizer : Normalizer
	{
		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax declaration)
		{
			// Nothing to do here for methods without expression bodies
			if (declaration.ExpressionBody == null)
				return declaration;

			// Nothing to do here for methods not defined in fault effects or for methods that are no overrides of some port
			var methodSymbol = declaration.GetMethodSymbol(SemanticModel);
			if (!methodSymbol.ContainingType.IsFaultEffect(SemanticModel) || !methodSymbol.IsOverride)
				return declaration;

			var originalDeclaration = declaration;
			var statements = AsStatementBody(methodSymbol, declaration.ExpressionBody.Expression);

			declaration = declaration.WithSemicolonToken(default(SyntaxToken)).WithExpressionBody(null);
			return declaration.WithBody(statements).EnsureLineCount(originalDeclaration);
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax declaration)
		{
			// Nothing to do here for properties without expression bodies
			if (declaration.ExpressionBody == null)
				return declaration;

			// Nothing to do here for properties not defined in fault effects or for properties that are no overrides of some port
			var propertySymbol = declaration.GetPropertySymbol(SemanticModel);
			if (!propertySymbol.ContainingType.IsFaultEffect(SemanticModel) || !propertySymbol.IsOverride)
				return declaration;

			var originalDeclaration = declaration;
			var statements = AsStatementBody(propertySymbol.GetMethod, declaration.ExpressionBody.Expression);

			var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, statements);
			var accessorList = SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getter));
			declaration = declaration.WithAccessorList(accessorList);

			declaration = declaration.WithExpressionBody(null).WithSemicolonToken(default(SyntaxToken));
			return declaration.EnsureLineCount(originalDeclaration);
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax declaration)
		{
			// Nothing to do here for properties without expression bodies
			if (declaration.ExpressionBody == null)
				return declaration;

			// Nothing to do here for indexers not defined in fault effects or for indexers that are no overrides of some port
			var propertySymbol = declaration.GetPropertySymbol(SemanticModel);
			if (!propertySymbol.ContainingType.IsFaultEffect(SemanticModel) || !propertySymbol.IsOverride)
				return declaration;

			var originalDeclaration = declaration;
			var statements = AsStatementBody(propertySymbol.GetMethod, declaration.ExpressionBody.Expression);

			var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, statements);
			var accessorList = SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getter));
			declaration = declaration.WithAccessorList(accessorList);

			declaration = declaration.WithExpressionBody(null).WithSemicolonToken(default(SyntaxToken));
			return declaration.EnsureLineCount(originalDeclaration);
		}

		/// <summary>
		///   Converts the <paramref name="expression" /> body into a statement body.
		/// </summary>
		private static BlockSyntax AsStatementBody(IMethodSymbol methodSymbol, ExpressionSyntax expression)
		{
			StatementSyntax body;

			if (methodSymbol.ReturnsVoid)
				body = SyntaxFactory.ExpressionStatement(expression);
			else
			{
				var returnStatement = SyntaxFactory.ReturnStatement(expression);
				var returnKeyword = returnStatement.ReturnKeyword.WithTrailingSpace();

				body = returnStatement.WithReturnKeyword(returnKeyword);
			}

			body = body.EnsureIndentation(expression).PrependLineDirective(expression.GetLineNumber()).AppendLineDirective(-1);
			var block = SyntaxFactory.Block(body).PrependLineDirective(-1).AppendLineDirective(-1);
			block = block.WithOpenBraceToken(block.OpenBraceToken.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));
			block = block.WithCloseBraceToken(block.CloseBraceToken.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));
			return block;
		}
	}
}