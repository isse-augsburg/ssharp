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

namespace SafetySharp.Compiler.Analyzers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Diagnostics;
	using Roslyn.Symbols;
	using Roslyn.Syntax;

	/// <summary>
	///   Ensures that bindings resolve to a unique pair of bound ports.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp), UsedImplicitly]
	public sealed class BindingsAnalyzer : Analyzer
	{
		/// <summary>
		///   The error diagnostic emitted by the analyzer when a binding is cast to a non-delegate type.
		/// </summary>
		private static readonly DiagnosticInfo _nonDelegateBinding = DiagnosticInfo.Error(
			DiagnosticIdentifier.NonDelegateBinding,
			"Expected binding type to be a delegate.",
			"Expected binding type to be a delegate.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when a binding failed.
		/// </summary>
		private static readonly DiagnosticInfo _bindingFailure = DiagnosticInfo.Error(
			DiagnosticIdentifier.BindingFailure,
			"There are no accessible signature-compatible ports that could be bound.",
			"There are no accessible signature-compatible ports that could be bound. Candidate required ports: {0}. Candidate provided ports: {1}.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when a binding is ambiguous.
		/// </summary>
		private static readonly DiagnosticInfo _ambiguousBinding = DiagnosticInfo.Error(
			DiagnosticIdentifier.AmbiguousBinding,
			"There are multiple signature-compatible ports that could be bound.",
			"Port binding is ambiguous: There are multiple accessible and signature-compatible ports " +
			"that could be bound. You can disambiguate the binding by explicitly specifying a " +
			"delegate type with the signature of the ports you intend to use. For instance, use 'Bind<Action<int>>(...)'" +
			"if the ports you want to bind are signature-compatible to the 'System.Action<int>' " +
			"delegate. Candidate required ports: {0}. Candidate provided ports: {1}.");

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public BindingsAnalyzer()
			: base(_bindingFailure, _ambiguousBinding, _nonDelegateBinding)
		{
		}

		/// <summary>
		///   Called once at session start to register actions in the analysis context.
		/// </summary>
		/// <param name="context" />
		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
		}

		/// <summary>
		///   Performs the analysis on the given binding.
		/// </summary>
		/// <param name="context">The context in which the analysis should be performed.</param>
		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var semanticModel = context.SemanticModel;
			var expression = (InvocationExpressionSyntax)context.Node;

			var methodSymbol = semanticModel.GetSymbolInfo(expression.Expression).Symbol as IMethodSymbol;
			if (methodSymbol == null || !methodSymbol.IsBindMethod(semanticModel))
				return;

			if (methodSymbol.Arity == 1 && methodSymbol.TypeArguments[0].TypeKind != TypeKind.Delegate)
				_nonDelegateBinding.Emit(context, expression);

			var requiredPortReferenceExpression = (InvocationExpressionSyntax)expression.ArgumentList.Arguments[0].Expression;
			var providedPortReferenceExpression = (InvocationExpressionSyntax)expression.ArgumentList.Arguments[1].Expression;

			var requiredPorts = requiredPortReferenceExpression.ResolvePortReferences(semanticModel);
			var providedPorts = providedPortReferenceExpression.ResolvePortReferences(semanticModel);

			requiredPorts.RemoveWhere(symbol => !symbol.IsRequiredPort(semanticModel));
			providedPorts.RemoveWhere(symbol => !symbol.IsProvidedPort(semanticModel));

			var requiredPortCandidates = requiredPorts.ToArray();
			var providedPortCandidates = providedPorts.ToArray();

			if (methodSymbol.Arity == 1)
			{
				var delegateType = (INamedTypeSymbol)methodSymbol.TypeArguments[0];
				MethodSymbolFilter.Filter(requiredPorts, delegateType);
				MethodSymbolFilter.Filter(providedPorts, delegateType);
			}
			else
				MethodSymbolFilter.Filter(requiredPorts, providedPorts);

			if (requiredPorts.Count == 0 || providedPorts.Count == 0)
				_bindingFailure.Emit(context, expression, PortSetToString(requiredPortCandidates), PortSetToString(providedPortCandidates));
			else if (requiredPorts.Count > 1 || providedPorts.Count > 1)
				_ambiguousBinding.Emit(context, expression, PortSetToString(requiredPorts.ToArray()), PortSetToString(providedPorts.ToArray()));
		}

		/// <summary>
		///   Gets a string representation of <paramref name="ports" /> for inclusing in a diagnostic message.
		/// </summary>
		/// <param name="ports">The ports that should be included in the string.</param>
		private static string PortSetToString(IMethodSymbol[] ports)
		{
			return ports.Length == 0 ? "<none>" : String.Join(", ", ports.Select(port => $"'{port.ToDisplayString()}'"));
		}
	}
}