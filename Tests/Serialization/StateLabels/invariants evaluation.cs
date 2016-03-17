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

namespace Tests.Serialization.StateLabels
{
	using SafetySharp.Modeling;
	using SafetySharp.Analysis;
	using Shouldly;
	using Utilities;

	internal class InvariantsEvaluation : TestModel
	{
		protected override void Check()
		{
			var c = new C { F = 3, G = 7, X = 3 };
			var m = TestModel.New(c);
			Formula s1 = c.F == 3;
			Formula s2 = c.G == 7;
			Formula s3 = c.F == c.X;

			var f0 = s1 || s2;
			var f1 = s1 && s2;
			var f2 = s1.Implies(s3);
			var f3 = s1.EquivalentTo(s3);
			var f4 = f3.Implies(f2);
			var f5 = f2.EquivalentTo(s2);

			Create(m, f0, f1, f2, f3, f4, f5);
			RootComponents.Length.ShouldBe(1);

			Formulas.Length.ShouldBe(6);
			StateFormulas.Length.ShouldBe(3);

			var root = RootComponents[0];
			root.ShouldBeOfType<C>();
			c = (C)root;

			Formulas[0].Evaluate().ShouldBe(true);
			Formulas[1].Evaluate().ShouldBe(true);
			Formulas[2].Evaluate().ShouldBe(true);
			Formulas[3].Evaluate().ShouldBe(true);
			Formulas[4].Evaluate().ShouldBe(true);
			Formulas[5].Evaluate().ShouldBe(true);
			
			c.X = 9;
			Formulas[2].Evaluate().ShouldBe(false);
			Formulas[3].Evaluate().ShouldBe(false);
			Formulas[4].Evaluate().ShouldBe(true);
			Formulas[5].Evaluate().ShouldBe(false);

			c.X = 3;
			c.G = 8;

			Formulas[0].Evaluate().ShouldBe(true);
			Formulas[1].Evaluate().ShouldBe(false);

			c.F = 9;

			Formulas[0].Evaluate().ShouldBe(false);
			Formulas[1].Evaluate().ShouldBe(false);
			Formulas[2].Evaluate().ShouldBe(true);
			Formulas[3].Evaluate().ShouldBe(true);
			Formulas[4].Evaluate().ShouldBe(true);
			Formulas[5].Evaluate().ShouldBe(false);

		}

		private class C : Component
		{
			public int F;
			public int G;
			public int X;
		}
	}
}