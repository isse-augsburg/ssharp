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
	///   Provides extension methods for working with <see cref="SyntaxToken" /> instances.
	/// </summary>
	public static class SyntaxTokenExtensions
	{
		/// <summary>
		///   Returns a copy of <paramref name="token" /> with all leading and trailing trivia removed.
		/// </summary>
		/// <param name="token">The token the trivia should be removed from.</param>
		[Pure]
		public static SyntaxToken RemoveTrivia(this SyntaxToken token)
		{
			return token.WithLeadingTrivia().WithTrailingTrivia();
		}

		/// <summary>
		///   Returns a copy of <paramref name="token" /> with all leading and trailing trivia replaced by
		///   <paramref name="leadingTrivia" /> and <paramref name="trailingTrivia" />, respectively.
		/// </summary>
		/// <param name="token">The token that should have its trivia replaced.</param>
		/// <param name="leadingTrivia">The leading trivia of the returned token.</param>
		/// <param name="trailingTrivia">The trailing trivia of the returned token.</param>
		[Pure]
		public static SyntaxToken WithTrivia(this SyntaxToken token, SyntaxTriviaList leadingTrivia, SyntaxTriviaList trailingTrivia)
		{
			return token.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);
		}

		/// <summary>
		///   Returns a copy of <paramref name="token" /> with all leading and trailing trivia replaced by the leading and trailing
		///   trivia of <paramref name="node" />.
		/// </summary>
		/// <param name="token">The token that should have its trivia replaced.</param>
		/// <param name="node">The node the leading and trailing trivia should be copied from.</param>
		[Pure]
		public static SyntaxToken WithTrivia(this SyntaxToken token, [NotNull] SyntaxNode node)
		{
			Requires.NotNull(node, nameof(node));
			return token.WithTrailingTrivia(node.GetTrailingTrivia()).WithLeadingTrivia(node.GetLeadingTrivia());
		}

		/// <summary>
		///   Returns a copy of <paramref name="token" /> with all leading trivia replaced by the leading
		///   trivia of <paramref name="node" />.
		/// </summary>
		/// <param name="token">The token that should have its trivia replaced.</param>
		/// <param name="node">The node the leading and trailing trivia should be copied from.</param>
		[Pure]
		public static SyntaxToken WithLeadingTrivia(this SyntaxToken token, [NotNull] SyntaxNode node)
		{
			Requires.NotNull(node, nameof(node));
			return token.WithLeadingTrivia(node.GetLeadingTrivia());
		}

		/// <summary>
		///   Returns a copy of <paramref name="token" /> with all trailing trivia replaced by the trailing
		///   trivia of <paramref name="node" />.
		/// </summary>
		/// <param name="token">The token that should have its trivia replaced.</param>
		/// <param name="node">The node the leading and trailing trivia should be copied from.</param>
		[Pure]
		public static SyntaxToken WithTrailingTrivia(this SyntaxToken token, [NotNull] SyntaxNode node)
		{
			Requires.NotNull(node, nameof(node));
			return token.WithTrailingTrivia(node.GetTrailingTrivia());
		}

		/// <summary>
		///   Returns a copy of <paramref name="token" /> with all leading and trailing trivia replaced by the leading and trailing
		///   trivia of <paramref name="otherToken" />.
		/// </summary>
		/// <param name="token">The token that should have its trivia replaced.</param>
		/// <param name="otherToken">The token the leading and trailing trivia should be copied from.</param>
		[Pure]
		public static SyntaxToken WithTrivia(this SyntaxToken token, SyntaxToken otherToken)
		{
			return token.WithTrailingTrivia(otherToken.TrailingTrivia).WithLeadingTrivia(otherToken.LeadingTrivia);
		}

		/// <summary>
		///   Returns a copy of <paramref name="token" /> with all leading and trailing trivia replaced by a single space token.
		/// </summary>
		/// <param name="token">The token that should have its trivia replaced.</param>
		[Pure]
		public static SyntaxToken WithLeadingAndTrailingSpace(this SyntaxToken token)
		{
			return token.WithTrailingTrivia(SyntaxFactory.Space).WithLeadingTrivia(SyntaxFactory.Space);
		}

		/// <summary>
		///   Returns a copy of <paramref name="token" /> with all leading trivia replaced by a single space token.
		/// </summary>
		/// <param name="token">The syntax token that should have its trivia replaced.</param>
		[Pure]
		public static SyntaxToken WithLeadingSpace(this SyntaxToken token)
		{
			return token.WithLeadingTrivia(SyntaxFactory.Space);
		}

		/// <summary>
		///   Returns a copy of <paramref name="token" /> with all trailing trivia replaced by a single space token.
		/// </summary>
		/// <param name="token">The syntax token that should have its trivia replaced.</param>
		[Pure]
		public static SyntaxToken WithTrailingSpace(this SyntaxToken token)
		{
			return token.WithTrailingTrivia(SyntaxFactory.Space);
		}
	}
}