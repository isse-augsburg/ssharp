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
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Symbols;
	using Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="SyntaxNode" /> instances.
	/// </summary>
	public static class SyntaxNodeExtensions
	{
		/// <summary>
		///   Unwraps an expression contained in zero, one, or more <see cref="ParenthesizedExpressionSyntax" />. For instance,
		///   <c>x</c> remains <c>x</c>, <c>(x)</c>, <c>((x))</c>, etc. become <c>x</c>.
		/// </summary>
		/// <param name="syntaxNode">The syntax node that should be unwrapped.</param>
		[Pure, NotNull]
		public static SyntaxNode RemoveParentheses([NotNull] this SyntaxNode syntaxNode)
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));

			var parenthesized = syntaxNode as ParenthesizedExpressionSyntax;
			while (parenthesized != null)
			{
				syntaxNode = parenthesized.Expression;
				parenthesized = syntaxNode as ParenthesizedExpressionSyntax;
			}

			return syntaxNode;
		}

		/// <summary>
		///   Gets a list of descendant syntax nodes of type <typeparamref name="T" /> in prefix document order.
		/// </summary>
		/// <typeparam name="T">The type of the syntax nodes that should be returned.</typeparam>
		/// <param name="syntaxNode">The syntax node whose descendents should be returned.</param>
		[Pure, NotNull]
		public static IEnumerable<T> Descendants<T>([NotNull] this SyntaxNode syntaxNode)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			return syntaxNode.DescendantNodes().OfType<T>();
		}

		/// <summary>
		///   Gets a list of descendant syntax nodes (including <paramref name="syntaxNode" />) of type <typeparamref name="T" /> in
		///   prefix document order.
		/// </summary>
		/// <typeparam name="T">The type of the syntax nodes that should be returned.</typeparam>
		/// <param name="syntaxNode">The syntax node whose descendents should be returned.</param>
		[Pure, NotNull]
		public static IEnumerable<T> DescendantsAndSelf<T>([NotNull] this SyntaxNode syntaxNode)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			return syntaxNode.DescendantNodesAndSelf().OfType<T>();
		}

		/// <summary>
		///   Gets the symbol referenced by <paramref name="syntaxNode" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <param name="syntaxNode">The node the referenced symbol should be returned for.</param>
		/// <param name="semanticModel">The semantic model that should be used to determine the referenced symbol.</param>
		[Pure, NotNull]
		public static ISymbol GetReferencedSymbol([NotNull] this SyntaxNode syntaxNode, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);
			Assert.NotNull(symbolInfo.Symbol, $"Unable to determine the symbol referenced by syntax node '{syntaxNode}'.");

			return symbolInfo.Symbol;
		}

		/// <summary>
		///   Gets the symbol referenced by <paramref name="syntaxNode" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <typeparam name="T">The expected type of the referenced symbol.</typeparam>
		/// <param name="syntaxNode">The node the referenced symbol should be returned for.</param>
		/// <param name="semanticModel">The semantic model that should be used to determine the referenced symbol.</param>
		[Pure, NotNull]
		public static T GetReferencedSymbol<T>([NotNull] this SyntaxNode syntaxNode, [NotNull] SemanticModel semanticModel)
			where T : class, ISymbol
		{
			var symbol = syntaxNode.GetReferencedSymbol(semanticModel);
			Assert.OfType<T>(symbol, $"Expected a symbol of type '{typeof(T).FullName}'. However, the actual symbol type for syntax " +
									 $"node '{syntaxNode}' is '{symbol.GetType().FullName}'.");

			return (T)symbol;
		}

		/// <summary>
		///   Checks whether the <paramref name="syntaxNode" /> is marked with an attribute of type <paramref name="attributeType" />
		///   within the context of the <paramref name="semanticModel" />. This method only succeeds if
		///   <paramref name="syntaxNode" /> declares a symbol.
		/// </summary>
		/// <param name="syntaxNode">The syntax node that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve symbol information.</param>
		/// <param name="attributeType">The type of the attribute <paramref name="syntaxNode" /> should be marked with.</param>
		[Pure]
		public static bool HasAttribute([NotNull] this SyntaxNode syntaxNode, [NotNull] SemanticModel semanticModel, [NotNull] Type attributeType)
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			Requires.NotNull(semanticModel, nameof(semanticModel));
			Requires.NotNull(attributeType, nameof(attributeType));

			var declaredSymbol = semanticModel.GetDeclaredSymbol(syntaxNode);
			Assert.NotNull(declaredSymbol, $"Unable to determine symbol declared by syntax node '{syntaxNode}'.");

			return declaredSymbol.HasAttribute(semanticModel.GetTypeSymbol(attributeType));
		}

		/// <summary>
		///   Checks whether the <paramref name="syntaxNode" /> is marked with an attribute of type <typeparamref name="T" /> within
		///   the context of the <paramref name="semanticModel" />. This method only succeeds if <paramref name="syntaxNode" />
		///   declares a symbol.
		/// </summary>
		/// <typeparam name="T">The type of the attribute <paramref name="syntaxNode" /> should be marked with.</typeparam>
		/// <param name="syntaxNode">The syntax node that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve symbol information.</param>
		[Pure]
		public static bool HasAttribute<T>([NotNull] this SyntaxNode syntaxNode, [NotNull] SemanticModel semanticModel)
			where T : Attribute
		{
			return syntaxNode.HasAttribute(semanticModel, typeof(T));
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with all end-of-line trivia replaced by single spaces.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node that should have all of its end-of-line trivia removed.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have all of its end-of-line trivia removed.</param>
		[Pure, NotNull]
		public static T AsSingleLine<T>([NotNull] this T syntaxNode)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));

			var trivia = syntaxNode.DescendantTrivia().Where(t => t.Kind() == SyntaxKind.EndOfLineTrivia);
			return syntaxNode.ReplaceTrivia(trivia, (t1, t2) => SyntaxFactory.Space);
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with all comment trivia removed.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node that should have all of its comment trivia removed.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have all of its comment trivia removed.</param>
		[Pure, NotNull]
		public static T RemoveComments<T>([NotNull] this T syntaxNode)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));

			var trivia = syntaxNode.DescendantTrivia().Where(t =>
			{
				var kind = t.Kind();
				return kind == SyntaxKind.DocumentationCommentExteriorTrivia || kind == SyntaxKind.MultiLineCommentTrivia ||
					   kind == SyntaxKind.MultiLineDocumentationCommentTrivia || kind == SyntaxKind.SingleLineCommentTrivia ||
					   kind == SyntaxKind.SingleLineDocumentationCommentTrivia;
			});
			return syntaxNode.ReplaceTrivia(trivia, (t1, t2) => SyntaxFactory.Space);
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with <paramref name="newLineCount" /> many end-of-line trivia tokens
		///   appended to <paramref name="syntaxNode" />'s trailing trivia.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node that should have the end-of-line trivia appended.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have the end-of-line trivia appended.</param>
		/// <param name="newLineCount">
		///   The number of end-of-line trivia tokens that should be appended to <paramref name="syntaxNode" />.
		/// </param>
		[Pure, NotNull]
		public static T WithTrailingNewLines<T>([NotNull] this T syntaxNode, int newLineCount)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));

			if (newLineCount <= 0)
				return syntaxNode;

			var trivia = syntaxNode.GetTrailingTrivia().AddRange(Enumerable.Repeat(SyntaxFactory.EndOfLine("\n"), newLineCount));
			return syntaxNode.WithTrailingTrivia(trivia);
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with <paramref name="newLineCount" /> many end-of-line trivia tokens
		///   prepended to <paramref name="syntaxNode" />'s leading trivia.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node that should have the end-of-line trivia prepended.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have the end-of-line trivia prepended.</param>
		/// <param name="newLineCount">
		///   The number of end-of-line trivia tokens that should be prepended to <paramref name="syntaxNode" />.
		/// </param>
		[Pure, NotNull]
		public static T WithLeadingNewLines<T>([NotNull] this T syntaxNode, int newLineCount)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));

			if (newLineCount <= 0)
				return syntaxNode;

			var trivia = syntaxNode.GetLeadingTrivia();
			for (var i = 0; i < newLineCount; ++i)
				trivia = trivia.Insert(0, SyntaxFactory.EndOfLine("\n"));

			return syntaxNode.WithLeadingTrivia(trivia);
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with all leading and trailing trivia removed.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have all of its trivia removed.</param>
		[Pure, NotNull]
		public static T RemoveTrivia<T>([NotNull] this T syntaxNode)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			return syntaxNode.WithTrailingTrivia().WithLeadingTrivia();
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with all of its leading and trailing trivia replaced with
		///   <paramref name="leadingTrivia" /> and <paramref name="trailingTrivia" />, respectively.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have all of its trivia replaced.</param>
		/// <param name="leadingTrivia">The leading trivia of the returned syntax node.</param>
		/// <param name="trailingTrivia">The trailing trivia of the returned syntax node.</param>
		[Pure, NotNull]
		public static T WithTrivia<T>([NotNull] this T syntaxNode, SyntaxTriviaList leadingTrivia, SyntaxTriviaList trailingTrivia)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			return syntaxNode.WithTrailingTrivia(trailingTrivia).WithLeadingTrivia(leadingTrivia);
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with all of its leading and trailing trivia replaced with
		///   the leading and trailing trivia of <paramref name="templateNode" />.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have all of its trivia replaced.</param>
		/// <param name="templateNode">The syntax node the leading and trailing trivia should be copied from.</param>
		[Pure, NotNull]
		public static T WithTrivia<T>([NotNull] this T syntaxNode, [NotNull] SyntaxNode templateNode)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			Requires.NotNull(templateNode, nameof(templateNode));

			return syntaxNode.WithTrailingTrivia(templateNode.GetTrailingTrivia()).WithLeadingTrivia(templateNode.GetLeadingTrivia());
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with all of its leading and trailing trivia replaced with
		///   the leading and trailing trivia of <paramref name="syntaxToken" />.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have all of its trivia replaced.</param>
		/// <param name="syntaxToken">The syntax token the leading and trailing trivia should be copied from.</param>
		[Pure, NotNull]
		public static T WithTrivia<T>([NotNull] this T syntaxNode, SyntaxToken syntaxToken)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			return syntaxNode.WithTrailingTrivia(syntaxToken.TrailingTrivia).WithLeadingTrivia(syntaxToken.LeadingTrivia);
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with all of its leading trivia replaced with
		///   the leading trivia of <paramref name="templateNode" />.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have all of its leading trivia replaced.</param>
		/// <param name="templateNode">The syntax node the leading trivia should be copied from.</param>
		[Pure, NotNull]
		public static T WithLeadingTrivia<T>([NotNull] this T syntaxNode, [NotNull] SyntaxNode templateNode)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			Requires.NotNull(templateNode, nameof(templateNode));
			return syntaxNode.WithLeadingTrivia(templateNode.GetLeadingTrivia());
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with all of its leading trivia replaced with
		///   the leading trivia of <paramref name="syntaxToken" />.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have all of its leading trivia replaced.</param>
		/// <param name="syntaxToken">The syntax token the leading trivia should be copied from.</param>
		[Pure, NotNull]
		public static T WithLeadingTrivia<T>([NotNull] this T syntaxNode, SyntaxToken syntaxToken)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			return syntaxNode.WithLeadingTrivia(syntaxToken.LeadingTrivia);
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with all of its trailing trivia replaced with
		///   the trailing trivia of <paramref name="templateNode" />.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have all of its trailing trivia replaced.</param>
		/// <param name="templateNode">The syntax node the trailing trivia should be copied from.</param>
		[Pure, NotNull]
		public static T WithTrailingTrivia<T>([NotNull] this T syntaxNode, [NotNull] SyntaxNode templateNode)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			Requires.NotNull(templateNode, nameof(templateNode));
			return syntaxNode.WithTrailingTrivia(templateNode.GetTrailingTrivia());
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with all of its trailing trivia replaced with
		///   the trailing trivia of <paramref name="syntaxToken" />.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have all of its trailing trivia replaced.</param>
		/// <param name="syntaxToken">The syntax token the trailing trivia should be copied from.</param>
		[Pure, NotNull]
		public static T WithTrailingTrivia<T>([NotNull] this T syntaxNode, SyntaxToken syntaxToken)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			return syntaxNode.WithTrailingTrivia(syntaxToken.TrailingTrivia);
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with all leading and trailing trivia replaced by a single space token.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have its trivia replaced.</param>
		[Pure, NotNull]
		public static T WithLeadingAndTrailingSpace<T>([NotNull] this T syntaxNode)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			return syntaxNode.WithTrailingTrivia(SyntaxFactory.Space).WithLeadingTrivia(SyntaxFactory.Space);
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with all leading trivia replaced by a single space token.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have its trivia replaced.</param>
		/// <param name="count">The number of space characters to add.</param>
		[Pure, NotNull]
		public static T WithLeadingSpace<T>([NotNull] this T syntaxNode, int count = 1)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			return syntaxNode.WithLeadingTrivia(SyntaxFactory.Whitespace(new string(' ', count)));
		}

		/// <summary>
		///   Returns a copy of <paramref name="syntaxNode" /> with all trailing trivia replaced by a single space token.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node that should have its trivia replaced.</param>
		[Pure, NotNull]
		public static T WithTrailingSpace<T>([NotNull] this T syntaxNode)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			return syntaxNode.WithTrailingTrivia(SyntaxFactory.Space);
		}

		/// <summary>
		///   Creates a <c>#line</c> directive.
		/// </summary>
		/// <param name="line">The original line number.</param>
		/// <param name="filePath">The path of the original file; if null, only the line numbering will be affected by the directive.</param>
		[Pure]
		private static SyntaxTrivia CreateLineDirective(int line, string filePath)
		{
			var lineToken = line == -1 ? SyntaxFactory.Token(SyntaxKind.HiddenKeyword) : SyntaxFactory.Literal(line);
			var lineDirective = String.IsNullOrWhiteSpace(filePath)
				? SyntaxFactory.LineDirectiveTrivia(lineToken, true).NormalizeWhitespace()
				: SyntaxFactory.LineDirectiveTrivia(lineToken, SyntaxFactory.Literal(filePath), true).NormalizeWhitespace();
			return SyntaxFactory.Trivia(lineDirective.WithLeadingNewLines(1));
		}

		/// <summary>
		///   Prepends a <c>#line</c> directive at the beginning of <paramref name="syntaxNode" />'s leading trivia.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node the directive should be added to.</param>
		/// <param name="line">The original line number.</param>
		/// <param name="filePath">The path of the original file; if null, only the line numbering will be affected by the directive.</param>
		[Pure, NotNull]
		public static T PrependLineDirective<T>([NotNull] this T syntaxNode, int line, string filePath = null)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			return syntaxNode.WithLeadingTrivia(syntaxNode.GetLeadingTrivia().Insert(0, CreateLineDirective(line, filePath)));
		}

		/// <summary>
		///   Appends a <c>#line</c> directive at the end of <paramref name="syntaxNode" />'s trailing trivia.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node the directive should be added to.</param>
		/// <param name="line">The original line number.</param>
		/// <param name="filePath">The path of the original file; if null, only the line numbering will be affected by the directive.</param>
		[Pure, NotNull]
		public static T AppendLineDirective<T>([NotNull] this T syntaxNode, int line, string filePath = null)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			return syntaxNode.WithTrailingTrivia(syntaxNode.GetTrailingTrivia().Add(CreateLineDirective(line, filePath)));
		}

		/// <summary>
		///   Ensures that <paramref name="syntaxNode" /> ends at the same line count as <paramref name="templateNode" />.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node that is not allowed to change the line count.</param>
		/// <param name="templateNode">The template node that defines the original line count.</param>
		[Pure, NotNull]
		public static T EnsureLineCount<T>([NotNull] this T syntaxNode, SyntaxNode templateNode)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			Requires.NotNull(templateNode, nameof(templateNode));

			var line = templateNode.GetLastToken(true, true, true, true).GetLocation().GetMappedLineSpan().EndLinePosition.Line;
			return syntaxNode.AppendLineDirective(line + 2);
		}

		/// <summary>
		///   Ensures that <paramref name="syntaxNode" /> has the same indentation as <paramref name="templateNode" />.
		/// </summary>
		/// <typeparam name="T">The type of the syntax node.</typeparam>
		/// <param name="syntaxNode">The syntax node whose indentation should be changed.</param>
		/// <param name="templateNode">The template node that defines the original indentation.</param>
		[Pure, NotNull]
		public static T EnsureIndentation<T>([NotNull] this T syntaxNode, SyntaxNode templateNode)
			where T : SyntaxNode
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			Requires.NotNull(templateNode, nameof(templateNode));

			var expectedColumn = templateNode.GetLocation().GetLineSpan().StartLinePosition.Character;
			var actualColumn = syntaxNode.GetLocation().GetLineSpan().StartLinePosition.Character;
			var offset = Math.Max(0, expectedColumn - actualColumn);
			return syntaxNode.WithLeadingTrivia(syntaxNode.GetLeadingTrivia().Add(SyntaxFactory.Whitespace(new string(' ', offset))));
		}

		/// <summary>
		///   Gets the line number of the <paramref name="syntaxNode" />.
		/// </summary>
		/// <param name="syntaxNode">The syntax node the line number should be returned for.</param>
		[Pure]
		public static int GetLineNumber([NotNull] this SyntaxNode syntaxNode)
		{
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			return syntaxNode.GetLocation().GetMappedLineSpan().StartLinePosition.Line + 1;
		}
	}
}