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
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Editing;
	using Symbols;
	using Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="SyntaxGenerator" /> instances.
	/// </summary>
	public static class SyntaxGeneratorExtensions
	{
		/// <summary>
		///   Wraps the <paramref name="statements" /> in a <see cref="BlockSyntax" />, unless <paramref name="statements" /> is a
		///   single <see cref="BlockSyntax" /> already.
		/// </summary>
		/// <param name="syntaxGenerator">The syntax generator that should be used to generate the expression.</param>
		/// <param name="statements">The statements that should be wrapped in a block.</param>
		[Pure, NotNull]
		public static BlockSyntax AsBlock([NotNull] this SyntaxGenerator syntaxGenerator,
										  [NotNull] IEnumerable<StatementSyntax> statements)
		{
			Requires.NotNull(syntaxGenerator, nameof(syntaxGenerator));
			Requires.NotNull(statements, nameof(statements));

			var statementArray = statements.ToArray();

			if (statementArray.Length == 1 && statementArray[0] is BlockSyntax)
				return (BlockSyntax)statementArray[0];

			return SyntaxFactory.Block(statementArray);
		}

		/// <summary>
		///   Generates an <c>if (condition) { thenStatements } else { elseStatements }</c> statement.
		/// </summary>
		/// <param name="syntaxGenerator">The syntax generator that should be used to generate the expression.</param>
		/// <param name="condition">The condition of the statement.</param>
		/// <param name="thenStatements">The then statements of the then-path.</param>
		/// <param name="elseStatements">The then statements of the else-path; can be <c>null</c> if there is no else-path.</param>
		[Pure, NotNull]
		public static StatementSyntax IfThenElseStatement([NotNull] this SyntaxGenerator syntaxGenerator, [NotNull] ExpressionSyntax condition,
														  [NotNull] IEnumerable<StatementSyntax> thenStatements,
														  IEnumerable<StatementSyntax> elseStatements)
		{
			Requires.NotNull(syntaxGenerator, nameof(syntaxGenerator));
			Requires.NotNull(condition, nameof(condition));
			Requires.NotNull(thenStatements, nameof(thenStatements));

			var thenStatement = syntaxGenerator.AsBlock(thenStatements);
			var elseStatement = elseStatements != null ? syntaxGenerator.AsBlock(elseStatements) : null;
			var elseClause = elseStatement != null ? SyntaxFactory.ElseClause(elseStatement) : null;

			return SyntaxFactory.IfStatement(condition, thenStatement, elseClause);
		}

		/// <summary>
		///   Generates a <see cref="TypeSyntax" /> for type <typeparamref name="T" />.
		/// </summary>
		/// <param name="syntaxGenerator">The syntax generator that should be used to generate the expression.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve type information.</param>
		[Pure, NotNull]
		public static TypeSyntax TypeExpression<T>([NotNull] this SyntaxGenerator syntaxGenerator, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(syntaxGenerator, nameof(syntaxGenerator));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return (TypeSyntax)syntaxGenerator.TypeExpression(semanticModel.GetTypeSymbol<T>());
		}

		/// <summary>
		///   Generates a <see cref="TypeSyntax" /> for an array of type <typeparamref name="T" />.
		/// </summary>
		/// <param name="syntaxGenerator">The syntax generator that should be used to generate the expression.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve type information.</param>
		[Pure, NotNull]
		public static ArrayTypeSyntax ArrayTypeExpression<T>([NotNull] this SyntaxGenerator syntaxGenerator, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(syntaxGenerator, nameof(syntaxGenerator));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return (ArrayTypeSyntax)syntaxGenerator.ArrayTypeExpression(syntaxGenerator.TypeExpression<T>(semanticModel));
		}

		/// <summary>
		///   Generates a <c>typeof(T)</c> expression for type <typeparamref name="T" />.
		/// </summary>
		/// <param name="syntaxGenerator">The syntax generator that should be used to generate the expression.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve type information.</param>
		[Pure, NotNull]
		public static ExpressionSyntax TypeOfExpression<T>([NotNull] this SyntaxGenerator syntaxGenerator, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(syntaxGenerator, nameof(syntaxGenerator));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return SyntaxFactory.TypeOfExpression(syntaxGenerator.TypeExpression<T>(semanticModel));
		}

		/// <summary>
		///   Generates a <c>typeof(T)</c> expression for type <paramref name="typeSymbol" />.
		/// </summary>
		/// <param name="syntaxGenerator">The syntax generator that should be used to generate the expression.</param>
		/// <param name="typeSymbol">The type the expression should be generated for.</param>
		[Pure, NotNull]
		public static ExpressionSyntax TypeOfExpression([NotNull] this SyntaxGenerator syntaxGenerator, [NotNull] ITypeSymbol typeSymbol)
		{
			Requires.NotNull(syntaxGenerator, nameof(syntaxGenerator));
			Requires.NotNull(typeSymbol, nameof(typeSymbol));

			return SyntaxFactory.TypeOfExpression((TypeSyntax)syntaxGenerator.TypeExpression(typeSymbol));
		}

		/// <summary>
		///   Generates an array initialize of the form <c>new T[] { elements }</c>.
		/// </summary>
		/// <param name="syntaxGenerator">The syntax generator that should be used to generate the expression.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve type information.</param>
		/// <param name="elements">The elements the array should be initialized with.</param>
		[Pure, NotNull]
		public static ExpressionSyntax ArrayCreationExpression<T>([NotNull] this SyntaxGenerator syntaxGenerator,
																  [NotNull] SemanticModel semanticModel,
																  [NotNull] IEnumerable<ExpressionSyntax> elements)
		{
			Requires.NotNull(syntaxGenerator, nameof(syntaxGenerator));
			Requires.NotNull(semanticModel, nameof(semanticModel));
			Requires.NotNull(elements, nameof(elements));

			var elementList = SyntaxFactory.SeparatedList(elements);
			var initializer = SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, elementList);
			return SyntaxFactory.ArrayCreationExpression(syntaxGenerator.ArrayTypeExpression<T>(semanticModel), initializer);
		}

		/// <summary>
		///   Generates an array initialize of the form <c>new T[] { elements }</c>.
		/// </summary>
		/// <param name="syntaxGenerator">The syntax generator that should be used to generate the expression.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve type information.</param>
		/// <param name="elements">The elements the array should be initialized with.</param>
		[Pure, NotNull]
		public static ExpressionSyntax ArrayCreationExpression<T>([NotNull] this SyntaxGenerator syntaxGenerator,
																  [NotNull] SemanticModel semanticModel,
																  [NotNull] params ExpressionSyntax[] elements)
		{
			return syntaxGenerator.ArrayCreationExpression<T>(semanticModel, (IEnumerable<ExpressionSyntax>)elements);
		}

		/// <summary>
		///   Marks the <paramref name="syntaxNode" /> with an attribute of type <typeparamref name="TAttribute" /> with the optional
		///   <paramref name="attributeArguments" />.
		/// </summary>
		/// <typeparam name="TAttribute">The type of the attribute that should be added.</typeparam>
		/// <param name="syntaxGenerator">The syntax generator that should be used to generate the attribute.</param>
		/// <param name="syntaxNode">The syntax node that should be marked with the attribute.</param>
		/// <param name="attributeArguments">The optional constructor arguments for the attribute.</param>
		[Pure, NotNull]
		public static SyntaxNode AddAttribute<TAttribute>([NotNull] this SyntaxGenerator syntaxGenerator, [NotNull] SyntaxNode syntaxNode,
														  params SyntaxNode[] attributeArguments)
			where TAttribute : Attribute
		{
			Requires.NotNull(syntaxGenerator, nameof(syntaxGenerator));
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			Requires.NotNull(attributeArguments, nameof(attributeArguments));

			var attribute = (AttributeListSyntax)syntaxGenerator.Attribute(typeof(TAttribute).GetGlobalName(), attributeArguments);
			return syntaxGenerator.AddAttributes(syntaxNode, attribute);
		}

		/// <summary>
		///   In release builds, marks the <paramref name="syntaxNode" /> as <c>[DebuggerBrowsable(DebuggerBrowsableState.Never)]</c>
		/// </summary>
		/// <param name="syntaxGenerator">The syntax generator that should be used to generate the attribute.</param>
		/// <param name="syntaxNode">The syntax node that should be marked with the attribute.</param>
		[Pure, NotNull]
		public static SyntaxNode MarkAsNonDebuggerBrowsable([NotNull] this SyntaxGenerator syntaxGenerator, [NotNull] SyntaxNode syntaxNode)
		{
#if DEBUG
			return syntaxNode;
#else
			var attributeType = SyntaxFactory.ParseTypeName(typeof(DebuggerBrowsableState).FullName);
			var never = syntaxGenerator.MemberAccessExpression(attributeType, nameof(DebuggerBrowsableState.Never));
			return syntaxGenerator.AddAttribute<DebuggerBrowsableAttribute>(syntaxNode, never);
#endif
		}
	}
}