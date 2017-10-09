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
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;
	using PerformanceCounter = SafetySharp.Modeling.PerformanceCounter;

	internal class MicrostepParallelismWithPerformanceMeasurement : TestObject
	{
		protected override void Check()
		{
			var model = TestModel.InitializeModel(new CFast(), new CSlow(), new CSync());
			var simulator = new Simulator(model);
			simulator.SimulateStep();
		}

		private class CFast : Component
		{
			public override void Update()
			{
				MicrostepScheduler.Schedule(WorkAsync);
			}

			private async Task WorkAsync()
			{
				var watch = await PerformanceCounter.Measure(async () =>
				{
					await Task.Yield();
					Thread.Sleep(100);
					await Submethod();
				});
				watch.ElapsedMilliseconds.ShouldBeInRange(200, 250);
			}

			private async Task Submethod()
			{
				await Task.Yield();
				Thread.Sleep(100);
			}
		}

		private class CSlow : Component
		{
			public override void Update()
			{
				MicrostepScheduler.Schedule(WorkAsync);
			}

			private async Task WorkAsync()
			{
				var watch = await PerformanceCounter.Measure(async () =>
				{
					await Task.Yield();
					Thread.Sleep(2500);
					await Submethod();
				});
				watch.ElapsedMilliseconds.ShouldBeInRange(5000, 5050);
			}

			private async Task Submethod()
			{
				await Task.Yield();
				Thread.Sleep(2500);
			}
		}

		private class CSync : Component
		{
			public override void Update()
			{
				var watch = Stopwatch.StartNew();
				Thread.Sleep(1000);
				watch.ElapsedMilliseconds.ShouldBeInRange(1000, 1050);
			}
		}
	}
}