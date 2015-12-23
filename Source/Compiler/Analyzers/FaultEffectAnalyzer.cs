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
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.Diagnostics;
	using Modeling;
	using Roslyn.Symbols;

	/// <summary>
	///   Ensures that fault effect declarations are valid.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp), UsedImplicitly]
	public sealed class FaultEffectAnalyzer : Analyzer
	{
		/// <summary>
		///   The error diagnostic emitted by the analyzer when a fault effect is generic.
		/// </summary>
		private static readonly DiagnosticInfo _genericEffect = DiagnosticInfo.Error(
			DiagnosticIdentifier.GenericFaultEffect,
			"Fault effects are not allowed to declare any type parameters.",
			"'{0}' is not allowed to declare any type parameters.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when a fault effect has an invalid accessibility.
		/// </summary>
		private static readonly DiagnosticInfo _accessibility = DiagnosticInfo.Error(
			DiagnosticIdentifier.FaultEffectAccessibility,
			"Fault effect must have the same effective accessibility as its base type.",
			"Fault effect {0} must have the same effective accessibility as its base type. Try declaring the effect as 'public'.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when a fault effect is generic.
		/// </summary>
		private static readonly DiagnosticInfo _invalidBaseType = DiagnosticInfo.Error(
			DiagnosticIdentifier.InvalidFaultEffectBaseType,
			"Fault effects must be derived from non-fault effect components.",
			$"'{{0}}' is not allowed to derive from '{{1}}'; only non-fault effect classes derived from '{typeof(Component).FullName}' are allowed.");

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public FaultEffectAnalyzer()
			: base(_genericEffect, _accessibility, _invalidBaseType)
		{
		}

		/// <summary>
		///   Called once at session start to register actions in the analysis context.
		/// </summary>
		/// <param name="context">The analysis context that should be used to register analysis actions.</param>
		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
		}

		/// <summary>
		///   Performs the analysis.
		/// </summary>
		/// <param name="context">The context in which the analysis should be performed.</param>
		private static void Analyze(SymbolAnalysisContext context)
		{
			var compilation = context.Compilation;
			var symbol = context.Symbol as INamedTypeSymbol;

			if (symbol == null || !symbol.HasAttribute<FaultEffectAttribute>(compilation))
				return;

			if (symbol.Arity != 0)
				_genericEffect.Emit(context, symbol, symbol);

			if (symbol.BaseType.HasEffectivePublicAccessibility() && !symbol.HasEffectivePublicAccessibility())
				_accessibility.Emit(context, symbol, symbol);

			if (!symbol.BaseType.IsComponent(compilation))
				_invalidBaseType.Emit(context, symbol, symbol, symbol.BaseType);
		}
	}
}