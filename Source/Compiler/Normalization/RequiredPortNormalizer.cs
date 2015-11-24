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
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using CompilerServices;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Editing;
	using Modeling;
	using Roslyn.Symbols;
	using Roslyn.Syntax;
	using Runtime;
	using Utilities;

	/// <summary>
	///   Normalizes required component ports, adding the necessary infrastructure code to support bindings.
	/// </summary>
	public class RequiredPortNormalizer : SyntaxNormalizer
	{
		/// <summary>
		///   The name of the synthesized result variable.
		/// </summary>
		private readonly string _resultVariable = "result".ToSynthesized();

		/// <summary>
		///   The method symbol that is being normalized.
		/// </summary>
		private IMethodSymbol _methodSymbol;

		/// <summary>
		///   The number of ports declared by the compilation.
		/// </summary>
		private int _portCount;

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax declaration)
		{
			_methodSymbol = declaration.GetMethodSymbol(SemanticModel);
			if (!_methodSymbol.ContainingType.IsComponent(SemanticModel) || !_methodSymbol.IsRequiredPort(SemanticModel))
				return declaration;

			var body = CreateBindingCode();
			var originalDeclaration = declaration;
			var index = declaration.Modifiers.IndexOf(SyntaxKind.ExternKeyword);
			var delegateFieldName = Syntax.LiteralExpression(GetBindingDelegateFieldName());
			var infoFieldName = Syntax.LiteralExpression(GetBinderFieldName());

			declaration = declaration.WithModifiers(declaration.Modifiers.RemoveAt(index)).WithSemicolonToken(default(SyntaxToken));
			declaration = (MethodDeclarationSyntax)Syntax.AddAttribute<DebuggerHiddenAttribute>(declaration, SemanticModel);
			declaration = (MethodDeclarationSyntax)Syntax.AddAttribute<BindingMetadataAttribute>(declaration, SemanticModel,
				delegateFieldName, infoFieldName);

			return declaration.WithBody(body).EnsureLineCount(originalDeclaration);
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax declaration)
		{
			var propertySymbol = declaration.GetPropertySymbol(SemanticModel);
			if (!propertySymbol.ContainingType.IsComponent(SemanticModel) || !propertySymbol.IsExtern)
				return declaration;

			var originalDeclaration = declaration;
			var index = declaration.Modifiers.IndexOf(SyntaxKind.ExternKeyword);
			declaration = declaration.WithModifiers(declaration.Modifiers.RemoveAt(index)).WithSemicolonToken(default(SyntaxToken));

			var accessors = SyntaxFactory.List(NormalizerAccessors(originalDeclaration.AccessorList.Accessors));
			return declaration.WithAccessorList(declaration.AccessorList.WithAccessors(accessors)).EnsureLineCount(originalDeclaration);
		}

		/// <summary>
		///   Normalizes the <paramref name="accessors" />.
		/// </summary>
		private IEnumerable<AccessorDeclarationSyntax> NormalizerAccessors(IEnumerable<AccessorDeclarationSyntax> accessors)
		{
			foreach (var accessor in accessors)
			{
				_methodSymbol = accessor.GetMethodSymbol(SemanticModel);
				var body = CreateBindingCode();

				if (_methodSymbol.IsRequiredPort(SemanticModel))
				{
					// Cannot use the SyntaxGenerator extension method due to a Roslyn bug
					var hiddenAttribute = (AttributeListSyntax)Syntax.Attribute(typeof(DebuggerHiddenAttribute).GetGlobalName());
					var delegateFieldName = Syntax.LiteralExpression(GetBindingDelegateFieldName());
					var infoFieldName = Syntax.LiteralExpression(GetBinderFieldName());
					var fieldAttribute = (AttributeListSyntax)Syntax.Attribute(typeof(BindingMetadataAttribute).GetGlobalName(),
						delegateFieldName, infoFieldName);

					var requiredPortAccessor = accessor.AddAttributeLists(hiddenAttribute, fieldAttribute);
					yield return requiredPortAccessor.WithBody(body).WithSemicolonToken(default(SyntaxToken));
				}
				else
					yield return accessor.WithBody(body);
			}
		}

		/// <summary>
		///   Gets the name of the binding delegate for the current port.
		/// </summary>
		private string GetBindingDelegateName()
		{
			return ("BindingDelegate" + _portCount).ToSynthesized();
		}

		/// <summary>
		///   Gets the name of the binding field for the current port.
		/// </summary>
		private string GetBindingDelegateFieldName()
		{
			return ("bindingDelegate" + _portCount).ToSynthesized();
		}

		/// <summary>
		///   Gets the name of the binding field for the current port.
		/// </summary>
		private string GetBinderFieldName()
		{
			return ("binding" + _portCount).ToSynthesized();
		}

		/// <summary>
		///   Creates the arguments for a delegate invocation.
		/// </summary>
		private IEnumerable<ArgumentSyntax> CreateDelegateInvocationArguments()
		{
			return _methodSymbol.Parameters.Select(parameter =>
			{
				var argument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameter.Name));

				switch (parameter.RefKind)
				{
					case RefKind.Ref:
						return argument.WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword));
					case RefKind.Out:
						return argument.WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword));
					case RefKind.None:
						return argument;
					default:
						Assert.NotReached("Unsupported ref kind.");
						return null;
				}
			});
		}

		/// <summary>
		///   Generates the code that executes a binding.
		/// </summary>
		private BlockSyntax CreateBindingCode()
		{
			++_portCount;

			var delegateDeclaration = CreateDelegateDeclaration(GetBindingDelegateName(), true);
			var delegateField = CreateFieldDeclaration(GetBindingDelegateFieldName(), delegateDeclaration, CreateDefaultBindingLambda());
			var infoField = CreateBinderFieldDeclaration();
			AddMembers(_methodSymbol.ContainingType, delegateDeclaration, delegateField, infoField);

			var fieldReference = SyntaxFactory.IdentifierName(GetBindingDelegateFieldName());
			var arguments = CreateDelegateInvocationArguments();
			var argumentList = SyntaxFactory.SeparatedList(arguments);
			var delegateInvocation = SyntaxFactory.InvocationExpression(fieldReference, SyntaxFactory.ArgumentList(argumentList));

			var body = _methodSymbol.ReturnsVoid
				? Syntax.ExpressionStatement(delegateInvocation)
				: SyntaxFactory.ReturnStatement(delegateInvocation);

			return SyntaxFactory.Block((StatementSyntax)body).NormalizeWhitespace();
		}

		/// <summary>
		///   Creates a delegate declaration that is compatible with the method.
		/// </summary>
		private DelegateDeclarationSyntax CreateDelegateDeclaration(string name, bool requiredPort)
		{
			var parameters = _methodSymbol.Parameters.Select(parameter => Syntax.ParameterDeclaration(parameter));

			if (!_methodSymbol.ReturnsVoid && !requiredPort)
			{
				var parameterType = Syntax.TypeExpression(_methodSymbol.ReturnType);
				var returnParameter = Syntax.ParameterDeclaration(_resultVariable, parameterType, refKind: RefKind.Out);

				parameters = parameters.Concat(new[] { returnParameter });
			}

			var methodDelegate = Syntax.DelegateDeclaration(
				name: name,
				parameters: parameters,
				returnType: requiredPort ? Syntax.TypeExpression(_methodSymbol.ReturnType) : Syntax.TypeExpression(SpecialType.System_Boolean),
				accessibility: Accessibility.Private,
				modifiers: DeclarationModifiers.Unsafe);

			return (DelegateDeclarationSyntax)Syntax.AddAttribute<CompilerGeneratedAttribute>(methodDelegate, SemanticModel);
		}

		/// <summary>
		///   Creates a field declaration that stores a value of the <paramref name="delegateType" />.
		/// </summary>
		private FieldDeclarationSyntax CreateFieldDeclaration(string fieldName, DelegateDeclarationSyntax delegateType, SyntaxNode initializer)
		{
			var fieldType = SyntaxFactory.ParseTypeName(delegateType.Identifier.ValueText);
			var field = Syntax.FieldDeclaration(
				name: fieldName,
				type: fieldType,
				accessibility: Accessibility.Private,
				initializer: initializer);

			field = Syntax.AddAttribute<CompilerGeneratedAttribute>(field, SemanticModel);
			field = Syntax.MarkAsNonDebuggerBrowsable(field, SemanticModel);
			field = Syntax.AddAttribute<NonSerializableAttribute>(field, SemanticModel);
			return (FieldDeclarationSyntax)field;
		}

		/// <summary>
		///   Creates a field declaration that stores a <see cref="PortBinding" /> instance.
		/// </summary>
		private FieldDeclarationSyntax CreateBinderFieldDeclaration()
		{
			var field = Syntax.FieldDeclaration(
				name: GetBinderFieldName(),
				type: Syntax.TypeExpression<PortBinding>(SemanticModel),
				accessibility: Accessibility.Private);

			field = Syntax.AddAttribute<CompilerGeneratedAttribute>(field, SemanticModel);
			field = Syntax.MarkAsNonDebuggerBrowsable(field, SemanticModel);
			return (FieldDeclarationSyntax)field;
		}

		/// <summary>
		///   Creates the default lambda method that is assigned to a binding delegate.
		/// </summary>
		private SyntaxNode CreateDefaultBindingLambda()
		{
			var parameters = _methodSymbol.Parameters.Select(parameter => Syntax.ParameterDeclaration(parameter));
			var objectCreation = Syntax.ObjectCreationExpression(SemanticModel.GetTypeSymbol<UnboundPortException>());
			var throwStatement = SyntaxFactory.Block((StatementSyntax)Syntax.ThrowStatement(objectCreation));
			return Syntax.ValueReturningLambdaExpression(parameters, throwStatement);
		}
	}
}