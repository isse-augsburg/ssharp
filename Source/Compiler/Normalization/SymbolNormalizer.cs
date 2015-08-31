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
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	/// <summary>
	///   A base class for symbol-based C# normalizers that normalize certain C# language features.
	/// </summary>
	public abstract class SymbolNormalizer : Normalizer
	{
		/// <summary>
		///   Normalizes the type symbols declared by the <see cref="Compilation" />.
		/// </summary>
		protected override sealed Compilation Normalize()
		{
			foreach (var type in Compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type).OfType<INamedTypeSymbol>())
				NormalizeTypeSymbol(type);

			return Compilation;
		}

		/// <summary>
		///   Normalizes the <paramref name="typeSymbol" />.
		/// </summary>
		/// <param name="typeSymbol">The type symbol that should be normalized.</param>
		protected abstract void NormalizeTypeSymbol(INamedTypeSymbol typeSymbol);

		/// <summary>
		///   Adds a compilation unit containing a part of the partial <paramref name="type" /> containing the
		///   <paramref name="members" />.
		/// </summary>
		/// <param name="type">The type the part should be declared for.</param>
		/// <param name="members">The members that should be added to the type.</param>
		protected void AddMembers([NotNull] INamedTypeSymbol type, [NotNull] params MemberDeclarationSyntax[] members)
		{
			AddMembers(type, new UsingDirectiveSyntax[0], members);
		}
	}
}