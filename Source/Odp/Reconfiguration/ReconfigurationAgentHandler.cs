
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

namespace SafetySharp.Odp.Reconfiguration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Modeling;

	public class ReconfigurationAgentHandler : IReconfigurationStrategy
	{
		private readonly BaseAgent _baseAgent;
		private readonly Func<BaseAgent, ReconfigurationAgentHandler, ITask, IReconfigurationAgent> _createReconfAgent;

		public ReconfigurationAgentHandler(
			BaseAgent baseAgent,
			Func<BaseAgent, ReconfigurationAgentHandler, ITask, IReconfigurationAgent> createReconfAgent
		)
		{
			_baseAgent = baseAgent;
			_createReconfAgent = createReconfAgent;
		}

		// Reconfiguration always completes within one step, hence the dictionaries should
		// always be empty during serialization. Thus there is no need to include space
		// for the elements in the state vector, or to set a predefined capacity.
		[NonDiscoverable, Hidden(HideElements = true)]
		private readonly Dictionary<ITask, IReconfigurationAgent> _tasksUnderReconstruction = new Dictionary<ITask, IReconfigurationAgent>();

		[NonDiscoverable, Hidden(HideElements = true)]
		private readonly Dictionary<ITask, TaskCompletionSource<object>> _reconfigurationProcesses =
			new Dictionary<ITask, TaskCompletionSource<object>>();


		public async Task Reconfigure(IEnumerable<Tuple<ITask, BaseAgent.State>> reconfigurations)
		{
			var newReconfigurations = new List<Task>();

			foreach (var tuple in reconfigurations)
			{
				var task = tuple.Item1;
				var baseAgentState = tuple.Item2;
				var agent = baseAgentState.ReconfRequestSource ?? _baseAgent;

				LockAllocatedRoles(task);

				if (!_tasksUnderReconstruction.ContainsKey(task))
				{
					_tasksUnderReconstruction[task] = _createReconfAgent(_baseAgent, this, task);
					_reconfigurationProcesses[task] = new TaskCompletionSource<object>();
				}
				newReconfigurations.Add(_reconfigurationProcesses[task].Task);
				_tasksUnderReconstruction[task].StartReconfiguration(task, agent, baseAgentState);
			}

			await Task.WhenAll(newReconfigurations);
		}

		#region interface presented to reconfiguration agent
		public virtual void UpdateAllocatedRoles(ITask task, ConfigurationUpdate config)
		{
            _baseAgent.PrepareReconfiguration(task);

			// new roles must be locked
			config.LockAddedRoles();
			config.Apply(_baseAgent);

			_tasksUnderReconstruction[task].Acknowledge();
		}

		public virtual void Go(ITask task)
		{
			LockAllocatedRoles(task, false);
		}

		public virtual void Done(ITask task)
		{
			_tasksUnderReconstruction.Remove(task);
			_reconfigurationProcesses[task].SetResult(null);
			_reconfigurationProcesses.Remove(task);
		}
		#endregion

		private void LockAllocatedRoles(ITask task, bool locked = true)
		{
			_baseAgent.LockRoles(_baseAgent.AllocatedRoles.Where(role => role.Task == task), locked);
		}
	}
}
