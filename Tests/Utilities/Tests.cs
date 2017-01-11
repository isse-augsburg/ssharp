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

namespace Tests.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using System.Text;
	using System.Threading;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Diagnostics;
	using SafetySharp.Compiler;
	using SafetySharp.Compiler.Roslyn.Symbols;
	using SafetySharp.Compiler.Roslyn.Syntax;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Utilities;
	using Shouldly;
	using Xunit.Abstractions;

	/// <summary>
	///   A base class for all S# tests.
	/// </summary>
	public class Tests
	{
		/// <summary>
		///   The number of dynamically generated assemblies.
		/// </summary>
		private static int _assemblyCount;

		/// <summary>
		///   The assemblies that have to be loaded when compiling S# code.
		/// </summary>
		private static readonly string[] _assemblies;

		/// <summary>
		///   Initializes the type.
		/// </summary>
		static Tests()
		{
			const string facadesPath = @"Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.2\Facades";
			_assemblies = Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), facadesPath));
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="output">The stream that should be used to write the test output.</param>
		public Tests(ITestOutputHelper output = null)
		{
			Output = new TestTraceOutput(output);
		}

		/// <summary>
		///   Gets the output that writes to the test output stream.
		/// </summary>
		public TestTraceOutput Output { get; }

		/// <summary>
		///   Gets the absolute path to the directory that contains the tests.
		/// </summary>
		/// <param name="subdirectory">The subdirectory that contains the tests.</param>
		/// <param name="filePath">The name of the file; passed automatically by the C# compiler.</param>
		protected static string GetAbsoluteTestsDirectory(string subdirectory, [CallerFilePath] string filePath = null)
		{
			var path = Path.GetDirectoryName(filePath);
			if (path == null)
				throw new InvalidOperationException("Unknown path.");

			return Path.Combine(path, subdirectory);
		}

		/// <summary>
		///   Compiles the <paramref name="syntaxTree" />, instantiates all non-abstract classes implementing
		///   <see cref="ITestableObject" />, and returns them.
		/// </summary>
		/// <param name="syntaxTree">The syntax tree that should be compiled.</param>
		/// <param name="output">The output that should be used to write test output.</param>
		protected static IEnumerable<ITestableObject> GetTestableObjects(SyntaxTree syntaxTree, TestTraceOutput output)
		{
			var compilation = CreateCompilation(syntaxTree);
			var semanticModel = compilation.GetSemanticModel(syntaxTree);

			var testableTypes = syntaxTree
				.Descendants<ClassDeclarationSyntax>()
				.Select(declaration => declaration.GetTypeSymbol(semanticModel))
				.Where(symbol => !symbol.IsGenericType && !symbol.IsAbstract && symbol.ContainingType == null)
				.Where(symbol => symbol.IsDerivedFrom(semanticModel.GetTypeSymbol<ITestableObject>()))
				.Select(symbol => symbol.ToDisplayString())
				.ToArray();

			if (testableTypes.Length == 0)
				throw new TestException("Unable to find any testable class declarations.");

			var assembly = CompileSafetySharp(compilation, output);
			return testableTypes.Select(testableType => (ITestableObject)Activator.CreateInstance(assembly.GetType(testableType)));
		}

		/// <summary>
		///   Compiles the <paramref name="syntaxTree" />, instantiates all non-abstract classes implementing
		///   <see cref="ITestableObject" />, and returns them.
		/// </summary>
		/// <param name="syntaxTree">The syntax tree that should be compiled.</param>
		protected IEnumerable<ITestableObject> GetTestableObjects(SyntaxTree syntaxTree)
		{
			return GetTestableObjects(syntaxTree, Output);
		}


		/// <summary>
		///   Compiles the <paramref name="code" />, instantiates all non-abstract classes implementing
		///   <see cref="ITestableObject" />, and returns them.
		/// </summary>
		/// <param name="code">The code that should be compiled.</param>
		/// <param name="output">The output that should be used to write test output.</param>
		public static IEnumerable<ITestableObject> GetTestableObjects(string code, TestTraceOutput output)
		{
			var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, encoding: Encoding.UTF8);
			return GetTestableObjects(syntaxTree, output);
		}


		/// <summary>
		///   Compiles the <paramref name="syntaxTree" />, instantiates all non-abstract classes implementing
		///   <see cref="ModelBase" />, and returns them.
		/// </summary>
		/// <param name="syntaxTree">The syntax tree that should be compiled.</param>
		/// <param name="output">The output that should be used to write test output.</param>
		protected static IEnumerable<ModelBase> GetModelBases(SyntaxTree syntaxTree, TestTraceOutput output)
		{
			var compilation = CreateCompilation(syntaxTree);
			var semanticModel = compilation.GetSemanticModel(syntaxTree);

			var testableTypes = syntaxTree
				.Descendants<ClassDeclarationSyntax>()
				.Select(declaration => declaration.GetTypeSymbol(semanticModel))
				.Where(symbol => !symbol.IsGenericType && !symbol.IsAbstract && symbol.ContainingType == null)
				.Where(symbol => symbol.IsDerivedFrom(semanticModel.GetTypeSymbol<ModelBase>()))
				.Select(symbol => symbol.ToDisplayString())
				.ToArray();

			if (testableTypes.Length == 0)
				throw new TestException("Unable to find any ModelBase declarations.");

			var assembly = CompileSafetySharp(compilation, output);
			return testableTypes.Select(testableType => (ModelBase)Activator.CreateInstance(assembly.GetType(testableType)));
		}

		/// <summary>
		///   Compiles the <paramref name="code" />, instantiates all non-abstract classes implementing
		///   <see cref="ModelBase" />, and returns them.
		/// </summary>
		/// <param name="code">The code that should be compiled.</param>
		/// <param name="output">The output that should be used to write test output.</param>
		public static IEnumerable<ModelBase> GetModelBases(string code, TestTraceOutput output)
		{
			var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, path: ".",encoding: Encoding.UTF8);
			return GetModelBases(syntaxTree, output);
		}

		/// <summary>
		///   Compiles the <paramref name="file" />, instantiates all non-abstract classes implementing
		///   <see cref="ITestableObject" />, and executes the <see cref="ITestableObject.Test" /> method for each instance.
		/// </summary>
		/// <param name="file">The file that should be compiled and tested.</param>
		/// <param name="args">The arguments that should be passed to the test.</param>
		protected void ExecuteDynamicTests(string file, params object[] args)
		{
			foreach (var obj in GetTestableObjects(ParseFile(file)))
			{
				obj.Test(Output, args);
				(obj as IDisposable)?.Dispose();
			}
		}

		/// <summary>
		///   Parses the <paramref name="file" /> and returns the <see cref="SyntaxTree" />.
		/// </summary>
		/// <param name="file">The file that should be parsed.</param>
		protected SyntaxTree ParseFile(string file)
		{
			Requires.NotNullOrWhitespace(file, nameof(file));

			var code = File.ReadAllText(file).Replace("\t", "    ");
			return SyntaxFactory.ParseSyntaxTree(code, path: file, encoding: Encoding.UTF8);
		}

		/// <summary>
		///   Creates a compilation for the <paramref name="compilationUnits" />.
		/// </summary>
		/// <param name="compilationUnits">The compilation units the compilation should contain.</param>
		public static Compilation CreateCompilation(params string[] compilationUnits)
		{
			return CreateCompilation(compilationUnits.Select(unit => SyntaxFactory.ParseSyntaxTree(unit)).ToArray());
		}

		/// <summary>
		///   Creates a compilation for the <paramref name="syntaxTrees" />.
		/// </summary>
		/// <param name="syntaxTrees">The syntax trees the compilation should contain.</param>
		public static Compilation CreateCompilation(params SyntaxTree[] syntaxTrees)
		{
			return CreateCompilation(true, syntaxTrees);
		}

		/// <summary>
		///   Creates a compilation for the <paramref name="syntaxTrees" />.
		/// </summary>
		/// <param name="checkErrors">Indicates whether the compilation should be checked for errors.</param>
		/// <param name="syntaxTrees">The syntax trees the compilation should contain.</param>
		public static Compilation CreateCompilation(bool checkErrors, params SyntaxTree[] syntaxTrees)
		{
			var compilation = CSharpCompilation
				.Create("TestAssembly" + Interlocked.Increment(ref _assemblyCount))
				.WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true,
					assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default))
				.AddSyntaxTrees(syntaxTrees)
				.AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
				.AddReferences(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location))
				.AddReferences(MetadataReference.CreateFromFile(typeof(ISet<>).Assembly.Location))
				.AddReferences(_assemblies.Select(assembly => MetadataReference.CreateFromFile(assembly)))
				.AddReferences(MetadataReference.CreateFromFile(typeof(Tests).Assembly.Location))
				.AddReferences(MetadataReference.CreateFromFile(typeof(ExecutableModel<>).Assembly.Location))
				.AddReferences(MetadataReference.CreateFromFile(typeof(Component).Assembly.Location))
				.AddReferences(MetadataReference.CreateFromFile(typeof(Compiler).Assembly.Location))
				.AddReferences(MetadataReference.CreateFromFile(typeof(Should).Assembly.Location))
				.AddReferences(MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location));

			if (checkErrors)
			{
				var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
				if (errors.Length != 0)
					throw new CSharpException(errors, "Failed to create compilation.\n\n{0}", SyntaxTreesToString(compilation));
			}

			return compilation;
		}

		/// <summary>
		///   Runs the S# analyzers on the given compilation, ensuring that the compilation contains no errors.
		/// </summary>
		/// <param name="compilation">The compilation that should be checked.</param>
		public static void CheckForSafetySharpDiagnostics(Compilation compilation)
		{
			var errors = compilation
				.WithAnalyzers(Compiler.Analyzers)
				.GetAllDiagnosticsAsync().Result
				.Where(d => d.Severity == DiagnosticSeverity.Error && !d.Id.StartsWith("CS"))
				.ToArray();

			if (errors.Length != 0)
				throw new CSharpException(errors, "Failed to create compilation.\n\n{0}", SyntaxTreesToString(compilation));
		}

		/// <summary>
		///   Compiles the <paramref name="compilation" /> with the S# compiler and returns the resulting assembly that has been
		///   loaded into the app domain.
		/// </summary>
		/// <param name="compilation">The compilation that should be compiled.</param>
		/// <param name="output">The output that should be used to write test output.</param>
		public static Assembly CompileSafetySharp(Compilation compilation, TestTraceOutput output)
		{
			using (var workspace = new AdhocWorkspace())
			{
				var project = workspace
					.AddProject(compilation.AssemblyName, LanguageNames.CSharp)
					.AddMetadataReferences(compilation.References)
					.WithCompilationOptions(compilation.Options);

				foreach (var syntaxTree in compilation.SyntaxTrees)
					project = project.AddDocument(syntaxTree.FilePath, syntaxTree.GetRoot().GetText(Encoding.UTF8)).Project;

				var errorReporter = new TestErrorReporter(output);
				var compiler = new Compiler(errorReporter);

				try
				{
					var assembly = compiler.Compile(project);
					output.Trace("{0}", SyntaxTreesToString(compiler.Compilation));

					return assembly;
				}
				catch (CompilationException e)
				{
					throw new TestException("{0}\n\n{1}", e.Message, SyntaxTreesToString(compiler.Compilation));
				}
			}
		}

		/// <summary>
		///   Compiles the <paramref name="compilation" /> with the S# compiler and returns the resulting assembly that has been
		///   loaded into the app domain.
		/// </summary>
		/// <param name="compilation">The compilation that should be compiled.</param>
		protected Assembly CompileSafetySharp(Compilation compilation)
		{
			return CompileSafetySharp(compilation, Output);
		}

		/// <summary>
		///   Gets a string containing the contents of all syntax tress of the <paramref name="compilation" />.
		/// </summary>
		/// <param name="compilation">The compilation whose syntax trees should be written to a string.</param>
		protected static string SyntaxTreesToString(Compilation compilation)
		{
			var builder = new StringBuilder();

			foreach (var syntaxTree in compilation.SyntaxTrees)
			{
				builder.AppendLine();
				builder.AppendLine();
				builder.AppendLine("=============================================");
				builder.AppendLine(Path.GetFileName(syntaxTree.FilePath));
				builder.AppendLine("=============================================");
				builder.AppendLine(syntaxTree.ToString());
			}

			return builder.ToString();
		}

		/// <summary>
		///   Appends <paramref name="diagnostic" /> to the <paramref name="builder" />.
		/// </summary>
		/// <param name="builder">The builder the diagnostic should be appended to.</param>
		/// <param name="diagnostic">The diagnostic that should be appended.</param>
		public static void Write(StringBuilder builder, Diagnostic diagnostic)
		{
			var lineSpan = diagnostic.Location.GetLineSpan();
			var message = diagnostic.ToString();
			message = message.Substring(message.IndexOf(":", StringComparison.InvariantCulture) + 1);

			builder.AppendFormat("({1}-{2}) {0}\n\n", message, lineSpan.StartLinePosition, lineSpan.EndLinePosition);
		}

		/// <summary>
		///   Enumerates all C#-based test cases located at <paramref name="path" /> or any sub-directory.
		/// </summary>
		/// <param name="path">The path to the directory where C#-based tests are located.</param>
		protected static IEnumerable<object[]> EnumerateTestCases(string path)
		{
			var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

			foreach (var file in files.OrderBy(file => file))
			{
				var prefix = Path.GetDirectoryName(file).Substring(path.Length);
				var testName = String.IsNullOrWhiteSpace(prefix)
					? Path.GetFileNameWithoutExtension(file)
					: $"[{prefix.Substring(1)}] {Path.GetFileNameWithoutExtension(file)}";

				yield return new object[] { testName, file.Replace("\\", "/") };
			}
		}
	}
}