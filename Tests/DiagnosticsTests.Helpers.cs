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

namespace Tests
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Diagnostics;
	using Microsoft.CodeAnalysis.Text;
	using SafetySharp.Compiler.Analyzers;
	using SafetySharp.Compiler.Roslyn.Symbols;
	using SafetySharp.Compiler.Roslyn.Syntax;
	using Utilities;
	using Xunit.Abstractions;

	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public class DiagnosticAttribute : Attribute
	{
		[UsedImplicitly]
		public DiagnosticAttribute(DiagnosticIdentifier id, int line, int column, int length, params string[] arguments)
		{
		}
	}

	internal class DiagnosticComparer : IEqualityComparer<Diagnostic>
	{
		public bool Equals(Diagnostic x, Diagnostic y)
		{
			return x.Location == y.Location && x.ToString() == y.ToString();
		}

		public int GetHashCode(Diagnostic obj)
		{
			return 0;
		}
	}

	public partial class DiagnosticsTests
	{
		public DiagnosticsTests(ITestOutputHelper output)
			: base(output)
		{
		}

		private void CheckDiagnostics<T>(string file)
			where T : Analyzer, new()
		{
			var analyzer = new T();
			var code = ParseFile(file);
			var compilation = CreateCompilation(code).WithAnalyzers(ImmutableArray.Create((DiagnosticAnalyzer)analyzer));

			var expectedDiagnostics = GetExpectedDiagnostics(analyzer, compilation.Compilation).ToArray();
			var actualDiagnostics = compilation.GetAllDiagnosticsAsync().Result.Where(d => !d.Descriptor.Id.StartsWith("CS")).ToArray();
			var commonDiagnostics = expectedDiagnostics.Intersect(actualDiagnostics, new DiagnosticComparer());

			if (expectedDiagnostics.Length != actualDiagnostics.Length || expectedDiagnostics.Length != commonDiagnostics.Count())
			{
				var builder = new StringBuilder();
				builder.AppendLine();
				builder.AppendLine();
				builder.AppendLine("Actual Diagnostics:");
				builder.AppendLine("===================");

				foreach (var diagnostic in actualDiagnostics.OrderBy(d => d.Location.SourceSpan.Start))
					Write(builder, diagnostic);

				builder.AppendLine("Expected Diagnostics:");
				builder.AppendLine("=====================");

				foreach (var diagnostic in expectedDiagnostics.OrderBy(d => d.Location.SourceSpan.Start))
					Write(builder, diagnostic);

				throw new TestException(builder.ToString());
			}

			foreach (var diagnostic in actualDiagnostics)
				Output.Trace("{0}\n", diagnostic);
		}

		private static IEnumerable<Diagnostic> GetExpectedDiagnostics(Analyzer analyzer, Compilation compilation)
		{
			foreach (var syntaxTree in compilation.SyntaxTrees)
			{
				var semanticModel = compilation.GetSemanticModel(syntaxTree);

				foreach (var typeDeclaration in syntaxTree.Descendants<BaseTypeDeclarationSyntax>())
				{
					var symbol = typeDeclaration.GetTypeSymbol(semanticModel);
					foreach (var attribute in symbol.GetAttributes<DiagnosticAttribute>(semanticModel))
					{
						if (attribute == null)
							continue;

						var id = (DiagnosticIdentifier)attribute.ConstructorArguments[0].Value;
						var line = (int)attribute.ConstructorArguments[1].Value;
						var column = (int)attribute.ConstructorArguments[2].Value;
						var length = (int)attribute.ConstructorArguments[3].Value;
						var arguments = attribute.ConstructorArguments[4].Values.Select(v => v.Value).ToArray();

						var start = syntaxTree.GetText().Lines[line - 1].Start + column - 1;
						var location = Location.Create(syntaxTree, new TextSpan(start, length));

						yield return analyzer.GetDiagnosticInfo(id).CreateDiagnostic(location, arguments);
					}
				}
			}
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests(string directory)
		{
			return EnumerateTestCases(GetAbsoluteTestsDirectory(directory));
		}
	}
}