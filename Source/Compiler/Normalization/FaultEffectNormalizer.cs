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
	using System.Diagnostics;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Modeling;
	using Roslyn.Symbols;
	using Roslyn.Syntax;

	/// <summary>
	///   Normalizes classes marked with <see cref="FaultEffectAttribute" /> by implementing the <see cref="IFaultEffect" />
	///   interface and rewiring all base class accesses to the actual component instance.
	/// </summary>
	public sealed class FaultEffectNormalizer : SyntaxNormalizer
	{
		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax declaration)
		{
			var classSymbol = declaration.GetTypeSymbol(SemanticModel);
			if (classSymbol.IsFaultEffect(SemanticModel))
			{
				var componentType = Syntax.GetBaseAndInterfaceTypes(declaration)[0];
				var componentFieldName = "component".ToSynthesized();
				var componentField = Syntax.FieldDeclaration(
					name: componentFieldName,
					type: componentType,
					accessibility: Accessibility.Private);

				componentField = Syntax.MarkAsNonDebuggerBrowsable(componentField, SemanticModel);
				componentField = Syntax.AddAttribute<HiddenAttribute>(componentField, SemanticModel);
				componentField = componentField.NormalizeWhitespace();

				var componentProperty = Syntax.PropertyDeclaration(
					name: nameof(IFaultEffect.Component),
					type: Syntax.TypeExpression<IComponent>(SemanticModel),
					getAccessorStatements: new[] { Syntax.ReturnStatement(Syntax.IdentifierName(componentFieldName)) },
					setAccessorStatements: new[]
					{
						Syntax.AssignmentStatement(Syntax.IdentifierName(componentFieldName),
							Syntax.CastExpression(componentType, Syntax.IdentifierName("value")))
					});

				componentProperty = Syntax.AsPrivateInterfaceImplementation(componentProperty, Syntax.TypeExpression<IFaultEffect>(SemanticModel));
				componentProperty = Syntax.AddAttribute<DebuggerHiddenAttribute>(componentProperty, SemanticModel);
				componentProperty = Syntax.MarkAsNonDebuggerBrowsable(componentProperty, SemanticModel);
				componentProperty = componentProperty.NormalizeWhitespace();

				var faultFieldName = "fault".ToSynthesized();
				var faultField = Syntax.FieldDeclaration(
					name: faultFieldName,
					type: Syntax.TypeExpression<Fault>(SemanticModel),
					accessibility: Accessibility.Private);

				faultField = Syntax.MarkAsNonDebuggerBrowsable(faultField, SemanticModel);
				faultField = Syntax.AddAttribute<HiddenAttribute>(faultField, SemanticModel);
				faultField = faultField.NormalizeWhitespace();

				var faultProperty = Syntax.PropertyDeclaration(
					name: nameof(IFaultEffect.Fault),
					type: Syntax.TypeExpression<Fault>(SemanticModel),
					getAccessorStatements: new[] { Syntax.ReturnStatement(Syntax.IdentifierName(faultFieldName)) },
					setAccessorStatements: new[] { Syntax.AssignmentStatement(Syntax.IdentifierName(faultFieldName), Syntax.IdentifierName("value")) });

				faultProperty = Syntax.AsPrivateInterfaceImplementation(faultProperty, Syntax.TypeExpression<IFaultEffect>(SemanticModel));
				faultProperty = Syntax.AddAttribute<DebuggerHiddenAttribute>(faultProperty, SemanticModel);
				faultProperty = Syntax.MarkAsNonDebuggerBrowsable(faultProperty, SemanticModel);
				faultProperty = faultProperty.NormalizeWhitespace();

				AddBaseTypes(classSymbol, SemanticModel.GetTypeSymbol<IFaultEffect>());
				AddMembers(classSymbol,
					(MemberDeclarationSyntax)componentField,
					(MemberDeclarationSyntax)faultField,
					(MemberDeclarationSyntax)componentProperty,
					(MemberDeclarationSyntax)faultProperty);
			}

			return base.VisitClassDeclaration(declaration);
		}
	}
}