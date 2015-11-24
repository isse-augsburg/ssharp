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
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Editing;
	using Modeling;
	using Roslyn.Symbols;
	using Roslyn.Syntax;
	using Utilities;

	/// <summary>
	///   Normalizes classes marked with <see cref="FaultEffectAttribute" />.
	/// </summary>
	public sealed class FaultEffectNormalizer : SyntaxNormalizer
	{
		private readonly Dictionary<string, string[]> _faults = new Dictionary<string, string[]>();

		/// <summary>
		///   Normalizes the syntax trees of the <see cref="Compilation" />.
		/// </summary>
		protected override Compilation Normalize()
		{
			foreach (var component in Compilation.GetTypeSymbols(type => type.IsComponent(Compilation)))
			{
				var faults = Compilation
					.GetTypeSymbols(type => type.IsFaultEffect(Compilation) && type.BaseType.Equals(component))
					.OrderBy(type => type.GetPriority(Compilation))
					.ThenBy(type => type.Name)
					.Select(type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

				_faults.Add(component.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), faults.ToArray());
			}

			return base.Normalize();
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax declaration)
		{
			var classSymbol = declaration.GetTypeSymbol(SemanticModel);
			if (!classSymbol.IsFaultEffect(SemanticModel))
			{
				if (classSymbol.IsComponent(SemanticModel))
					AddRuntimeTypeField(classSymbol);

				return base.VisitClassDeclaration(declaration);
			}

			AddFaultField(classSymbol);

			declaration = (ClassDeclarationSyntax)base.VisitClassDeclaration(declaration);
			return ChangeBaseType(classSymbol, declaration);
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax declaration)
		{
			var methodSymbol = declaration.GetMethodSymbol(SemanticModel);
			if (!methodSymbol.ContainingType.IsFaultEffect(SemanticModel) || !methodSymbol.IsOverride)
				return declaration;

			var memberAccess = Syntax.MemberAccessExpression(Syntax.BaseExpression(), methodSymbol.Name);
			var invocation = Syntax.InvocationExpression(memberAccess, CreateInvocationArguments(methodSymbol));

			declaration = declaration.WithBody(CreateBody(declaration.Body, invocation, !methodSymbol.ReturnsVoid));
			return declaration;
		}

		/// <summary>
		///   Normalizes the <paramref name="accessor" />.
		/// </summary>
		public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax accessor)
		{
			var methodSymbol = accessor.GetMethodSymbol(SemanticModel);
			if (!methodSymbol.ContainingType.IsFaultEffect(SemanticModel) || !methodSymbol.IsOverride)
				return accessor;

			var isGetAccessor = accessor.Kind() == SyntaxKind.GetAccessorDeclaration;
			var baseExpression = Syntax.MemberAccessExpression(Syntax.BaseExpression(), methodSymbol.GetPropertySymbol().Name);
			if (!isGetAccessor)
				baseExpression = Syntax.AssignmentStatement(baseExpression, Syntax.IdentifierName("value"));

			accessor = accessor.WithBody(CreateBody(accessor.Body, baseExpression, isGetAccessor));
			return accessor;
		}

		/// <summary>
		///   Creates the arguments for a delegate invocation.
		/// </summary>
		private static IEnumerable<ArgumentSyntax> CreateInvocationArguments(IMethodSymbol methodSymbol)
		{
			return methodSymbol.Parameters.Select(parameter =>
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
		///   Creates the default lambda method that is assigned to a fault delegate.
		/// </summary>
		private BlockSyntax CreateBody(StatementSyntax originalBody, SyntaxNode baseEffect, bool hasResult)
		{ 
			originalBody = originalBody.AppendLineDirective(-1).PrependLineDirective(originalBody.GetLineNumber());

			var faultAccess = Syntax.MemberAccessExpression(Syntax.ThisExpression(), "fault".ToSynthesized());
			var isOccurring = Syntax.MemberAccessExpression(faultAccess, nameof(Fault.IsOccurring));
			var notOccurring = Syntax.LogicalNotExpression(isOccurring);
			var baseStatement = hasResult
				? new[] { Syntax.ReturnStatement(baseEffect) }
				: new[] { Syntax.ExpressionStatement(baseEffect), Syntax.ReturnStatement()};

			var ifStatement = Syntax.IfStatement(notOccurring, baseStatement).NormalizeWhitespace().WithTrailingNewLines(1);
			return SyntaxFactory.Block((StatementSyntax)ifStatement, originalBody).PrependLineDirective(-1);
		}

		/// <summary>
		///   Changes the base type of the fault effect declaration based on its location in the fault effect list.
		/// </summary>
		private ClassDeclarationSyntax ChangeBaseType(INamedTypeSymbol classSymbol, ClassDeclarationSyntax classDeclaration)
		{
			var baseTypeName = classSymbol.BaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			var faultIndex = Array.IndexOf(_faults[baseTypeName], classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
			if (faultIndex == 0)
				return classDeclaration;

			var baseType = SyntaxFactory.ParseTypeName(_faults[baseTypeName][faultIndex - 1]).WithTrivia(classDeclaration.BaseList.Types[0]);
			var baseTypes = SyntaxFactory.SingletonSeparatedList((BaseTypeSyntax)SyntaxFactory.SimpleBaseType(baseType));
			var baseList = SyntaxFactory.BaseList(classDeclaration.BaseList.ColonToken, baseTypes).WithTrivia(classDeclaration.BaseList);
			return classDeclaration.WithBaseList(baseList);
		}

		/// <summary>
		///   Adds the runtime type field to the component symbol.
		/// </summary>
		private void AddRuntimeTypeField(INamedTypeSymbol classSymbol)
		{
			var className = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			var faults = _faults[className];
			var runtimeType = faults.Length > 0 ? faults[faults.Length - 1] : className;
			var typeofExpression = SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(runtimeType));
			var field = Syntax.FieldDeclaration(
				name: "runtimeType".ToSynthesized(),
				type: Syntax.TypeExpression<Type>(SemanticModel),
				accessibility: Accessibility.Private,
				modifiers: DeclarationModifiers.Static | DeclarationModifiers.ReadOnly,
				initializer: typeofExpression);

			field = Syntax.MarkAsNonDebuggerBrowsable(field, SemanticModel);
			field = Syntax.AddAttribute<HiddenAttribute>(field, SemanticModel);
			field = field.NormalizeWhitespace();

			AddMembers(classSymbol, (MemberDeclarationSyntax)field);
		}

		/// <summary>
		///   Adds the fault field to the fault effect symbol.
		/// </summary>
		private void AddFaultField(INamedTypeSymbol classSymbol)
		{
			var faultField = Syntax.FieldDeclaration(
				name: "fault".ToSynthesized(),
				type: Syntax.TypeExpression<Fault>(SemanticModel),
				accessibility: Accessibility.Private);

			faultField = Syntax.MarkAsNonDebuggerBrowsable(faultField, SemanticModel);
			faultField = Syntax.AddAttribute<HiddenAttribute>(faultField, SemanticModel);
			faultField = faultField.NormalizeWhitespace();

			AddMembers(classSymbol, (MemberDeclarationSyntax)faultField);
		}
	}
}