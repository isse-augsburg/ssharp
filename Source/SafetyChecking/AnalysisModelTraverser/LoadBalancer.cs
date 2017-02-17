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

namespace ISSE.SafetyChecking.AnalysisModelTraverser
{
	using System.Collections.Concurrent;
	using System.Threading;
	using Utilities;

	/// <summary>
	///   Balances the load of multiple <see cref="Worker" /> instances.
	/// </summary>
	internal sealed class LoadBalancer
	{
		private readonly StateStack[] _stacks;
		private bool[] _awaitingWork;
		private ConcurrentQueue<int> _idleWorkers;
		private volatile bool _terminated;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="stacks"></param>
		public LoadBalancer(StateStack[] stacks)
		{
			_stacks = stacks;
			Reset();
		}

		/// <summary>
		///   Gets the number of workers.
		/// </summary>
		private int WorkerCount => _stacks.Length;

		/// <summary>
		///   Gets a value indicating whether model traversal has been terminated.
		/// </summary>
		public bool IsTerminated => _terminated;

		/// <summary>
		///   Balances the load between <see cref="Worker" /> instances. Returns <c>false</c> to indicate that the worker should
		///   terminate.
		/// </summary>
		public bool LoadBalance(int workerIndex)
		{
			// If the invariant check has been terminated, terminate the worker
			if (_terminated)
				return false;

			var hasWork = _stacks[workerIndex].FrameCount > 0;
			var areWorkersIdle = !_idleWorkers.IsEmpty;

			// If the worker still has work and no other worker is idle, let the worker continue
			if (hasWork && !areWorkersIdle)
				return true;

			// If the worker doesn't have any work, wait until new work is assigned to it or there is no more work
			if (!hasWork)
				return AwaitWork(workerIndex);

			// Try to assign some of the worker's work to an idle worker, if possible
			if (_stacks[workerIndex].CanSplit)
				return AssignWork(workerIndex);

			// Otherwise, let the worker continue
			return true;
		}

		/// <summary>
		///   Assigns work to an idle worker.
		/// </summary>
		private bool AssignWork(int workerIndex)
		{
			Assert.That(_stacks[workerIndex].FrameCount != 0, "Idle worker tries to assign work.");

			int idleWorker;
			if (!_idleWorkers.TryDequeue(out idleWorker))
				return true;

			// At this point we've got an idle worker that we can assign work to
			Assert.That(_stacks[idleWorker].FrameCount == 0, "Trying to assign work to non-idle worker.");
			Assert.That(workerIndex != idleWorker, "Worker tries to assign work to itself.");

			// If the worker actually got some new work, notify it, otherwise continue waiting
			if (_stacks[workerIndex].SplitWork(_stacks[idleWorker]))
			{
				Assert.That(_stacks[idleWorker].FrameCount != 0, "No work was assigned to non-idle worker.");
				Volatile.Write(ref _awaitingWork[idleWorker], false);
			}
			else
			{
				Assert.That(_stacks[idleWorker].FrameCount == 0, "Unexpected work assigned to idle worker.");
				_idleWorkers.Enqueue(idleWorker);
			}

			return true;
		}

		/// <summary>
		///   Stalls the worker until work has been assigned to it or there is no more work.
		/// </summary>
		private bool AwaitWork(int workerIndex)
		{
			Assert.That(_stacks[workerIndex].FrameCount == 0, "Non-idle worker awaits work.");

			Volatile.Write(ref _awaitingWork[workerIndex], true);
			_idleWorkers.Enqueue(workerIndex);

			var spinWait = new SpinWait();
			while (Volatile.Read(ref _awaitingWork[workerIndex]) && !_terminated)
			{
				// If all workers are idle, terminate the invariant check, otherwise wait a bit
				// before checking again for new work
				if (_idleWorkers.Count == WorkerCount)
					Terminate();
				else
					spinWait.SpinOnce();
			}

			// The worker now either has work available and it can continue, or the invariant check has been
			// terminated and so the worker should terminate
			return !_terminated;
		}

		/// <summary>
		///   Terminates the invariant check.
		/// </summary>
		public void Terminate()
		{
			_terminated = true;
		}

		/// <summary>
		///   Resets the load balancer so that a new invariant check can be started.
		/// </summary>
		public void Reset()
		{
			_terminated = false;
			_idleWorkers = new ConcurrentQueue<int>();
			_awaitingWork = new bool[_stacks.Length];
		}
	}
}