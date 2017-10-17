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

	/// <summary>
	///   Measures the time an asynchronous function takes to complete, excluding wait times.
	/// </summary>
	/// <remarks>
	///   This method captures the current <see cref="SynchronizationContext"/> and wraps it in another one.
	///   Hence, the measured functions should not rely on the exact type of the current <see cref="SynchronizationContext"/>.
	///   Also, the measured functions must not use <c>ConfigureAwait(false)</c> or await other objects that
	///   do not capture the current SynchronizationContext.
	/// </remarks>
	public static class AsyncPerformance
	{
		/// <param name="asyncAction">The function to measure.</param>
		/// <returns>A <see cref="Stopwatch"/> representing the elapsed time.</returns>
		public static async Task<Stopwatch> Measure(Func<Task> asyncAction)
		{
			// just wrap the action and delegate to the real implementation
			Func<Task<object>> wrappedAction = async () => { await asyncAction(); return null; };
			return (await Measure(wrappedAction)).Item2;
		}

		/// <typeparam name="T">The return type of the function to measure.</typeparam>
		/// <param name="asyncFunc">The function to measure.</param>
		/// <returns>A tuple containing the return value of the function and a <see cref="Stopwatch"/> representing the elapsed time.</returns>
		public static async Task<Tuple<T, Stopwatch>> Measure<T>(Func<Task<T>> asyncFunc)
		{
			var ctx = PerformanceSynchronizationContext.Create();
			var value = await ctx.MeasureAsync(asyncFunc);
			return Tuple.Create(value, ctx.Stopwatch);
		}

		// Used internally to measure the time needed for async functions.
		private class PerformanceSynchronizationContext : SynchronizationContext
		{
			private readonly SynchronizationContext _ctx;
			public Stopwatch Stopwatch { get; } = new Stopwatch();

			private PerformanceSynchronizationContext(SynchronizationContext ctx)
			{
				_ctx = ctx;
			}

			// Creates a new instance, capturing the current SynchronizationContext.
			public static PerformanceSynchronizationContext Create()
			{
				return new PerformanceSynchronizationContext(Current);
			}

			public T MeasureAsync<T>(Func<T> measuredFunction)
			{
				// replace sync context and start measuring
				SetSynchronizationContext(this);
				Stopwatch.Start();

				// Execute the synchronous prefix of the async function.
				// The first await will capture the SynchronizationContext
				// (this instance) and then return. When the function is
				// ready to continue, the continuation will be posted to
				// the captured SynchronizationContext (this instance), and
				// thus will be measured too.
				var t = measuredFunction();

				// pause measuring and reset SynchronizationContext
				Stopwatch.Stop();
				SetSynchronizationContext(_ctx);

				return t;
			}

			private void MeasureAsync(Action measuredAction)
			{
				Func<object> wrappedAction = () => { measuredAction(); return null; };
				MeasureAsync(wrappedAction);
			}

			// Actual SynchronizationContext implementation
			public override void Post(SendOrPostCallback callback, object state)
			{
				// wrap the callback: the synchronous prefix must be measured.
				SendOrPostCallback wrappedCallback = (s) => MeasureAsync(() => { Debug.Assert(Current == this); callback(s); });

				// delegate to the "real" SynchronizationContext
				_ctx.Post(wrappedCallback, state);
			}
		}
	}
}
