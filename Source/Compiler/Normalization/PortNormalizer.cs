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
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Editing;
	using Modeling;
	using Roslyn.Symbols;
	using Roslyn.Syntax;
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
		///   Represents the <c>[CompilerGenerated]</c> attribute syntax.
		/// </summary>
		private AttributeListSyntax _compilerGeneratedAttribute;

		/// <summary>
		///   The method symbol that is being normalized.
		/// </summary>
		private IMethodSymbol _methodSymbol;

		/// <summary>
		///   The number of ports declared by the compilation.
		/// </summary>
		private int _portCount;

		/// <summary>
		///   Normalizes the syntax trees of the <see cref="Compilation" />.
		/// </summary>
		protected override Compilation Normalize()
		{
			_compilerGeneratedAttribute = (AttributeListSyntax)Syntax.Attribute(typeof(CompilerGeneratedAttribute).FullName);
			return base.Normalize();
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax declaration)
		{
			_methodSymbol = declaration.GetMethodSymbol(SemanticModel);
			if (!_methodSymbol.ContainingType.OriginalDefinition.IsDerivedFromComponent(SemanticModel))
				return declaration;

			var body = Normalize(declaration.GetStatements(_methodSymbol), declaration, declaration.GetBodyLineNumber());
			if (body == null)
				return declaration;

            return declaration.WithStatementBody(body);
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax declaration)
		{
			var propertySymbol = declaration.GetPropertySymbol(SemanticModel);
			if (!propertySymbol.ContainingType.OriginalDefinition.IsDerivedFromComponent(SemanticModel))
				return declaration;

			// We have to deal with expression-bodied properties explicitly
			if (declaration.AccessorList == null)
			{
				_methodSymbol = propertySymbol.GetMethod;

				var statementBody = declaration.ExpressionBody.Expression.AsStatementBody(_methodSymbol.ReturnType);
				var body = Normalize(statementBody, declaration, declaration.ExpressionBody.Expression.GetLineNumber());
				if (body == null)
					return declaration;

				var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, body);
				declaration = declaration.AddAccessorListAccessors(accessor);
				return declaration.WithExpressionBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None));
			}

			var accessors = declaration.AccessorList.Accessors.Select(accessor =>
			{
				_methodSymbol = accessor.GetMethodSymbol(SemanticModel);

				var body = accessor.Body ?? SyntaxFactory.Block();
				var lineNumber = accessor.Body?.GetLineNumber() ?? -1;
				body = Normalize(body, declaration, lineNumber);

				if (body == null)
					return accessor;

				return accessor.WithBody(body);
			});

			return declaration.WithAccessorList(declaration.AccessorList.WithAccessors(SyntaxFactory.List(accessors)));
		}
		
		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax declaration)
		{
			return base.VisitIndexerDeclaration(declaration);
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax declaration)
		{
			return base.VisitEventDeclaration(declaration);
		}

		/// <summary>
		///   Normalizes the <paramref name="statements" />.
		/// </summary>
		private BlockSyntax Normalize(BlockSyntax statements, SyntaxNode originalDeclaration, int bodyLineNumber)
		{
			// No need for fault normalization when the method cannot be affected by faults
			if (!_methodSymbol.CanBeAffectedByFaults(SemanticModel))
				return null;

			++_portCount;

			var delegateDeclaration = CreateDelegateDeclaration();
			var fieldDeclaration = CreateFieldDeclaration(delegateDeclaration.Identifier.ValueText, CreateProvidedPortLambda());
			AddMembers(_methodSymbol.ContainingType, delegateDeclaration, fieldDeclaration);

			statements = SyntaxFactory.Block(MakeFaultEffectSensitive(statements, bodyLineNumber));
			return statements.PrependLineDirective(-1).EnsureLineCount(originalDeclaration);
		}

		/// <summary>
		///   Generates the code to make the method sensitive to fault effects.
		/// </summary>
		private IEnumerable<StatementSyntax> MakeFaultEffectSensitive(BlockSyntax statements, int lineNumber)
		{
			var faultBlock = SyntaxFactory.Block(CreateFaultEffectCode()).NormalizeWhitespace().WithTrailingNewLines(1);
			yield return faultBlock.AppendLineDirective(lineNumber);
			yield return statements;
		}

		/// <summary>
		///   Gets the name of the delegate for the current port.
		/// </summary>
		private string GetDelegateName()
		{
			return ("Delegate" + _portCount).ToSynthesized();
		}

		/// <summary>
		///   Gets the name of the field for the current port.
		/// </summary>
		private string GetFieldName()
		{
			return ("backingField" + _portCount).ToSynthesized();
		}

		/// <summary>
		///   Creates a delegate declaration that is compatible with the method.
		/// </summary>
		private DelegateDeclarationSyntax CreateDelegateDeclaration()
		{
			var parameters = _methodSymbol.Parameters.Select(parameter => Syntax.ParameterDeclaration(parameter));

			if (!_methodSymbol.ReturnsVoid)
			{
				var parameterType = Syntax.TypeExpression(_methodSymbol.ReturnType);
				var returnParameter = Syntax.ParameterDeclaration(_resultVariable, parameterType, refKind: RefKind.Out);

				parameters = parameters.Concat(new[] { returnParameter });
			}

			var methodDelegate = (DelegateDeclarationSyntax)Syntax.DelegateDeclaration(
				name: GetDelegateName(),
				parameters: parameters,
				returnType: Syntax.TypeExpression(SpecialType.System_Boolean),
				accessibility: Accessibility.Private,
				modifiers: DeclarationModifiers.Unsafe);

			return methodDelegate.AddAttributeLists(_compilerGeneratedAttribute);
		}

		/// <summary>
		///   Creates a field declaration that stores a value of the <paramref name="delegateType" />.
		/// </summary>
		private FieldDeclarationSyntax CreateFieldDeclaration(string delegateType, SyntaxNode initializer)
		{
			var fieldType = SyntaxFactory.ParseTypeName(delegateType);
			var field = (FieldDeclarationSyntax)Syntax.FieldDeclaration(
				name: GetFieldName(),
				type: fieldType,
				accessibility: Accessibility.Private,
				initializer: initializer);

			return field.AddAttributeLists(_compilerGeneratedAttribute);
		}

		/// <summary>
		///   Generates the code that checks for and executes active fault effects.
		/// </summary>
		private IEnumerable<StatementSyntax> CreateFaultEffectCode()
		{
			var resultIdentifier = SyntaxFactory.IdentifierName(_resultVariable);
			var fieldReference = SyntaxFactory.ParseExpression("this." + GetFieldName());

			var arguments = _methodSymbol.Parameters.Select(parameter =>
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
		///   Creates the default lambda expression that is assigned to a provided port.
		/// </summary>
		/// <returns></returns>
		private SyntaxNode CreateProvidedPortLambda()
		{
			var parameters = _methodSymbol.Parameters.Select(parameter => Syntax.ParameterDeclaration(parameter));

			if (!_methodSymbol.ReturnsVoid)
			{
				var parameterType = Syntax.TypeExpression(_methodSymbol.ReturnType);
				var returnParameter = Syntax.ParameterDeclaration(_resultVariable, parameterType, refKind: RefKind.Out);

				parameters = parameters.Concat(new[] { returnParameter });
			}

			var statements = new List<SyntaxNode>();
			foreach (var parameter in _methodSymbol.Parameters.Where(parameter => parameter.RefKind == RefKind.Out))
				statements.Add(Syntax.AssignmentStatement(Syntax.IdentifierName(parameter.Name), Syntax.DefaultExpression(parameter.Type)));

			if (!_methodSymbol.ReturnsVoid)
				statements.Add(Syntax.AssignmentStatement(Syntax.IdentifierName(_resultVariable), Syntax.DefaultExpression(_methodSymbol.ReturnType)));

			statements.Add(Syntax.ReturnStatement(Syntax.FalseLiteralExpression()));
			return Syntax.ValueReturningLambdaExpression(parameters, statements);
		}
	}
}