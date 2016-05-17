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
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Symbols;
	using Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="ArgumentSyntax" /> instances.
	/// </summary>
	public static class ArgumentExtensions
	{
		/// <summary>
		///   Gets the <see cref="IMethodSymbol" /> of the method that is called with <paramref name="argument" /> within the context
		///   of the <paramref name="semanticModel" />.
		/// </summary>
		/// <param name="argument">The argument the method symbol should be returned for.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve the method symbol.</param>
		[Pure, NotNull]
		public static IMethodSymbol GetMethodSymbol([NotNull] this ArgumentSyntax argument, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(argument, nameof(argument));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			var methodCallExpression = argument.GetInvocationExpression();
			return methodCallExpression.GetReferencedSymbol<IMethodSymbol>(semanticModel);
		}

		/// <summary>
		///   Checks whether the <see cref="IParameterSymbol" /> corresponding to the <paramref name="argument" /> of a
		///   method call is marked with an attribute of type <typeparamref name="T" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <typeparam name="T">
		///   The type of the attribute the method parameter corresponding to the <paramref name="argument" /> should
		///   be marked with.
		/// </typeparam>
		/// <param name="argument">The argument that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve symbols.</param>
		[Pure]
		public static bool HasAttribute<T>([NotNull] this ArgumentSyntax argument, [NotNull] SemanticModel semanticModel)
			where T : Attribute
		{
			Requires.NotNull(argument, nameof(argument));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			var parameterSymbol = argument.GetParameterSymbol(semanticModel);
			return parameterSymbol.HasAttribute<T>(semanticModel);
		}

		/// <summary>
		///   Checks whether <paramref name="argument" /> is of type <typeparamref name="T" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <typeparam name="T">The expected type of <paramref name="argument" />.</typeparam>
		/// <param name="argument">The argument that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve symbols.</param>
		[Pure]
		public static bool IsOfType<T>([NotNull] this ArgumentSyntax argument, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(argument, nameof(argument));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			var typeSymbol = semanticModel.GetTypeSymbol<T>();
			Requires.That(typeSymbol != null, "Unable to determine type symbol of type '{0}'.", typeof(T).FullName);

			return Equals(argument.GetParameterSymbol(semanticModel).Type, typeSymbol);
		}

		/// <summary>
		///   Checks whether <paramref name="argument" /> is of type <paramref name="argumentType" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <param name="argument">The argument that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve symbols.</param>
		/// <param name="argumentType">The expected type of <paramref name="argument" />.</param>
		[Pure]
		public static bool IsOfType([NotNull] this ArgumentSyntax argument, [NotNull] SemanticModel semanticModel,
									[NotNull] ITypeSymbol argumentType)
		{
			Requires.NotNull(argument, nameof(argument));
			Requires.NotNull(semanticModel, nameof(semanticModel));
			Requires.NotNull(argumentType, nameof(argumentType));

			return Equals(argument.GetParameterSymbol(semanticModel).Type, argumentType);
		}

		/// <summary>
		///   Gets the <see cref="InvocationExpressionSyntax" />, <see cref="ObjectCreationExpressionSyntax" />,
		///   <see cref="ConstructorInitializerSyntax" />, <see cref="ElementAccessExpressionSyntax" />, or
		///   <see cref="ImplicitElementAccessSyntax"/> that contains the <paramref name="argument" />.
		/// </summary>
		/// <param name="argument">The argument the method call expression should be returned for.</param>
		[Pure, NotNull]
		public static SyntaxNode GetInvocationExpression([NotNull] this ArgumentSyntax argument)
		{
			Requires.NotNull(argument, nameof(argument));

			for (var node = argument.Parent; node != null; node = node.Parent)
			{
				var isInvocation =
					node is InvocationExpressionSyntax ||
					node is ObjectCreationExpressionSyntax ||
					node is ConstructorInitializerSyntax ||
					node is ElementAccessExpressionSyntax ||
					node is ImplicitElementAccessSyntax;

				if (isInvocation)
					return node;
			}

			Assert.NotReached($"Unable to find the method call expression containing argument '{argument}'.");
			return null;
		}

		/// <summary>
		///   Gets the <see cref="IParameterSymbol" /> corresponding to <paramref name="argument" /> within the context of the
		///   <paramref name="semanticModel" />.
		/// </summary>
		/// <param name="argument">The argument the parameter symbol should be returned for.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve symbols.</param>
		/// <remarks>
		///   There might be an official Roslyn API one day that should be used to replace this method
		///   (see also https://roslyn.codeplex.com/discussions/541303).
		/// </remarks>
		[Pure, NotNull]
		public static IParameterSymbol GetParameterSymbol([NotNull] this ArgumentSyntax argument, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(argument, nameof(argument));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			var invocation = argument.GetInvocationExpression();
			Requires.That(!(invocation as InvocationExpressionSyntax)?.IsNameOfOperator(semanticModel) ?? true, 
				"Cannot get parameter symbol for nameof operator.");

			var methodSymbol = invocation.GetReferencedSymbol(semanticModel) as IMethodSymbol;
			var propertySymbol = invocation.GetReferencedSymbol(semanticModel) as IPropertySymbol;
			var parameterSymbols = methodSymbol?.Parameters ?? propertySymbol.Parameters;

			// If this is a named argument, simply look up the parameter symbol by name.
			if (argument.NameColon != null)
				return parameterSymbols.Single(parameter => parameter.Name == argument.NameColon.Name.Identifier.ValueText);

			// Otherwise, get the corresponding invocation or object creation expression and match the argument.
			var arguments = default(SeparatedSyntaxList<ArgumentSyntax>);
			var invocationExpression = invocation as InvocationExpressionSyntax;
			var objectCreationExpression = invocation as ObjectCreationExpressionSyntax;
			var constructorInitializer = invocation as ConstructorInitializerSyntax;
			var elementAccessExpression = invocation as ElementAccessExpressionSyntax;
			var implicitElementAccessExpression = invocation as ImplicitElementAccessSyntax;

			if (invocationExpression != null)
				arguments = invocationExpression.ArgumentList.Arguments;
			else if (objectCreationExpression != null)
				arguments = objectCreationExpression.ArgumentList.Arguments;
			else if (constructorInitializer != null)
				arguments = constructorInitializer.ArgumentList.Arguments;
			else if (elementAccessExpression != null)
				arguments = elementAccessExpression.ArgumentList.Arguments;
			else if (implicitElementAccessExpression != null)
				arguments = implicitElementAccessExpression.ArgumentList.Arguments;
			else
				Assert.NotReached("Expected an invocation expression or an object creation expression.");

			for (var i = 0; i < arguments.Count; ++i)
			{
				// If this is a method with a params parameter at the end, we might have more arguments than parameters. In that case,
				// return the parameter symbol for the params parameter if the argument exceeds the parameter count.
				if (i >= parameterSymbols.Length)
				{
					var lastParameter = parameterSymbols[methodSymbol.Parameters.Length - 1];
					if (lastParameter.IsParams)
						return lastParameter;

					Assert.NotReached("There are more arguments than parameters.");
				}

				if (arguments[i] == argument)
					return parameterSymbols[i];
			}

			Assert.NotReached($"Unable to determine parameter symbol for argument '{argument}'.");
			return null;
		}
	}
}