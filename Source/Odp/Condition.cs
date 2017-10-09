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

	public struct Condition : IEquatable<Condition>
	{
		// actual data fields
		public BaseAgent Port { get; }

		public ITask Task { get; }

		private readonly byte _statePrefixLength;

		// accessors
		public IEnumerable<ICapability> State =>
			Task?.RequiredCapabilities.Take(_statePrefixLength) ?? Enumerable.Empty<ICapability>();

		public int StateLength => _statePrefixLength;

		public bool StateMatches(Condition other)
		{
			return Task == other.Task
				   && _statePrefixLength == other._statePrefixLength;
		}

		// (copy) constructors
		public Condition(ITask task, int stateLength, BaseAgent port = null)
		{
			Port = port;
			Task = task;
			_statePrefixLength = (byte)stateLength;
		}

		public Condition WithPort(BaseAgent port)
		{
			return new Condition(Task, _statePrefixLength, port);
		}

		public Condition WithCapability(ICapability capability)
		{
			if (_statePrefixLength >= Task.RequiredCapabilities.Length)
				throw new InvalidOperationException("All required capabilities already applied.");
			if (!Task.RequiredCapabilities[_statePrefixLength].Equals(capability))
				throw new InvalidOperationException("Capabilities must be applied according to task.");

			return new Condition(Task, StateLength + 1, Port);
		}

		// equality (generated code)
		public bool Equals(Condition other)
		{
			return _statePrefixLength == other._statePrefixLength && Equals(Port, other.Port) && Equals(Task, other.Task);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			return obj is Condition && Equals((Condition)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = _statePrefixLength.GetHashCode();
				hashCode = (hashCode * 397) ^ (Port != null ? Port.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Task != null ? Task.GetHashCode() : 0);
				return hashCode;
			}
		}

		public static bool operator ==(Condition left, Condition right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Condition left, Condition right)
		{
			return !left.Equals(right);
		}
	}
}
