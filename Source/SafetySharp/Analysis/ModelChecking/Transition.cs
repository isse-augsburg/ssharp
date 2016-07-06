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

namespace SafetySharp.Analysis.ModelChecking
{
	using System.Runtime.InteropServices;
	using ModelTraversal.TraversalModifiers;

	/// <summary>
	///   Represents a transition.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	internal unsafe struct Transition
	{
		/// <summary>
		///   A pointer to the transition's target state that can only be accessed during <see cref="ITransitionModifier" /> execution.
		/// </summary>
		[FieldOffset(0)]
		public byte* TargetState;

		/// <summary>
		///   The unique index of the target state that can only be accessed after <see cref="ITransitionModifier" /> execution.
		/// </summary>
		[FieldOffset(0)]
		public int TargetStateIndex;

		/// <summary>
		///   The state formulas holding in the target state.
		/// </summary>
		[FieldOffset(8)]
		public StateFormulaSet Formulas;

		/// <summary>
		///   The faults that are activated by the transition.
		/// </summary>
		[FieldOffset(12)]
		public FaultSet ActivatedFaults;

		/// <summary>
		///   Indicates whether the transition is valid or should be ignored.
		/// </summary>
		[FieldOffset(20)]
		public bool IsValid;
	}
}