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

namespace Tests.Formulas.StateFormulas
{
	using SafetySharp.Analysis;

	internal class Return : FormulaTestObject
	{
		private static int x;
		private readonly X1 x1 = new X1();
		private readonly X2 x2 = new X2();

		private Formula P1
		{
			get { return x == 2; }
		}

		private Formula P2 => x == 4;

		private Formula M1()
		{
			return x == 1;
		}

		private Formula M2() => x == 3;

		protected override void Check()
		{
			for (var i = 0; i < 7; ++i)
				Check(i);
		}

		private void Check(int value)
		{
			x = value;
			Check(M1(), () => x == 1);
			Check(P1, () => x == 2);
			Check(M2(), () => x == 3);
			Check(P2, () => x == 4);
			Check(x1[0], () => x == 5);
			Check(x2[0], () => x == 6);
		}

		private class X1
		{
			public Formula this[int i] => x == 5;
		}

		private class X2
		{
			public Formula this[int i]
			{
				get { return x == 6; }
			}
		}
	}
}