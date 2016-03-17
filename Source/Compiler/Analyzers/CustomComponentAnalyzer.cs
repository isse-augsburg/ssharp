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
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.Diagnostics;
	using Modeling;
	using Roslyn.Symbols;

	/// <summary>
	///   Ensures that no class implements <see cref="IComponent" /> without being derived from <see cref="Component" /> and that
	///   components do not reimplement <see cref="IInitializable" />.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp), UsedImplicitly]
	public sealed class CustomComponentAnalyzer : Analyzer
	{
		/// <summary>
		///   The error emitted by the analyzer when the interface is implemented explicitly.
		/// </summary>
		private static readonly DiagnosticInfo _customComponent = DiagnosticInfo.Error(
			DiagnosticIdentifier.CustomComponent,
			$"A class cannot implement '{typeof(IComponent).FullName}' when it is not derived from '{typeof(Component).FullName}'.",
			$"Class '{{0}}' cannot implement '{typeof(IComponent).FullName}' explicitly; derive from '{typeof(Component).FullName}' instead.");

		/// <summary>
		///   The error emitted by the analyzer when the interface is reimplemented by a component class.
		/// </summary>
		private static readonly DiagnosticInfo _reimplementation = DiagnosticInfo.Error(
			DiagnosticIdentifier.ComponentInterfaceReimplementation,
			$"Interface '{typeof(IComponent).FullName}' cannot be reimplemented by a class derived from '{typeof(Component).FullName}'.",
			$"Class '{{0}}' cannot reimplement '{typeof(IComponent).FullName}'.");

		/// <summary>
		///   The error emitted by the analyzer when the interface is reimplemented by a component class.
		/// </summary>
		private static readonly DiagnosticInfo _initializable = DiagnosticInfo.Error(
			DiagnosticIdentifier.ComponentIsInitializable,
			$"Interface '{typeof(IInitializable).FullName}' cannot be reimplemented by a class derived from '{typeof(Component).FullName}'.",
			$"Component '{{0}}' cannot reimplement '{typeof(IInitializable).FullName}'. " +
			$"Override '{typeof(Component).FullName}.{nameof(Component.Update)}' instead.");

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public CustomComponentAnalyzer()
			: base(_customComponent, _reimplementation, _initializable)
		{
		}

		/// <summary>
		///   Called once at session start to register actions in the analysis context.
		/// </summary>
		protected override void Initialize(CompilationStartAnalysisContext context)
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
			var symbol = context.Symbol as ITypeSymbol;

			if (symbol == null || symbol.TypeKind != TypeKind.Class)
				return;

			var interfaceSymbol = context.Compilation.GetTypeSymbol<IComponent>();
			if (!symbol.AllInterfaces.Contains(interfaceSymbol))
				return;

			var initializableSymbol = context.Compilation.GetTypeSymbol<IInitializable>();
			var isComponent = symbol.IsDerivedFrom(compilation.GetTypeSymbol<Component>());

			if (isComponent && symbol.Interfaces.Any(i => i.Equals(interfaceSymbol)))
				_reimplementation.Emit(context, symbol, symbol.ToDisplayString());
			else if (isComponent && symbol.Interfaces.Any(i => i.Equals(initializableSymbol)))
				_initializable.Emit(context, symbol, symbol.ToDisplayString());
			else if (!isComponent)
				_customComponent.Emit(context, symbol, symbol.ToDisplayString());
		}
	}
}