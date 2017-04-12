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

namespace ISSE.SafetyChecking.AnalysisModel
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;
	using Utilities;

	/// <summary>
	///   An efficient representation of a state formula set, indicating whether the state formulas hold in a state.
	/// </summary>
	public struct StateFormulaSet : IEquatable<StateFormulaSet>
	{
		private readonly int _formulas;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="formulas">The state formulas the set should contain.</param>
		internal StateFormulaSet(Func<bool>[] formulas)
		{
			_formulas = 0;

			for (var i = 0; i < formulas.Length; ++i)
				_formulas |= formulas[i]() ? 1 << i : 0;
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="formulas">The state formulas the set should contain.</param>
		internal StateFormulaSet(IReadOnlyList<bool> formulas)
		{
			CheckFormulaCount(formulas.Count);
			_formulas = 0;

			for (var i = 0; i < formulas.Count; ++i)
				_formulas |= formulas[i] ? 1 << i : 0;
		}

		/// <summary>
		///   Gets a value indicating whether the state formula at the zero-based <paramref name="index" /> holds.
		/// </summary>
		/// <param name="index">The zero-based index of the formula that should be checked.</param>
		internal bool this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { return (_formulas & (1 << index)) != 0; }
		}

		/// <summary>
		///   Checks whether the number of <paramref name="formulas" /> is supported.
		/// </summary>
		/// <param name="formulas">The formula count that should be checked.</param>
		internal static void CheckFormulaCount(int formulas)
		{
			Requires.That(formulas < 32, "More than 31 state formulas are not supported.");
		}

		/// <summary>
		///   Indicates whether the current state formula set is equal to <paramref name="other" />.
		/// </summary>
		/// <param name="other">The other state formula set to compare to.</param>
		public bool Equals(StateFormulaSet other)
		{
			return _formulas == other._formulas;
		}

		/// <summary>
		///   Indicates whether the current state formula set is equal to <paramref name="other" />.
		/// </summary>
		/// <param name="other">The other state formula set to compare to.</param>
		public override bool Equals(object other)
		{
			if (ReferenceEquals(null, other))
				return false;

			return other is StateFormulaSet && Equals((StateFormulaSet)other);
		}

		/// <summary>
		///   Returns the hash code for this state formula set.
		/// </summary>
		public override int GetHashCode()
		{
			return _formulas;
		}

		/// <summary>
		///   Indicates whether the two state formula sets <paramref name="left" /> and <paramref name="right" /> are equal.
		/// </summary>
		/// <param name="left">The state formula set on the left-hand side to compare.</param>
		/// <param name="right">The state formula set on the right-hand side to compare.</param>
		public static bool operator ==(StateFormulaSet left, StateFormulaSet right)
		{
			return left.Equals(right);
		}

		/// <summary>
		///   Indicates whether the two state formula sets <paramref name="left" /> and <paramref name="right" /> are not equal.
		/// </summary>
		/// <param name="left">The state formula set on the left-hand side to compare.</param>
		/// <param name="right">The state formula set on the right-hand side to compare.</param>
		public static bool operator !=(StateFormulaSet left, StateFormulaSet right)
		{
			return !left.Equals(right);
		}
	}
}