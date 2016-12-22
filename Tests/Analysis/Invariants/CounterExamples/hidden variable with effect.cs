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

namespace Tests.Analysis.Invariants.CounterExamples
{
	using System;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Shouldly;

	internal class HiddenVariableWithEffect : AnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			var e = Should.Throw<AnalysisException<SafetySharpRuntimeModel>>(() => CheckInvariant(true, c));
			e.InnerException.ShouldBeOfType<NondeterminismException>();

			SimulateCounterExample(e.CounterExample, simulator =>
			{
				c = (C)simulator.Model.Roots[0];

				c.X.ShouldBe(0);
				c.Y.ShouldBe(0);

				Should.Throw<InvalidOperationException>(() => simulator.SimulateStep());
				c.X.ShouldBe(2);
				c.Y.ShouldBe(2);
			});
		}

		private class C : Component
		{
			public int X;

			[Hidden]
			public int Y;

			public override void Update()
			{
				if (Y != 1)
					X = Choose(1, 2, 3);

				Y = X;
			}
		}
	}
}