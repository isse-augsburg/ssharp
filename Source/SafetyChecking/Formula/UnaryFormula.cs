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

namespace SafetySharp.Analysis
{
	using FormulaVisitors;
	using Utilities;

	/// <summary>
	///   Represents the application of a <see cref="UnaryOperator" /> to a <see cref="Formula" /> instance.
	/// </summary>
	public sealed class UnaryFormula : Formula
	{
		/// <summary>
		///   Initializes a new instance of the <see cref="UnaryFormula" /> class.
		/// </summary>
		/// <param name="operand">The operand of the unary formula.</param>
		/// <param name="unaryOperator">The operator of the unary formula.</param>
		public UnaryFormula(Formula operand, UnaryOperator unaryOperator)
		{
			Requires.NotNull(operand, nameof(operand));
			Requires.InRange(unaryOperator, nameof(unaryOperator));

			Operand = operand;
			Operator = unaryOperator;
		}

		/// <summary>
		///   Gets the operand of the unary formula.
		/// </summary>
		public Formula Operand { get; }

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
			visitor.VisitUnaryFormula(this);
		}
	}
}