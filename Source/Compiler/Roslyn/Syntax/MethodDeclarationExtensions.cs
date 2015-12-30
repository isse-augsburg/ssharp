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

namespace SafetySharp.Compiler.Roslyn.Syntax
{
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="MethodDeclarationSyntax" /> instances.
	/// </summary>
	public static class MethodDeclarationExtensions
	{
		/// <summary>
		///   Gets the <see cref="IMethodSymbol" /> declared by <paramref name="methodDeclaration" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <param name="methodDeclaration">The method declaration the declared symbol should be returned for.</param>
		/// <param name="semanticModel">The semantic model that should be used to determine the declared symbol.</param>
		[Pure, NotNull]
		public static IMethodSymbol GetMethodSymbol([NotNull] this BaseMethodDeclarationSyntax methodDeclaration,
													[NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(methodDeclaration, nameof(methodDeclaration));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			var symbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
			Assert.NotNull(symbol, $"Unable to determine method symbol of method declaration '{methodDeclaration}'.");

			return symbol;
		}

		/// <summary>
		///   Gets the line number where the <paramref name="methodDeclaration" />'s body begins.
		/// </summary>
		/// <param name="methodDeclaration">The method declaration the body line number should be returned for.</param>
		[Pure]
		public static int GetBodyLineNumber([NotNull] this MethodDeclarationSyntax methodDeclaration)
		{
			Requires.NotNull(methodDeclaration, nameof(methodDeclaration));

			if (methodDeclaration.ExpressionBody != null)
				return methodDeclaration.ExpressionBody.Expression.GetLineNumber();

			if (methodDeclaration.Body != null)
				return methodDeclaration.Body.GetLineNumber();

			return -1;
		}
	}
}