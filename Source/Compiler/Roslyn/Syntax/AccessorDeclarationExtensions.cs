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
	///   Provides extension methods for working with <see cref="AccessorDeclarationSyntax" /> instances.
	/// </summary>
	public static class AccessorDeclarationExtensions
	{
		/// <summary>
		///   Gets the <see cref="IMethodSymbol" /> declared by <paramref name="accessorDeclaration" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <param name="accessorDeclaration">The accessor declaration the declared symbol should be returned for.</param>
		/// <param name="semanticModel">The semantic model that should be used to determine the declared symbol.</param>
		[Pure, NotNull]
		public static IMethodSymbol GetMethodSymbol([NotNull] this AccessorDeclarationSyntax accessorDeclaration,
													[NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(accessorDeclaration, nameof(accessorDeclaration));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			var symbol = semanticModel.GetDeclaredSymbol(accessorDeclaration);
			Assert.NotNull(symbol, $"Unable to determine method symbol of accessor declaration '{accessorDeclaration}'.");

			return symbol;
		}

		/// <summary>
		///   Replaces the <paramref name="accessorDeclaration" />'s body with the given <paramref name="statements" />.
		/// </summary>
		/// <param name="accessorDeclaration">The accessor declaration whose body should be replaced.</param>
		/// <param name="statements">The new body of the method.</param>
		[Pure, NotNull]
		public static AccessorDeclarationSyntax WithStatementBody([NotNull] this AccessorDeclarationSyntax accessorDeclaration,
																  [NotNull] BlockSyntax statements)
		{
			Requires.NotNull(accessorDeclaration, nameof(accessorDeclaration));
			Requires.NotNull(statements, nameof(statements));

			statements = statements.WithOpenBraceToken(statements.OpenBraceToken.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));
			accessorDeclaration = accessorDeclaration.WithBody(statements);

			if (accessorDeclaration.SemicolonToken.Kind() == SyntaxKind.None)
				return accessorDeclaration;

			var leadingTrivia = accessorDeclaration.SemicolonToken.LeadingTrivia;
			var trailingTrivia = accessorDeclaration.SemicolonToken.TrailingTrivia;
			return accessorDeclaration.WithSemicolonToken(SyntaxFactory.Token(leadingTrivia, SyntaxKind.None, trailingTrivia));
		}
	}
}