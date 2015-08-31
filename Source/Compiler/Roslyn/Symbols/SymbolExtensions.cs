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

namespace SafetySharp.Compiler.Roslyn.Symbols
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="ISymbol" /> instances.
	/// </summary>
	public static class SymbolExtensions
	{
		/// <summary>
		///   Checks whether <paramref name="symbol" /> is marked with <paramref name="attribute" />.
		/// </summary>
		/// <param name="symbol">The symbol that should be checked.</param>
		/// <param name="attribute">The symbol of the attribute that <paramref name="symbol" /> should be marked with.</param>
		[Pure]
		public static bool HasAttribute([NotNull] this ISymbol symbol, [NotNull] INamedTypeSymbol attribute)
		{
			Requires.NotNull(symbol, nameof(symbol));
			Requires.NotNull(attribute, nameof(attribute));

			return symbol.GetAttributes().Any(a => Equals(a.AttributeClass, attribute));
		}

		/// <summary>
		///   Checks whether <paramref name="symbol" /> is marked with an attribute of type <typeparamref name="T" /> within the
		///   context of the <paramref name="compilation" />.
		/// </summary>
		/// <typeparam name="T">The type of the attribute.</typeparam>
		/// <param name="symbol">The symbol that should be checked.</param>
		/// <param name="compilation">The compilation that should be used to resolve the type symbol for <typeparamref name="T" />.</param>
		[Pure]
		public static bool HasAttribute<T>([NotNull] this ISymbol symbol, [NotNull] Compilation compilation)
			where T : Attribute
		{
			Requires.NotNull(symbol, nameof(symbol));
			Requires.NotNull(compilation, nameof(compilation));

			return symbol.HasAttribute(compilation.GetTypeSymbol<T>());
		}

		/// <summary>
		///   Checks whether <paramref name="symbol" /> is marked with an attribute of type <typeparamref name="T" /> within the
		///   context of the <paramref name="semanticModel" />.
		/// </summary>
		/// <typeparam name="T">The type of the attribute.</typeparam>
		/// <param name="symbol">The symbol that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve the type symbol for <typeparamref name="T" />.</param>
		[Pure]
		public static bool HasAttribute<T>([NotNull] this ISymbol symbol, [NotNull] SemanticModel semanticModel)
			where T : Attribute
		{
			Requires.NotNull(symbol, nameof(symbol));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return symbol.HasAttribute(semanticModel.GetTypeSymbol<T>());
		}

		/// <summary>
		///   Gets all instance of <paramref name="attribute" /> that <paramref name="symbol" /> is marked with.
		/// </summary>
		/// <param name="symbol">The symbol that should be checked.</param>
		/// <param name="attribute">The symbol of the attribute that <paramref name="symbol" /> should be marked with.</param>
		[Pure]
		public static IEnumerable<AttributeData> GetAttributes([NotNull] this ISymbol symbol, [NotNull] INamedTypeSymbol attribute)
		{
			Requires.NotNull(symbol, nameof(symbol));
			Requires.NotNull(attribute, nameof(attribute));

			return symbol.GetAttributes().Where(a => Equals(a.AttributeClass, attribute));
		}

		/// <summary>
		///   Gets all attribute instance of <typeparamref name="T" /> that <paramref name="symbol" /> is marked with.
		/// </summary>
		/// <typeparam name="T">The type of the attribute.</typeparam>
		/// <param name="symbol">The symbol that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve the type symbol for <typeparamref name="T" />.</param>
		[Pure]
		public static IEnumerable<AttributeData> GetAttributes<T>([NotNull] this ISymbol symbol, [NotNull] SemanticModel semanticModel)
			where T : Attribute
		{
			Requires.NotNull(symbol, nameof(symbol));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return symbol.GetAttributes(semanticModel.GetTypeSymbol<T>());
		}
	}
}