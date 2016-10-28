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
	using System.Threading.Tasks;
	using SafetySharp.Modeling;
	using Shouldly;

	internal class MicrostepParallelism : AnalysisTestObject
	{
		protected override void Check()
		{
			var result = Dcca(C.X != 0, new C(), new C());
			result.MinimalCriticalSets.ShouldBeEmpty();
		}

		class C : Component
		{
			public static int X = 0;

			public readonly Fault F1 = new PermanentFault();
			public readonly Fault F2 = new PermanentFault();

			public override void Update()
			{
				MicrostepScheduler.Schedule(WorkAsync);
			}

			protected virtual async Task WorkAsync()
			{
				++X;
				await Task.Yield();

				++X;
				await Task.Yield();

				X -= 2;
			}

			[FaultEffect(Fault = nameof(F1))]
			private class F1Effect : C
			{
			}

			[FaultEffect(Fault = nameof(F2))]
			private class F2Effect : C
			{
			}
		}
	}
}