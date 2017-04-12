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

namespace ISSE.SafetyChecking.MinimalCriticalSetAnalysis
{
	using Modeling;
	using ExecutableModel;
	using AnalysisModel;
	using Utilities;

	/// <summary>
	///   Indicates that the <see cref="FirstFault" /> must be activated before or at the same time as <see cref="SecondFault" />.
	/// </summary>
	public sealed class OrderRelationship<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		/// <summary>
		///   The fault that must be activated first for the hazard to occur.
		/// </summary>
		public readonly Fault FirstFault;

		/// <summary>
		///   Determines the kind of the order relationship.
		/// </summary>
		public readonly OrderRelationshipKind Kind;

		/// <summary>
		///   The fault that must be activated second for the hazard to occur.
		/// </summary>
		public readonly Fault SecondFault;

		/// <summary>
		///   A witness showing how the activation of <see cref="FirstFault" /> before or at the same time as <see cref="SecondFault" />
		///   causes the hazard.
		/// </summary>
		public readonly CounterExample<TExecutableModel> Witness;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="result">The result of the analysis</param>
		/// <param name="firstFault">The fault that must be activated first.</param>
		/// <param name="secondFault">The fault that must be activated subsequently.</param>
		/// <param name="kind">Determines the kind of the order relationship.</param>
		internal OrderRelationship(AnalysisResult<TExecutableModel> result, Fault firstFault, Fault secondFault, OrderRelationshipKind kind)
		{
			Requires.NotNull(result, nameof(result));
			Requires.NotNull(firstFault, nameof(firstFault));
			Requires.NotNull(secondFault, nameof(secondFault));
			Requires.InRange(kind, nameof(kind));

			Witness = result.CounterExample;
			FirstFault = firstFault;
			SecondFault = secondFault;
			Kind = kind;
		}

		/// <summary>
		///   Gets a string representation for the <see cref="Kind" />.
		/// </summary>
		private string KindOperator
		{
			get
			{
				switch (Kind)
				{
					case OrderRelationshipKind.Simultaneously:
						return "=";
					case OrderRelationshipKind.Precedes:
						return "<=";
					case OrderRelationshipKind.StrictlyPrecedes:
						return "<";
					default:
						return Assert.NotReached<string>();
				}
			}
		}

		/// <summary>
		///   Returns a string that represents the current object.
		/// </summary>
		public override string ToString()
		{
			return $"{FirstFault.Name} {KindOperator} {SecondFault.Name}";
		}
	}
}