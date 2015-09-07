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
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Roslyn.Symbols;
	using Roslyn.Syntax;

	/// <summary>
	///   Replaces all expression-bodied members with regular statement-based members.
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
	public class ExpressionBodyNormalizer : SyntaxNormalizer
	{
		/// <summary>
		///   The symbol of the method that is being normalized.
		/// </summary>
		private IMethodSymbol _methodSymbol;

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax declaration)
		{
			var originalDeclaration = declaration;
			if (declaration.ExpressionBody == null)
				return declaration;

			_methodSymbol = declaration.GetMethodSymbol(SemanticModel);
			if (!_methodSymbol.ContainingType.OriginalDefinition.IsDerivedFromComponent(SemanticModel))
				return declaration;

			var statements = AsStatementBody(declaration.ExpressionBody.Expression);
			
			declaration = declaration.WithBody(statements).WithExpressionBody(null);

			var leadingTrivia = declaration.SemicolonToken.LeadingTrivia;
			var trailingTrivia = declaration.SemicolonToken.TrailingTrivia;
			declaration = declaration.WithSemicolonToken(default(SyntaxToken)).WithTrailingTrivia(leadingTrivia.AddRange(trailingTrivia));
			return declaration.EnsureLineCount(originalDeclaration);
		}

		/// <summary>
		///   Converts the <paramref name="expression" /> into a statement body.
		/// </summary>
		private BlockSyntax AsStatementBody(ExpressionSyntax expression)
		{
			StatementSyntax body;

			if (_methodSymbol.ReturnsVoid)
				body = SyntaxFactory.ExpressionStatement(expression);
			else
			{
				var returnStatement = SyntaxFactory.ReturnStatement(expression);
				var returnKeyword = returnStatement.ReturnKeyword.WithTrailingSpace();

				body = returnStatement.WithReturnKeyword(returnKeyword);
			}

			var column = expression.GetLocation().GetLineSpan().StartLinePosition.Character;
			body = body.WithLeadingSpace(column).PrependLineDirective(expression.GetLineNumber()).AppendLineDirective(-1);

			var block = SyntaxFactory.Block(body).PrependLineDirective(-1).AppendLineDirective(-1);
			block = block.WithOpenBraceToken(block.OpenBraceToken.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));
			block = block.WithCloseBraceToken(block.CloseBraceToken.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));
			return block;
		}
	}
}