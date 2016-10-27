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
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// This class can be used to simulate parallel execution within a microstep.
	/// </summary>
	public static class MicrostepScheduler
	{
		[ThreadStatic]
		private static SingleThreadSynchronizationContext _context
			= new SingleThreadSynchronizationContext();

		/// <summary>
		/// Schedules a callback for simulated asynchronous execution.
		/// </summary>
		/// <param name="action">The scheduled callback, which may be marked as <c>async</c>.</param>
		public static void Schedule(Action action)
		{
			_context.Post(o => action(), null);
		}

		public static void Schedule(Func<Task> action)
		{
			Schedule(() => { action(); });
		}

		public static void Schedule<T>(Func<Task<T>> action)
		{
			Schedule(() => { action(); });
		}

		/// <summary>
		/// Processes the scheduled callbacks.
		/// </summary>
		internal static void CompleteSchedule()
		{
			_context.Run();
		}

		private class SingleThreadSynchronizationContext : SynchronizationContext
		{
			private readonly Queue<Tuple<SendOrPostCallback, object>> _queue = new Queue<Tuple<SendOrPostCallback, object>>();

			public override void Post(SendOrPostCallback d, object state)
			{
				_queue.Enqueue(Tuple.Create(d, state));
			}

			public void Run()
			{
				var oldContext = Current;
				SetSynchronizationContext(this);

				while (_queue.Count > 0)
				{
					var job = _queue.Dequeue();
					job.Item1(job.Item2);
				}

				SetSynchronizationContext(oldContext);
			}
		}
	}
}
