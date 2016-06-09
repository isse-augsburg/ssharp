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

namespace Tests.Formulas.Operators
{
	using System;
	using SafetySharp.Analysis;
	using Shouldly;

	internal class Compilation : FormulaTestObject
	{
		private int _i;
		private int _j;

		protected override void Check()
		{
			Not();
			And();
			Or();
			Implication();
			Equivalence();
		}

		private void Not()
		{
			Check(true).ShouldBe(false);
			_i.ShouldBe(1);

			Check(false).ShouldBe(true);
			_i.ShouldBe(1);
		}

		private void And()
		{
			Check(true, true, BinaryOperator.And).ShouldBe(true);
			_i.ShouldBe(1);
			_j.ShouldBe(1);

			Check(true, false, BinaryOperator.And).ShouldBe(false);
			_i.ShouldBe(1);
			_j.ShouldBe(1);

			Check(false, true, BinaryOperator.And).ShouldBe(false);
			_i.ShouldBe(1);
			_j.ShouldBe(0);

			Check(false, false, BinaryOperator.And).ShouldBe(false);
			_i.ShouldBe(1);
			_j.ShouldBe(0);
		}

		private void Or()
		{
			Check(true, true, BinaryOperator.Or).ShouldBe(true);
			_i.ShouldBe(1);
			_j.ShouldBe(0);

			Check(true, false, BinaryOperator.Or).ShouldBe(true);
			_i.ShouldBe(1);
			_j.ShouldBe(0);

			Check(false, true, BinaryOperator.Or).ShouldBe(true);
			_i.ShouldBe(1);
			_j.ShouldBe(1);

			Check(false, false, BinaryOperator.Or).ShouldBe(false);
			_i.ShouldBe(1);
			_j.ShouldBe(1);
		}

		private void Implication()
		{
			Check(true, true, BinaryOperator.Implication).ShouldBe(true);
			_i.ShouldBe(1);
			_j.ShouldBe(1);

			Check(true, false, BinaryOperator.Implication).ShouldBe(false);
			_i.ShouldBe(1);
			_j.ShouldBe(1);

			Check(false, true, BinaryOperator.Implication).ShouldBe(true);
			_i.ShouldBe(1);
			_j.ShouldBe(0);

			Check(false, false, BinaryOperator.Implication).ShouldBe(true);
			_i.ShouldBe(1);
			_j.ShouldBe(0);
		}

		private void Equivalence()
		{
			Check(true, true, BinaryOperator.Equivalence).ShouldBe(true);
			_i.ShouldBe(1);
			_j.ShouldBe(1);

			Check(true, false, BinaryOperator.Equivalence).ShouldBe(false);
			_i.ShouldBe(1);
			_j.ShouldBe(1);

			Check(false, true, BinaryOperator.Equivalence).ShouldBe(false);
			_i.ShouldBe(1);
			_j.ShouldBe(1);

			Check(false, false, BinaryOperator.Equivalence).ShouldBe(true);
			_i.ShouldBe(1);
			_j.ShouldBe(1);
		}

		private bool Check(bool first, bool second, BinaryOperator op)
		{
			_i = 0;
			_j = 0;

			Func<bool> left = () =>
			{
				++_i;
				return first;
			};

			Func<bool> right = () =>
			{
				++_j;
				return second;
			};

			return new BinaryFormula(left(), op, right()).Compile()();
		}

		private bool Check(bool value)
		{
			_i = 0;
			_j = 0;

			Func<bool> formula = () =>
			{
				++_i;
				return value;
			};

			return new UnaryFormula(formula(), UnaryOperator.Not).Compile()();
		}
	}
}