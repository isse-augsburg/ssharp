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
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Editing;
	using Roslyn.Symbols;
	using Roslyn.Syntax;
	using Runtime;
	using Utilities;

	/// <summary>
	///   Normalizes component ports, adding the necessary infrastructure code to support fault injections and bindings.
	/// </summary>
	public class PortNormalizer : SyntaxNormalizer
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
			if (!_methodSymbol.ContainingType.IsComponent(SemanticModel))
				return declaration;

			var body = Normalize(declaration.Body, declaration.GetBodyLineNumber());
			if (body == null)
				return declaration;

			var originalDeclaration = declaration;
			if (_methodSymbol.IsRequiredPort(SemanticModel))
			{
				var index = declaration.Modifiers.IndexOf(SyntaxKind.ExternKeyword);
				declaration = declaration.WithModifiers(declaration.Modifiers.RemoveAt(index)).WithSemicolonToken(default(SyntaxToken));
				declaration = (MethodDeclarationSyntax)Syntax.MarkAsDebuggerHidden(declaration, SemanticModel);
			}

			return declaration.WithBody(body).EnsureLineCount(originalDeclaration);
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax declaration)
		{
			var propertySymbol = declaration.GetPropertySymbol(SemanticModel);
			if (!propertySymbol.ContainingType.IsComponent(SemanticModel))
				return declaration;

			var originalDeclaration = declaration;
			if (propertySymbol.IsExtern)
			{
				var index = declaration.Modifiers.IndexOf(SyntaxKind.ExternKeyword);
				declaration = declaration.WithModifiers(declaration.Modifiers.RemoveAt(index)).WithSemicolonToken(default(SyntaxToken));
			}

			var accessors = SyntaxFactory.List(NormalizerAccessors(originalDeclaration.AccessorList.Accessors));
			return declaration.WithAccessorList(declaration.AccessorList.WithAccessors(accessors)).EnsureLineCount(originalDeclaration);
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax declaration)
		{
			var propertySymbol = declaration.GetPropertySymbol(SemanticModel);
			if (!propertySymbol.ContainingType.IsComponent(SemanticModel))
				return declaration;

			var originalDeclaration = declaration;
			if (propertySymbol.IsExtern)
			{
				var index = declaration.Modifiers.IndexOf(SyntaxKind.ExternKeyword);
				declaration = declaration.WithModifiers(declaration.Modifiers.RemoveAt(index)).WithSemicolonToken(default(SyntaxToken));
			}

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

				var body = accessor.Body ?? SyntaxFactory.Block();
				var lineNumber = accessor.Body?.GetLineNumber() ?? -1;
				body = Normalize(body, lineNumber);

				if (body == null)
					yield return accessor;
				else
				{
					if (_methodSymbol.IsRequiredPort(SemanticModel))
					{
						// Cannot use the SyntaxGenerator extension method due to a Roslyn bug
						var attribute = (AttributeListSyntax)Syntax.Attribute(typeof(DebuggerHiddenAttribute).GetGlobalName());
						yield return accessor.AddAttributeLists(attribute).WithBody(body).WithSemicolonToken(default(SyntaxToken));
					}
					else
						yield return accessor.WithBody(body);
				}
			}
		}

		/// <summary>
		///   Normalizes the <paramref name="statements" />.
		/// </summary>
		private BlockSyntax Normalize(BlockSyntax statements, int bodyLineNumber)
		{
			var isRequiredPort = _methodSymbol.IsRequiredPort(SemanticModel);
			var isProvidedPort = _methodSymbol.IsProvidedPort(SemanticModel);

			if (!isRequiredPort && !isProvidedPort)
				return null;

			++_portCount;

			if (isRequiredPort)
			{
				var delegateDeclaration = CreateDelegateDeclaration(GetBindingDelegateName(), true);
				var fieldDeclaration = CreateFieldDeclaration(GetBindingFieldName(), delegateDeclaration, CreateDefaultBindingLambda());
				AddMembers(_methodSymbol.ContainingType, delegateDeclaration, fieldDeclaration);

				statements = CreateBindingCode();

				// No need for fault normalization when the required port cannot be affected by faults
				if (!_methodSymbol.CanBeAffectedByFaults(SemanticModel))
					return statements;
			}

			// No need for fault normalization when the provided port cannot be affected by faults
			if (isProvidedPort && !_methodSymbol.CanBeAffectedByFaults(SemanticModel))
				return null;

			statements = SyntaxFactory.Block(MakeFaultEffectSensitive(statements, bodyLineNumber));
			return statements.PrependLineDirective(-1);
		}

		/// <summary>
		///   Generates the code to make the method sensitive to fault effects.
		/// </summary>
		private IEnumerable<StatementSyntax> MakeFaultEffectSensitive(BlockSyntax statements, int lineNumber)
		{
			var delegateDeclaration = CreateDelegateDeclaration(GetFaultDelegateName(), false);
			var fieldDeclaration = CreateFieldDeclaration(GetFaultFieldName(), delegateDeclaration, CreateDefaultFaultLambda());
			AddMembers(_methodSymbol.ContainingType, delegateDeclaration, fieldDeclaration);

			var faultBlock = SyntaxFactory.Block(CreateFaultEffectCode()).NormalizeWhitespace().WithTrailingNewLines(1);
			yield return faultBlock.AppendLineDirective(lineNumber);
			yield return statements.AppendLineDirective(-1).EnsureIndentation(statements);
		}

		/// <summary>
		///   Gets the name of the fault delegate for the current port.
		/// </summary>
		private string GetFaultDelegateName()
		{
			return ("FaultDelegate" + _portCount).ToSynthesized();
		}

		/// <summary>
		///   Gets the name of the binding delegate for the current port.
		/// </summary>
		private string GetBindingDelegateName()
		{
			return ("BindingDelegate" + _portCount).ToSynthesized();
		}

		/// <summary>
		///   Gets the name of the fault field for the current port.
		/// </summary>
		private string GetFaultFieldName()
		{
			return ("faultField" + _portCount).ToSynthesized();
		}

		/// <summary>
		///   Gets the name of the binding field for the current port.
		/// </summary>
		private string GetBindingFieldName()
		{
			return ("bindingField" + _portCount).ToSynthesized();
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
		///   Generates the code that checks for and executes active fault effects.
		/// </summary>
		private IEnumerable<StatementSyntax> CreateFaultEffectCode()
		{
			var resultIdentifier = SyntaxFactory.IdentifierName(_resultVariable);
			var fieldReference = SyntaxFactory.IdentifierName(GetFaultFieldName());

			var arguments = CreateDelegateInvocationArguments();
			if (!_methodSymbol.ReturnsVoid)
			{
				yield return (StatementSyntax)Syntax.LocalDeclarationStatement(_methodSymbol.ReturnType, _resultVariable);

				var returnArgument = SyntaxFactory.Argument(resultIdentifier);
				returnArgument = returnArgument.WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword));

				arguments = arguments.Concat(new[] { returnArgument });
			}

			var argumentList = SyntaxFactory.SeparatedList(arguments);
			var delegateInvocation = SyntaxFactory.InvocationExpression(fieldReference, SyntaxFactory.ArgumentList(argumentList));

			var returnStatement = SyntaxFactory.ReturnStatement(_methodSymbol.ReturnsVoid ? null : resultIdentifier);
			yield return SyntaxFactory.IfStatement(delegateInvocation, returnStatement);
		}

		/// <summary>
		///   Generates the code that executes a binding.
		/// </summary>
		private BlockSyntax CreateBindingCode()
		{
			var fieldReference = SyntaxFactory.IdentifierName(GetBindingFieldName());

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

			return (DelegateDeclarationSyntax)Syntax.MarkAsCompilerGenerated(methodDelegate, SemanticModel);
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

			field = Syntax.MarkAsCompilerGenerated(field, SemanticModel);
			field = Syntax.MarkAsNonDebuggerBrowsable(field, SemanticModel);
			return (FieldDeclarationSyntax)field;
		}

		/// <summary>
		///   Creates the default lambda method that is assigned to a fault delegate.
		/// </summary>
		private SyntaxNode CreateDefaultFaultLambda()
		{
			var parameters = new List<SyntaxNode>(_methodSymbol.Parameters.Select(parameter => Syntax.ParameterDeclaration(parameter)));

			if (!_methodSymbol.ReturnsVoid)
			{
				var parameterType = Syntax.TypeExpression(_methodSymbol.ReturnType);
				var returnParameter = Syntax.ParameterDeclaration(_resultVariable, parameterType, refKind: RefKind.Out);

				parameters.Add(returnParameter);
			}

			var statements = new List<SyntaxNode>();
			foreach (var parameter in _methodSymbol.Parameters.Where(parameter => parameter.RefKind == RefKind.Out))
				statements.Add(Syntax.AssignmentStatement(Syntax.IdentifierName(parameter.Name), Syntax.DefaultExpression(parameter.Type)));

			if (!_methodSymbol.ReturnsVoid)
				statements.Add(Syntax.AssignmentStatement(Syntax.IdentifierName(_resultVariable), Syntax.DefaultExpression(_methodSymbol.ReturnType)));

			statements.Add(Syntax.ReturnStatement(Syntax.FalseLiteralExpression()));
			return Syntax.ValueReturningLambdaExpression(parameters, statements);
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