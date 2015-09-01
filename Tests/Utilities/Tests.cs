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
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Diagnostics;
	using SafetySharp.Compiler;
	using SafetySharp.Compiler.Analyzers;
	using SafetySharp.Compiler.Roslyn.Symbols;
	using SafetySharp.Compiler.Roslyn.Syntax;
	using SafetySharp.Modeling;
	using SafetySharp.Utilities;
	using Shouldly;
	using Xunit.Abstractions;

	/// <summary>
	///   A base class for all S# tests.
	/// </summary>
	public class Tests
	{
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
		///   Gets the name of the file that calls this method.
		/// </summary>
		/// <param name="filePath">The name of the file; passed automatically by the C# compiler.</param>
		protected static string GetFileName([CallerFilePath] string filePath = null)
		{
			return filePath;
		}

		/// <summary>
		///   Compiles the <paramref name="syntaxTree" />, instantiates all non-abstract classes implementing
		///   <see cref="ITestableObject" />, and returns them.
		/// </summary>
		/// <param name="syntaxTree">The syntax tree that should be compiled.</param>
		protected IEnumerable<ITestableObject> GetTestableObjects(SyntaxTree syntaxTree)
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

			var assembly = CompileSafetySharp(compilation);
			return testableTypes.Select(testableType => (ITestableObject)Activator.CreateInstance(assembly.GetType(testableType)));
		}

		/// <summary>
		///   Compiles the <paramref name="syntaxTree" />, instantiates all non-abstract classes implementing
		///   <see cref="ITestableObject" />, and executes the <see cref="ITestableObject.Test" /> method for each instance.
		/// </summary>
		/// <param name="syntaxTree">The syntax tree that should be compiled and tested.</param>
		protected void ExecuteDynamicTests(SyntaxTree syntaxTree)
		{
			foreach (var obj in GetTestableObjects(syntaxTree))
				obj.Test(Output);
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
			var compilation = CSharpCompilation
				.Create("DynamicTestAssembly")
				.WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
				.AddSyntaxTrees(syntaxTrees)
				.AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
				.AddReferences(MetadataReference.CreateFromFile(typeof(DynamicAttribute).Assembly.Location))
				.AddReferences(MetadataReference.CreateFromFile(typeof(Tests).Assembly.Location))
				.AddReferences(MetadataReference.CreateFromFile(typeof(Component).Assembly.Location))
				.AddReferences(MetadataReference.CreateFromFile(typeof(DiagnosticIdentifier).Assembly.Location))
				.AddReferences(MetadataReference.CreateFromFile(typeof(Should).Assembly.Location))
				.AddReferences(MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location));

			var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
			if (errors.Length != 0)
				throw new CSharpException(errors, "Failed to create compilation.\n\n{0}", SyntaxTreesToString(compilation));

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

				var errorReporter = new CSharpErrorReporter(output);
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
		///   Fails the test with the explanatory <paramref name="message" />.
		/// </summary>
		/// <param name="message">The explanatory format message.</param>
		/// <param name="args">The format arguments.</param>
		[StringFormatMethod("message")]
		protected static void Fail(string message, params object[] args)
		{
			throw new TestException(message, args);
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
				var code = File.ReadAllText(file).Replace("\t", "    ");
				var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, path: file, encoding: Encoding.UTF8);

				yield return new object[] { testName, syntaxTree };
			}
		}

		/// <summary>
		///   Checks that no exceptions escape unhandled during the execution of <paramref name="action" />.
		/// </summary>
		/// <param name="action">The action that should be checked.</param>
		public static void NoThrow(Action action)
		{
			Requires.NotNull(action, nameof(action));

			try
			{
				action();
			}
			catch (Exception e)
			{
				var message = "Expected no exception to be thrown, but an exception of type '{0}' was raised:\n{1}";
				Fail(message, e.GetType().FullName, e.Message);
			}
		}

		/// <summary>
		///   Checks whether <paramref name="action" /> raises an exception of type <typeparamref name="T" /> satisfying the
		///   <paramref name="assertion" />.
		/// </summary>
		/// <typeparam name="T">The type of the exception that is expected to be thrown.</typeparam>
		/// <param name="action">The action that should be checked.</param>
		/// <param name="assertion">The assertion that should be checked on the thrown exception.</param>
		public static void RaisesWith<T>(Action action, Action<T> assertion)
			where T : Exception
		{
			Requires.NotNull(action, nameof(action));

			Exception exception = null;

			try
			{
				action();
			}
			catch (Exception e)
			{
				exception = e;
			}

			if (exception == null)
				Fail("Expected an exception of type '{0}', but no exception was thrown.", typeof(T).FullName);

			var typedException = exception as T;
			if (typedException != null)
			{
				assertion?.Invoke(typedException);
			}
			else
			{
				var message = "Expected an exception of type '{0}', but an exception of type '{1}' was thrown instead.\n\nMessage:\n{2}";
				Fail(message, typeof(T).FullName, exception.GetType().FullName, exception.Message);
			}
		}

		/// <summary>
		///   Checks whether <paramref name="action" /> raises an exception of type <typeparamref name="T" />.
		/// </summary>
		/// <typeparam name="T">The type of the exception that is expected to be thrown.</typeparam>
		/// <param name="action">The action that should be checked.</param>
		public static void Raises<T>(Action action)
			where T : Exception
		{
			RaisesWith<T>(action, null);
		}

		/// <summary>
		///   Checks whether <paramref name="action" /> raises an <see cref="InvalidOperationException" />.
		/// </summary>
		/// <param name="action">The action that should be checked.</param>
		public static void RaisesInvalidOpException(Action action)
		{
			Raises<InvalidOperationException>(action);
		}
	}
}