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

namespace SafetySharp.Compiler
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using Analyzers;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.Diagnostics;
	using Microsoft.CodeAnalysis.Editing;
	using Microsoft.CodeAnalysis.MSBuild;
	using Normalization;
	using Utilities;

	/// <summary>
	///   Compiles a S# modeling project authored in C# to a S# modeling assembly.
	/// </summary>
	public class Compiler
	{
		/// <summary>
		///   The diagnostic analyzers that are used to diagnose the C# code before compilation.
		/// </summary>
		private static ImmutableArray<DiagnosticAnalyzer> _analyzers;

		/// <summary>
		///   The error reporter used by the compiler.
		/// </summary>
		private readonly ErrorReporter _log;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="errorReporter">The error reporter used by the compiler.</param>
		public Compiler(ErrorReporter errorReporter)
		{
			Requires.NotNull(errorReporter, nameof(errorReporter));
			_log = errorReporter;
		}

		/// <summary>
		///   Gets the diagnostic analyzers that are used to diagnose the C# code before compilation.
		/// </summary>
		public static ImmutableArray<DiagnosticAnalyzer> Analyzers
		{
			get
			{
				if (_analyzers.IsDefault)
				{
					_analyzers = typeof(Compiler)
						.Assembly.GetTypes()
						.Where(type => type.IsClass && !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
						.Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type))
						.ToImmutableArray();
				}

				return _analyzers;
			}
		}

		/// <summary>
		///   Gets the compilation that has been compiled.
		/// </summary>
		public Compilation Compilation { get; private set; }

		/// <summary>
		///   Compiles the S# modeling project identified by the <paramref name="projectFile" /> for the given
		///   <paramref name="configuration" /> and <paramref name="platform" />.
		/// </summary>
		/// <param name="projectFile">The C# project file that should be compiled.</param>
		/// <param name="configuration">The configuration the C# project should be compiled in.</param>
		/// <param name="platform">The platform the C# project should be compiled for.</param>
		public bool Compile([NotNull] string projectFile, [NotNull] string configuration, [NotNull] string platform)
		{
			Requires.NotNullOrWhitespace(projectFile, nameof(projectFile));
			Requires.NotNullOrWhitespace(configuration, nameof(configuration));
			Requires.NotNullOrWhitespace(platform, nameof(platform));

			if (!File.Exists(projectFile))
				return ReportError("0001", "Project file '{0}' could not be found.", projectFile);

			if (String.IsNullOrWhiteSpace(configuration))
				return ReportError("0002", "Invalid project configuration: Configuration name cannot be the empty string.");

			if (String.IsNullOrWhiteSpace(platform))
				return ReportError("0003", "Invalid compilation platform: Platform name cannot be the empty string.");

			var msBuildProperties = new Dictionary<string, string> { { "Configuration", configuration }, { "Platform", platform } };

			var workspace = MSBuildWorkspace.Create(msBuildProperties);
			var project = workspace.OpenProjectAsync(projectFile).Result;

			try
			{
				byte[] assembly, pdb;
				Compile(project, out assembly, out pdb);

				File.WriteAllBytes(project.OutputFilePath, assembly);
				File.WriteAllBytes(Path.ChangeExtension(project.OutputFilePath, ".pdb"), pdb);

				OutputCode("Compiled Code");
				return true;
			}
			catch (CompilationException)
			{
				OutputCode("Failed");
				return false;
			}
			catch (Exception)
			{
				OutputCode("Failed");
				throw;
			}
		}

		/// <summary>
		///   Compiles S# <paramref name="project" /> and returns the bytes of the compiled assembly.
		/// </summary>
		/// <param name="project">The project that should be compiled.</param>
		/// <param name="peBytes">Returns the compiled assembly.</param>
		/// <param name="pdbBytes">The returns the compiled program database.</param>
		public void Compile([NotNull] Project project, out byte[] peBytes, out byte[] pdbBytes)
		{
			Requires.NotNull(project, nameof(project));

			Compilation = project.GetCompilationAsync().Result;

			var diagnosticOptions = Compilation.Options.SpecificDiagnosticOptions.Add("CS0626", ReportDiagnostic.Suppress);
			var options = (CSharpCompilationOptions)Compilation.Options;
			options = options.WithAllowUnsafe(true).WithSpecificDiagnosticOptions(diagnosticOptions);
			var syntaxGenerator = SyntaxGenerator.GetGenerator(project.Solution.Workspace, LanguageNames.CSharp);

			if (!Diagnose())
				throw new CompilationException();

			Compilation = Compilation.WithOptions(options);
			ApplyNormalizers(syntaxGenerator);

			EmitInMemory(out peBytes, out pdbBytes);
		}

		/// <summary>
		///   Compiles and loads the S# <paramref name="project" />.
		/// </summary>
		/// <param name="project">The project that should be compiled.</param>
		public Assembly Compile([NotNull] Project project)
		{
			byte[] assembly, pdb;
			Compile(project, out assembly, out pdb);

			File.WriteAllBytes(project.AssemblyName + ".dll", assembly);
			File.WriteAllBytes(project.AssemblyName + ".pdb", pdb);

			return Assembly.Load(project.AssemblyName);
		}

		/// <summary>
		///   Reports <paramref name="diagnostic" /> depending on its severity. If <paramref name="errorsOnly" /> is <c>true</c>, only
		///   error diagnostics are reported.
		/// </summary>
		/// <param name="diagnostic">The diagnostic that should be reported.</param>
		/// <param name="errorsOnly">Indicates whether error diagnostics should be reported exclusively.</param>
		private void Report(Diagnostic diagnostic, bool errorsOnly)
		{
			switch (diagnostic.Severity)
			{
				case DiagnosticSeverity.Error:
					_log.Error("{0}", diagnostic);
					break;
				case DiagnosticSeverity.Warning:
					if (!errorsOnly)
						_log.Warn("{0}", diagnostic);
					break;
				case DiagnosticSeverity.Info:
				case DiagnosticSeverity.Hidden:
					if (!errorsOnly)
						_log.Info("{0}", diagnostic);
					break;
				default:
					Assert.NotReached("Unknown diagnostic severity.");
					break;
			}
		}

		/// <summary>
		///   Reports all <paramref name="diagnostics" /> depending on their severities. If <paramref name="errorsOnly" /> is
		///   <c>true</c>, only error diagnostics are reported. The function returns <c>false</c> when at least one error diagnostic
		///   has been reported.
		/// </summary>
		/// <param name="diagnostics">The diagnostics that should be reported.</param>
		/// <param name="errorsOnly">Indicates whether error diagnostics should be reported exclusively.</param>
		private bool Report([NotNull] IEnumerable<Diagnostic> diagnostics, bool errorsOnly)
		{
			var containsError = false;
			foreach (var diagnostic in diagnostics)
			{
				Report(diagnostic, errorsOnly);
				containsError |= diagnostic.Severity == DiagnosticSeverity.Error;
			}

			return !containsError;
		}

		/// <summary>
		///   Reports an error diagnostic with the given <paramref name="identifier" /> and <paramref name="message" />.
		/// </summary>
		/// <param name="identifier">The identifier of the diagnostic that should be reported.</param>
		/// <param name="message">The message of the diagnostic that should be reported.</param>
		/// <param name="formatArgs">The format arguments of the message.</param>
		[StringFormatMethod("message")]
		private bool ReportError([NotNull] string identifier, [NotNull] string message, params object[] formatArgs)
		{
			identifier = DiagnosticInfo.Prefix + identifier;
			message = String.Format(message, formatArgs);

			var diagnostic = Diagnostic.Create(identifier, DiagnosticInfo.Category, message, DiagnosticSeverity.Error,
				DiagnosticSeverity.Error, true, 0);
			Report(diagnostic, true);
			return false;
		}

		/// <summary>
		///   Writes the C# code contained in the <see cref="Compilation" /> to the directory denoted by
		///   <paramref name="path" />.
		/// </summary>
		/// <param name="path">The target path the code should be output to.</param>
		[Conditional("DEBUG")]
		private void OutputCode([NotNull] string path)
		{
			path = Path.Combine(Path.GetDirectoryName(typeof(Compiler).Assembly.Location), "obj", path);
			Directory.CreateDirectory(path);

			var index = 0;
			foreach (var syntaxTree in Compilation.SyntaxTrees)
			{
				var fileName = Path.GetFileNameWithoutExtension(syntaxTree.FilePath ?? String.Empty);
				var filePath = Path.Combine(path, $"{fileName}{index}.cs");

				File.WriteAllText(filePath, syntaxTree.GetRoot().ToFullString());
				++index;
			}
		}

		/// <summary>
		///   Runs the S# diagnostic analyzers on the <see cref="Compilation" />, reporting all generated diagnostics. The function
		///   returns
		///   <c>false</c> when at least one error diagnostic has been reported.
		/// </summary>
		private bool Diagnose()
		{
			if (!Report(Compilation.GetDiagnostics(), true))
				return false;

			return Report(Compilation.WithAnalyzers(Analyzers).GetAnalyzerDiagnosticsAsync().Result, false);
		}

		/// <summary>
		///   Applies the normalizers to the <see cref="Compilation" />.
		/// </summary>
		/// <param name="syntaxGenerator">The syntax generator that the normalizers should use to generate syntax nodes.</param>
		private void ApplyNormalizers([NotNull] SyntaxGenerator syntaxGenerator)
		{
			Compilation = Normalizer.ApplyNormalizer<LineDirectiveNormalizer>(Compilation, syntaxGenerator);
			Compilation = Normalizer.ApplyNormalizer<PartialNormalizer>(Compilation, syntaxGenerator);
			Compilation = Normalizer.ApplyNormalizer<FormulaNormalizer>(Compilation, syntaxGenerator);
			Compilation = Normalizer.ApplyNormalizer<LiftedExpressionNormalizer>(Compilation, syntaxGenerator);
			Compilation = Normalizer.ApplyNormalizer<TransitionNormalizer>(Compilation, syntaxGenerator);
			Compilation = Normalizer.ApplyNormalizer<ExpressionBodyNormalizer>(Compilation, syntaxGenerator);
			Compilation = Normalizer.ApplyNormalizer<PortNormalizer>(Compilation, syntaxGenerator);
		}

		/// <summary>
		///   Emits the code for the <see cref="Compilation" /> in-memory.
		/// </summary>
		/// <param name="peBytes">Returns the compiled assembly.</param>
		/// <param name="pdbBytes">The returns the compiled program database.</param>
		private void EmitInMemory(out byte[] peBytes, out byte[] pdbBytes)
		{
			using (var peStream = new MemoryStream())
			using (var pdbStream = new MemoryStream())
			{
				var emitResult = Compilation.Emit(peStream, pdbStream);
				if (!emitResult.Success)
				{
					Report(emitResult.Diagnostics, true);
					throw new CompilationException();
				}

				peBytes = peStream.ToArray();
				pdbBytes = pdbStream.ToArray();
			}
		}
	}
}