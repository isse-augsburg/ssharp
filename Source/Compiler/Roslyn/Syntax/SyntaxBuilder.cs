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
	using Utilities;

	/// <summary>
	///   Provides helper methods for creating C# syntax nodes.
	/// </summary>
	public static class SyntaxBuilder
	{
		/// <summary>
		///   The type node for the C# integer type.
		/// </summary>
		public static readonly TypeSyntax IntType = SyntaxFactory.ParseTypeName("int");

		/// <summary>
		///   The type node for the C# void type.
		/// </summary>
		public static readonly TypeSyntax VoidType = SyntaxFactory.ParseTypeName("void");

		/// <summary>
		///   Creates a <see cref="SyntaxTokenList" /> containing the appropriate modifier(s) for the desired
		///   <paramref name="visibility" />.
		/// </summary>
		/// <param name="visibility">The visibility the <see cref="SyntaxTokenList" /> should be created for.</param>
		[Pure]
		private static SyntaxTokenList VisibilityModifier(Visibility visibility)
		{
			switch (visibility)
			{
				case Visibility.Private:
					return SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
				case Visibility.Protected:
					return SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
				case Visibility.ProtectedInternal:
					var @protected = SyntaxFactory.Token(SyntaxKind.ProtectedKeyword).WithTrailingTrivia(SyntaxFactory.Space);
					var @internal = SyntaxFactory.Token(SyntaxKind.InternalKeyword);
					return SyntaxFactory.TokenList(@protected, @internal);
				case Visibility.Internal:
					return SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
				case Visibility.Public:
					return SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
				default:
					throw new ArgumentOutOfRangeException("visibility");
			}
		}

		/// <summary>
		///   Creates a <see cref="FieldDeclarationSyntax" />.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="fieldType">The type of the field.</param>
		/// <param name="visibility">The visibility of the field.</param>
		/// <param name="attributes">Optional attributes the field should be marked with.</param>
		public static FieldDeclarationSyntax Field([NotNull] string name, [NotNull] string fieldType, Visibility visibility,
												   [NotNull] params AttributeListSyntax[] attributes)
		{
			Requires.NotNullOrWhitespace(fieldType, nameof(fieldType));
			return Field(name, SyntaxFactory.ParseTypeName(fieldType), visibility, attributes);
		}

		/// <summary>
		///   Creates a <see cref="FieldDeclarationSyntax" />.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="fieldType">The type of the field.</param>
		/// <param name="visibility">The visibility of the field.</param>
		/// <param name="attributes">Optional attributes the field should be marked with.</param>
		public static FieldDeclarationSyntax Field([NotNull] string name, [NotNull] TypeSyntax fieldType, Visibility visibility,
												   [NotNull] params AttributeListSyntax[] attributes)
		{
			Requires.NotNullOrWhitespace(name, nameof(name));
			Requires.NotNull(fieldType, nameof(fieldType));
			Requires.InRange(visibility, nameof(visibility));
			Requires.NotNull(attributes, nameof(attributes));

			var attributeLists = SyntaxFactory.List(attributes);
			var modifiers = VisibilityModifier(visibility);
			var declarator = SyntaxFactory.VariableDeclarator(name);
			var declaratorList = SyntaxFactory.SingletonSeparatedList(declarator);
			var variableDeclaration = SyntaxFactory.VariableDeclaration(fieldType, declaratorList);
			return SyntaxFactory.FieldDeclaration(attributeLists, modifiers, variableDeclaration).NormalizeWhitespace();
		}

		/// <summary>
		///   Creates an <see cref="AccessorDeclarationSyntax" /> with the given <paramref name="accessorType" /> and
		///   <paramref name="visibility" />.
		/// </summary>
		/// <param name="accessorType">The type of the accessor.</param>
		/// <param name="visibility">
		///   The visibility of the accessor. A value of <c>null</c> indicates that no visibility modifier should
		///   be added to the accessor.
		/// </param>
		[Pure, NotNull]
		private static AccessorDeclarationSyntax Accessor(SyntaxKind accessorType, Visibility? visibility)
		{
			Requires.InRange(accessorType, nameof(accessorType));

			var accessor = SyntaxFactory.AccessorDeclaration(accessorType).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

			if (!visibility.HasValue)
				return accessor;

			return accessor.WithLeadingSpace().WithModifiers(VisibilityModifier(visibility.Value));
		}

		/// <summary>
		///   Creates a <see cref="PropertyDeclarationSyntax" /> for an auto-implemented property with both a getter and a setter.
		/// </summary>
		/// <param name="propertyName">The name of the property.</param>
		/// <param name="propertyType">The type of the property.</param>
		/// <param name="visibility">The visibility of the property.</param>
		/// <param name="getterVisibility">
		///   The visibility of the property's getter. A value of <c>null</c> indicates that no visibility
		///   modifier should be added to the getter.
		/// </param>
		/// <param name="setterVisibility">
		///   The visibility of the property's setter. A value of <c>null</c> indicates that no visibility
		///   modifier should be added to the setter.
		/// </param>
		[Pure, NotNull]
		public static PropertyDeclarationSyntax AutoProperty([NotNull] string propertyName, [NotNull] string propertyType,
															 Visibility visibility, Visibility? getterVisibility, Visibility? setterVisibility)
		{
			Requires.NotNullOrWhitespace(propertyName, nameof(propertyName));
			Requires.NotNullOrWhitespace(propertyType, nameof(propertyType));
			Requires.That(!getterVisibility.HasValue || !setterVisibility.HasValue, "Cannot specify visibility modifiers for both accessors.");

			var type = SyntaxFactory.ParseTypeName(propertyType).WithLeadingAndTrailingSpace();
			var property = SyntaxFactory.PropertyDeclaration(type, propertyName).WithModifiers(VisibilityModifier(visibility));
			var getter = Accessor(SyntaxKind.GetAccessorDeclaration, getterVisibility).WithLeadingSpace();
			var setter = Accessor(SyntaxKind.SetAccessorDeclaration, setterVisibility).WithLeadingAndTrailingSpace();
			var accessors = SyntaxFactory.AccessorList(SyntaxFactory.List(new[] { getter, setter })).WithLeadingSpace();

			return property.WithAccessorList(accessors);
		}

		/// <summary>
		///   Creates a <see cref="PropertyDeclarationSyntax" /> within an interface.
		/// </summary>
		/// <param name="propertyName">The name of the property.</param>
		/// <param name="propertyType">The type of the property.</param>
		/// <param name="hasGetter">A value indicating whether the property has a getter.</param>
		/// <param name="hasSetter">A value indicating whether the property has a setter.</param>
		[Pure, NotNull]
		public static PropertyDeclarationSyntax InterfaceProperty([NotNull] string propertyName, [NotNull] string propertyType,
																  bool hasGetter, bool hasSetter)
		{
			Requires.NotNullOrWhitespace(propertyName, nameof(propertyName));
			Requires.NotNullOrWhitespace(propertyType, nameof(propertyType));
			Requires.That(hasGetter || hasSetter, "Cannot specify property with neither a getter nor a setter.");

			var type = SyntaxFactory.ParseTypeName(propertyType).WithTrailingSpace();
			var property = SyntaxFactory.PropertyDeclaration(type, propertyName);

			AccessorDeclarationSyntax[] accessors;
			if (hasGetter && hasSetter)
			{
				var getter = Accessor(SyntaxKind.GetAccessorDeclaration, null).WithLeadingSpace();
				var setter = Accessor(SyntaxKind.SetAccessorDeclaration, null).WithLeadingAndTrailingSpace();
				accessors = new[] { getter, setter };
			}
			else if (hasGetter)
				accessors = new[] { Accessor(SyntaxKind.GetAccessorDeclaration, null).WithLeadingAndTrailingSpace() };
			else
				accessors = new[] { Accessor(SyntaxKind.SetAccessorDeclaration, null).WithLeadingAndTrailingSpace() };

			return property.WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)).WithLeadingSpace());
		}

		/// <summary>
		///   Creates a <see cref="ParenthesizedLambdaExpressionSyntax" /> or <see cref="SimpleLambdaExpressionSyntax" />.
		/// </summary>
		/// <param name="parameters">The parameters of the lambda function.</param>
		/// <param name="body">The body of the lambda function.</param>
		[Pure, NotNull]
		public static ExpressionSyntax Lambda([NotNull] IEnumerable<ParameterSyntax> parameters, [NotNull] CSharpSyntaxNode body)
		{
			Requires.NotNull(parameters, nameof(parameters));
			Requires.NotNull(body, nameof(body));

			// We construct the lambda with some simple body originally and replace it later on with the real body, as we don't want 
			// to normalize the whitespace of the lambda's body.
			var tempBody = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

			if (parameters.Count() == 1 && parameters.Single().Type == null)
				return SyntaxFactory.SimpleLambdaExpression(parameters.Single(), tempBody).NormalizeWhitespace().WithBody(body);

			var parameterList = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
			return SyntaxFactory.ParenthesizedLambdaExpression(parameterList, tempBody).NormalizeWhitespace().WithBody(body);
		}

		/// <summary>
		///   Creates an <see cref="AttributeListSyntax" /> for an attribute of <paramref name="type" /> with the given constructor
		///   <paramref name="arguments" />.
		/// </summary>
		/// <param name="type">The type of the attribute.</param>
		/// <param name="arguments">The optional arguments to the constructor.</param>
		[Pure, NotNull]
		public static AttributeListSyntax Attribute([NotNull] string type, [NotNull] params ExpressionSyntax[] arguments)
		{
			Requires.NotNullOrWhitespace(type, nameof(type));
			Requires.NotNull(arguments, nameof(arguments));

			var typeName = SyntaxFactory.ParseName(type);
			var attributeArguments = arguments.Select(SyntaxFactory.AttributeArgument);
			var argumentList = SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(attributeArguments));
			var attribute = SyntaxFactory.Attribute(typeName, argumentList);
			var attributes = SyntaxFactory.SingletonSeparatedList(attribute);
			return SyntaxFactory.AttributeList(attributes);
		}
	}
}