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

namespace Tests.Analysis.Invariants.NotViolated
{
	using System;
	using SafetySharp.Modeling;
	using ISSE.SafetyChecking.Modeling;
	using Shouldly;

	internal class DisabledFaults : AnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C
			{
				X = 3,
				F1 = { Activation = Activation.Suppressed },
				F2 = { Activation = Activation.Suppressed }
			};

			CheckInvariant(c.X == 3, c).ShouldBe(true);
		}

		private class C : Component
		{
			public readonly Fault F1 = new TransientFault();
			public readonly Fault F2 = new TransientFault();
			public int X;

			[FaultEffect(Fault = nameof(F1))]
			internal class E1 : C
			{
				public override void Update()
				{
					Console.WriteLine("no ");
					X = 77;
				}
			}

			[FaultEffect(Fault = nameof(F2))]
			internal class E2 : C
			{
				public override void Update()
				{
					Console.WriteLine("no !!");
					X = 717;
				}
			}
		}
	}
}