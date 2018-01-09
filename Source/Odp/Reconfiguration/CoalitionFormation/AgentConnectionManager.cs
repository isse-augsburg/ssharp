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

namespace SafetySharp.Odp.Reconfiguration.CoalitionFormation
{
	using System;
	using System.Collections.Generic;
	using JetBrains.Annotations;

	/// <summary>
	///   Manages information about resource flow connections between agents.
	/// </summary>
	/// <inheritdoc cref="IConnectionOracle"/>
	public class AgentConnectionManager : IConnectionOracle
	{
		private readonly Dictionary<AgentTuple, Tuple<bool, BaseAgent[]>> _connections = new Dictionary<AgentTuple, Tuple<bool, BaseAgent[]>>();

		/// <inheritdoc cref="IConnectionOracle.CanConnect"/>
		[Pure]
		public bool? CanConnect([NotNull] BaseAgent source, [NotNull] BaseAgent destination)
		{
			var tuple = new AgentTuple(source, destination);
			if (!_connections.ContainsKey(tuple))
				return null;

			var connection = _connections[tuple];
			return connection.Item1;
		}

		/// <inheritdoc cref="IConnectionOracle.ConnectionImpossible"/>
		[Pure]
		public bool ConnectionImpossible([NotNull, ItemNotNull] BaseAgent[] agents)
		{
			if (agents == null)
				throw new ArgumentNullException(nameof(agents));

			for (var i = 1; i < agents.Length; ++i)
			{
				if (CanConnect(agents[i - 1], agents[i]) == false)
					return true;
			}
			return false;
		}

		/// <summary>
		///   Records the existence of a connection between agents.
		/// </summary>
		/// <param name="source">The source agent.</param>
		/// <param name="destination">The destination agent.</param>
		/// <param name="connection">The connection between agents.</param>
		public void RecordConnection([NotNull] BaseAgent source, [NotNull] BaseAgent destination, [NotNull, ItemNotNull] BaseAgent[] connection)
		{
			var tuple = new AgentTuple(source, destination);
			if (_connections.ContainsKey(tuple))
				throw new InvalidOperationException($"Connection between {source.Id} and {destination.Id} already recorded.");

			_connections[tuple] = Tuple.Create(true, connection);
		}

		/// <summary>
		///   Records the non-existence of a connection between agents.
		/// </summary>
		/// <param name="source">The source agent.</param>
		/// <param name="destination">The destination agent.</param>
		public void RecordConnectionFailure([NotNull] BaseAgent source, [NotNull] BaseAgent destination)
		{
			var tuple = new AgentTuple(source, destination);
			if (_connections.ContainsKey(tuple))
				throw new InvalidOperationException($"Connection between {source.Id} and {destination.Id} already recorded.");

			_connections[tuple] = Tuple.Create(false, (BaseAgent[])null);
		}

		/// <summary>
		///   Retrieves a recorded connection, if there is one.
		/// </summary>
		/// <param name="source">The source agent.</param>
		/// <param name="destination">The destination agent.</param>
		/// <returns>A connection between the given agents, if one was previously recorded by <see cref="RecordConnection"/>. <c>null</c> otherwise.</returns>
		[CanBeNull, Pure]
		public BaseAgent[] GetConnection([NotNull] BaseAgent source, [NotNull] BaseAgent destination)
		{
			var tuple = new AgentTuple(source, destination);
			if (_connections.ContainsKey(tuple))
				return _connections[tuple].Item2;

			return null;
		}

		private struct AgentTuple : IEquatable<AgentTuple>
		{
			[NotNull]
			public BaseAgent Source { get; }

			[NotNull]
			public BaseAgent Destination { get; }

			public AgentTuple([NotNull] BaseAgent source, [NotNull] BaseAgent destination)
			{
				if (source == null)
					throw new ArgumentNullException(nameof(source));
				if (destination == null)
					throw new ArgumentNullException(nameof(destination));

				Source = source;
				Destination = destination;
			}

			public bool Equals(AgentTuple other)
			{
				return Source.Equals(other.Source) && Destination.Equals(other.Destination);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj))
					return false;
				return obj is AgentTuple && Equals((AgentTuple)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Source.GetHashCode() * 397) ^ Destination.GetHashCode();
				}
			}

			public static bool operator ==(AgentTuple left, AgentTuple right)
			{
				return left.Equals(right);
			}

			public static bool operator !=(AgentTuple left, AgentTuple right)
			{
				return !left.Equals(right);
			}
		}
	}
}
