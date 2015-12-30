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
	using System.IO;
	using System.Text;
	using JetBrains.Annotations;
	using SafetySharp.Analysis;
	using SafetySharp.Analysis.FormulaVisitors;
	using Shouldly;
	using Utilities;
	using Xunit.Abstractions;

	public abstract class FormulaTestObject : TestObject
	{
		protected void Check(Formula actual, Func<bool> expected)
		{
			Check(actual, new StateFormula(expected));
		}

		protected void Check(Formula actual, Formula expected)
		{
			var builder = new StringBuilder();
			builder.AppendLine("Actual:");
			builder.AppendLine(actual.ToString());
			builder.AppendLine();

			builder.AppendLine("Expected:");
			builder.AppendLine(expected.ToString());

			Output.Log("{0}", builder);

			IsStructurallyEquivalent(actual, expected).ShouldBe(true);
		}

		private static bool IsStructurallyEquivalent(Formula f1, Formula f2)
		{
			var stateFormula1 = f1 as StateFormula;
			var stateFormula2 = f2 as StateFormula;
			var unaryFormula1 = f1 as UnaryFormula;
			var unaryFormula2 = f2 as UnaryFormula;
			var binaryFormula1 = f1 as BinaryFormula;
			var binaryFormula2 = f2 as BinaryFormula;

			if (stateFormula1 != null && stateFormula2 != null)
			{
				// Unfortunately, the C# compiler generates different methods for the same lambdas, so we
				// can't simply compare the delegates
				// We execute the expressions instead, that way we can at least guess whether they are the same
				return stateFormula1.Expression() == stateFormula2.Expression();
			}

			if (unaryFormula1 != null && unaryFormula2 != null)
			{
				return unaryFormula1.Operator == unaryFormula2.Operator &&
					   IsStructurallyEquivalent(unaryFormula1.Operand, unaryFormula2.Operand);
			}

			if (binaryFormula1 != null && binaryFormula2 != null)
			{
				return binaryFormula1.Operator == binaryFormula2.Operator &&
					   IsStructurallyEquivalent(binaryFormula1.LeftOperand, binaryFormula2.LeftOperand) &&
					   IsStructurallyEquivalent(binaryFormula1.RightOperand, binaryFormula2.RightOperand);
			}

			return false;
		}
	}

	partial class FormulaTests : Tests
	{
		public FormulaTests(ITestOutputHelper output)
			: base(output)
		{
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests(string directory)
		{
			return EnumerateTestCases(GetAbsoluteTestsDirectory(directory));
		}
	}
}