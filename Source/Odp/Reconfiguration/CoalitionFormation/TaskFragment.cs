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

namespace SafetySharp.Odp.Reconfiguration.CoalitionFormation
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using JetBrains.Annotations;

	/// <summary>
	///  Represents a continuous fragment of a task.
	/// </summary>
	/// <remarks>
	///  This type is immutable.
	///  This type is a commutative monoid.
	/// </remarks>
	public struct TaskFragment
	{
		#region data

		/// <summary>
		///  The <see cref="ITask"/> this fragment belongs to.
		/// </summary>
		[NotNull]
		public ITask Task { get; }

		/// <summary>
		///  The index of the first capability that is part of the fragment.
		/// </summary>
		public int Start { get; }

		/// <summary>
		///  The index of the last capability that is part of the fragment.
		/// </summary>
		public int End { get; }

		#endregion

		#region accessors

		/// <summary>
		///  The sequence of capabilities that are part of this fragment.
		/// </summary>
		[NotNull]
		public IEnumerable<ICapability> Capabilities => Task.RequiredCapabilities.Slice(Start, End);

		/// <summary>
		///  The capability indices that are part of this fragment.
		/// </summary>
		[NotNull]
		public IEnumerable<int> CapabilityIndices => Enumerable.Range(Start, Length);

		/// <summary>
		///   Accesses the capability at the given <paramref name="index"/> in the fragment.
		/// </summary>
		[NotNull]
		public ICapability this[int index]
		{
			get
			{
				if (index >= Length)
					throw new ArgumentOutOfRangeException(nameof(index), index, $"Task fragment has length {Length}.");
				return Task.RequiredCapabilities[Start + index];
			}
		}

		/// <summary>
		///  The number of capabilities that are part of this fragment.
		/// </summary>
		public int Length => Math.Max(0, End - Start + 1);

		/// <summary>
		///   Tests if this fragment intersects with the given <paramref name="other"/> fragment.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if the fragments do not belong to the same task.</exception>
		[Pure]
		public bool IntersectsWith(TaskFragment other)
		{
			if (Task != other.Task)
				throw new InvalidOperationException("Cannot compare fragments of different tasks.");

			return Start <= other.End && other.Start <= End;
		}

		/// <summary>
		///   Tests if this fragment intersects with the fragment applied by the given <paramref name="role"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if the fragment and the role do not belong to the same task.</exception>
		[Pure]
		public bool IntersectsWith(Role role)
		{
			return IntersectsWith(FromRole(role));
		}

		/// <summary>
		///   Checks if this task fragment immediately precedes the fragment specified by the given <paramref name="role"/>.
		/// </summary>
		[Pure]
		public bool Precedes(Role role)
		{
			return Equals(Task, role.Task) && End + 1 == role.PreCondition.StateLength;
		}

		/// <summary>
		///   Checks if this task fragment immediately follows the fragment specified by the given <paramref name="role"/>.
		/// </summary>
		[Pure]
		public bool Succeedes(Role role)
		{
			return Equals(Task, role.Task) && role.PostCondition.StateLength == Start;
		}

		#endregion

		/// <summary>
		///  Creates a new fragment.
		///  Always use this instead of the default constructor.
		/// </summary>
		/// <param name="task">The fragment's <see cref="Task"/>.</param>
		/// <param name="start">The fragment's <see cref="Start"/>.</param>
		/// <param name="end">The fragment's <see cref="End"/>.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="start"/> or <paramref name="end"/> are invalid.</exception>
		public TaskFragment([NotNull] ITask task, int start, int end)
		{
			if (task == null)
				throw new ArgumentNullException(nameof(task));
			if (start < 0 || start >= task.RequiredCapabilities.Length)
				throw new ArgumentOutOfRangeException(nameof(start));
			if (end < 0 || end >= task.RequiredCapabilities.Length)
				throw new ArgumentOutOfRangeException(nameof(end));

			Task = task;
			Start = start;
			End = end;
		}

		/// <summary>
		///   Returns the task fragment applied by the given role.
		/// </summary>
		[Pure]
		public static TaskFragment FromRole(Role role)
		{
			Debug.Assert(role.IsValid);
			return new TaskFragment(role.Task, role.PreCondition.StateLength, role.PostCondition.StateLength - 1);
		}

		#region monoid operations

		/// <summary>
		///  Merges two fragments of the same <see cref="Task"/>.
		/// </summary>
		/// <remarks>This operation is associative and commutative.</remarks>
		/// <exception cref="InvalidOperationException">Thrown if the fragments do not belong to the same task.</exception>
		[MustUseReturnValue, Pure]
		public TaskFragment Merge(TaskFragment other)
		{
			if (Task != other.Task)
				throw new InvalidOperationException("Cannot merge fragments of different tasks.");

			return new TaskFragment(Task, Math.Min(Start, other.Start), Math.Max(End, other.End));
		}

		/// <summary>
		///  Returns a task fragment that is the identity for the merge operation (among all fragments of the given <paramref name="task"/>).
		/// </summary>
		[Pure]
		public static TaskFragment Identity(ITask task)
		{
			return new TaskFragment(task, task.RequiredCapabilities.Length - 1, 0);
		}

		/// <summary>
		///  See <see cref="Merge(TaskFragment)"/>.
		/// </summary>
		[Pure]
		public static TaskFragment Merge(TaskFragment a, TaskFragment b)
		{
			return a.Merge(b);
		}

		/// <summary>
		///  Extension of <see cref="Merge(TaskFragment)"/>
		///  to arbitrary sequences of operands (including the empty sequence).
		/// </summary>
		[Pure]
		public static TaskFragment Merge(ITask task, IEnumerable<TaskFragment> fragments)
		{
			return fragments.Aggregate(Identity(task), Merge);
		}

		#endregion
	}
}