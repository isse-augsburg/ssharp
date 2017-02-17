// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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
	using ISSE.SafetyChecking.Utilities;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="IdentifierNameSyntax" /> instances as well as
	///   <see cref="string" /> and <see cref="SyntaxToken" /> instances representing identifiers.
	/// </summary>
	public static class IdentifierExtensions
	{
		/// <summary>
		///   Gets a value indicating whether <paramref name="name" /> is a synthesized name.
		/// </summary>
		/// <param name="name">The name that should be checked.</param>
		public static bool IsSynthesized(this string name)
		{
			return name.StartsWith("__") && name.EndsWith("__");
		}

		/// <summary>
		///   Gets a value indicating whether <paramref name="name" /> is a synthesized name.
		/// </summary>
		/// <param name="name">The name that should be checked.</param>
		public static bool IsSynthesized(this IdentifierNameSyntax name)
		{
			return name.Identifier.ValueText.IsSynthesized();
		}

		/// <summary>
		///   Gets a value indicating whether <paramref name="identifier" /> is a synthesized name.
		/// </summary>
		/// <param name="identifier">The identifier that should be checked.</param>
		public static bool IsSynthesized(this SyntaxToken identifier)
		{
			return IsSynthesized(identifier.ValueText);
		}

		/// <summary>
		///   Converts <paramref name="name" /> to a synthesized name.
		/// </summary>
		/// <param name="name">The name that should be converted.</param>
		public static string ToSynthesized(this string name)
		{
			Requires.NotNullOrWhitespace(name, nameof(name));
			Requires.That(!IsSynthesized(name), nameof(name), "The name has already been escaped.");

			return $"__{name}__";
		}

		/// <summary>
		///   Converts <paramref name="name" /> to a synthesized name.
		/// </summary>
		/// <param name="name">The name that should be converted.</param>
		public static SyntaxToken ToSynthesized(this SyntaxToken name)
		{
			Requires.That(!IsSynthesized(name), nameof(name), "The name has already been escaped.");
			return SyntaxFactory.Identifier(name.ValueText.ToSynthesized());
		}

		/// <summary>
		///   Converts <paramref name="name" /> to a synthesized name.
		/// </summary>
		/// <param name="name">The name that should be converted.</param>
		public static IdentifierNameSyntax ToSynthesized(this IdentifierNameSyntax name)
		{
			Requires.NotNull(name, nameof(name));
			Requires.That(!IsSynthesized(name), nameof(name), "The name has already been escaped.");

			return name.WithIdentifier(name.Identifier.ToSynthesized());
		}
	}
}