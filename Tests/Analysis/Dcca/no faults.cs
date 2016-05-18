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

namespace Tests.Analysis.Dcca
{
	using SafetySharp.Modeling;
	using Shouldly;

	internal class NoFaults : AnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			var result = Dcca(c.X > 4, c);

			result.CheckedSets.Count.ShouldBe(1);
			result.MinimalCriticalSets.ShouldBeEmpty();
			result.CounterExamples.ShouldBeEmpty();
			result.Exceptions.ShouldBeEmpty();
			result.IsComplete.ShouldBe(true);
			result.SuppressedFaults.ShouldBeEmpty();
			result.ForcedFaults.ShouldBeEmpty();

			ShouldContain(result.CheckedSets);
			result = DccaWithMaxCardinality(c.X > 4, 0, c);

			result.CheckedSets.Count.ShouldBe(1);
			result.MinimalCriticalSets.ShouldBeEmpty();
			result.CounterExamples.ShouldBeEmpty();
			result.Exceptions.ShouldBeEmpty();
			result.IsComplete.ShouldBe(true);
		}

		private class C : Component
		{
			public int X;

			public override void Update()
			{
			}
		}
	}
}