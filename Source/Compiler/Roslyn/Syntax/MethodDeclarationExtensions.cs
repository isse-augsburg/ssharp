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
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Symbols;
	using Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="MethodDeclarationSyntax" /> instances.
	/// </summary>
	public static class MethodDeclarationExtensions
	{
		/// <summary>
		///   Gets the <see cref="IMethodSymbol" /> declared by <paramref name="methodDeclaration" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <param name="methodDeclaration">The method declaration the declared symbol should be returned for.</param>
		/// <param name="semanticModel">The semantic model that should be used to determine the declared symbol.</param>
		[Pure, NotNull]
		public static IMethodSymbol GetMethodSymbol([NotNull] this BaseMethodDeclarationSyntax methodDeclaration,
													[NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(methodDeclaration, nameof(methodDeclaration));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			var symbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
			Assert.NotNull(symbol, $"Unable to determine method symbol of method declaration '{methodDeclaration}'.");

			return symbol;
		}

		/// <summary>
		///   Gets the visibility of the <paramref name="methodDeclaration" />.
		/// </summary>
		/// <param name="methodDeclaration">The method declaration the visibility should be returned for.</param>
		[Pure]
		public static Visibility GetVisibility([NotNull] this MethodDeclarationSyntax methodDeclaration)
		{
			Requires.NotNull(methodDeclaration, nameof(methodDeclaration));

			var defaultVisibility = methodDeclaration.ExplicitInterfaceSpecifier == null ? Visibility.Private : Visibility.Public;
			return methodDeclaration.Modifiers.GetVisibility(defaultVisibility);
		}

		/// <summary>
		///   Gets the delegate type corresponding to the <paramref name="methodDeclaration" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <param name="methodDeclaration">The method declaration the delegate type should be returned for.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve type information.</param>
		[Pure, NotNull]
		public static string GetDelegateType([NotNull] this MethodDeclarationSyntax methodDeclaration, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(methodDeclaration, nameof(methodDeclaration));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			Func<string, string[], string> generateType = (delegateType, arguments) =>
			{
				if (arguments.Length == 0)
					return "System.Action";

				return $"{delegateType}<{String.Join(", ", arguments)}>";
			};

			var argumentTypes = methodDeclaration.ParameterList.Parameters.Select(parameter => parameter.Type.ToString());
			var returnType = methodDeclaration.ReturnType.GetReferencedSymbol<INamedTypeSymbol>(semanticModel);

			if (returnType.SpecialType == SpecialType.System_Void)
				return generateType("System.Action", argumentTypes.ToArray());

			argumentTypes = argumentTypes.Concat(new[] { methodDeclaration.ReturnType.ToString() });
			return generateType("System.Func", argumentTypes.ToArray());
		}

		/// <summary>
		///   Changes the <paramref name="methodDeclaration" />'s accessibility to <paramref name="accessibility" /> while preserving
		///   all trivia.
		/// </summary>
		/// <param name="methodDeclaration">The method declaration whose accessibility should be changed.</param>
		/// <param name="accessibility">The new accessibility of the method.</param>
		public static MethodDeclarationSyntax WithAccessibility(this MethodDeclarationSyntax methodDeclaration, Accessibility accessibility)
		{
			Requires.NotNull(methodDeclaration, nameof(methodDeclaration));
			Requires.InRange(accessibility, nameof(accessibility));
			Requires.That(accessibility != Accessibility.NotApplicable, nameof(accessibility), "Unsupported accessibility.");
			Requires.That(accessibility != Accessibility.ProtectedAndInternal, nameof(accessibility), "Unsupported accessibility.");

			var leadingTrivia = SyntaxTriviaList.Empty;
			var trailingTrivia = SyntaxTriviaList.Empty;
			var modifiers = methodDeclaration.Modifiers;

			var accessibilityKeywords = new[]
			{
				SyntaxKind.PrivateKeyword, SyntaxKind.PublicKeyword, SyntaxKind.InternalKeyword, SyntaxKind.ProtectedKeyword
			};

			foreach (var keyword in accessibilityKeywords)
			{
				var keywordIndex = modifiers.IndexOf(keyword);
				if (keywordIndex == -1)
					continue;

				var modifier = modifiers[keywordIndex];
				modifiers = modifiers.RemoveAt(keywordIndex);

				leadingTrivia = leadingTrivia.AddRange(modifier.LeadingTrivia);
				trailingTrivia = trailingTrivia.AddRange(modifier.TrailingTrivia);
			}

			if (trailingTrivia == SyntaxTriviaList.Empty)
				trailingTrivia = SyntaxTriviaList.Create(SyntaxFactory.Space);

			if (accessibility == Accessibility.ProtectedOrInternal)
			{
				var protectedKeyword = SyntaxFactory.Token(SyntaxKind.ProtectedKeyword).WithLeadingTrivia(leadingTrivia).WithTrailingSpace();
				var internalKeyword = SyntaxFactory.Token(SyntaxKind.InternalKeyword).WithTrailingTrivia(trailingTrivia);
				modifiers = modifiers.Insert(0, protectedKeyword);
				modifiers = modifiers.Insert(1, internalKeyword);
			}
			else
			{
				SyntaxKind kind;
				switch (accessibility)
				{
					case Accessibility.Private:
						kind = SyntaxKind.PrivateKeyword;
						break;
					case Accessibility.Protected:
						kind = SyntaxKind.ProtectedKeyword;
						break;
					case Accessibility.Internal:
						kind = SyntaxKind.InternalKeyword;
						break;
					case Accessibility.Public:
						kind = SyntaxKind.PublicKeyword;
						break;
					default:
						throw new ArgumentOutOfRangeException("accessibility", accessibility, null);
				}

				var keyword = SyntaxFactory.Token(kind).WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);
				modifiers = modifiers.Insert(0, keyword);
			}

			return methodDeclaration.WithModifiers(modifiers);
		}

		/// <summary>
		///   Gets a value indicating whether metadata for the <paramref name="methodDeclaration" />'s body should be generated.
		/// </summary>
		/// <param name="methodDeclaration">The method declaration that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used for semantic analysis.</param>
		[Pure]
		public static bool RequiresBoundTreeGeneration([NotNull] this MethodDeclarationSyntax methodDeclaration,
													   [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(methodDeclaration, nameof(methodDeclaration));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			var methodSymbol = methodDeclaration.GetMethodSymbol(semanticModel);
			if (methodSymbol.IsAbstract || methodSymbol.IsExtern)
				return false;

			if (methodSymbol.ContainingType.TypeKind != TypeKind.Class)
				return false;

			return methodSymbol.IsProvidedPort(semanticModel) ||
				   methodSymbol.IsUpdateMethod(semanticModel) ||
				   methodSymbol.IsFaultEffect(semanticModel);
		}
	}
}