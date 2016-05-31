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

namespace SafetySharp.Runtime
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using Modeling;
	using Utilities;

	/// <summary>
	///   An efficient immutable representation of a fault set.
	/// </summary>
	[DebuggerDisplay("{_faults}")]
	internal struct FaultSet : IEquatable<FaultSet>
	{
		private readonly long _faults;

		public uint Cardinality { get; }

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="faults">The faults the set should contain.</param>
		private FaultSet(long faults)
		{
			_faults = faults;

			// count bits set to 1:
			Cardinality = 0;
			for (long n = _faults; n != 0; n &= n - 1) // n &= n-1 removes the last 1
				Cardinality++;
		}

		/// <summary>
		///   Gets a value indicating whether the fault set is empty.
		/// </summary>
		internal bool IsEmpty => _faults == 0;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="faults">The faults the set should contain.</param>
		internal FaultSet(params Fault[] faults)
		{
			_faults = 0;

			foreach (var fault in faults)
				_faults |= 1L << fault.Identifier;

			Cardinality = (uint)faults.Length;
		}

		/// <summary>
		///   Creates a fault set that contains all activated <paramref name="faults" />.
		/// </summary>
		/// <param name="faults">The faults the set should contain.</param>
		internal static FaultSet FromActivatedFaults(params Fault[] faults)
		{
			var mask = 0L;

			foreach (var fault in faults)
				mask |= fault.IsActivated ? 1L << fault.Identifier : 0;

			return new FaultSet(mask);
		}

		/// <summary>
		///   Returns a new <see cref="FaultSet" /> that represents the intersection between this instance and
		///   <paramref name="other." />
		/// </summary>
		/// <param name="other">The other fault set that this instance should be intersected with.</param>
		internal FaultSet GetIntersection(FaultSet other)
		{
			return new FaultSet(_faults & other._faults);
		}

		/// <summary>
		///   Returns a new <see cref="FaultSet" /> that represents the union between this instance and
		///   <paramref name="other." />
		/// </summary>
		/// <param name="other">The other fault set that this instance should be unioned with.</param>
		public FaultSet GetUnion(FaultSet other)
		{
			return new FaultSet(_faults | other._faults);
		}

		/// <summary>
		///   Checks whether the <paramref name="fault" /> is contained in the set.
		/// </summary>
		/// <param name="fault">The fault that should be checked.</param>
		internal bool Contains(Fault fault)
		{
			Requires.NotNull(fault, nameof(fault));
			return (_faults & (1 << fault.Identifier)) != 0;
		}

		/// <summary>
		///   Returns a copy of the fault set that contains <paramref name="fault" />.
		/// </summary>
		/// <param name="fault">The fault that should be added.</param>
		internal FaultSet Add(Fault fault)
		{
			Requires.NotNull(fault, nameof(fault));
			return new FaultSet(_faults | (1L << fault.Identifier));
		}

		/// <summary>
		///   Sets the <see cref="Fault.Activation" /> property of the <paramref name="faults" />, enabling or disabling the faults
		///   contained in the set.
		/// </summary>
		/// <param name="faults">The faults whose activation should be set.</param>
		internal void SetActivation(Fault[] faults)
		{
			Requires.NotNull(faults, nameof(faults));
			CheckFaultCount(faults.Length);

			foreach (var fault in faults)
				fault.Activation = (_faults & (1L << fault.Identifier)) != 0 ? Activation.Nondeterministic : Activation.Suppressed;
		}

		/// <summary>
		///   Returns a <see cref="Fault" />-based representation of the set.
		/// </summary>
		/// <param name="faults">The faults that can potentially be contained in the set.</param>
		internal IEnumerable<Fault> ToFaultSequence(Fault[] faults)
		{
			Requires.NotNull(faults, nameof(faults));

			for (var i = 1; i <= faults.Length; ++i)
			{
				if ((_faults & (1L << (i - 1))) != 0)
					yield return faults[i - 1];
			}
		}

		/// <summary>
		///   Returns a string representation of the <paramref name="faults" /> contained in the set.
		/// </summary>
		/// <param name="faults">The faults that can potentially be contained in the set.</param>
		internal string ToString(Fault[] faults)
		{
			Requires.NotNull(faults, nameof(faults));

			var faultNames = ToFaultSequence(faults).Select(fault => fault.Name).OrderBy(name => name);
			return String.Join(", ", faultNames);
		}

		/// <summary>
		///   Checks whether this fault set is a subset of <paramref name="faultSet" />.
		/// </summary>
		/// <param name="faultSet">The fault set that is expected to be a super set.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool IsSubsetOf(FaultSet faultSet)
		{
			return (_faults & faultSet._faults) == _faults;
		}

		/// <summary>
		///   Checks whether the number of faults in <paramref name="faultCount" /> is supported.
		/// </summary>
		/// <param name="faultCount">The fault count that should be checked.</param>
		internal static void CheckFaultCount(int faultCount)
		{
			Requires.That(faultCount < 64, "More than 63 faults are not supported.");
		}

		/// <summary>
		///   Indicates whether the current fault set is equal to <paramref name="other" />.
		/// </summary>
		/// <param name="other">The other fault set to compare to.</param>
		public bool Equals(FaultSet other)
		{
			return _faults == other._faults;
		}

		/// <summary>
		///   Indicates whether the current fault set is equal to <paramref name="other" />.
		/// </summary>
		/// <param name="other">The other fault set to compare to.</param>
		public override bool Equals(object other)
		{
			if (ReferenceEquals(null, other))
				return false;

			return other is FaultSet && Equals((FaultSet)other);
		}

		/// <summary>
		///   Returns the hash code for this fault set.
		/// </summary>
		public override int GetHashCode()
		{
			return (int)_faults;
		}

		/// <summary>
		///   Indicates whether the two fault sets <paramref name="left" /> and <paramref name="right" /> are equal.
		/// </summary>
		/// <param name="left">The fault set on the left-hand side to compare.</param>
		/// <param name="right">The fault set on the right-hand side to compare.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(FaultSet left, FaultSet right)
		{
			return left.Equals(right);
		}

		/// <summary>
		///   Indicates whether the two fault sets <paramref name="left" /> and <paramref name="right" /> are not equal.
		/// </summary>
		/// <param name="left">The fault set on the left-hand side to compare.</param>
		/// <param name="right">The fault set on the right-hand side to compare.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FaultSet left, FaultSet right)
		{
			return !left.Equals(right);
		}
	}
}