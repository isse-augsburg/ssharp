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
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using ISSE.SafetyChecking.Utilities;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="SyntaxTree" /> instances.
	/// </summary>
	public static class SyntaxTreeExtensions
	{
		/// <summary>
		///   Gets a list of descendant syntax nodes of <paramref name="syntaxTree" />'s root node of type <typeparamref name="T" />
		///   in prefix document order.
		/// </summary>
		/// <typeparam name="T">The type of the syntax nodes that should be returned.</typeparam>
		/// <param name="syntaxTree">The syntax tree whose descendents should be returned.</param>
		[Pure, NotNull]
		public static IEnumerable<T> Descendants<T>([NotNull] this SyntaxTree syntaxTree)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxTree, nameof(syntaxTree));
			return syntaxTree.GetRoot().DescendantNodes().OfType<T>();
		}

		/// <summary>
		///   Gets a list of descendant syntax nodes of <paramref name="syntaxTree" />'s root node (including the root node) of type
		///   <typeparamref name="T" /> in prefix document order.
		/// </summary>
		/// <typeparam name="T">The type of the syntax nodes that should be returned.</typeparam>
		/// <param name="syntaxTree">The syntax tree whose descendents should be returned.</param>
		[Pure, NotNull]
		public static IEnumerable<T> DescendantsAndSelf<T>([NotNull] this SyntaxTree syntaxTree)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxTree, nameof(syntaxTree));
			return syntaxTree.GetRoot().DescendantNodesAndSelf().OfType<T>();
		}

		/// <summary>
		///   Replaces <paramref name="syntaxTree" />'s current root node with <paramref name="rootNode" />.
		/// </summary>
		/// <param name="syntaxTree">The syntax tree whose root node should be changed.</param>
		/// <param name="rootNode">The new root node of the syntax tree.</param>
		public static SyntaxTree WithRoot([NotNull] this SyntaxTree syntaxTree, [NotNull] SyntaxNode rootNode)
		{
			Requires.NotNull(syntaxTree, nameof(syntaxTree));
			Requires.NotNull(rootNode, nameof(rootNode));

			return syntaxTree.WithChangedText(rootNode.GetText(syntaxTree.GetText().Encoding ?? Encoding.UTF8));
		}
	}
}