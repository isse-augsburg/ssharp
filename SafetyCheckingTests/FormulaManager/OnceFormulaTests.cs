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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.FormulaManager
{
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.GenericDataStructures;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	using static Formula.TestFormulaGenerator;

	public class OnceFormulaTests
	{

		public TestTraceOutput Output { get; }

		public OnceFormulaTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}
		
		[Fact]
		public void CollectMaximalNormalizableFormulasWithBinary()
		{
			var vistor = new CollectMaximalNormalizableFormulasVisitor();
			var formula = CreateBinaryFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(vistor.CollectedStateFormulas.Count(),1);
		}

		[Fact]
		public void CollectMaximalNormalizableFormulasWithNestedOnce()
		{
			var vistor = new CollectMaximalNormalizableFormulasVisitor();
			var formula = CreateNestedOnceFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(1, vistor.CollectedStateFormulas.Count());
		}

		[Fact]
		public void CollectMaximalNormalizableFormulasWithConnectedNestedOnce()
		{
			var vistor = new CollectMaximalNormalizableFormulasVisitor();
			var formula = CreateConnectedNestedOnceFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(1, vistor.CollectedStateFormulas.Count());
		}

		[Fact]
		public void CollectMaximalNormalizableFormulasWithFinallyNestedOnce()
		{
			var vistor = new CollectMaximalNormalizableFormulasVisitor();
			var formula = CreateFinallyNestedFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(1, vistor.CollectedStateFormulas.Count());
		}

		[Fact]
		public void CollectMaximalNormalizableFormulasWithFinallyConnectedNestedOnce()
		{
			var vistor = new CollectMaximalNormalizableFormulasVisitor();
			var formula = CreateFinallyConnectedNestedFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(1, vistor.CollectedStateFormulas.Count());
		}

		[Fact]
		public void CollectMaximalNormalizableFormulasWithNestedTwice()
		{
			var vistor = new CollectMaximalNormalizableFormulasVisitor();
			var formula = CreateNestedTwice();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(1, vistor.CollectedStateFormulas.Count());
		}
		
		[Fact]
		public void CollectDeepestOnceFormulasWithCompilableOperandWithBinary()
		{
			var vistor = new CollectDeepestOnceFormulasWithCompilableOperandVisitor();
			var formula = CreateBinaryFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(0,vistor.DeepestOnceFormulasWithCompilableOperand.Count());
		}

		[Fact]
		public void CollectDeepestOnceFormulasWithCompilableOperandWithNestedOnce()
		{
			var vistor = new CollectDeepestOnceFormulasWithCompilableOperandVisitor();
			var formula = CreateNestedOnceFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(1, vistor.DeepestOnceFormulasWithCompilableOperand.Count());
		}

		[Fact]
		public void CollectDeepestOnceFormulasWithCompilableOperandWithConnectedNestedOnce()
		{
			var vistor = new CollectDeepestOnceFormulasWithCompilableOperandVisitor();
			var formula = CreateConnectedNestedOnceFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(2, vistor.DeepestOnceFormulasWithCompilableOperand.Count());
		}

		[Fact]
		public void CollectDeepestOnceFormulasWithCompilableOperandWithFinallyNestedOnce()
		{
			var vistor = new CollectDeepestOnceFormulasWithCompilableOperandVisitor();
			var formula = CreateFinallyNestedFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(1, vistor.DeepestOnceFormulasWithCompilableOperand.Count());
		}

		[Fact]
		public void CollectDeepestOnceFormulasWithCompilableOperandWithFinallyConnectedNestedOnce()
		{
			var vistor = new CollectDeepestOnceFormulasWithCompilableOperandVisitor();
			var formula = CreateFinallyConnectedNestedFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(2, vistor.DeepestOnceFormulasWithCompilableOperand.Count());
		}

		[Fact]
		public void CollectDeepestOnceFormulasWithCompilableOperandWithNestedTwice()
		{
			var vistor = new CollectDeepestOnceFormulasWithCompilableOperandVisitor();
			var formula = CreateNestedTwice();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(1, vistor.DeepestOnceFormulasWithCompilableOperand.Count());
		}
		
		
		[Fact]
		public void CollectMaximalCompilableFormulasWithBinary()
		{
			var vistor = new CollectMaximalCompilableFormulasVisitor();
			var formula = CreateBinaryFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(vistor.CollectedStateFormulas.Count(),1);
		}

		[Fact]
		public void CollectMaximalCompilableFormulasWithNestedOnce()
		{
			var vistor = new CollectMaximalCompilableFormulasVisitor();
			var formula = CreateNestedOnceFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(1, vistor.CollectedStateFormulas.Count());
		}

		[Fact]
		public void CollectMaximalCompilableFormulasWithConnectedNestedOnce()
		{
			var vistor = new CollectMaximalCompilableFormulasVisitor();
			var formula = CreateConnectedNestedOnceFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(2, vistor.CollectedStateFormulas.Count());
		}

		[Fact]
		public void CollectMaximalCompilableFormulasWithFinallyNestedOnce()
		{
			var vistor = new CollectMaximalCompilableFormulasVisitor();
			var formula = CreateFinallyNestedFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(1, vistor.CollectedStateFormulas.Count());
		}

		[Fact]
		public void CollectMaximalCompilableFormulasWithFinallyConnectedNestedOnce()
		{
			var vistor = new CollectMaximalCompilableFormulasVisitor();
			var formula = CreateFinallyConnectedNestedFormula();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(2, vistor.CollectedStateFormulas.Count());
		}

		[Fact]
		public void CollectMaximalCompilableFormulasWithNestedTwice()
		{
			var vistor = new CollectMaximalNormalizableFormulasVisitor();
			var formula = CreateNestedTwice();
			vistor.VisitNewTopLevelFormula(formula);
			Assert.Equal(1, vistor.CollectedStateFormulas.Count());
		}
	}
}
