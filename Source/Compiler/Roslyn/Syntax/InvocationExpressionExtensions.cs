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
	using System.Collections.Generic;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="InvocationExpressionSyntax" /> instances.
	/// </summary>
	public static class InvocationExpressionExtensions
	{
		/// <summary>
		///   Checks whether the <paramref name="invocationExpression" /> represents the application of the <c>nameof</c> operator
		///   within the context of the <paramref name="semanticModel" />.
		/// </summary>
		/// <param name="invocationExpression">The invocation expression that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve the invoked symbol.</param>
		[Pure]
		public static bool IsNameOfOperator([NotNull] this InvocationExpressionSyntax invocationExpression, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(invocationExpression, nameof(invocationExpression));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			if (semanticModel.GetSymbolInfo(invocationExpression).Symbol != null)
				return false;

			if (invocationExpression.Parent.Kind() == SyntaxKind.SimpleMemberAccessExpression)
				return false;

			if (invocationExpression.Parent.Kind() == SyntaxKind.PointerMemberAccessExpression)
				return false;

			return true;
		}

		/// <summary>
		///   Resolves a port reference candidate, i.e., a <c>nameof(portName)</c> expression, to the set of symbols it binds to.
		/// </summary>
		/// <param name="invocationExpression">The invocation expression that should be resolved.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve the referenced port symbol.</param>
		[Pure, NotNull]
		private static IEnumerable<ISymbol> ResolvePortCandidates([NotNull] this InvocationExpressionSyntax invocationExpression,
																  [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(invocationExpression, nameof(invocationExpression));
			Requires.NotNull(semanticModel, nameof(semanticModel));
			Requires.That(invocationExpression.IsNameOfOperator(semanticModel), nameof(invocationExpression), "Expected a port reference.");

			var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression.ArgumentList.Arguments[0].Expression);
			if (symbolInfo.Symbol != null)
				return new[] { symbolInfo.Symbol };

			return symbolInfo.CandidateSymbols;
		}

		/// <summary>
		///   Resolves a port reference, i.e., a <c>nameof(portName)</c> expression, to the set of <see cref="IMethodSymbol" />s it
		///   binds to.
		/// </summary>
		/// <param name="invocationExpression">The invocation expression that should be resolved.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve the referenced port symbol.</param>
		[Pure, NotNull]
		public static HashSet<IMethodSymbol> ResolvePortReferences([NotNull] this InvocationExpressionSyntax invocationExpression,
																   [NotNull] SemanticModel semanticModel)
		{
			var set = new HashSet<IMethodSymbol>();

			foreach (var candidate in invocationExpression.ResolvePortCandidates(semanticModel))
			{
				switch (candidate.Kind)
				{
					case SymbolKind.Method:
						set.Add((IMethodSymbol)candidate);
						break;
					case SymbolKind.Property:
						var property = (IPropertySymbol)candidate;
						if (property.GetMethod != null)
							set.Add(property.GetMethod);
						if (property.SetMethod != null)
							set.Add(property.SetMethod);
						break;
					default:
						Assert.NotReached($"Unsupported port symbol kind: '{candidate.Kind}'.");
						break;
				}
			}

			return set;
		}
	}
}