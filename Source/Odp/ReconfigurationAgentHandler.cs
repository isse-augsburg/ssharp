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

namespace SafetySharp.Odp
{
	using System;
	using System.Collections.Generic;

	public class ReconfigurationAgentHandler<A, T, R> : IReconfigurationStrategy<A, T, R>
		where A : BaseAgent<A, T, R>
		where T : class, ITask
	{
		private readonly Func<T, IReconfigurationAgent<T>> _createReconfAgent;

		public ReconfigurationAgentHandler(Func<T, IReconfigurationAgent<T>> createReconfAgent)
		{
			_createReconfAgent = createReconfAgent;
		}

		protected readonly Dictionary<T, IReconfigurationAgent<T>> _tasksUnderReconstruction
			= new Dictionary<T, IReconfigurationAgent<T>>();

		public void Reconfigure(IEnumerable<T> deficientTasks)
		{
			foreach (var task in deficientTasks)
			{
				// TODO: what are these values?
				object agent = null;
				object state = null;

				LockConfigurations(task);
				if (!_tasksUnderReconstruction.ContainsKey(task))
				{
					_tasksUnderReconstruction.Add(task, _createReconfAgent(task));
				}
				_tasksUnderReconstruction[task].StartReconfiguration(task, agent, state);
			}
		}

		#region interface presented to reconfiguration agent
		public virtual void UpdateConfigurations(object conf)
		{
			throw new NotImplementedException();
		}

		public virtual void Go(T task)
		{
			UnlockConfigurations(task);
		}

		public virtual void Done(T task)
		{
			_tasksUnderReconstruction.Remove(task);
		}
		#endregion

		protected virtual void LockConfigurations(T task)
		{
			throw new NotImplementedException();
		}

		protected virtual void UnlockConfigurations(T task)
		{
			throw new NotImplementedException();
		}
	}
}
