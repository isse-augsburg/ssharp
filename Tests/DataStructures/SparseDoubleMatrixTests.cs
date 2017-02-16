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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.DataStructures
{
	using ISSE.SafetyChecking.GenericDataStructures;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public class SparseDoubleMatrixTests
	{
		/// <summary>
		///   Gets the output that writes to the test output stream.
		/// </summary>
		public TestTraceOutput Output { get; }

		private SparseDoubleMatrix _matrix;

		private void CreateExemplaryMatrix()
		{
			//_matrix = new SparseDoubleMatrix(6, 20);
			_matrix = new SparseDoubleMatrix(1024, 1024);
			_matrix.SetRow(0);
			_matrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(0,1.0));
			_matrix.FinishRow();
			_matrix.SetRow(1);
			_matrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(1, 2.0));
			_matrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(2, 3.0));
			_matrix.FinishRow();
			_matrix.SetRow(2);
			_matrix.FinishRow();
			_matrix.SetRow(3);
			_matrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(0, 4.0));
			_matrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(4, 5.0));
			_matrix.FinishRow();
			_matrix.SetRow(4);
			_matrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(4, 6.0));
			_matrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(3, 7.0));
			_matrix.FinishRow();
		}

		public SparseDoubleMatrixTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		[Fact]
		public void PassingTestOptimized()
		{
			CreateExemplaryMatrix();
			_matrix.OptimizeAndSeal();
			var enumerator = _matrix.GetEnumerator();
			var counter = 0.0;
			while (enumerator.MoveNextRow())
			{
				while (enumerator.MoveNextColumn())
				{
					if (enumerator.CurrentColumnValue!=null)
						counter += enumerator.CurrentColumnValue.Value.Value;
					else
						throw new Exception("Entry must not be null");
				}
			}
			Assert.Equal(28.0, counter);
		}

		[Fact]
		public void PassingTestUnoptimized()
		{
			CreateExemplaryMatrix();
			var enumerator = _matrix.GetEnumerator();
			var counter = 0.0;
			while (enumerator.MoveNextRow())
			{
				while (enumerator.MoveNextColumn())
				{
					if (enumerator.CurrentColumnValue != null)
						counter += enumerator.CurrentColumnValue.Value.Value;
					else
						throw new Exception("Entry must not be null");
				}
			}
			Assert.Equal(28.0, counter);
		}
	}
}
