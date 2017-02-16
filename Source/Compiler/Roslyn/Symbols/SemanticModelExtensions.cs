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

namespace SafetySharp.Compiler.Roslyn.Symbols
{
	using System;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using ISSE.SafetyChecking.Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="SemanticModel" /> instances.
	/// </summary>
	public static class SemanticModelExtensions
	{
		/// <summary>
		///   Gets the <see cref="INamedTypeSymbol" /> representing <typeparamref name="T" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <typeparam name="T">The type the symbol should be returned for.</typeparam>
		/// <param name="semanticModel">The semantic model the type symbol should be returned for.</param>
		[Pure, NotNull]
		public static INamedTypeSymbol GetTypeSymbol<T>([NotNull] this SemanticModel semanticModel)
		{
			Requires.NotNull(semanticModel, nameof(semanticModel));
			return semanticModel.Compilation.GetTypeByMetadataName(typeof(T).FullName);
		}

		/// <summary>
		///   Gets the <see cref="INamedTypeSymbol" /> representing <paramref name="type" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <param name="semanticModel">The semantic model the type symbol should be returned for.</param>
		/// <param name="type">The type the symbol should be returned for.</param>
		[Pure, NotNull]
		public static INamedTypeSymbol GetTypeSymbol([NotNull] this SemanticModel semanticModel, [NotNull] Type type)
		{
			Requires.NotNull(semanticModel, nameof(semanticModel));
			Requires.NotNull(type, nameof(type));

			return semanticModel.Compilation.GetTypeByMetadataName(type.FullName);
		}
	}
}