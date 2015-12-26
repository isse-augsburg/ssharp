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
	using System.Linq;
	using CompilerServices;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Roslyn.Symbols;
	using Roslyn.Syntax;

	/// <summary>
	///   Replaces the parameters of method invocations or object creations with a lifted lambda if the corresponding
	///   method argument has the <see cref="LiftExpressionAttribute" /> applied.
	///   For instance, with method M declared as <c>void M([LiftExpression] int a, int b)</c>:
	///   <code>
	///  		M(1 + 2, 4); 
	///  		// becomes:
	///  		M(() => 1 + 2, 4);
	/// 	</code>
	/// </summary>
	public sealed class LiftedExpressionNormalizer : Normalizer
	{
		/// <summary>
		///   A stack that indicates for each method call whether the call has lifted arguments.
		/// </summary>
		private readonly Stack<bool> _liftedMethodStack = new Stack<bool>();

		/// <summary>
		///   Checks whether we have to lift the arguments.
		/// </summary>
		public override SyntaxNode VisitElementAccessExpression(ElementAccessExpressionSyntax expression)
		{
			var isLifted = false;

			var typeSymbol = SemanticModel.GetTypeInfo(expression.Expression).ConvertedType;
			if (typeSymbol.TypeKind != TypeKind.Array && typeSymbol.TypeKind != TypeKind.Pointer)
			{
				var propertySymbol = expression.GetReferencedSymbol(SemanticModel) as IPropertySymbol;
				if (propertySymbol != null)
				{
					if (propertySymbol.Parameters.Any(parameter => parameter.HasAttribute<LiftExpressionAttribute>(SemanticModel)))
						isLifted = true;
				}
			}

			try
			{
				_liftedMethodStack.Push(isLifted);
				return base.VisitElementAccessExpression(expression);
			}
			finally
			{
				_liftedMethodStack.Pop();
			}
		}

		/// <summary>
		///   Checks whether we have to lift the arguments.
		/// </summary>
		public override SyntaxNode VisitConstructorInitializer(ConstructorInitializerSyntax expression)
		{
			var isLifted = false;

			var methodSymbol = SemanticModel.GetSymbolInfo(expression).Symbol as IMethodSymbol;
			if (methodSymbol != null)
			{
				if (methodSymbol.Parameters.Any(parameter => parameter.HasAttribute<LiftExpressionAttribute>(SemanticModel)))
					isLifted = true;
			}

			try
			{
				_liftedMethodStack.Push(isLifted);
				return base.VisitConstructorInitializer(expression);
			}
			finally
			{
				_liftedMethodStack.Pop();
			}
		}

		/// <summary>
		///   Checks whether we have to lift the arguments.
		/// </summary>
		public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax expression)
		{
			var isLifted = false;

			var methodSymbol = SemanticModel.GetSymbolInfo(expression).Symbol as IMethodSymbol;
			if (methodSymbol != null)
			{
				if (methodSymbol.Parameters.Any(parameter => parameter.HasAttribute<LiftExpressionAttribute>(SemanticModel)))
					isLifted = true;
			}

			try
			{
				_liftedMethodStack.Push(isLifted);
				return base.VisitInvocationExpression(expression);
			}
			finally
			{
				_liftedMethodStack.Pop();
			}
		}

		/// <summary>
		///   Checks whether we have to lift the arguments.
		/// </summary>
		public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax expression)
		{
			var isLifted = false;

			var methodSymbol = expression.GetReferencedSymbol(SemanticModel) as IMethodSymbol;
			if (methodSymbol != null)
			{
				if (methodSymbol.Parameters.Any(parameter => parameter.HasAttribute<LiftExpressionAttribute>(SemanticModel)))
					isLifted = true;
			}

			try
			{
				_liftedMethodStack.Push(isLifted);
				return base.VisitObjectCreationExpression(expression);
			}
			finally
			{
				_liftedMethodStack.Pop();
			}
		}

		/// <summary>
		///   Lifts the expression represented by <paramref name="argument" />, if necessary.
		/// </summary>
		public override SyntaxNode VisitArgument(ArgumentSyntax argument)
		{
			if (!_liftedMethodStack.Peek())
				return base.VisitArgument(argument);

			var requiresLifting = argument.HasAttribute<LiftExpressionAttribute>(SemanticModel);
			argument = (ArgumentSyntax)base.VisitArgument(argument);

			if (!requiresLifting)
				return argument;

			var lambda = Syntax.ValueReturningLambdaExpression(Enumerable.Empty<ParameterSyntax>(), argument.Expression).WithTrivia(argument);
			return argument.WithExpression((ExpressionSyntax)lambda);
		}
	}
}