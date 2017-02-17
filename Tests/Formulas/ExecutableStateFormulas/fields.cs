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

namespace Tests.Formulas.ExecutableStateFormulas
{
	using ISSE.SafetyChecking.Formula;
	using SafetySharp.Analysis;

	internal class Fields : FormulaTestObject
	{
		private static int x;

		private readonly Formula f = x == 2;
		private readonly Formula f1 = x == 3, f2 = x == 4;
		private readonly Formula f3 = (Formula)(x == 5);

		protected override void Check()
		{
			for (var i = 0; i < 6; ++i)
				Check(i);
		}

		private void Check(int value)
		{
			x = value;

			Check(f, () => x == 2);
			Check(f1, () => x == 3);
			Check(f2, () => x == 4);
			Check(f3, () => x == 5);
		}
	}
}