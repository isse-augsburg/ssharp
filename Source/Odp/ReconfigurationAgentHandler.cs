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

	public class ReconfigurationAgentHandler<TAgent> : Component, IReconfigurationStrategy<TAgent>
		where TAgent : BaseAgent<TAgent>
	{
		private readonly TAgent _baseAgent;
		private readonly Func<TAgent, ITask, IReconfigurationAgent<TAgent>> _createReconfAgent;

		public ReconfigurationAgentHandler(
			TAgent baseAgent,
			Func<TAgent, ITask, IReconfigurationAgent<TAgent>> createReconfAgent
		)
		{
			_baseAgent = baseAgent;
			_createReconfAgent = createReconfAgent;
		}

		protected readonly Dictionary<ITask, IReconfigurationAgent<TAgent>> _tasksUnderReconstruction
			= new Dictionary<ITask, IReconfigurationAgent<TAgent>>();

		public void Reconfigure(IEnumerable<Tuple<ITask, BaseAgent<TAgent>.State>> reconfigurations)
		{
			foreach (var tuple in reconfigurations)
			{
				var task = tuple.Item1;
				var baseAgentState = tuple.Item2;
				var agent = baseAgentState.ReconfRequestSource ?? _baseAgent;

				LockAllocatedRoles(task);
				if (!_tasksUnderReconstruction.ContainsKey(task))
				{
					_tasksUnderReconstruction.Add(task, _createReconfAgent(_baseAgent, task));
				}

				_tasksUnderReconstruction[task].StartReconfiguration(task, agent, baseAgentState);
			}
		}

		#region interface presented to reconfiguration agent
		public virtual void UpdateAllocatedRoles(ITask task, Role<TAgent>[] newRoles)
		{
			// new roles must be locked
			for (int i = 0; i < newRoles.Length; ++i)
			{
				var role = newRoles[i];
				role.IsLocked = true;
				newRoles[i] = role;
			}

			_baseAgent.RemoveAllocatedRoles(task);
			_baseAgent.AllocateRoles(newRoles);
		}

		public virtual void Go(ITask task)
		{
			UnlockRoleAllocations(task);
		}

		public virtual void Done(ITask task)
		{
			_tasksUnderReconstruction.Remove(task);
		}
		#endregion

		protected virtual void LockAllocatedRoles(ITask task)
		{
			for (int i = 0; i < _baseAgent.AllocatedRoles.Count; ++i)
			{
				// necessary as long as roles are structs
				var role = _baseAgent.AllocatedRoles[i];
				if (role.Task == task)
				{
					role.IsLocked = true;
					_baseAgent.AllocatedRoles[i] = role;
				}
			}
		}

		protected virtual void UnlockRoleAllocations(ITask task)
		{
			for (int i = 0; i < _baseAgent.AllocatedRoles.Count; ++i)
			{
				// necessary as long as roles are structs
				var role = _baseAgent.AllocatedRoles[i];
				if (role.Task == task)
				{
					role.IsLocked = false;
					_baseAgent.AllocatedRoles[i] = role;
				}
			}
		}
	}
}
