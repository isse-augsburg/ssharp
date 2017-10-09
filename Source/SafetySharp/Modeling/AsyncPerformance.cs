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

namespace SafetySharp.Modeling
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;

	public static class AsyncPerformance
	{
		public static async Task<Stopwatch> Measure(Func<Task> asyncBlock)
		{
			Func<Task<object>> wrappedBlock = async () => { await asyncBlock(); return null; };
			return (await Measure(wrappedBlock)).Item2;
		}

		public static async Task<Tuple<T, Stopwatch>> Measure<T>(Func<Task<T>> asyncBlock)
		{
			var ctx = new PerformanceSynchronizationContext();
			var task = ctx.MeasureAsync(asyncBlock);
			var value = await task;
			return Tuple.Create(value, ctx.Stopwatch);
		}

		private class PerformanceSynchronizationContext : SynchronizationContext
		{
			private SynchronizationContext _ctx;
			public Stopwatch Stopwatch { get; } = new Stopwatch();

			public T MeasureAsync<T>(Func<T> measuredFunction)
			{
				_ctx = Current;
				SetSynchronizationContext(this);
				Stopwatch.Start();

				var t = measuredFunction();

				Stopwatch.Stop();
				SetSynchronizationContext(_ctx);

				return t;
			}

			private void MeasureAsync(Action measuredAction)
			{
				Func<object> measuredFunction = () =>
				{
					measuredAction();
					return null;
				};
				MeasureAsync(measuredFunction);
			}

			public override void Post(SendOrPostCallback callback, object state)
			{
				SendOrPostCallback wrappedCallback = (s) => MeasureAsync(() => callback(s));
				_ctx.Post(wrappedCallback, state);
			}
		}
	}
}
