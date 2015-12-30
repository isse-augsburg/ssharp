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

namespace Tests.Analysis.Ltl.Violated
{
	using SafetySharp.Modeling;
	using Shouldly;
	using static SafetySharp.Analysis.Tl;

	internal class UndoFaultAfterSuccessfulActivation : AnalysisTestObject
	{
		protected override void Check()
		{
			var d = new D();
			Check(G(d.G != 8), d).ShouldBe(false);
			Check(G(d.G != 7), d).ShouldBe(false);
		}

		private class D : Component
		{
			private readonly Fault _f1 = new TransientFault();
			private readonly Fault _f2 = new TransientFault();

			public int F;
			public int G;

			protected virtual int M => 1;
			protected virtual int N => 1;

			public override void Update()
			{
				G = M;
				F = N;
			}

			[FaultEffect(Fault = nameof(_f1)), Priority(0)]
			public class E1 : D
			{
				protected override int M => 7;
			}

			[FaultEffect(Fault = nameof(_f2)), Priority(0)]
			public class E2 : D
			{
				protected override int M => 8;
			}

			[FaultEffect(Fault = nameof(_f1)), Priority(0)]
			public class E3 : D
			{
				protected override int N => 1; // should not cause the activation to be undone
			}
		}
	}
}