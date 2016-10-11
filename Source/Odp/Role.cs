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
	using System.Linq;

	public struct Role<TAgent, TTask>
		where TAgent : BaseAgent<TAgent, TTask>
		where TTask : class, ITask
	{
		public bool IsLocked { get; set; }

		public Condition<TAgent, TTask> PreCondition;
		public Condition<TAgent, TTask> PostCondition;

		public TTask Task => PreCondition.Task ?? PostCondition.Task;

		public IEnumerable<ICapability> CapabilitiesToApply =>
			Task.RequiredCapabilities.Skip(_capabilitiesToApplyStart).Take(_capabilitiesToApplyCount);

		private byte _capabilitiesToApplyStart;
		private byte _capabilitiesToApplyCount;
		private byte _current;

		public bool IsCompleted => _current >= _capabilitiesToApplyCount;

		public void ExecuteStep(TAgent agent)
		{
			if (IsCompleted)
				throw new InvalidOperationException("The role has already been completely executed and must be reset.");

			var capability = Task.RequiredCapabilities[_capabilitiesToApplyStart + _current++];
			agent.ApplyCapability(capability);
		}

		public void Reset()
		{
			_current = 0;
		}

		public void Clear()
		{
			_capabilitiesToApplyStart = checked((byte)PreCondition.State.Count());
			_capabilitiesToApplyCount = 0;
		}

		public void AddCapability(ICapability capability)
		{
			if (_capabilitiesToApplyStart + _capabilitiesToApplyCount >= Task.RequiredCapabilities.Length)
				throw new InvalidOperationException("All required capabilities already applied.");
			if (!capability.Equals(Task.RequiredCapabilities[_capabilitiesToApplyStart + _capabilitiesToApplyCount]))
				throw new InvalidOperationException("Cannot apply capability that is not required.");
			_capabilitiesToApplyCount++;
		}

		public bool IsEmpty()
		{
			return _capabilitiesToApplyCount == 0;
		}
	}
}
