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

	public class RoleExecutor
	{
		public BaseAgent Agent { get; }

		public bool IsExecuting { get; private set; }

		private Role _role;

		private int _executionState;

		public RoleExecutor(BaseAgent agent)
		{
			Agent = agent;
		}

		public Role? Role => IsExecuting ? (Role?) _role : null;

		public bool IsCompleted => IsExecuting && (_role.PreCondition.StateLength + _executionState == _role.PostCondition.StateLength);

		public IEnumerable<ICapability> ExecutionState => Task.RequiredCapabilities.Take(_role.PreCondition.StateLength + _executionState);

		internal void BeginExecution(Role role)
		{
			if (IsExecuting)
				throw new InvalidOperationException("Already executing a role.");

			_role = role;
			IsExecuting = true;
		}

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

		internal void EndExecution()
		{
			if (!IsExecuting)
				throw new InvalidOperationException("No role is being executed.");

			IsExecuting = false;
		}

		// convenience accessors
		public BaseAgent Input => Role?.Input;
		public BaseAgent Output => Role?.Output;
		public ITask Task => Role?.Task;
	}
}
