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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers
{
	using System.Collections.Generic;
	using System.Diagnostics.Contracts;
	using System.Linq;

	internal struct Condition
	{
		private readonly byte _statePrefixLength;
		public readonly Agent Port;
		public readonly Task Task;

		public Condition(Task task, Agent port, int statePrefixLength)
		{
			_statePrefixLength = checked((byte)statePrefixLength);
			Port = port;
			Task = task;
		}

#if ENABLE_F4 // error F4: incorrect Condition.State format
		public IEnumerable<Capability> State =>
			Task?.Capabilities.Skip(Task.Capabilities.Length - _statePrefixLength) ?? Enumerable.Empty<Capability>();
#else
		public IEnumerable<Capability> State =>
			Task?.Capabilities.Take(_statePrefixLength) ?? Enumerable.Empty<Capability>();
#endif

		[Pure]
		public bool StateMatches(IEnumerable<Capability> other)
		{
			return State.SequenceEqual(other, StateComparer.Default);
		}

		private class StateComparer : IEqualityComparer<Capability>
		{
			public static readonly StateComparer Default = new StateComparer();

			public bool Equals(Capability x, Capability y)
			{
				return x.IsEquivalentTo(y);
			}

			public int GetHashCode(Capability obj)
			{
				return obj.GetHashCode();
			}
		}
	}
}