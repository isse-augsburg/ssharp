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
	using System;
	using System.Linq;
	using System.Text;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Editing;
	using Utilities;

	/// <summary>
	///   A base class for C# normalizers that normalize certain C# language features.
	/// </summary>
	public abstract class Normalizer : CSharpSyntaxRewriter
	{
		/// <summary>
		///   Gets or sets the compilation that is currently being normalized.
		/// </summary>
		protected Compilation Compilation { get; set; }

		/// <summary>
		///   Gets the syntax generator that the normalizer can use to generate syntax nodes.
		/// </summary>
		protected SyntaxGenerator Syntax { get; private set; }

		/// <summary>
		///   Normalizes the <paramref name="compilation" />.
		/// </summary>
		/// <param name="compilation">The compilation that should be normalized.</param>
		/// <param name="syntaxGenerator">The syntax generator that the normalizer should use to generate syntax nodes.</param>
		[NotNull]
		public Compilation Normalize([NotNull] Compilation compilation, [NotNull] SyntaxGenerator syntaxGenerator)
		{
			Requires.NotNull(compilation, nameof(compilation));

			Syntax = syntaxGenerator;
			Compilation = compilation;

			return Normalize();
		}

		/// <summary>
		///   Applies a normalizer of type <typeparamref name="T" /> to the <paramref name="compilation." />
		/// </summary>
		/// <typeparam name="T">The type of the normalizer that should be applied to <paramref name="compilation" />.</typeparam>
		/// <param name="compilation">The compilation that should be normalized.</param>
		/// <param name="syntaxGenerator">The syntax generator that the normalizer should use to generate syntax nodes.</param>
		[NotNull]
		public static Compilation ApplyNormalizer<T>([NotNull] Compilation compilation, [NotNull] SyntaxGenerator syntaxGenerator)
			where T : Normalizer, new()
		{
			return new T().Normalize(compilation, syntaxGenerator);
		}

		/// <summary>
		///   Normalizes the <see cref="Compilation" />.
		/// </summary>
		protected abstract Compilation Normalize();

		/// <summary>
		///   Adds the <paramref name="compilationUnit" /> to the normalized compilation.
		/// </summary>
		/// <param name="compilationUnit">The compilation unit that should be added.</param>
		/// <param name="fileName">The name of the generated file.</param>
		protected void AddCompilationUnit([NotNull] CompilationUnitSyntax compilationUnit, string fileName = null)
		{
			Requires.NotNull(compilationUnit, nameof(compilationUnit));

			var path = $"{fileName ?? String.Empty}.g.cs{Guid.NewGuid()}";

			// Ideally, we'd construct the syntax tree from the compilation unit directly instead of printing out the
			// compilation unit and then parsing it again. However, if we do that, we can no longer use any C# 6 features
			// in the generated code - probably some Roslyn bug.
			var options = new CSharpParseOptions();
			var syntaxTree = SyntaxFactory.ParseSyntaxTree(compilationUnit.ToFullString(), options, path, Encoding.UTF8);

			Compilation = Compilation.AddSyntaxTrees(syntaxTree);
		}

		/// <summary>
		///   Adds the appropriate namespaces and nested parent classes to <paramref name="type" /> containing the given
		///   <paramref name="member" /> and adds the generated code to the <see cref="Compilation" />.
		/// </summary>
		/// <param name="type">The type the code should be generated for.</param>
		/// <param name="usings">The <c>using</c> directives that should be added to the compilation unit.</param>
		/// <param name="member">The member that should be added to the generated type.</param>
		/// <param name="fileName">The name of the generated file.</param>
		private void AddNamespacedAndNested([NotNull] INamedTypeSymbol type, [NotNull] UsingDirectiveSyntax[] usings,
											MemberDeclarationSyntax member, string fileName = null)
		{
			fileName = fileName ?? type.ToDisplayString().Replace("<", "{").Replace(">", "}");

			if (type.ContainingType != null)
			{
				var generatedClass = (MemberDeclarationSyntax)Syntax.ClassDeclaration(
					name: type.ContainingType.Name,
					typeParameters: type.ContainingType.TypeParameters.Select(t => t.Name),
					modifiers: DeclarationModifiers.Partial,
					members: new[] { member });

				AddNamespacedAndNested(type.ContainingType, usings, generatedClass, fileName);
			}
			else
			{
				var code = !type.ContainingNamespace.IsGlobalNamespace
					? Syntax.NamespaceDeclaration(type.ContainingNamespace.ToDisplayString(), member)
					: member;

				var compilationUnit = (CompilationUnitSyntax)Syntax.CompilationUnit(code);
				AddCompilationUnit(compilationUnit.AddUsings(usings), fileName);
			}
		}

		/// <summary>
		///   Adds a compilation unit containing a part of the partial <paramref name="type" /> containing the
		///   <paramref name="members" />.
		/// </summary>
		/// <param name="type">The type the part should be declared for.</param>
		/// <param name="usings">The <c>using</c> directives that should be added to the compilation unit.</param>
		/// <param name="members">The members that should be added to the type.</param>
		protected void AddMembers([NotNull] INamedTypeSymbol type, [NotNull] UsingDirectiveSyntax[] usings,
								  [NotNull] params MemberDeclarationSyntax[] members)
		{
			Requires.NotNull(type, nameof(type));
			Requires.NotNull(members, nameof(members));

			var generatedClass = (MemberDeclarationSyntax)Syntax.ClassDeclaration(
				name: type.Name,
				typeParameters: type.TypeParameters.Select(t => t.Name),
				modifiers: DeclarationModifiers.Partial,
				members: members);

			AddNamespacedAndNested(type, usings, generatedClass);
		}

		/// <summary>
		///   Adds a compilation unit containing a part of the partial <paramref name="type" />, adding the
		///   <paramref name="attributes" /> to the type.
		/// </summary>
		/// <param name="type">The type the part should be declared for.</param>
		/// <param name="attributes">The attributes that should be added to the type.</param>
		protected void AddAttributes([NotNull] INamedTypeSymbol type, [NotNull] AttributeListSyntax attributes)
		{
			Requires.NotNull(type, nameof(type));
			Requires.NotNull(attributes, nameof(attributes));

			var generatedClass = (ClassDeclarationSyntax)Syntax.ClassDeclaration(
				name: type.Name,
				typeParameters: type.TypeParameters.Select(t => t.Name),
				modifiers: DeclarationModifiers.Partial);

			generatedClass = generatedClass.AddAttributeLists(attributes);
			AddNamespacedAndNested(type, new UsingDirectiveSyntax[0], generatedClass);
		}
	}
}