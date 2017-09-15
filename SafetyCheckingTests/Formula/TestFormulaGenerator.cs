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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Formula
{
	using ISSE.SafetyChecking.Formula;

	public static class TestFormulaGenerator
	{
		public static Formula CreateBinaryFormula()
		{
			var ap1 = new AtomarPropositionFormula();
			var ap2 = new AtomarPropositionFormula();
			var bexp = new BinaryFormula(ap1, BinaryOperator.And, ap2);
			return bexp;
		}

		public static Formula CreateNestedOnceFormula()
		{
			var bexp = CreateBinaryFormula();
			var once = new UnaryFormula(bexp, UnaryOperator.Once);
			return once;
		}

		public static Formula CreateConnectedNestedOnceFormula()
		{
			var once1 = CreateNestedOnceFormula();
			var once2 = CreateNestedOnceFormula();
			var conOnce = new BinaryFormula(once1, BinaryOperator.Or, once2);
			return conOnce;
		}

		public static Formula CreateFinallyNestedFormula()
		{
			var once = CreateNestedOnceFormula();
			var fin = new UnaryFormula(once, UnaryOperator.Finally);
			return fin;
		}

		public static Formula CreateFinallyConnectedNestedFormula()
		{
			var conOnce = CreateConnectedNestedOnceFormula();
			var fin = new UnaryFormula(conOnce, UnaryOperator.Finally);
			return fin;
		}

		public static Formula CreateNestedTwice()
		{
			var once = CreateNestedOnceFormula();
			var twice = new UnaryFormula(once, UnaryOperator.Once);
			return twice;
		}
	}
}
