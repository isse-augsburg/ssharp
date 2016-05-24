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
	using System;
	using FormulaVisitors;
	using Utilities;

	public abstract class ProbabilitisticFormula : Formula
	{
		public Formula Operand { get; }

		protected ProbabilitisticFormula(Formula operand)
		{
			Operand = operand;
		}

		/// <summary>
		///   Executes the <paramref name="visitor" /> for this formula.
		/// </summary>
		/// <param name="visitor">The visitor that should be executed.</param>
		internal override void Visit(FormulaVisitor visitor)
		{
			visitor.VisitProbabilisticFormula(this);
		}
	}

	public sealed class ProbabilityToReachStateFormula : ProbabilitisticFormula
	{
		//TODO: enum >= <= > <
		public ProbabilityToReachStateFormula(Formula operand)
			:base(operand)
		{
		}
	}

	public sealed class CalculateProbabilityToReachStateFormula : ProbabilitisticFormula
	{
		public CalculateProbabilityToReachStateFormula(Formula operand)
			: base(operand)
		{
		}
	}

	/*
	/// <summary>
	///   Represents the application of a <see cref="BinaryOperator" /> to two <see cref="Formula" /> instances.
	/// </summary>
	internal sealed class BinaryFormula : Formula
	{
		/// <summary>
		///   Initializes a new instance of the <see cref="BinaryFormula" /> class.
		/// </summary>
		/// <param name="leftOperand">The formula on the left-hand side of the binary operator.</param>
		/// <param name="binaryOperator">The operator of the binary formula.</param>
		/// <param name="rightOperand">The formula on the right-hand side of the binary operator.</param>
		internal BinaryFormula(Formula leftOperand, BinaryOperator binaryOperator, Formula rightOperand)
		{
			Requires.NotNull(leftOperand, nameof(leftOperand));
			Requires.InRange(binaryOperator, nameof(binaryOperator));
			Requires.NotNull(rightOperand, nameof(rightOperand));

			LeftOperand = leftOperand;
			Operator = binaryOperator;
			RightOperand = rightOperand;
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
		///   Executes the <paramref name="visitor" /> for this formula.
		/// </summary>
		/// <param name="visitor">The visitor that should be executed.</param>
		internal override void Visit(FormulaVisitor visitor)
		{
			visitor.VisitBinaryFormula(this);
		}
	}*/
}