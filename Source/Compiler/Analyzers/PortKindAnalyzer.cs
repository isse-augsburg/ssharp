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
	///   Ensures that a method or property marked with the <see cref="ProvidedAttribute" /> is not <c>extern</c>.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp), UsedImplicitly]
	public sealed class PortKindAnalyzer : Analyzer
	{
		/// <summary>
		///   The error diagnostic emitted by the analyzer when the update method is extern.
		/// </summary>
		private static readonly DiagnosticInfo _externUpdateMethod = DiagnosticInfo.Error(
			DiagnosticIdentifier.ExternUpdateMethod,
			"A component's Update method cannot be extern.",
			$"'{{0}}' cannot be extern as it overrides '{typeof(Component).FullName}.Update()'.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when a provided port is extern.
		/// </summary>
		private static readonly DiagnosticInfo _externProvidedPort = DiagnosticInfo.Error(
			DiagnosticIdentifier.ExternProvidedPort,
			$"A method or property marked with '{typeof(ProvidedAttribute).FullName}' cannot be extern.",
			"Provided port '{0}' cannot be extern.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when a required port is not extern.
		/// </summary>
		private static readonly DiagnosticInfo _nonExternRequiredPort = DiagnosticInfo.Error(
			DiagnosticIdentifier.NonExternRequiredPort,
			$"A method or property marked with '{typeof(RequiredAttribute).FullName}' must be extern.",
			"Required port '{0}' must be extern.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when a port is static.
		/// </summary>
		private static readonly DiagnosticInfo _staticPort = DiagnosticInfo.Error(
			DiagnosticIdentifier.StaticPort, "A port cannot be static.", "Port '{0}' cannot be static.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when the update method is marked as a port.
		/// </summary>
		private static readonly DiagnosticInfo _updateMethodMarkedAsPort = DiagnosticInfo.Error(
			DiagnosticIdentifier.UpdateMethodMarkedAsPort,
			$"A component's Update method cannot be marked with '{typeof(RequiredAttribute).FullName}'.",
			$"'{{0}}' overrides '{typeof(Component).FullName}.Update()' and therefore cannot be marked with '{typeof(RequiredAttribute).FullName}'.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer if a method or property is marked as both required and provided.
		/// </summary>
		private static readonly DiagnosticInfo _ambiguousPortKind = DiagnosticInfo.Error(
			DiagnosticIdentifier.AmbiguousPortKind,
			$"A method or property cannot be marked with both '{typeof(RequiredAttribute).FullName}' and '{typeof(ProvidedAttribute).FullName}'.",
			$"'{{0}}' cannot be marked with both '{typeof(RequiredAttribute).FullName}' and '{typeof(ProvidedAttribute).FullName}'.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when a property accessor is marked as either required or provided.
		/// </summary>
		private static readonly DiagnosticInfo _portPropertyAccessor = DiagnosticInfo.Error(
			DiagnosticIdentifier.PortPropertyAccessor,
			$"Property getters and setters cannot be marked with either '{typeof(RequiredAttribute).FullName}' or '{typeof(ProvidedAttribute).FullName}'.",
			$"'{{0}}' cannot be marked with either '{typeof(RequiredAttribute).FullName}' or '{typeof(ProvidedAttribute).FullName}'.");

		/// <summary>
		///   The error diagnostic emitted by the analyzer when an interface method or property is unmarked.
		/// </summary>
		private static readonly DiagnosticInfo _unmarkedInterfacePort = DiagnosticInfo.Error(
			DiagnosticIdentifier.UnmarkedInterfacePort,
			$"A method or property within a component interface must be marked with either '{typeof(RequiredAttribute).FullName}' or '{typeof(ProvidedAttribute).FullName}'.",
			$"'{{0}}' must be marked with either '{typeof(RequiredAttribute).FullName}' or '{typeof(ProvidedAttribute).FullName}'.");

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public PortKindAnalyzer()
			: base(_externProvidedPort, _nonExternRequiredPort, _ambiguousPortKind, _updateMethodMarkedAsPort, _staticPort,
				_externUpdateMethod, _portPropertyAccessor, _unmarkedInterfacePort)
		{
		}

		/// <summary>
		///   Called once at session start to register actions in the analysis context.
		/// </summary>
		/// <param name="context">The analysis context that should be used to register analysis actions.</param>
		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSymbolAction(Analyze, SymbolKind.Method, SymbolKind.Property);
		}

		/// <summary>
		///   Performs the analysis.
		/// </summary>
		/// <param name="context">The context in which the analysis should be performed.</param>
		private static void Analyze(SymbolAnalysisContext context)
		{
			var compilation = context.Compilation;
			var symbol = context.Symbol;

			if (!symbol.ContainingType.AllInterfaces.Contains(compilation.GetTypeSymbol<IComponent>()))
				return;

			var methodSymbol = symbol as IMethodSymbol;
			var hasRequiredAttribute = symbol.HasAttribute<RequiredAttribute>(compilation);
			var hasProvidedAttribute = symbol.HasAttribute<ProvidedAttribute>(compilation);

			if (symbol.IsStatic)
			{
				if (hasRequiredAttribute || hasProvidedAttribute)
					_staticPort.Emit(context, symbol, symbol.ToDisplayString());
			}

			var isAccessor = methodSymbol?.AssociatedSymbol is IPropertySymbol;
			if (isAccessor)
			{
				if (hasProvidedAttribute || hasRequiredAttribute)
					_portPropertyAccessor.Emit(context, symbol, symbol.ToDisplayString());

				return;
			}

			if (methodSymbol != null && methodSymbol.IsUpdateMethod(compilation))
			{
				if (hasRequiredAttribute || hasProvidedAttribute)
					_updateMethodMarkedAsPort.Emit(context, symbol, symbol.ToDisplayString());
				else if (methodSymbol.IsExtern)
					_externUpdateMethod.Emit(context, symbol, symbol.ToDisplayString());

				return;
			}

			if (hasRequiredAttribute && hasProvidedAttribute)
			{
				_ambiguousPortKind.Emit(context, symbol, symbol.ToDisplayString());
				return;
			}

			if (symbol.ContainingType.TypeKind == TypeKind.Interface)
			{
				if (!hasRequiredAttribute && !hasProvidedAttribute)
					_unmarkedInterfacePort.Emit(context, symbol, symbol.ToDisplayString());
			}
			else
			{
				if (hasProvidedAttribute && symbol.IsExtern)
					_externProvidedPort.Emit(context, symbol, symbol.ToDisplayString());
				else if (hasRequiredAttribute && !symbol.IsExtern)
					_nonExternRequiredPort.Emit(context, symbol, symbol.ToDisplayString());
			}
		}
	}
}