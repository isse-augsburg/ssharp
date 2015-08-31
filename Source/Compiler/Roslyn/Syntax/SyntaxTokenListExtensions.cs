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
	using Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="SyntaxTokenList" /> instances.
	/// </summary>
	public static class SyntaxTokenListExtensions
	{
		/// <summary>
		///   Deduces the type or member visibility from the <paramref name="tokenList" />, returning the
		///   <paramref name="defaultVisibility" /> if <paramref name="tokenList" /> does not contain any visibility modifiers.
		/// </summary>
		/// <param name="tokenList">The token list the visibility should be deduced from.</param>
		/// <param name="defaultVisibility">
		///   The default visibility that should be returned if <paramref name="tokenList" /> does not
		///   contain any visibility modifiers.
		/// </param>
		[Pure]
		public static Visibility GetVisibility(this SyntaxTokenList tokenList, Visibility defaultVisibility = Visibility.Private)
		{
			Requires.InRange(defaultVisibility, nameof(defaultVisibility));

			var isPrivate = tokenList.Any(SyntaxKind.PrivateKeyword);
			var isProtected = tokenList.Any(SyntaxKind.ProtectedKeyword);
			var isInternal = tokenList.Any(SyntaxKind.InternalKeyword);
			var isPublic = tokenList.Any(SyntaxKind.PublicKeyword);

			if (isPrivate && !isProtected && !isInternal && !isPublic)
				return Visibility.Private;

			if (isProtected && !isPrivate && !isInternal && !isPublic)
				return Visibility.Protected;

			if (isInternal && !isPrivate && !isProtected && !isPublic)
				return Visibility.Internal;

			if (isProtected && isInternal && !isPrivate && !isPublic)
				return Visibility.ProtectedInternal;

			if (isPublic && !isPrivate && !isProtected && !isInternal)
				return Visibility.Public;

			return defaultVisibility;
		}
	}
}