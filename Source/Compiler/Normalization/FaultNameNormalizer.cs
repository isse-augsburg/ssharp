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

namespace SafetySharp.Compiler.Normalization
{
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Modeling;
	using Roslyn.Symbols;
	using Roslyn.Syntax;

	/// <summary>
	///   Assigns default name to fault instantiations.
	/// </summary>
	public class FaultNameNormalizer : Normalizer
	{
		/// <summary>
		///   Normalizes the <paramref name="assignment" />.
		/// </summary>
		public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax assignment)
		{
			var objectCreation = assignment.Right as ObjectCreationExpressionSyntax;
			if (objectCreation == null)
				return assignment;

			var fault = SemanticModel.GetTypeSymbol<Fault>();
			if (SemanticModel.GetTypeInfo(objectCreation).Type?.IsDerivedFrom(fault) == false)
				return assignment;

			var targetSymbol = SemanticModel.GetSymbolInfo(assignment.Left).Symbol;
			if (targetSymbol == null || (targetSymbol.Kind != SymbolKind.Field && targetSymbol.Kind != SymbolKind.Property))
				return assignment;

			return assignment.WithRight(AddNameInitializer(fault, objectCreation, targetSymbol.Name));
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax declaration)
		{
			var objectCreation = declaration?.Initializer?.Value as ObjectCreationExpressionSyntax;
			if (objectCreation == null)
				return declaration;

			var symbol = declaration.GetPropertySymbol(SemanticModel);

			var fault = SemanticModel.GetTypeSymbol<Fault>();
			if (!symbol.Type.Equals(fault) && !symbol.Type.IsDerivedFrom(fault))
				return declaration;

			return declaration.WithInitializer(declaration.Initializer.WithValue(AddNameInitializer(fault, objectCreation, symbol.Name)));
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax declaration)
		{
			var objectCreation = declaration?.Initializer?.Value as ObjectCreationExpressionSyntax;
			if (objectCreation == null)
				return declaration;

			var symbol = SemanticModel.GetDeclaredSymbol(declaration);
			if (symbol == null)
				return declaration;

			ITypeSymbol type;
			string name;

			switch (symbol.Kind)
			{
				case SymbolKind.Field:
					var fieldSymbol = ((IFieldSymbol)symbol);
					type = fieldSymbol.Type;
					name = fieldSymbol.Name;
					break;
				case SymbolKind.Local:
					var localSymbol = ((ILocalSymbol)symbol);
					type = localSymbol.Type;
					name = localSymbol.Name;
					break;
				default:
					return declaration;
			}

			var fault = SemanticModel.GetTypeSymbol<Fault>();
			if (!type.Equals(fault) && !type.IsDerivedFrom(fault))
				return declaration;

			return declaration.WithInitializer(declaration.Initializer.WithValue(AddNameInitializer(fault, objectCreation, name)));
		}

		/// <summary>
		///   Adds the name initializer to the creation expression.
		/// </summary>
		private ObjectCreationExpressionSyntax AddNameInitializer(ITypeSymbol fault, ObjectCreationExpressionSyntax expression, string name)
		{
			if (expression.Initializer != null)
			{
				foreach (var initializer in expression.Initializer.Expressions)
				{
					var assignment = initializer as AssignmentExpressionSyntax;
					var symbol = assignment?.Left.GetReferencedSymbol(SemanticModel) as IPropertySymbol;

					if (symbol != null && symbol.ContainingType.Equals(fault) && symbol.Name == nameof(Fault.Name))
						return expression;
				}
			}

			var nameExpression = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("Name"),
				(ExpressionSyntax)Syntax.LiteralExpression(name));
			var expressions = SyntaxFactory.SingletonSeparatedList((ExpressionSyntax)nameExpression);
			var objectInitializer = expression.Initializer != null
				? expression.Initializer.AddExpressions(nameExpression)
				: SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, expressions);

			return expression.WithInitializer(objectInitializer);
		}
	}
}