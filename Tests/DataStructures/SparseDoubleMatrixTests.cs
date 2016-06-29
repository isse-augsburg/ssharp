using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.DataStructures
{
	using System.Diagnostics;
	using SafetySharp.Analysis;
	using SafetySharp.Runtime;
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
			_matrix = new SparseDoubleMatrix(6,20);
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
			_matrix.OptimizeAndSeal();
		}

		public SparseDoubleMatrixTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		[Fact]
		public void PassingTest()
		{
			CreateExemplaryMatrix();
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
			Assert.Equal(counter, 28.0);
		}
	}
}
