// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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

namespace SafetySharp.Modeling.Formulas
{
	using Utilities;

	/// <summary>
	///   Represents the application of a <see cref="UnaryOperator" /> to a <see cref="Formula" /> instance.
	/// </summary>
	internal sealed class UnaryFormula : Formula
	{
		/// <summary>
		///   Initializes a new instance of the <see cref="UnaryFormula" /> class.
		/// </summary>
		/// <param name="operand">The operand of the unary formula.</param>
		/// <param name="unaryOperator">The operator of the unary formula.</param>
		internal UnaryFormula(Formula operand, UnaryOperator unaryOperator)
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
		///   Gets a value indicating whether the formula is a valid linear temporal logic formula.
		/// </summary>
		public override bool IsLinearFormula => (Operator != UnaryOperator.All && Operator != UnaryOperator.Exists) && Operand.IsLinearFormula;

		/// <summary>
		///   Returns a string that represents the current object.
		/// </summary>
		public override string ToString()
		{
			var operatorSymbol = "";

			switch (Operator)
			{
				case UnaryOperator.Next:
					operatorSymbol = "X";
					break;
				case UnaryOperator.Finally:
					operatorSymbol = "F";
					break;
				case UnaryOperator.Globally:
					operatorSymbol = "G";
					break;
				case UnaryOperator.Not:
					operatorSymbol = "!";
					break;
				case UnaryOperator.All:
					operatorSymbol = "A";
					break;
				case UnaryOperator.Exists:
					operatorSymbol = "E";
					break;
				default:
					Assert.NotReached("Unknown unary operator.");
					break;
			}

			return $"({operatorSymbol} {Operand})";
		}
	}
}