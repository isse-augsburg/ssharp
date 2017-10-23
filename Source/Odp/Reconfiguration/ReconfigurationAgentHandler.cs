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
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;
	using Modeling;

	/// <summary>
	///   An <see cref="IReconfigurationStrategy"/> that manages (partially) distributed reconfigurations
	///   using communicating reconfiguration agents.
	/// </summary>
	/// <remarks>One instance of this class is associated to exactly one <see cref="BaseAgent"/>.</remarks>
	public class ReconfigurationAgentHandler : Component, IReconfigurationStrategy
	{
		// the BaseAgent the handler is associated to
		private readonly BaseAgent _baseAgent;

		// a factory for new reconfiguration agents
		private readonly Func<BaseAgent, ReconfigurationAgentHandler, ITask, IReconfigurationAgent> _createReconfAgent;

		/// <summary>
		///   Creates a new handler.
		/// </summary>
		/// <param name="baseAgent">The agent the handler belongs to.</param>
		/// <param name="createReconfAgent">A factory for new reconfiguration agents.</param>
		public ReconfigurationAgentHandler(
			BaseAgent baseAgent,
			Func<BaseAgent, ReconfigurationAgentHandler, ITask, IReconfigurationAgent> createReconfAgent
		)
		{
			_baseAgent = baseAgent;
			_createReconfAgent = createReconfAgent;
		}

		protected override void Initialize()
		{
			Debug.WriteLine("Resetting {0} for agent {1}", nameof(ReconfigurationAgentHandler), _baseAgent.Id);

			// Because these dictionaries are marked as non-discoverable, S# does not reset them when it resets the model.
			// Usually, this is no problem, since they should be empty at the end of each step anyway.
			// However, if an exception is thrown and the step is aborted, this is not the case.
			// Hence, in the next step the dictionaries are non-empty and cause further errors or incorrect behaviour.
			_tasksUnderReconstruction.Clear();
			_reconfigurationProcesses.Clear();
		}

		// Reconfiguration always completes within one step, hence the dictionaries should
		// always be empty during serialization. Thus there is no need to include space
		// for the elements in the state vector, or to set a predefined capacity.

		// associates ITask instances with existing reconfiguration agents
		[NonDiscoverable, Hidden(HideElements = true)]
		private readonly Dictionary<ITask, IReconfigurationAgent> _tasksUnderReconstruction = new Dictionary<ITask, IReconfigurationAgent>();

		// associates ITask instances with TaskCompletionSources
		// The source's Task represents the reconfiguration process for the task, and the source is used to mark it as complete.
		[NonDiscoverable, Hidden(HideElements = true)]
		private readonly Dictionary<ITask, TaskCompletionSource<object>> _reconfigurationProcesses =
			new Dictionary<ITask, TaskCompletionSource<object>>();

		/// <summary>
		///   cf. <see cref="IReconfigurationStrategy.Reconfigure"/>.
		/// </summary>
		public async Task Reconfigure(IEnumerable<ReconfigurationRequest> reconfigurations)
		{
			var newReconfigurations = new List<Task>();

			foreach (var request in reconfigurations)
			{
				var task = request.Task;
				LockAllocatedRoles(task);

				if (!_tasksUnderReconstruction.ContainsKey(task))
				{
					_tasksUnderReconstruction[task] = _createReconfAgent(_baseAgent, this, task);
					_reconfigurationProcesses[task] = new TaskCompletionSource<object>();
				}

				newReconfigurations.Add(_reconfigurationProcesses[task].Task);
				_tasksUnderReconstruction[task].StartReconfiguration(request);
			}

			await Task.WhenAll(newReconfigurations);
		}

		#region interface presented to reconfiguration agent

		/// <summary>
		///   Called by a reconfiguration agent once it knows the roles its <see cref="BaseAgent"/>
		///   will lose or receive.
		/// </summary>
		/// <param name="task">The task being reconfigured by the calling agent.</param>
		/// <param name="config">The configuration changes to be applied to the <see cref="BaseAgent"/>.</param>
		/// <remarks>
		///   <list type="bullet">
		///     <item><description><paramref name="config"/> may include changes for other agents, which will simply be ignored.</description></item>
		///     <item><description>This method will call <see cref="IReconfigurationAgent.Acknowledge"/> when done.</description></item>
		///   </list>
		/// </remarks>
		public virtual void UpdateAllocatedRoles(ITask task, ConfigurationUpdate config)
		{
            _baseAgent.PrepareReconfiguration(task);

			// new roles must be locked
			config.LockAddedRoles();
			config.Apply(_baseAgent);

			_tasksUnderReconstruction[task].Acknowledge();
		}

		/// <summary>
		///   Called by a reconfiguration agent to notify the handler the newly applied configuration is ready to be used.
		/// </summary>
		/// <param name="task">The <see cref="ITask"/> reconfigured by the calling agent.</param>
		public virtual void Go(ITask task)
		{
			LockAllocatedRoles(task, false);
		}

		/// <summary>
		///   Called by a reconfiguration agent to notify the handler it has fully ompleted the reconfiguration.
		/// </summary>
		/// <param name="task">The <see cref="ITask"/> reconfigured by the calling agent.</param>
		public virtual void Done(ITask task)
		{
			_tasksUnderReconstruction.Remove(task);
			_reconfigurationProcesses[task].SetResult(null);
			_reconfigurationProcesses.Remove(task);
		}

		#endregion

		// Locks (or unlocks) all base agent roles for the given task.
		private void LockAllocatedRoles(ITask task, bool locked = true)
		{
			_baseAgent.LockRoles(_baseAgent.AllocatedRoles.Where(role => role.Task == task), locked);
		}
	}
}
