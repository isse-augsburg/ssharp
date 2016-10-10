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
	using Modeling;

	public class ReconfigurationAgentHandler<TAgent, TTask, TResource> : Component, IReconfigurationStrategy<TAgent, TTask, TResource>
		where TAgent : BaseAgent<TAgent, TTask, TResource>
		where TTask : class, ITask
	{
		private readonly TAgent _baseAgent;
		private readonly Func<TAgent, TTask, IReconfigurationAgent<TTask>> _createReconfAgent;

		public ReconfigurationAgentHandler(TAgent baseAgent, Func<TAgent, TTask, IReconfigurationAgent<TTask>> createReconfAgent)
		{
			_baseAgent = baseAgent;
			_createReconfAgent = createReconfAgent;
		}

		protected readonly Dictionary<TTask, IReconfigurationAgent<TTask>> _tasksUnderReconstruction
			= new Dictionary<TTask, IReconfigurationAgent<TTask>>();

		public void Reconfigure(IEnumerable<TTask> deficientTasks)
		{
			foreach (var task in deficientTasks)
			{
				// TODO: what are these values?
				object agent = null;
				object state = null;

				LockConfigurations(task);
				if (!_tasksUnderReconstruction.ContainsKey(task))
				{
					_tasksUnderReconstruction.Add(task, _createReconfAgent(_baseAgent, task));
				}
				_tasksUnderReconstruction[task].StartReconfiguration(task, agent, state);
			}
		}

		#region interface presented to reconfiguration agent
		public virtual void UpdateConfigurations(object conf)
		{
			throw new NotImplementedException();
		}

		public virtual void Go(TTask task)
		{
			UnlockConfigurations(task);
		}

		public virtual void Done(TTask task)
		{
			_tasksUnderReconstruction.Remove(task);
		}
		#endregion

		protected virtual void LockConfigurations(TTask task)
		{
			throw new NotImplementedException();
		}

		protected virtual void UnlockConfigurations(TTask task)
		{
			throw new NotImplementedException();
		}
	}
}
