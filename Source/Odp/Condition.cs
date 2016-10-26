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

	public struct Condition
	{
		public Condition(BaseAgent port, ITask task, int statePrefixLength)
		{
			Port = port;
			Task = task;
			_statePrefixLength = checked((byte)statePrefixLength);
		}

		public BaseAgent Port { get; set; }
		public ITask Task { get; set; }

		private byte _statePrefixLength;

		public IEnumerable<ICapability> State =>
			Task?.RequiredCapabilities.Take(_statePrefixLength) ?? Enumerable.Empty<ICapability>();

		public bool StateMatches(Condition other)
		{
			return Task == other.Task
				   && _statePrefixLength == other._statePrefixLength;
		}

		public void AppendToState(ICapability capability)
		{
			if (_statePrefixLength >= Task.RequiredCapabilities.Length)
				throw new InvalidOperationException("Condition already has maximum state.");
			if (Task.RequiredCapabilities[_statePrefixLength] != capability)
				throw new InvalidOperationException("Invalid capability order in Condition state.");

			_statePrefixLength++;
		}

		public void ResetState()
		{
			_statePrefixLength = 0;
		}

		public void CopyStateFrom(Condition other)
		{
			if (other.Task != Task)
				throw new InvalidOperationException("Invalid task: cannot copy Condition state");
			_statePrefixLength = other._statePrefixLength;
		}
	}
}
