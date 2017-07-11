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

namespace ISSE.SafetyChecking.ExecutableModel
{
	using System.Collections.Generic;
	using Modeling;
	using System.Runtime.CompilerServices;
	using Utilities;

	/// <summary>
	///   Represents a stack that is used to resolve nondeterministic choices during state space enumeration.
	/// </summary>
	public abstract class ChoiceResolver : DisposableObject
	{
		/// <summary>
		///   Gets the index of the last choice that has been made.
		/// </summary>
		// ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
		internal abstract int LastChoiceIndex { get; }
		
		/// <summary>
		///   Is ForwardOptimization enabled.
		/// </summary>
		internal bool UseForwardOptimization { get; }

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="useForwardOptimization">Use Forward Optimization.</param>
		protected ChoiceResolver(bool useForwardOptimization)
		{
			UseForwardOptimization = useForwardOptimization;
		}

		/// <summary>
		///   Prepares the resolver for resolving the choices of the next state.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public abstract void PrepareNextState();

		/// <summary>
		///   Prepares the resolver for the next path. Returns <c>true</c> to indicate that all paths have been enumerated.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public abstract bool PrepareNextPath();

		/// <summary>
		///   Handles a nondeterministic choice that chooses between <paramref name="valueCount" /> values.
		/// </summary>
		/// <param name="valueCount">The number of values that can be chosen.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public abstract int HandleChoice(int valueCount);
		
		/// <summary>
		///   Handles a probabilistic choice that chooses between <paramref name="valueCount" /> options.
		/// </summary>
		/// <param name="valueCount">The number of values that can be chosen.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public abstract int HandleProbabilisticChoice(int valueCount);

				/// <summary>
		///   Sets the probability of the taken option of the last taken probabilistic choice.
		/// </summary>
		/// <param name="probability">The probability of the last probabilistic choice.</param>
		public abstract void SetProbabilityOfLastChoice(Probability probability);

		/// <summary>
		///   Makes taken choice identified by the <paramref name="choiceIndexToForward" /> deterministic
		///   when forward optimization is enabled.
		/// </summary>
		/// <param name="choiceIndexToForward">The index of the choice that should be undone.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal abstract void ForwardUntakenChoicesAtIndex(int choiceIndexToForward);

		/// <summary>
		///   Sets the choices that should be made during the next step.
		/// </summary>
		/// <param name="choices">The choices that should be made.</param>
		internal abstract void SetChoices(int[] choices);

		/// <summary>
		///   Clears all choice information.
		/// </summary>
		internal abstract void Clear();

		/// <summary>
		///   Gets the choices that were made to generate the last transitions.
		/// </summary>
		internal abstract IEnumerable<int> GetChoices();
	}
}