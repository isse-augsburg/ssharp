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

namespace SafetySharp.Odp.Reconfiguration
{
	using System;
	using System.Linq;
	using JetBrains.Annotations;

	/// <summary>
	///   Represents the cause of a reconfiguration initiated by a <see cref="BaseAgent"/>.
	/// </summary>
	public abstract class ReconfigurationReason : IEquatable<ReconfigurationReason>
	{
		public abstract bool Equals(ReconfigurationReason other);
	}

	/// <summary>
	///   A <see cref="ReconfigurationReason"/> for initial configurations.
	/// </summary>
	public sealed class InitialReconfiguration : ReconfigurationReason, IEquatable<InitialReconfiguration>
	{
		public static InitialReconfiguration Instance = new InitialReconfiguration();

		private InitialReconfiguration() { }

		#region equality

		public override bool Equals(ReconfigurationReason other) => Equals(other as InitialReconfiguration);

		public bool Equals(InitialReconfiguration other) => ReferenceEquals(this, other);

		#endregion
	}

	/// <summary>
	///   A <see cref="ReconfigurationReason"/> representing a request by another <see cref="IAgent"/>
	///   to participate in a reconfiguration.
	/// </summary>
	public sealed class ParticipationRequested : ReconfigurationReason, IEquatable<ParticipationRequested>
	{
		/// <summary>
		///   The agent that requested the participation.
		/// </summary>
		[NotNull]
		public IAgent RequestingAgent { get; }

		public ParticipationRequested([NotNull] IAgent requestingAgent)
		{
			if (requestingAgent == null)
				throw new ArgumentNullException(nameof(requestingAgent));

			RequestingAgent = requestingAgent;
		}

		#region equality

		public override bool Equals(object obj) => Equals(obj as ParticipationRequested);

		public override bool Equals(ReconfigurationReason other) => Equals(other as ParticipationRequested);

		public bool Equals(ParticipationRequested other)
		{
			if (ReferenceEquals(other, null))
				return false;
			return RequestingAgent.Equals(other.RequestingAgent);
		}

		public override int GetHashCode() => RequestingAgent.GetHashCode();

		#endregion
	}

	/// <summary>
	///   A <see cref="ReconfigurationReason"/> used if the <see cref="BaseAgent"/>
	///   detected invariant violations.
	/// </summary>
	public class InvariantsViolated : ReconfigurationReason, IEquatable<InvariantsViolated>
	{
		/// <summary>
		///   The violated predicates.
		/// </summary>
		[NotNull, ItemNotNull]
		public InvariantPredicate[] ViolatedPredicates { get; }

		public InvariantsViolated([NotNull, ItemNotNull] InvariantPredicate[] violatedPredicates)
		{
			if (violatedPredicates == null)
				throw new ArgumentNullException(nameof(violatedPredicates));

			ViolatedPredicates = violatedPredicates;
		}

		#region equality

		public override bool Equals(object obj) => Equals(obj as InvariantsViolated);

		public override bool Equals(ReconfigurationReason other) => Equals(other as InvariantsViolated);

		public bool Equals(InvariantsViolated other)
		{
			if (ReferenceEquals(other, null))
				return false;
			return ViolatedPredicates.SequenceEqual(other.ViolatedPredicates);
		}

		public override int GetHashCode() => ViolatedPredicates.Sum(x => x.GetHashCode());

		#endregion
	}
}
