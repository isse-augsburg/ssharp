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
	using System.Collections.Immutable;
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.Diagnostics;
	using ISSE.SafetyChecking.Utilities;

	/// <summary>
	///   A base class for S# code analyzers.
	/// </summary>
	public abstract class Analyzer : DiagnosticAnalyzer
	{
		/// <summary>
		///   The set of supported diagnostics.
		/// </summary>
		private readonly DiagnosticInfo[] _diagnostics;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="diagnostics">The diagnostics supported by the analyzer.</param>
		protected Analyzer([NotNull] params DiagnosticInfo[] diagnostics)
		{
			Requires.NotNull(diagnostics, nameof(diagnostics));
			Requires.That(diagnostics.Select(d => d.Id).Distinct().Count() == diagnostics.Length, "Duplicate diagnostic ids.");

			_diagnostics = diagnostics;
			SupportedDiagnostics = diagnostics.Select(message => message.Descriptor).ToImmutableArray();
		}

		/// <summary>
		///   Called once at session start to register actions in the analysis context.
		/// </summary>
		public sealed override void Initialize(AnalysisContext context)
		{
			context.RegisterCompilationStartAction(startContext =>
			{
				// Check if we compile with the CompilingSSharpCode preprocessor symbol defined; if so, deactivate all analyzers
				// as the code has already been normalized
				var syntaxTree = startContext.Compilation.SyntaxTrees.FirstOrDefault();
				if (syntaxTree == null)
					return;

				if (syntaxTree.Options.PreprocessorSymbolNames.Any(symbol => symbol == "CompilingSSharpCode"))
					return;

				Initialize(startContext);
			});
		}

		/// <summary>
		///   Called once at session start to register actions in the analysis context.
		/// </summary>
		protected abstract void Initialize(CompilationStartAnalysisContext context);

		/// <summary>
		///   Returns a set of descriptors for the diagnostics that this analyzer is capable of producing.
		/// </summary>
		public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

		/// <summary>
		///   Gets the <see cref="DiagnosticInfo" /> corresponding to the <paramref name="id" />.
		/// </summary>
		/// <param name="id">The diagnostic identifier the <see cref="DiagnosticInfo" /> should be returned for.</param>
		public DiagnosticInfo GetDiagnosticInfo(DiagnosticIdentifier id)
		{
			Requires.InRange(id, nameof(id));

			var diagnostics = _diagnostics.Where(d => d.Id == id).ToArray();
			Requires.That(diagnostics.Length == 1, $"Analyzer '{GetType().FullName}' does not emit diagnostic '{id}'.");

			return diagnostics[0];
		}
	}
}