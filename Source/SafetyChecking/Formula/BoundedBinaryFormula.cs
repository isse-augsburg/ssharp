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

namespace ISSE.SafetyChecking.Formula
{
	using Utilities;

	/// <summary>
	///   Represents the application of a <see cref="BinaryOperator" /> to two <see cref="Formula" /> instances.
	/// </summary>
	public sealed class BoundedBinaryFormula : Formula
	{
		/// <summary>
		///   Initializes a new instance of the <see cref="BoundedBinaryFormula" /> class.
		/// </summary>
		/// <param name="leftOperand">The formula on the left-hand side of the binary operator.</param>
		/// <param name="binaryOperator">The operator of the binary formula.</param>
		/// <param name="rightOperand">The formula on the right-hand side of the binary operator.</param>
		/// <param name="bound">The maximal number of steps.</param>
		public BoundedBinaryFormula(Formula leftOperand, BinaryOperator binaryOperator, Formula rightOperand, int bound)
		{
			Requires.NotNull(leftOperand, nameof(leftOperand));
			Requires.InRange(binaryOperator, nameof(binaryOperator));
			Requires.NotNull(rightOperand, nameof(rightOperand));

			LeftOperand = leftOperand;
			Operator = binaryOperator;
			RightOperand = rightOperand;
			Bound = bound;
		}

		/// <summary>
		///   Gets the formula on the left-hand side of the binary operator.
		/// </summary>
		public Formula LeftOperand { get; }

		/// <summary>
		///   Gets the operator of the binary formula.
		/// </summary>
		public BinaryOperator Operator { get; }

		/// <summary>
		///   Gets the formula on the right-hand side of the binary operator.
		/// </summary>
		public Formula RightOperand { get; }

		/// <summary>
		///   Gets the maximal number of steps.
		/// </summary>
		public int Bound { get; }

		/// <summary>
		///   Executes the <paramref name="visitor" /> for this formula.
		/// </summary>
		/// <param name="visitor">The visitor that should be executed.</param>
		internal override void Visit(FormulaVisitor visitor)
		{
			visitor.VisitBoundedBinaryFormula(this);
		}
	}
}