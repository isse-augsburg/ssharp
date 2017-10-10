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
	using JetBrains.Annotations;

	/// <summary>
	///  Describes the condition in which a <see cref="Resource"/> is received before / left after execution of a <see cref="Role"/>.
	///
	///  This type is immutable.
	/// </summary>
	public struct Condition : IEquatable<Condition>
	{
		#region data

		/// <summary>
		///  The <see cref="BaseAgent"/> from which the resource is received / to which it is given.
		///  This may be null if no such agent exists, i.e. the resource is produced / consumed by the <see cref="Role"/>.
		/// </summary>
		[CanBeNull]
		public BaseAgent Port { get; }

		/// <summary>
		///  The <see cref="ITask"/> according to which the resource is processed.
		/// </summary>
		[NotNull]
		public ITask Task { get; }

		private readonly byte _statePrefixLength;

		#endregion

		#region accessors

		/// <summary>
		///  The state described by this <see cref="Condition"/>, i.e., the sequence of capabilities already applied to a <see cref="Resource"/>
		///  in this condition.
		/// </summary>
		[NotNull]
		public IEnumerable<ICapability> State =>
			Task?.RequiredCapabilities.Take(_statePrefixLength) ?? Enumerable.Empty<ICapability>();

		/// <summary>
		///  The number of capabilities already applied to a <see cref="Resource"/> in this condition.
		/// </summary>
		public int StateLength => _statePrefixLength;

		/// <summary>
		///  Compares two conditions to see if their state matches, i.e., they have the same <see cref="ITask"/> and state.
		///  Ignores the conditions' ports.
		/// </summary>
		public bool StateMatches(Condition other)
		{
			return Task == other.Task
				   && _statePrefixLength == other._statePrefixLength;
		}

		#endregion

		#region (copy) constructors

		/// <summary>
		///  Creates a new <see cref="Condition"/> with the given data.
		///  Always use this instead of the default constructor.
		/// </summary>
		/// <param name="task">The condition's <see cref="Task"/>.</param>
		/// <param name="stateLength">The condition's <see cref="StateLength"/>.</param>
		/// <param name="port">The condition's <see cref="Port"/>. <c>null</c> if omitted.</param>
		public Condition([NotNull] ITask task, int stateLength, BaseAgent port = null)
		{
			if (task == null)
				throw new ArgumentNullException(nameof(task));

			Debug.Assert(stateLength >= 0 && stateLength <= byte.MaxValue);

			Port = port;
			Task = task;
			_statePrefixLength = (byte)stateLength;
		}

		/// <summary>
		///  Returns a copy of the condition with the given <paramref name="port"/>.
		/// </summary>
		[MustUseReturnValue]
		public Condition WithPort(BaseAgent port)
		{
			return new Condition(Task, _statePrefixLength, port);
		}

		/// <summary>
		///  Returns a copy of the condition, with the given <paramref name="capability"/> is appended to its state.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		///  The given <paramref name="capability"/> is not the next to be applied,
		///  according to the <see cref="Task"/>.
		/// </exception>
		[MustUseReturnValue]
		public Condition WithCapability(ICapability capability)
		{
			if (_statePrefixLength >= Task.RequiredCapabilities.Length)
				throw new InvalidOperationException("All required capabilities already applied.");
			if (!Task.RequiredCapabilities[_statePrefixLength].Equals(capability))
				throw new InvalidOperationException("Capabilities must be applied according to task.");

			return new Condition(Task, StateLength + 1, Port);
		}

		#endregion

		#region equality (generated code)

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

		#endregion
	}
}
