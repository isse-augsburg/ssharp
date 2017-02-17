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

namespace Tests.Analysis.Invariants.Violated
{
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using ISSE.SafetyChecking.Modeling;
	using ISSE.SafetyChecking.Formula;
	using Shouldly;

	internal class NonInlineFormulas : AnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();

			CheckInvariant(c.Invariant1, c).ShouldBe(false);
			CheckInvariant(c.Invariant2, c).ShouldBe(false);
			CheckInvariant(c.Invariant3(), c).ShouldBe(false);
			CheckInvariant(c.Invariant4, c).ShouldBe(false);
			CheckInvariant(c.Invariant5(), c).ShouldBe(false);
		}

		private class C : Component
		{
			private int _f;
			public readonly Formula Invariant2;

			public C()
			{
				Invariant1 = _f != 3;
				Invariant2 = _f != 4;
			}

			public Formula Invariant1 { get; }
			public Formula Invariant4 => Invariant1;

			public override void Update()
			{
				if (_f < 10)
					_f++;
			}

			public Formula Invariant3() => Invariant1;
			public Formula Invariant5() => _f == 5;
		}
	}
}