// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Diagnostics;
	using ISSE.SafetyChecking.Modeling;
	using Roslyn.Symbols;
	using Modeling;
	/// <summary>
	///   Ensures that <see cref="Component" />-derived classes do not access certain<see cref="Fault" /> members.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp), UsedImplicitly]
	public sealed class FaultAnalyzer : Analyzer
	{
		/// <summary>
		///   The error diagnostic emitted by the analyzer when a fault effect overrides an abstract member.
		/// </summary>
		private static readonly DiagnosticInfo _invalidMemberAccess = DiagnosticInfo.Error(
			DiagnosticIdentifier.InvalidFaultMemberAccess,
			$"Classes derived from '{typeof(Component).FullName}' cannot access certain '{typeof(Fault).FullName}' members.",
			"Cannot access '{0}' here.");

		/// <summary>
		///   The members that cannot be accessed.
		/// </summary>
		private static readonly string[] _invalidMembers =
		{
			nameof(Fault.IsActivated),
			nameof(Fault.Activation),
			nameof(FaultExtensions.SuppressActivations),
			nameof(FaultExtensions.SuppressActivation),
			nameof(FaultExtensions.ForceActivations),
			nameof(FaultExtensions.ForceActivation),
			nameof(FaultExtensions.ToggleActivationMode)
		};

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public FaultAnalyzer()
			: base(_invalidMemberAccess)
		{
		}

		/// <summary>
		///   Called once at session start to register actions in the analysis context.
		/// </summary>
		protected override void Initialize(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleMemberAccessExpression);
		}

		/// <summary>
		///   Performs the analysis.
		/// </summary>
		/// <param name="context">The context in which the analysis should be performed.</param>
		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var semanticModel = context.SemanticModel;
			var memberAccess = (MemberAccessExpressionSyntax)context.Node;

			if (_invalidMembers.All(member => memberAccess?.Name?.Identifier.ValueText.Equals(member) != true))
				return;

			var enclosingSymbol = semanticModel.GetEnclosingSymbol(memberAccess.GetLocation().SourceSpan.Start);
			var methodSymbol = enclosingSymbol as IMethodSymbol;
			if (methodSymbol?.MethodKind == MethodKind.Constructor)
				return;

			if (!enclosingSymbol.ContainingType.IsComponent(semanticModel) && !enclosingSymbol.ContainingType.IsFaultEffect(semanticModel))
				return;

			var symbol = semanticModel.GetSymbolInfo(memberAccess).Symbol;
			if (symbol?.ContainingType == null)
				return;

			var isFaultMethod =
				symbol.ContainingType.Equals(semanticModel.GetTypeSymbol(typeof(Fault))) ||
				symbol.ContainingType.Equals(semanticModel.GetTypeSymbol(typeof(FaultExtensions)));

			if (isFaultMethod)
				_invalidMemberAccess.Emit(context, memberAccess.Name, symbol.ToDisplayString());
		}
	}
}