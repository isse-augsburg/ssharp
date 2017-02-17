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

namespace SafetySharp.Compiler
{
	using System;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using JetBrains.Annotations;
	using Microsoft.Build.Utilities;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.Diagnostics;
	using Microsoft.CodeAnalysis.Text;
	using Normalization;
	using ISSE.SafetyChecking.Utilities;

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
		///   Initializes a new instance.
		/// </summary>
		/// <param name="logger">The logger that should be used to report messages to MSBuild.</param>
		public Compiler(TaskLoggingHelper logger)
		{
			Requires.NotNull(logger, nameof(logger));
			_log = new MSBuildReporter(logger);
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
		///   Normalizes the <paramref name="files" />.
		/// </summary>
		/// <param name="files">The files that should be normalized.</param>
		/// <param name="references">The references the files should be compiled with.</param>
		/// <param name="intermediateDirectory">The intermediate directory that files can be written to.</param>
		public string[] NormalizeProject([NotNull] string[] files, [NotNull] string[] references, [NotNull] string intermediateDirectory)
		{
			try
			{
				var diagnosticOptions = ImmutableDictionary
					.Create<string, ReportDiagnostic>()
					.Add("CS0626", ReportDiagnostic.Suppress)
					.Add("CS1701", ReportDiagnostic.Suppress);

				var workspace = new AdhocWorkspace();
				var project = workspace
					.CurrentSolution.AddProject("ssharp", "ssharp", "C#")
					.WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true,
						specificDiagnosticOptions: diagnosticOptions, assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default))
					.WithMetadataReferences(references.Select(p => MetadataReference.CreateFromFile(p)));

				foreach (var file in files)
				{
					var text = SourceText.From(File.ReadAllText(file));
					project = project.AddDocument(file, text).Project;
				}

				Compilation = project.GetCompilationAsync().Result;

				if (!Diagnose())
					return null;

				Compilation = Normalizer.ApplyNormalizers(Compilation);
				return Compilation.SyntaxTrees.Select(tree => tree.ToString()).ToArray();
			}
			catch (Exception)
			{
				OutputCode(Path.Combine(intermediateDirectory, "failed"));
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

			if (!Diagnose())
				throw new CompilationException();

			Compilation = Compilation.WithOptions(options);
			Compilation = Normalizer.ApplyNormalizers(Compilation);

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
		///   Writes the C# code contained in the <see cref="Compilation" /> to the directory denoted by
		///   <paramref name="path" />.
		/// </summary>
		/// <param name="path">The target path the code should be output to.</param>
		[Conditional("DEBUG")]
		private void OutputCode([NotNull] string path)
		{
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
		///   returns <c>false</c> when at least one error diagnostic has been reported.
		/// </summary>
		private bool Diagnose()
		{
			if (!_log.Report(Compilation.GetDiagnostics(), errorsOnly: true))
				return false;

			return _log.Report(Compilation.WithAnalyzers(Analyzers).GetAnalyzerDiagnosticsAsync().Result, errorsOnly: false);
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
					_log.Report(emitResult.Diagnostics, errorsOnly: false);
					throw new CompilationException();
				}

				peBytes = peStream.ToArray();
				pdbBytes = pdbStream.ToArray();
			}
		}
	}
}