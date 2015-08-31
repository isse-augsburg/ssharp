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
	///   Provides extension methods for working with <see cref="VariableDeclaratorSyntax" /> instances.
	/// </summary>
	public static class VariableDeclaratorExtensions
	{
		/// <summary>
		///   Gets the symbol declared by <paramref name="variableDeclarator" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <typeparam name="T">The expected type of the declared symbol.</typeparam>
		/// <param name="variableDeclarator">The variable declarator the declared symbol should be returned for.</param>
		/// <param name="semanticModel">The semantic model that should be used to determine the declared symbol.</param>
		[Pure, NotNull]
		public static T GetDeclaredSymbol<T>([NotNull] this VariableDeclaratorSyntax variableDeclarator, [NotNull] SemanticModel semanticModel)
			where T : class, ISymbol
		{
			Requires.NotNull(variableDeclarator, nameof(variableDeclarator));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			var symbol = semanticModel.GetDeclaredSymbol(variableDeclarator);
			Requires.That(symbol != null, $"Unable to determine symbol of variable declaration '{variableDeclarator}'.");

			var typedSymbol = symbol as T;
			Requires.That(typedSymbol != null, $"Expected a symbol of type '{typeof(T).FullName}'. However, the actual symbol type for " +
											   $"syntax node '{variableDeclarator}' is '{symbol.GetType().FullName}'.");

			return typedSymbol;
		}

		/// <summary>
		///   Gets the <see cref="ITypeSymbol" /> of the variable declared by the <see cref="variableDeclarator" />.
		/// </summary>
		/// <param name="variableDeclarator">The variable declarator the type symbol should be returned for.</param>
		/// <param name="semanticModel">The semantic model that should be used to determine the declared symbol.</param>
		[Pure]
		public static ITypeSymbol GetVariableType([NotNull] this VariableDeclaratorSyntax variableDeclarator,
												  [NotNull] SemanticModel semanticModel)
		{
			var declaredSymbol = variableDeclarator.GetDeclaredSymbol<ISymbol>(semanticModel);
			var fieldSymbol = declaredSymbol as IFieldSymbol;
			if (fieldSymbol != null)
				return fieldSymbol.Type;

			var localSymbol = declaredSymbol as ILocalSymbol;
			if (localSymbol != null)
				return localSymbol.Type;

			Assert.NotReached("Unable to determine the type of the declared variable.");
			return null;
		}
	}
}