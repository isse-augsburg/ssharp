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

namespace Tests.Formulas.Operators
{
	using ISSE.SafetyChecking.Formula;
	using SafetySharp.Analysis;
	using Shouldly;
	using static SafetySharp.Analysis.Operators;

	internal class T9 : FormulaTestObject
	{
		protected override void Check()
		{
			var intValue = 7;

			{
				var actual = ((Formula)false).Implies(intValue < 7);
				var expected = new BinaryFormula(
					new ExecutableStateFormula(() => false),
					BinaryOperator.Implication,
					new ExecutableStateFormula(() => intValue < 7));

				Check(actual, expected);
			}

			{
				var actual = ((Formula)false).Implies(F(intValue < 7));
				var expected = new BinaryFormula(
					new ExecutableStateFormula(() => false),
					BinaryOperator.Implication,
					new UnaryFormula(new ExecutableStateFormula(() => intValue < 7), UnaryOperator.Finally));

				Check(actual, expected);
			}

			{
				var actual = false.Implies(intValue < 7);
				var expected = new ExecutableStateFormula(() => !false || intValue < 7);

				Check(actual, expected);
			}

			{
				var actual = false.Implies(F(intValue < 7));
				var expected = new BinaryFormula(
					new ExecutableStateFormula(() => false),
					BinaryOperator.Implication,
					new UnaryFormula(new ExecutableStateFormula(() => intValue < 7), UnaryOperator.Finally));

				Check(actual, expected);
			}

			true.Implies(true).ShouldBe(true);
			false.Implies(true).ShouldBe(true);
			true.Implies(false).ShouldBe(false);
			false.Implies(false).ShouldBe(true);
		}
	}
}