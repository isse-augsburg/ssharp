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
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.Diagnostics;
	using Utilities;

	/// <summary>
	///   Represents a diagnostic produced by a <see cref="Analyzer" />, providing information about errors and
	///   warnings in a S# model.
	/// </summary>
	public class DiagnosticInfo
	{
		/// <summary>
		///   The prefix that is used for all diagnostic identifiers.
		/// </summary>
		public const string Prefix = "SS";

		/// <summary>
		///   The category that is used for all diagnostics.
		/// </summary>
		public const string Category = "SafetySharp";

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		private DiagnosticInfo()
		{
		}

		/// <summary>
		///   Gets the descriptor for the diagnostic emitted by the analyzer.
		/// </summary>
		public DiagnosticDescriptor Descriptor { get; private set; }

		/// <summary>
		///   Gets the identifier of the diagnostic.
		/// </summary>
		public DiagnosticIdentifier Id { get; private set; }

		/// <summary>
		///   Describes the error diagnostic of the analyzer.
		/// </summary>
		/// <param name="identifier">The identifier of the analyzer's diagnostic.</param>
		/// <param name="description">The description of the diagnostic.</param>
		/// <param name="messageFormat">The message format of the diagnostic.</param>
		public static DiagnosticInfo Error(DiagnosticIdentifier identifier, [NotNull] string description, [NotNull] string messageFormat)
		{
			return Initialize(identifier, description, messageFormat, DiagnosticSeverity.Error);
		}

		/// <summary>
		///   Describes the error diagnostic of the analyzer.
		/// </summary>
		/// <param name="identifier">The identifier of the analyzer's diagnostic.</param>
		/// <param name="description">The description of the diagnostic.</param>
		/// <param name="messageFormat">The message format of the diagnostic.</param>
		public static DiagnosticInfo Warning(DiagnosticIdentifier identifier, [NotNull] string description, [NotNull] string messageFormat)
		{
			return Initialize(identifier, description, messageFormat, DiagnosticSeverity.Warning);
		}

		/// <summary>
		///   Describes the error diagnostic of the analyzer.
		/// </summary>
		/// <param name="identifier">The identifier of the analyzer's diagnostic.</param>
		/// <param name="description">The description of the diagnostic.</param>
		/// <param name="messageFormat">The message format of the diagnostic.</param>
		/// <param name="severity">The severity of the diagnostic.</param>
		private static DiagnosticInfo Initialize(DiagnosticIdentifier identifier, [NotNull] string description,
												 [NotNull] string messageFormat, DiagnosticSeverity severity)
		{
			Requires.NotNullOrWhitespace(description, nameof(description));
			Requires.NotNullOrWhitespace(messageFormat, nameof(messageFormat));
			Requires.InRange(severity, nameof(severity));

			return new DiagnosticInfo
			{
				Descriptor = new DiagnosticDescriptor(Prefix + (int)identifier, description, messageFormat, Category, severity, true),
				Id = identifier
			};
		}

		/// <summary>
		///   Emits a diagnostic for <paramref name="symbol" /> using the <paramref name="messageArgs" /> to format the
		///   diagnostic message.
		/// </summary>
		/// <param name="context">The context in which the diagnostic should be emitted.</param>
		/// <param name="symbol">The symbol node the diagnostic is emitted for.</param>
		/// <param name="messageArgs">The arguments for formatting the diagnostic message.</param>
		public void Emit(CompilationAnalysisContext context, [NotNull] ISymbol symbol, params object[] messageArgs)
		{
			context.ReportDiagnostic(CreateDiagnostic(symbol.Locations[0], messageArgs));
		}

		/// <summary>
		///   Emits a diagnostic for <paramref name="syntaxNode" /> using the <paramref name="messageArgs" /> to format the
		///   diagnostic message.
		/// </summary>
		/// <param name="context">The context in which the diagnostic should be emitted.</param>
		/// <param name="syntaxNode">The syntax node the diagnostic is emitted for.</param>
		/// <param name="messageArgs">The arguments for formatting the diagnostic message.</param>
		public void Emit(SyntaxNodeAnalysisContext context, [NotNull] SyntaxNode syntaxNode, params object[] messageArgs)
		{
			context.ReportDiagnostic(CreateDiagnostic(syntaxNode.GetLocation(), messageArgs));
		}

		/// <summary>
		///   Emits a diagnostic for <paramref name="syntaxNode" /> using the <paramref name="messageArgs" /> to format the
		///   diagnostic message.
		/// </summary>
		/// <param name="context">The context in which the diagnostic should be emitted.</param>
		/// <param name="syntaxNode">The syntax node the diagnostic is emitted for.</param>
		/// <param name="messageArgs">The arguments for formatting the diagnostic message.</param>
		public void Emit(SyntaxTreeAnalysisContext context, [NotNull] SyntaxNode syntaxNode, params object[] messageArgs)
		{
			context.ReportDiagnostic(CreateDiagnostic(syntaxNode.GetLocation(), messageArgs));
		}

		/// <summary>
		///   Emits a diagnostic for <paramref name="syntaxToken" /> using the <paramref name="messageArgs" /> to format the
		///   diagnostic message.
		/// </summary>
		/// <param name="context">The context in which the diagnostic should be emitted.</param>
		/// <param name="syntaxToken">The syntax token the diagnostic is emitted for.</param>
		/// <param name="messageArgs">The arguments for formatting the diagnostic message.</param>
		public void Emit(SyntaxTreeAnalysisContext context, SyntaxToken syntaxToken, params object[] messageArgs)
		{
			context.ReportDiagnostic(CreateDiagnostic(syntaxToken.GetLocation(), messageArgs));
		}

		/// <summary>
		///   Emits a diagnostic for <paramref name="syntaxNode" /> using the <paramref name="messageArgs" /> to format the
		///   diagnostic message.
		/// </summary>
		/// <param name="context">The context in which the diagnostic should be emitted.</param>
		/// <param name="syntaxNode">The syntax node the diagnostic is emitted for.</param>
		/// <param name="messageArgs">The arguments for formatting the diagnostic message.</param>
		public void Emit(SemanticModelAnalysisContext context, [NotNull] SyntaxNode syntaxNode, params object[] messageArgs)
		{
			context.ReportDiagnostic(CreateDiagnostic(syntaxNode.GetLocation(), messageArgs));
		}

		/// <summary>
		///   Emits a diagnostic for <paramref name="syntaxToken" /> using the <paramref name="messageArgs" /> to format the
		///   diagnostic message.
		/// </summary>
		/// <param name="context">The context in which the diagnostic should be emitted.</param>
		/// <param name="syntaxToken">The syntax token the diagnostic is emitted for.</param>
		/// <param name="messageArgs">The arguments for formatting the diagnostic message.</param>
		public void Emit(SemanticModelAnalysisContext context, SyntaxToken syntaxToken, params object[] messageArgs)
		{
			context.ReportDiagnostic(CreateDiagnostic(syntaxToken.GetLocation(), messageArgs));
		}

		/// <summary>
		///   Emits a diagnostic for <paramref name="location" /> using the <paramref name="messageArgs" /> to format the
		///   diagnostic message.
		/// </summary>
		/// <param name="context">The context in which the diagnostic should be emitted.</param>
		/// <param name="location">The location the diagnostic is emitted for.</param>
		/// <param name="messageArgs">The arguments for formatting the diagnostic message.</param>
		public void Emit(SemanticModelAnalysisContext context, [NotNull] Location location, params object[] messageArgs)
		{
			context.ReportDiagnostic(CreateDiagnostic(location, messageArgs));
		}

		/// <summary>
		///   Emits a diagnostic for <paramref name="symbol" /> using the <paramref name="messageArgs" /> to format the diagnostic
		///   message.
		/// </summary>
		/// <param name="context">The context in which the diagnostic should be emitted.</param>
		/// <param name="symbol">The symbol the diagnostic is emitted for.</param>
		/// <param name="messageArgs">The arguments for formatting the diagnostic message.</param>
		public void Emit(SymbolAnalysisContext context, [NotNull] ISymbol symbol, params object[] messageArgs)
		{
			context.ReportDiagnostic(CreateDiagnostic(symbol.Locations[0], messageArgs));
		}

		/// <summary>
		///   Creates a diagnostic for <paramref name="location" /> using the <paramref name="messageArgs" /> to format the
		///   diagnostic message.
		/// </summary>
		/// <param name="location">The location the diagnostic is emitted for.</param>
		/// <param name="messageArgs">The arguments for formatting the diagnostic message.</param>
		public Diagnostic CreateDiagnostic([NotNull] Location location, params object[] messageArgs)
		{
			return Diagnostic.Create(Descriptor, location, messageArgs);
		}
	}
}