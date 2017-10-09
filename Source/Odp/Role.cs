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
	using System.Diagnostics;
	using System.Linq;

	public struct Role : IEquatable<Role>
	{
		// actual data fields
		public Condition PreCondition { get; }

		public Condition PostCondition { get; }

		public bool IsLocked { get; }

		// accessors
		public ITask Task => PreCondition.Task ?? PostCondition.Task;

		public IEnumerable<ICapability> CapabilitiesToApply => Task.RequiredCapabilities
				.Skip(PreCondition.StateLength)
				.Take(PostCondition.StateLength - PreCondition.StateLength);

		public bool IsEmpty => PreCondition.StateLength == PostCondition.StateLength;

		// (copy) constructors
		public Role(Condition pre, Condition post, bool locked = false)
		{
			Debug.Assert(pre.Task == post.Task);

			PreCondition = pre;
			PostCondition = post;
			IsLocked = locked;
		}

		public Role Lock(bool locked = true)
		{
			return new Role(PreCondition, PostCondition, locked);
		}

		public Role WithInput(BaseAgent port)
		{
			return new Role(PreCondition.WithPort(port), PostCondition, IsLocked);
		}

		public Role WithOutput(BaseAgent port)
		{
			return new Role(PreCondition, PostCondition.WithPort(port), IsLocked);
		}

		public Role WithCapability(ICapability capability)
		{
			return new Role(PreCondition, PostCondition.WithCapability(capability), IsLocked);
		}

		// equality (generated code)
		public bool Equals(Role other)
		{
			return PreCondition.Equals(other.PreCondition) && PostCondition.Equals(other.PostCondition) && IsLocked == other.IsLocked;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			return obj is Role && Equals((Role)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = PreCondition.GetHashCode();
				hashCode = (hashCode * 397) ^ PostCondition.GetHashCode();
				hashCode = (hashCode * 397) ^ IsLocked.GetHashCode();
				return hashCode;
			}
		}

		public static bool operator ==(Role left, Role right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Role left, Role right)
		{
			return !left.Equals(right);
		}
	}
}
