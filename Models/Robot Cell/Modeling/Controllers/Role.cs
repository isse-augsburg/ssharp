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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Odp;

	internal struct Role
	{
		private byte _current;
		private readonly byte _capabilitiesToApplyStart;
		private readonly byte _capabilitiesToApplyCount;

		private Task Task => PreCondition.Task ?? PostCondition.Task;

		public IEnumerable<ICapability> CapabilitiesToApply =>
			Task.RequiredCapabilities.Skip(_capabilitiesToApplyStart).Take(_capabilitiesToApplyCount);

		public readonly Condition PreCondition;
		public readonly Condition PostCondition;

		public bool IsCompleted => _current >= _capabilitiesToApplyCount;

		public Role(Condition preCondition, Condition postCondition, int offset, int count)
			: this()
		{
			PreCondition = preCondition;
			PostCondition = postCondition;

			_capabilitiesToApplyStart = checked((byte)offset);
			_capabilitiesToApplyCount = checked((byte)count);
		}

		public void Execute(Agent agent)
		{
			if (_current >= _capabilitiesToApplyCount)
				throw new InvalidOperationException("The role has already been completely executed and must be reset.");

			var capability = Task.RequiredCapabilities[_capabilitiesToApplyStart + _current++];
			agent.AvailableCapabilities.First(c => c.IsEquivalentTo(capability)).Execute(agent);
		}

		public void Reset()
		{
			_current = 0;
		}
	}
}