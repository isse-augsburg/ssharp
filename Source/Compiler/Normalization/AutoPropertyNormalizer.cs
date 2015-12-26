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
	using System.Runtime.CompilerServices;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Editing;
	using Roslyn.Symbols;
	using Roslyn.Syntax;

	/// <summary>
	///   Replaces all automatically implemented property declarations with backing fields and a regular property declarations of
	///   fault effects. When we normalize getter-only auto properties, we also have to redirect all writes to the property within a
	///   constructor to the backing field.
	/// 
	///   For instance:
	///   <code>
	///     	public int X { get; private set }
	///     	// becomes:
	/// 		int __BackingField_X__;
	///     	public int X { get { return __BackingField_X__; } private set { __BackingField_X__ = value; } }
	///    	</code>
	/// </summary>
	public class AutoPropertyNormalizer : Normalizer
	{
		/// <summary>
		///   Indicates whether we're currently normalizing a constructor.
		/// </summary>
		private bool _inConstructor;

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax declaration)
		{
			var methodSymbol = declaration.GetMethodSymbol(SemanticModel);
			if (!methodSymbol.ContainingType.IsFaultEffect(SemanticModel))
				return declaration;

			try
			{
				_inConstructor = true;
				return base.VisitConstructorDeclaration(declaration);
			}
			finally
			{
				_inConstructor = false;
			}
		}

		/// <summary>
		///   Normalizes the <paramref name="expression" />.
		/// </summary>
		public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax expression)
		{
			if (!_inConstructor)
				return expression;

			var propertySymbol = SemanticModel.GetSymbolInfo(expression.Left).Symbol as IPropertySymbol;
			expression = (AssignmentExpressionSyntax)base.VisitAssignmentExpression(expression);

			if (propertySymbol == null || !propertySymbol.IsAutoProperty())
				return expression;

			if (propertySymbol.GetMethod != null && !propertySymbol.GetMethod.CanBeAffectedByFaults(SemanticModel))
				return expression;

			var fieldExpression = (ExpressionSyntax)Syntax.IdentifierName(GetBackingFieldName(propertySymbol)).WithTrivia(expression.Left);
			return expression.WithLeft(fieldExpression);
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax declaration)
		{
			// Nothing to do here for properties with expression bodies
			if (declaration.ExpressionBody != null)
				return declaration;

			// Nothing to do here for properties not defined in fault effects or for properties that are no overrides of some port
			var propertySymbol = declaration.GetPropertySymbol(SemanticModel);
			if (!propertySymbol.ContainingType.IsFaultEffect(SemanticModel) || !propertySymbol.IsOverride)
				return declaration;

			// Nothing to do here for required ports
			if (propertySymbol.IsExtern)
				return declaration;

			var getter = (AccessorDeclarationSyntax)Syntax.GetAccessor(declaration, DeclarationKind.GetAccessor);
			var setter = (AccessorDeclarationSyntax)Syntax.GetAccessor(declaration, DeclarationKind.SetAccessor);

			// Nothing to do here for properties with accessors that have a body
			if (getter == null || getter.Body != null || setter?.Body != null)
				return declaration;

			var fieldName = GetBackingFieldName(propertySymbol);
			var fieldIdentifier = (ExpressionSyntax)Syntax.IdentifierName(fieldName);
			var fieldModifiers = setter == null
				? DeclarationModifiers.Unsafe | DeclarationModifiers.ReadOnly
				: DeclarationModifiers.Unsafe;
			var fieldDeclaration = Syntax.FieldDeclaration(
				name: fieldName,
				type: Syntax.TypeExpression(propertySymbol.Type),
				accessibility: Accessibility.Private,
				modifiers: fieldModifiers,
				initializer: declaration.Initializer?.Value);

			fieldDeclaration = Syntax.AddAttribute<CompilerGeneratedAttribute>(fieldDeclaration);
			fieldDeclaration = Syntax.MarkAsNonDebuggerBrowsable(fieldDeclaration);
			AddMembers(propertySymbol.ContainingType, (FieldDeclarationSyntax)fieldDeclaration);

			var getterBody = (StatementSyntax)Syntax.ReturnStatement(fieldIdentifier).NormalizeWhitespace();
			getterBody = getterBody.AppendLineDirective(-1).PrependLineDirective(-1);
			var getterBlock = SyntaxFactory.Block(getterBody).EnsureIndentation(getter).PrependLineDirective(getter.GetLineNumber());
			getter = getter.WithStatementBody(getterBlock);

			AccessorListSyntax accessors;
			if (setter == null)
				accessors = declaration.AccessorList.WithAccessors(SyntaxFactory.List(new[] { getter }));
			else
			{
				var assignment = Syntax.AssignmentStatement(fieldIdentifier, Syntax.IdentifierName("value"));
				var setterBody = (StatementSyntax)Syntax.ExpressionStatement(assignment);
				setterBody = setterBody.AppendLineDirective(-1).PrependLineDirective(-1);
				var setterBlock = SyntaxFactory.Block(setterBody).EnsureIndentation(setter).PrependLineDirective(setter.GetLineNumber());
				setter = setter.WithStatementBody(setterBlock);

				accessors = declaration.AccessorList.WithAccessors(SyntaxFactory.List(new[] { getter, setter }));
			}

			var originalDeclaration = declaration;
			declaration = declaration.WithInitializer(null).WithSemicolonToken(default(SyntaxToken));
			return declaration.WithAccessorList(accessors).EnsureLineCount(originalDeclaration);
		}

		/// <summary>
		///   Gets the name of the <paramref name="propertySymbol" />'s generated backing field.
		/// </summary>
		private static string GetBackingFieldName(IPropertySymbol propertySymbol)
		{
			return $"BackingField_{propertySymbol.Name}".ToSynthesized();
		}
	}
}