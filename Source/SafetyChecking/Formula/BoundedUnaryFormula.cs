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
	///   Represents the application of a <see cref="UnaryOperator" /> to a <see cref="Formula" /> instance with a step bound.
	/// </summary>
	public sealed class BoundedUnaryFormula : Formula
	{
		/// <summary>
		///   Initializes a new instance of the <see cref="UnaryFormula" /> class.
		/// </summary>
		/// <param name="operand">The operand of the unary formula.</param>
		/// <param name="unaryOperator">The operator of the unary formula.</param>
		/// <param name="bound">The maximal number of steps.</param>
		/// <param name="label">
		///   The name that should be used for the state label of the formula. If <c>null</c>, a unique name is generated.
		/// </param>
		public BoundedUnaryFormula(Formula operand, UnaryOperator unaryOperator, int bound, string label = null) : base(label)
		{
			Requires.NotNull(operand, nameof(operand));
			Requires.InRange(unaryOperator, nameof(unaryOperator));

			Operand = operand;
			Operator = unaryOperator;
			Bound = bound;
		}

		/// <summary>
		///   Gets the operand of the unary formula.
		/// </summary>
		public Formula Operand { get; }

		/// <summary>
		///   Gets the maximal number of steps.
		/// </summary>
		public int Bound { get; }

		/// <summary>
		///   Gets the operator of the unary formula.
		/// </summary>
		public UnaryOperator Operator { get; }

		/// <summary>
		///   Executes the <paramref name="visitor" /> for this formula.
		/// </summary>
		/// <param name="visitor">The visitor that should be executed.</param>
		internal override void Visit(FormulaVisitor visitor)
		{
			visitor.VisitBoundedUnaryFormula(this);
		}
	}
}