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

namespace Tests.Execution.Simulation
{
	using System;
	using System.Threading.Tasks;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class MicrostepParallelismWithException : TestObject
	{
		protected override void Check()
		{
			var simulator = new Simulator(TestModel.InitializeModel(new C()));
			Should.Throw<E>(() =>  simulator.SimulateStep());
			C.X.ShouldBe(3);

			simulator = new Simulator(TestModel.InitializeModel(new D()));
			Should.Throw<E>(() => simulator.SimulateStep());
		}

		private class C : Component
		{
			public static int X { get; set; } = 0;

			public override void Update()
			{
				X++;
				MicrostepScheduler.Schedule(AsyncMethod);
			}

			private async Task AsyncMethod()
			{
				X++;
				await Task.Yield();

				X++;
				throw new E();
			}
		}

		private class D : Component
		{
			public override void Update()
			{
				MicrostepScheduler.Schedule(AsyncMethod);
			}

#pragma warning disable 1998
			private async Task AsyncMethod()
#pragma warning restore 1998
			{
				throw new E();
			}
		}

		private class E : Exception
		{ }
	}
}