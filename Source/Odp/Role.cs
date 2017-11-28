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
	///  Describes a sequence of capabilities a <see cref="BaseAgent"/> applies to a <see cref="Resource"/>,
	///  as well as source and destination agent of that <see cref="Resource"/>.
	///
	///  This type is immutable.
	/// </summary>
	public struct Role : IEquatable<Role>
	{
		#region data

		/// <summary>
		///  The condition in which a <see cref="BaseAgent"/> executing this <see cref="Role"/> expects to receive a <see cref="Resource"/>,
		///  including the source agent.
		/// </summary>
		public Condition PreCondition { get; }

		/// <summary>
		///  The condition in which a <see cref="Resource"/> is left after execution of this <see cref="Role"/>,
		///  including the agent that will receive it next.
		/// </summary>
		public Condition PostCondition { get; }

		/// <summary>
		///  Indicates whether or not this <see cref="Role"/> is locked and may thus not be executed.
		/// </summary>
		public bool IsLocked { get; }

		#endregion

		#region accessors

		/// <summary>
		///  The <see cref="ITask"/> according to which this <see cref="Role"/> processes resources.
		/// </summary>
		[NotNull]
		public ITask Task => PreCondition.Task;

		/// <summary>
		///  The capabilities applied during execution of this <see cref="Role"/>.
		/// </summary>
		[NotNull, ItemNotNull]
		public IEnumerable<ICapability> CapabilitiesToApply => Task.RequiredCapabilities
				.Skip(PreCondition.StateLength)
				.Take(PostCondition.StateLength - PreCondition.StateLength);

		/// <summary>
		///  Indicates whether this <see cref="Role"/> applies any capabilities at all.
		/// </summary>
		public bool IsEmpty => PreCondition.StateLength == PostCondition.StateLength;

		/// <summary>
		///  The <see cref="BaseAgent"/> from which an agent executing this <see cref="Role"/> receives resources.
		/// </summary>
		[CanBeNull]
		public BaseAgent Input => PreCondition.Port;

		/// <summary>
		///  The <see cref="BaseAgent"/> to which an agent should send a <see cref="Resource"/> after this <see cref="Role"/> was applied to it.
		/// </summary>
		[CanBeNull]
		public BaseAgent Output => PostCondition.Port;

		/// <summary>
		///  Indicates if the <see cref="Role"/> is valid or not.
		/// </summary>
		public bool IsValid => PreCondition.IsValid && PostCondition.IsValid
							   && PostCondition.StateLength >= PreCondition.StateLength
							   && PostCondition.StateLength > 0
							   && (PreCondition.Port == null) == (PreCondition.StateLength == 0)
							   && (PostCondition.Port == null) == (PostCondition.StateLength == Task.RequiredCapabilities.Length);

		/// <summary>
		///   Indicates if the capability with the given <paramref name="index"/> is applied by this role.
		/// </summary>
		public bool IncludesCapability(int index) => PreCondition.StateLength <= index && index < PostCondition.StateLength;

		#endregion

		#region (copy) constructors

		/// <summary>
		///  Creates a new <see cref="Role"/> with the given data.
		///  Always use this instead of the default constructor.
		/// </summary>
		/// <param name="pre">The role's <see cref="PreCondition"/>.</param>
		/// <param name="post">The role's <see cref="PostCondition"/>.</param>
		/// <param name="locked">Whether or not the role is locked (see <see cref="IsLocked"/>). Defaults to <c>false</c>.</param>
		public Role(Condition pre, Condition post, bool locked = false)
		{
			Debug.Assert(pre.Task == post.Task);
			Debug.Assert(pre.StateLength <= post.StateLength);

			PreCondition = pre;
			PostCondition = post;
			IsLocked = locked;
		}

		/// <summary>
		///  Returns a copy of the resource that is locked iff <paramref name="locked"/> is <c>true</c>.
		/// </summary>
		[MustUseReturnValue, Pure]
		public Role Lock(bool locked = true)
		{
			return new Role(PreCondition, PostCondition, locked);
		}

		/// <summary>
		///  Returns a copy of the <see cref="Role"/> with the given <see cref="port"/> as <see cref="Input"/>.
		///  Updates the <see cref="PreCondition"/> accordingly.
		/// </summary>
		[MustUseReturnValue, Pure]
		public Role WithInput([CanBeNull] BaseAgent port)
		{
			return new Role(PreCondition.WithPort(port), PostCondition, IsLocked);
		}

		/// <summary>
		///  Returns a copy of the <see cref="Role"/> with the given <see cref="port"/> as <see cref="Output"/>.
		///  Updates the <see cref="PostCondition"/> accordingly.
		/// </summary>
		[MustUseReturnValue, Pure]
		public Role WithOutput([CanBeNull] BaseAgent port)
		{
			return new Role(PreCondition, PostCondition.WithPort(port), IsLocked);
		}

		/// <summary>
		///  Returns a copy of the <see cref="Role"/> that additionally applies the given <paramref name="capability"/>.
		///  Updates the <see cref="PostCondition"/> accordingly.
		/// </summary>
		[MustUseReturnValue, Pure]
		public Role WithCapability([NotNull] ICapability capability)
		{
			return new Role(PreCondition, PostCondition.WithCapability(capability), IsLocked);
		}

		/// <summary>
		///   Returns a copy of the <see cref="Role"/> that additionally applies the capabilities applied by the given <see cref="Role"/>.
		///   Does not affect condition ports.
		/// </summary>
		/// <param name="other">The role whose capabilities shall be appended.</param>
		/// <exception cref="InvalidOperationException">Thrown if this instance's <see cref="PostCondition"/> and <paramref name="other"/>'s <see cref="PreCondition"/> do not have matching states.</exception>
		public Role AppendCapabilities(Role other)
		{
			if (!PostCondition.StateMatches(other.PreCondition))
				throw new InvalidOperationException("Cannot append two roles whose pre-/postconditions do not match.");

			return other.CapabilitiesToApply.Aggregate(this, (role, capability) => role.WithCapability(capability));
		}

		#endregion

		#region equality (generated code)

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

		#endregion
	}
}
