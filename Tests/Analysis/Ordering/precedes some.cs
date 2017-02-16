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

namespace Tests.Analysis.Ordering
{
	using System.Linq;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;

	internal class PrecedesSome : AnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			var result = AnalyzeOrder(c.X == 6, c);

			result.SafetyAnalysisResults.MinimalCriticalSets.Count.ShouldBe(1);
			ShouldContain(result.SafetyAnalysisResults.MinimalCriticalSets, c.F1, c.F2);
			var relationships = result.OrderRelationships[result.SafetyAnalysisResults.MinimalCriticalSets.Single()].ToArray();
			relationships.Length.ShouldBe(1);

			ShouldContain(relationships, c.F1, c.F2, OrderRelationshipKind.Precedes);
		}

		private class C : Component
		{
			public readonly Fault F1 = new TransientFault();
			public readonly Fault F2 = new TransientFault();
			public readonly Fault F3 = new TransientFault();
			public int X;
			public virtual int X1 => 0;
			public virtual int X2 => 0;
			public virtual int X3 => 0;

			public override void Update()
			{
				if (X == 0 && X1 == 2 && X2 == 0 && X3 == 0)
					X = 2;
				else if (X == 0 && X1 == 2 && X2 == 2)
					X = 6;
				else if (X == 2 && X2 == 2)
					X = 6;
			}

			[FaultEffect(Fault = nameof(F1))]
			private class E1 : C
			{
				public override int X1 => 2;
			}

			[FaultEffect(Fault = nameof(F2))]
			private class E2 : C
			{
				public override int X2 => 2;
			}

			[FaultEffect(Fault = nameof(F3))]
			private class E3 : C
			{
				public override int X3 => 2;
			}
		}
	}
}