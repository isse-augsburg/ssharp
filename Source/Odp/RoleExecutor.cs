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

namespace SafetySharp.Odp
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using JetBrains.Annotations;

	/// <summary>
	///  Executes roles for a <see cref="BaseAgent"/>.
	///
	///  While the class and its state are exposed publicly,
	///  all mutators are internal and should only ever be used
	///  by the <see cref="BaseAgent"/> class.
	/// </summary>
	public class RoleExecutor
	{
		#region data

		/// <summary>
		///  The <see cref="BaseAgent"/> for which roles are executed.
		/// </summary>
		[NotNull]
		public BaseAgent Agent { get; }

		/// <summary>
		///  Indicates whether or not a role is currently being executed.
		/// </summary>
		public bool IsExecuting { get; private set; }

		/// <summary>
		///  The <see cref="Odp.Role"/> being executed, if any.
		/// </summary>
		private Role _role;

		/// <summary>
		///  If a <see cref="Odp.Role"/> is being executed, the number of steps already made.
		/// </summary>
		private int _executionState;

		#endregion

		/// <summary>
		///  Creates a new instance associated to the given <paramref name="agent"/>.
		/// </summary>
		internal RoleExecutor([NotNull] BaseAgent agent)
		{
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));
			Agent = agent;
		}

		#region accessors

		/// <summary>
		///  The <see cref="Odp.Role"/> being currently executed, if any. <c>null</c> otherwise.
		/// </summary>
		public Role? Role => IsExecuting ? (Role?) _role : null;

		/// <summary>
		///  The current <see cref="Role"/>'s input agent.
		/// </summary>
		[CanBeNull]
		public BaseAgent Input => Role?.Input;

		/// <summary>
		///  The current <see cref="Role"/>'s output agent.
		/// </summary>
		[CanBeNull]
		public BaseAgent Output => Role?.Output;

		/// <summary>
		///  The current <see cref="Role"/>'s task.
		/// </summary>
		[CanBeNull]
		public ITask Task => Role?.Task;

		/// <summary>
		///  If a <see cref="Odp.Role"/> is being executed, indicates whether or not execution is already complete.
		/// </summary>
		public bool IsCompleted => IsExecuting && (_role.PreCondition.StateLength + _executionState == _role.PostCondition.StateLength);

		/// <summary>
		///  If a <see cref="Odp.Role"/> is being executed, lists the capabilities already applied to the <see cref="Resource"/> being worked on.
		/// </summary>
		/// <exception cref="NullReferenceException">Thrown if no role is currently executed.</exception>
		[NotNull]
		public IEnumerable<ICapability> ExecutionState => Task.RequiredCapabilities.Take(_role.PreCondition.StateLength + _executionState);

        /// <summary>
        ///  Checks if the execution of the role is complete and the resource can be handed over to the next agent.
        /// </summary>
        public bool CanHandover => IsExecuting && IsCompleted && Output != null && Output.CanReceive(Agent, Role.Value.PostCondition);

		#endregion

		/// <summary>
		///  Starts execution of the given <paramref name="role"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if a <see cref="Odp.Role"/> is already being executed, or if <paramref name="role"/> is locked.</exception>
		internal void BeginExecution(Role role)
		{
			if (IsExecuting)
				throw new InvalidOperationException("Already executing a role.");
			if (role.IsLocked)
				throw new InvalidOperationException("Cannot execute a locked role.");

			_role = role;
			_executionState = 0;
			IsExecuting = true;
		}

		/// <summary>
		///  Executes the next step of the current <see cref="Role"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if no <see cref="Odp.Role"/> is being executed, or if execution is already complete.</exception>
		internal void ExecuteStep()
		{
			if (!IsExecuting)
				throw new InvalidOperationException("There is no role to execute.");
			if (IsCompleted)
				throw new InvalidOperationException("The role has already been completely executed.");

			var capability = _role.CapabilitiesToApply.ElementAt(_executionState);
			capability.Execute(Agent);
			_executionState++;
		}

		/// <summary>
		///  Ends (or aborts) execution of a <see cref="Odp.Role"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if no <see cref="Odp.Role"/> is being executed.</exception>
		internal void EndExecution()
		{
			if (!IsExecuting)
				throw new InvalidOperationException("No role is being executed.");

			IsExecuting = false;
			_executionState = 0;
		}
	}
}
