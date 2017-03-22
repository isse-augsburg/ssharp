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
	using System.Runtime.CompilerServices;

	public static class TransitionFlags
	{
		/// <summary>
		///   Indicates whether the transition is valid or should be ignored.
		/// </summary>
		public const uint IsValidFlag = 1;

		/// <summary>
		///   Indicates whether the transition should lead to the stuttering state.
		/// </summary>
		public const uint ToStutteringStateFlag = 2;

		/// <summary>
		///   First bit which is free for use
		/// </summary>
		public const uint FirstUnspecifiedBit = 4;

		/// <summary>
		///   Returns true, if IsUsedFlag is set
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsValid(uint flag)
		{
			return (flag & IsValidFlag) != 0;
		}

		/// <summary>
		///   Returns true, if IsUsedFlag is set
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint RemoveValid(uint flag)
		{
			return flag & (~IsValidFlag);
		}

		/// <summary>
		///   Sets bit of IsValidFlag to true if and only if condition is true
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint SetIsValidIffCondition(uint flag, bool condition)
		{
			if (condition)
				return flag | IsValidFlag;
			return flag & (~IsValidFlag);
		}

		/// <summary>
		///   Returns true, if ToStutteringStateFlag flag is set
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsToStutteringState(uint flag)
		{
			return (flag & ToStutteringStateFlag) != 0;
		}
	}
}
