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
	using ISSE.SafetyChecking.Utilities;
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
		/// <param name="syntax">The syntax generator that should be used to generate the expression.</param>
		/// <param name="statements">The statements that should be wrapped in a block.</param>
		[Pure, NotNull]
		public static BlockSyntax AsBlock([NotNull] this SyntaxGenerator syntax,
										  [NotNull] IEnumerable<StatementSyntax> statements)
		{
			Requires.NotNull(syntax, nameof(syntax));
			Requires.NotNull(statements, nameof(statements));

			var statementArray = statements.ToArray();

			if (statementArray.Length == 1 && statementArray[0] is BlockSyntax)
				return (BlockSyntax)statementArray[0];

			return SyntaxFactory.Block(statementArray);
		}

		/// <summary>
		///   Generates an <c>if (condition) { thenStatements } else { elseStatements }</c> statement.
		/// </summary>
		/// <param name="syntax">The syntax generator that should be used to generate the expression.</param>
		/// <param name="condition">The condition of the statement.</param>
		/// <param name="thenStatements">The then statements of the then-path.</param>
		/// <param name="elseStatements">The then statements of the else-path; can be <c>null</c> if there is no else-path.</param>
		[Pure, NotNull]
		public static StatementSyntax IfThenElseStatement([NotNull] this SyntaxGenerator syntax, [NotNull] ExpressionSyntax condition,
														  [NotNull] IEnumerable<StatementSyntax> thenStatements,
														  IEnumerable<StatementSyntax> elseStatements)
		{
			Requires.NotNull(syntax, nameof(syntax));
			Requires.NotNull(condition, nameof(condition));
			Requires.NotNull(thenStatements, nameof(thenStatements));

			var thenStatement = syntax.AsBlock(thenStatements);
			var elseStatement = elseStatements != null ? syntax.AsBlock(elseStatements) : null;
			var elseClause = elseStatement != null ? SyntaxFactory.ElseClause(elseStatement) : null;

			return SyntaxFactory.IfStatement(condition, thenStatement, elseClause);
		}

		/// <summary>
		///   Generates a <see cref="TypeSyntax" /> for the <paramref name="type" />.
		/// </summary>
		/// <param name="syntax">The syntax generator that should be used to generate the expression.</param>
		/// <param name="type">The type symbol the type expression should be generated for.</param>
		[Pure, NotNull]
		public static TypeSyntax GlobalTypeExpression([NotNull] this SyntaxGenerator syntax, [NotNull] ITypeSymbol type)
		{
			Requires.NotNull(syntax, nameof(syntax));
			Requires.NotNull(type, nameof(type));

			switch (type.SpecialType)
			{
				case SpecialType.System_Object:
				case SpecialType.System_Boolean:
				case SpecialType.System_Char:
				case SpecialType.System_SByte:
				case SpecialType.System_Byte:
				case SpecialType.System_Int16:
				case SpecialType.System_UInt16:
				case SpecialType.System_Int32:
				case SpecialType.System_UInt32:
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
				case SpecialType.System_Decimal:
				case SpecialType.System_Single:
				case SpecialType.System_Double:
				case SpecialType.System_String:
					return (TypeSyntax)syntax.TypeExpression(type.SpecialType);
				default:
					return (TypeSyntax)syntax.TypeExpression(type);
			}
		}

		/// <summary>
		///   Generates a <see cref="ParameterSyntax" /> for the <paramref name="parameter" />.
		/// </summary>
		/// <param name="syntax">The syntax generator that should be used to generate the declaration.</param>
		/// <param name="parameter">The parameter the declaration should be generated for..</param>
		[Pure, NotNull]
		public static ParameterSyntax GlobalParameterDeclaration([NotNull] this SyntaxGenerator syntax, [NotNull] IParameterSymbol parameter)
		{
			Requires.NotNull(syntax, nameof(syntax));
			Requires.NotNull(parameter, nameof(parameter));

			return (ParameterSyntax)syntax.ParameterDeclaration(
				parameter.Name, syntax.GlobalTypeExpression(parameter.Type), refKind: parameter.RefKind);
		}

		/// <summary>
		///   Generates a <see cref="TypeSyntax" /> for type <typeparamref name="T" />.
		/// </summary>
		/// <param name="syntax">The syntax generator that should be used to generate the expression.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve type information.</param>
		[Pure, NotNull]
		public static TypeSyntax TypeExpression<T>([NotNull] this SyntaxGenerator syntax, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(syntax, nameof(syntax));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return (TypeSyntax)syntax.TypeExpression(semanticModel.GetTypeSymbol<T>());
		}

		/// <summary>
		///   Generates a <see cref="TypeSyntax" /> for an array of type <typeparamref name="T" />.
		/// </summary>
		/// <param name="syntax">The syntax generator that should be used to generate the expression.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve type information.</param>
		[Pure, NotNull]
		public static ArrayTypeSyntax ArrayTypeExpression<T>([NotNull] this SyntaxGenerator syntax, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(syntax, nameof(syntax));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return (ArrayTypeSyntax)syntax.ArrayTypeExpression(syntax.TypeExpression<T>(semanticModel));
		}

		/// <summary>
		///   Generates a <c>typeof(T)</c> expression for type <typeparamref name="T" />.
		/// </summary>
		/// <param name="syntax">The syntax generator that should be used to generate the expression.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve type information.</param>
		[Pure, NotNull]
		public static ExpressionSyntax TypeOfExpression<T>([NotNull] this SyntaxGenerator syntax, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(syntax, nameof(syntax));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return SyntaxFactory.TypeOfExpression(syntax.TypeExpression<T>(semanticModel));
		}

		/// <summary>
		///   Generates a <c>typeof(T)</c> expression for type <paramref name="typeSymbol" />.
		/// </summary>
		/// <param name="syntax">The syntax generator that should be used to generate the expression.</param>
		/// <param name="typeSymbol">The type the expression should be generated for.</param>
		[Pure, NotNull]
		public static ExpressionSyntax TypeOfExpression([NotNull] this SyntaxGenerator syntax, [NotNull] ITypeSymbol typeSymbol)
		{
			Requires.NotNull(syntax, nameof(syntax));
			Requires.NotNull(typeSymbol, nameof(typeSymbol));

			return SyntaxFactory.TypeOfExpression((TypeSyntax)syntax.TypeExpression(typeSymbol));
		}

		/// <summary>
		///   Generates an array initialize of the form <c>new T[] { elements }</c>.
		/// </summary>
		/// <param name="syntax">The syntax generator that should be used to generate the expression.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve type information.</param>
		/// <param name="elements">The elements the array should be initialized with.</param>
		[Pure, NotNull]
		public static ExpressionSyntax ArrayCreationExpression<T>([NotNull] this SyntaxGenerator syntax,
																  [NotNull] SemanticModel semanticModel, [NotNull] IEnumerable<ExpressionSyntax> elements)
		{
			Requires.NotNull(syntax, nameof(syntax));
			Requires.NotNull(semanticModel, nameof(semanticModel));
			Requires.NotNull(elements, nameof(elements));

			var elementList = SyntaxFactory.SeparatedList(elements);
			var initializer = SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, elementList);
			return SyntaxFactory.ArrayCreationExpression(syntax.ArrayTypeExpression<T>(semanticModel), initializer);
		}

		/// <summary>
		///   Generates an array initialize of the form <c>new T[] { elements }</c>.
		/// </summary>
		/// <param name="syntax">The syntax generator that should be used to generate the expression.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve type information.</param>
		/// <param name="elements">The elements the array should be initialized with.</param>
		[Pure, NotNull]
		public static ExpressionSyntax ArrayCreationExpression<T>([NotNull] this SyntaxGenerator syntax,
																  [NotNull] SemanticModel semanticModel, [NotNull] params ExpressionSyntax[] elements)
		{
			return syntax.ArrayCreationExpression<T>(semanticModel, (IEnumerable<ExpressionSyntax>)elements);
		}

		/// <summary>
		///   Marks the <paramref name="syntaxNode" /> with an attribute of type <typeparamref name="TAttribute" /> with the optional
		///   <paramref name="attributeArguments" />.
		/// </summary>
		/// <typeparam name="TAttribute">The type of the attribute that should be added.</typeparam>
		/// <param name="syntax">The syntax generator that should be used to generate the attribute.</param>
		/// <param name="syntaxNode">The syntax node that should be marked with the attribute.</param>
		/// <param name="attributeArguments">The optional constructor arguments for the attribute.</param>
		[Pure, NotNull]
		public static SyntaxNode AddAttribute<TAttribute>([NotNull] this SyntaxGenerator syntax, [NotNull] SyntaxNode syntaxNode,
														  params SyntaxNode[] attributeArguments) where TAttribute : Attribute
		{
			Requires.NotNull(syntax, nameof(syntax));
			Requires.NotNull(syntaxNode, nameof(syntaxNode));
			Requires.NotNull(attributeArguments, nameof(attributeArguments));

			var attribute = (AttributeListSyntax)syntax.Attribute(typeof(TAttribute).GetGlobalName(), attributeArguments);
			return syntax.AddAttributes(syntaxNode, attribute);
		}

		/// <summary>
		///   In release builds, marks the <paramref name="syntaxNode" /> as <c>[DebuggerBrowsable(DebuggerBrowsableState.Never)]</c>
		/// </summary>
		/// <param name="syntax">The syntax generator that should be used to generate the attribute.</param>
		/// <param name="syntaxNode">The syntax node that should be marked with the attribute.</param>
		[Pure, NotNull]
		public static SyntaxNode MarkAsNonDebuggerBrowsable([NotNull] this SyntaxGenerator syntax, [NotNull] SyntaxNode syntaxNode)
		{
#if DEBUG
			return syntaxNode;
#else
			var attributeType = SyntaxFactory.ParseTypeName(typeof(DebuggerBrowsableState).GetGlobalName());
			var never = syntax.MemberAccessExpression(attributeType, nameof(DebuggerBrowsableState.Never));
			return syntax.AddAttribute<DebuggerBrowsableAttribute>(syntaxNode, never);
#endif
		}
	}
}