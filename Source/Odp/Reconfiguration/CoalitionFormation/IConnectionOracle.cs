﻿// The MIT License (MIT)
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
	using JetBrains.Annotations;

	/// <summary>
	///   Provides information about possible resource flow connections between agents.
	/// </summary>
	public interface IConnectionOracle
	{
		/// <summary>
		///   Checks if a connection between the two given agents exists.
		/// </summary>
		/// <returns><c>true</c> if a connection is known, <c>false</c> if it is known that there is no connection, <c>null</c> otherwise.</returns>
		[Pure]
		bool? CanConnect([NotNull] BaseAgent source, [NotNull] BaseAgent destination);

		/// <summary>
		///   Checks if connecting the given sequence of agents is known to be impossible.
		/// </summary>
		[Pure]
		bool ConnectionImpossible([NotNull, ItemNotNull] BaseAgent[] agents);
	}

	/// <summary>
	///   A dummy implementation of <see cref="IConnectionOracle"/> that holds no knowledge.
	/// </summary>
	/// <inheritdoc cref="IConnectionOracle"/>
	public class DefaultConnectionOracle : IConnectionOracle
	{
		public static DefaultConnectionOracle Instance { get; } = new DefaultConnectionOracle();

		private DefaultConnectionOracle() { }

		public bool? CanConnect(BaseAgent source, BaseAgent destination)
		{
			return null;
		}

		public bool ConnectionImpossible([NotNull, ItemNotNull] BaseAgent[] agents)
		{
			return false;
		}
	}
}
