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

namespace SafetySharp.Compiler.Analyzers
{
	using System;
	using System.Linq;
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
		///   The warning diagnostic emitted by the analyzer when a member is affected by multiple non-prioritized fault effects.
		/// </summary>
		private static readonly DiagnosticInfo _multipleEffectsWithoutPriority = DiagnosticInfo.Warning(
			DiagnosticIdentifier.MultipleFaultEffectsWithoutPriority,
			"Detected multiple fault effects affecting the same member.",
			"'{0}' is affected by multiple fault effects without an explicit priority specification, resulting in possibly " +
			"unintended additional nondeterminism. Consider making priorities explicit by adding the " +
			$"'{typeof(PriorityAttribute).FullName}' to: {{1}}.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when a fault effect overrides an abstract member.
		/// </summary>
		private static readonly DiagnosticInfo _abstractOverride = DiagnosticInfo.Error(
			DiagnosticIdentifier.AbstractFaultEffectOverride,
			"Fault effects cannot override abstract members.",
			"'{0}' cannot override abstract member '{1}'.");

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
			"Fault effect '{0}' must have the same effective accessibility as its base type. Try declaring the effect as 'public'.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when a fault effect is generic.
		/// </summary>
		private static readonly DiagnosticInfo _invalidBaseType = DiagnosticInfo.Error(
			DiagnosticIdentifier.InvalidFaultEffectBaseType,
			"Fault effects must be derived from non-fault effect components.",
			$"'{{0}}' is not allowed to derive from '{{1}}'; only non-fault effect classes derived from '{typeof(Component).FullName}' are allowed.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when a fault effect has a closed generic base type.
		/// </summary>
		private static readonly DiagnosticInfo _closedGenericBaseType = DiagnosticInfo.Error(
			DiagnosticIdentifier.ClosedGenericBaseType,
			"Fault effects cannot specify type parameters for their base type.",
			"'{0}' is not allowed to specify any type parameters for base type '{1}'.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when a fault effect is sealed.
		/// </summary>
		private static readonly DiagnosticInfo _sealedEffect = DiagnosticInfo.Error(
			DiagnosticIdentifier.SealedFaultEffect, "Fault effects cannot be sealed.", "Fault effect '{0}' is not allowed to be sealed.");

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public FaultEffectAnalyzer()
			: base(_genericEffect, _accessibility, _invalidBaseType, _abstractOverride, _multipleEffectsWithoutPriority, _sealedEffect, _closedGenericBaseType)
		{
		}

		/// <summary>
		///   Called once at session start to register actions in the analysis context.
		/// </summary>
		protected override void Initialize(CompilationStartAnalysisContext context)
		{
			context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
			context.RegisterSymbolAction(AnalyzeMember, SymbolKind.Method, SymbolKind.Property);
			context.RegisterCompilationEndAction(AnalyzeCompilation);
		}

		/// <summary>
		///   Performs the analysis.
		/// </summary>
		/// <param name="context">The context in which the analysis should be performed.</param>
		private static void AnalyzeCompilation(CompilationAnalysisContext context)
		{
			var compilation = context.Compilation;
			var types = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type).OfType<INamedTypeSymbol>().ToArray();
			var components = types.Where(type => type.IsComponent(compilation)).ToArray();
			var faultEffects = types.Where(type => type.IsFaultEffect(compilation) && !type.HasAttribute<PriorityAttribute>(compilation)).ToArray();

			foreach (var component in components)
			{
				var effects = faultEffects.Where(faultEffect => faultEffect.BaseType.Equals(component)).ToArray();
				var nondeterministic = effects.GroupBy(fault => fault.GetPriority(compilation)).Where(group => group.Count() > 1).ToArray();

				foreach (var method in component.GetFaultAffectableMethods(context.Compilation))
				{
					var unprioritizedTypes = nondeterministic
						.Where(typeGroup => typeGroup.Count(f => f.GetMembers().OfType<IMethodSymbol>().Any(m => m.Overrides(method))) > 1)
						.SelectMany(typeGroup => typeGroup)
						.Where(type => type.GetMembers().OfType<IMethodSymbol>().Any(m => m.Overrides(method)))
						.Select(type => $"'{type.ToDisplayString()}'")
						.OrderBy(type => type)
						.ToArray();

					if (unprioritizedTypes.Length <= 0)
						continue;

					if (method.ContainingType.Equals(component))
						_multipleEffectsWithoutPriority.Emit(context, method, method.ToDisplayString(), String.Join(", ", unprioritizedTypes));
					else
						_multipleEffectsWithoutPriority.Emit(context, component, method.ToDisplayString(), String.Join(", ", unprioritizedTypes));
				}
			}
		}

		/// <summary>
		///   Performs the analysis.
		/// </summary>
		/// <param name="context">The context in which the analysis should be performed.</param>
		private static void AnalyzeType(SymbolAnalysisContext context)
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

			if (symbol.IsSealed)
				_sealedEffect.Emit(context, symbol, symbol.ToDisplayString());

			if (symbol.BaseType.TypeArguments.Any(a => a.TypeKind != TypeKind.TypeParameter))
				_closedGenericBaseType.Emit(context, symbol, symbol.ToDisplayString(), symbol.BaseType.ConstructedFrom.ToDisplayString());
		}

		/// <summary>
		///   Performs the analysis.
		/// </summary>
		/// <param name="context">The context in which the analysis should be performed.</param>
		private static void AnalyzeMember(SymbolAnalysisContext context)
		{
			if (!context.Symbol.ContainingType.IsFaultEffect(context.Compilation))
				return;

			var methodSymbol = context.Symbol as IMethodSymbol;
			var propertySymbol = context.Symbol as IPropertySymbol;

			if (methodSymbol != null && !methodSymbol.IsPropertyAccessor())
			{
				if (methodSymbol.IsOverride && methodSymbol.OverriddenMethod.IsAbstract)
					_abstractOverride.Emit(context, methodSymbol, methodSymbol, methodSymbol.OverriddenMethod);
			}

			if (propertySymbol != null)
			{
				if (propertySymbol.IsOverride && propertySymbol.OverriddenProperty.IsAbstract)
					_abstractOverride.Emit(context, propertySymbol, propertySymbol, propertySymbol.OverriddenProperty);
			}
		}
	}
}