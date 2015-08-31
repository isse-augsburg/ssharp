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
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="IFieldSymbol" /> instances.
	/// </summary>
	public static class FieldSymbolExtensions
	{
		/// <summary>
		///   Checks whether the type of the given field implements the component interface.
		///   Note that it is sufficient to check whether the type implements IComponent, as all
		///   Component derived classes implement IComponent as well.
		/// </summary>
		[Pure]
		private static bool IsSubcomponentField([NotNull] IFieldSymbol fieldSymbol, [NotNull] ITypeSymbol componentInterfaceSymbol)
		{
			return fieldSymbol.Type.IsDerivedFrom(componentInterfaceSymbol) || fieldSymbol.Type.Equals(componentInterfaceSymbol);
		}

		/// <summary>
		///   Checks whether <paramref name="fieldSymbol" /> is a subcomponent field.
		/// </summary>
		/// <param name="fieldSymbol">The field symbol that should be checked.</param>
		/// <param name="compilation">The compilation that should be used to resolve symbol information.</param>
		[Pure]
		public static bool IsSubcomponentField([NotNull] this IFieldSymbol fieldSymbol, [NotNull] Compilation compilation)
		{
			Requires.NotNull(fieldSymbol, nameof(fieldSymbol));
			Requires.NotNull(compilation, nameof(compilation));

			return IsSubcomponentField(fieldSymbol, compilation.GetComponentInterfaceSymbol());
		}

		/// <summary>
		///   Checks whether <paramref name="fieldSymbol" /> is a subcomponent field.
		/// </summary>
		/// <param name="fieldSymbol">The field symbol that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve symbol information.</param>
		[Pure]
		public static bool IsSubcomponentField([NotNull] this IFieldSymbol fieldSymbol, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(fieldSymbol, nameof(fieldSymbol));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return IsSubcomponentField(fieldSymbol, semanticModel.GetComponentInterfaceSymbol());
		}
	}
}