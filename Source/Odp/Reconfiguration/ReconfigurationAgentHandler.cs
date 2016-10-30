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

		// Reconfiguration always completes within one step, hence the dictionary should
		// always be empty during serialization. Thus there is no need to include space
		// for the elements in the state vector, or to set a predefined capacity.
		[NonDiscoverable, Hidden(HideElements = true)]
		private readonly Dictionary<ITask, IReconfigurationAgent> _tasksUnderReconstruction
			= new Dictionary<ITask, IReconfigurationAgent>();

		public void Reconfigure(IEnumerable<Tuple<ITask, BaseAgent.State>> reconfigurations)
		{
			foreach (var tuple in reconfigurations)
			{
				var task = tuple.Item1;
				var baseAgentState = tuple.Item2;
				var agent = baseAgentState.ReconfRequestSource ?? _baseAgent;

				LockAllocatedRoles(task);

				if (!_tasksUnderReconstruction.ContainsKey(task))
					_tasksUnderReconstruction[task] = _createReconfAgent(_baseAgent, this, task);
				_tasksUnderReconstruction[task].StartReconfiguration(task, agent, baseAgentState);
			}
		}

		#region interface presented to reconfiguration agent
		public virtual void UpdateAllocatedRoles(ConfigurationUpdate config)
		{
			// new roles must be locked
			config.LockAddedRoles();
			config.Apply(_baseAgent);
		}

		public virtual void Go(ITask task)
		{
			LockAllocatedRoles(task, false);
		}

		public virtual void Done(ITask task)
		{
			_tasksUnderReconstruction.Remove(task);
		}
		#endregion

		private void LockAllocatedRoles(ITask task, bool locked = true)
		{
			for (int i = 0; i < _baseAgent.AllocatedRoles.Count; ++i)
			{
				// necessary as long as roles are structs
				var role = _baseAgent.AllocatedRoles[i];
				if (role.Task == task)
					_baseAgent.AllocatedRoles[i] = role.Lock(locked);
			}
		}
	}
}
