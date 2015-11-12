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
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Roslyn.Syntax;

	/// <summary>
	///   A base class for syntax-based C# normalizers that normalize certain C# language features.
	/// </summary>
	public abstract class SyntaxNormalizer : Normalizer
	{
		/// <summary>
		///   The root node of the syntax tree that is currently being normalized.
		/// </summary>
		private SyntaxNode _rootNode;

		/// <summary>
		///   Gets the semantic model that should be used for semantic analysis during normalization.
		/// </summary>
		protected SemanticModel SemanticModel { get; private set; }

		/// <summary>
		///   Normalizes the syntax trees of the <see cref="Compilation" />.
		/// </summary>
		protected override Compilation Normalize()
		{
			foreach (var syntaxTree in Compilation.SyntaxTrees)
			{
				var normalizedSyntaxTree = Normalize(syntaxTree);
				Compilation = Compilation.ReplaceSyntaxTree(syntaxTree, normalizedSyntaxTree);
			}

			return Compilation;
		}

		/// <summary>
		///   Normalizes the <paramref name="syntaxTree" /> of the <see cref="Compilation" />.
		/// </summary>
		/// <param name="syntaxTree">The syntax tree that should be normalized.</param>
		protected virtual SyntaxTree Normalize(SyntaxTree syntaxTree)
		{
			SemanticModel = Compilation.GetSemanticModel(syntaxTree);

			_rootNode = syntaxTree.GetRoot();
			var normalizedRoot = Visit(_rootNode);

			if (_rootNode == normalizedRoot)
				return syntaxTree;

			return syntaxTree.WithRoot(normalizedRoot);
		}

		/// <summary>
		///   Adds a compilation unit containing a part of the partial <paramref name="type" /> containing the
		///   <paramref name="members" />.
		/// </summary>
		/// <param name="type">The type the part should be declared for.</param>
		/// <param name="members">The members that should be added to the type.</param>
		protected void AddMembers([NotNull] INamedTypeSymbol type, [NotNull] params MemberDeclarationSyntax[] members)
		{
			var usings = _rootNode.Descendants<UsingDirectiveSyntax>().Select(usingDirective =>
			{
				var importedSymbol = SemanticModel.GetSymbolInfo(usingDirective.Name).Symbol;
				return usingDirective.WithName(SyntaxFactory.ParseName(importedSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
			});

			AddMembers(type, usings.ToArray(), members);
		}
	}
}