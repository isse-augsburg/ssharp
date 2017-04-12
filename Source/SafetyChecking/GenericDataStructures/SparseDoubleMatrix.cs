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

namespace ISSE.SafetyChecking.GenericDataStructures
{
	using System.Diagnostics;
	using System.Globalization;
	using Utilities;

	// See PhD thesis of David Anthony Parker (Implementation of symbolic model checking for probabilistic systems)
	// Chapter 3.6 Sparse Matrices
	internal sealed unsafe class SparseDoubleMatrix : DisposableObject
	{
		// Small difference: Mix column and value to improve caching
		internal struct ColumnValue //Contains Transition
		{
			internal ColumnValue(int column, double value)
			{
				Column = column;
				Value = value;
			}

			public int Column; //No of outgoing state
			public double Value; //Probability of outgoing state
		}

		private readonly long _spaceLimitNumberOfRows; //pessimistic limit
		private readonly long _spaceLimitNumberOfEntries; //pessimistic limit

		private readonly MemoryBuffer _rowBufferUnoptimized = new MemoryBuffer();
		private readonly MemoryBuffer _rowBufferOptimized = new MemoryBuffer();
		private int* _rowMemory;

		private readonly MemoryBuffer _rowColumnCountBufferUnoptimized = new MemoryBuffer();
		private int* _rowColumnCountMemory;

		private readonly MemoryBuffer _columnValueBufferUnoptimized = new MemoryBuffer();
		private readonly MemoryBuffer _columnValueBufferOptimized = new MemoryBuffer();
		private ColumnValue* _columnValueMemory;

		private bool _isSealed = false;

		private int _currentRow = -1;
		public int TotalColumnValueEntries { get; private set; }
		private int _columnCountOfCurrentRow = 0;

		public int Rows { get; private set; } = 0;

		// The outgoing transitions of state s are stored between
		// _columnValueMemory[_rowMemory[s]] and _columnValueMemory[_rowMemory[s]+_rowColumnCountMemory[s]]
		// or when optimized and sealed between
		// _columnValueMemory[_rowMemory[s]] and _columnValueMemory[_rowMemory[s+1]]
		public SparseDoubleMatrix(long spaceLimitNumberOfRows, long spaceLimitNumberOfEntries)
		{
			Requires.InRange(spaceLimitNumberOfRows, nameof(spaceLimitNumberOfRows), 1, Int32.MaxValue-1);
			Requires.InRange(spaceLimitNumberOfEntries, nameof(spaceLimitNumberOfEntries), 1, Int32.MaxValue);

			_spaceLimitNumberOfRows = spaceLimitNumberOfRows;
			_spaceLimitNumberOfEntries = spaceLimitNumberOfEntries;

			_rowBufferUnoptimized.Resize((long)spaceLimitNumberOfRows * sizeof(int), zeroMemory: false);
			_rowMemory = (int*)_rowBufferUnoptimized.Pointer;

			_rowColumnCountBufferUnoptimized.Resize((long)spaceLimitNumberOfRows * sizeof(int), zeroMemory: false);
			_rowColumnCountMemory = (int*)_rowColumnCountBufferUnoptimized.Pointer;

			_columnValueBufferUnoptimized.Resize((long)spaceLimitNumberOfEntries * sizeof(ColumnValue), zeroMemory: false);
			_columnValueMemory = (ColumnValue*)_columnValueBufferUnoptimized.Pointer;

			SetRowEntriesToInvalid();
		}
		
		private void SetRowEntriesToInvalid()
		{
			for (var i = 0; i < _spaceLimitNumberOfRows; i++)
			{
				_rowMemory[i] = -1;
			}
			for (var i = 0; i < _spaceLimitNumberOfRows; i++)
			{
				_rowColumnCountMemory[i] = -1;
			}
		}

		[Conditional("DEBUG")]
		private void CheckForInvalidRowEntries()
		{
			for (var i = 0; i < Rows; i++)
			{
				if (_rowMemory[i] == -1)
					throw new Exception("Entry should not be -1");
			}
			if (!_isSealed)
			{
				for (var i = 0; i < Rows; i++)
				{
					if (_rowColumnCountMemory[i] == -1)
						throw new Exception("Entry should not be -1");
				}
			}
			else
			{
				if (_rowMemory[Rows] == -1)
					throw new Exception("Entry should not be -1");
			}
		}

		[Conditional("DEBUG")]
		private void AssertNotSealed()
		{
			if (_isSealed)
			{
				throw new Exception("Matrix must not be sealed at this point");
			}
		}

		internal void SetRow(int row)
		{
			Assert.InRange(row, 0, _spaceLimitNumberOfRows);
			AssertNotSealed();
			if (_rowMemory[row] != -1 || _rowColumnCountMemory[row] != -1)
			{
				throw new Exception("Row has already been selected. Every row might only be selected once.");
			}
			_currentRow = row;
			_columnCountOfCurrentRow = 0;
			if (row >= Rows)
				Rows =  row+1;
			_rowMemory[row] = TotalColumnValueEntries;
		}

		internal void FinishRow()
		{
			AssertNotSealed();
			if (_currentRow != -1)
			{
				//finish row
				_rowColumnCountMemory[_currentRow] = _columnCountOfCurrentRow;
			}
			//TODO: Check if row has at least one entry (this is required and asserted by our application for probability matrices)
		}
		
		internal void AddColumnValueToCurrentRow(ColumnValue columnValue)
		{
			Assert.InRange(columnValue.Column, 0, _spaceLimitNumberOfRows);
			Assert.InRange(TotalColumnValueEntries, 0, _spaceLimitNumberOfEntries);
			AssertNotSealed();
			if (TotalColumnValueEntries >= _spaceLimitNumberOfEntries || TotalColumnValueEntries < 0)
				throw new OutOfMemoryException("Unable to store entry. Try increasing the transition capacity.");

			var nextColumnValueIndex = TotalColumnValueEntries;
			_columnValueMemory[nextColumnValueIndex] = columnValue;
			TotalColumnValueEntries++;
			_columnCountOfCurrentRow++;
		}

		internal void SortRow(int row)
		{
			AssertNotSealed();
			// we implement InsertionSort here with the ability to merge two entries
			// First inner loop tries to merge.
			// If merging succeeds, row is one element smaller and the last element gets at the former position of the merged element.
			// If merging does not work the element is inserted at the right place.
			// see wiki https://en.wikipedia.org/wiki/Insertion_sort

			var l = _rowMemory[row];
			var h = l + _rowColumnCountMemory[row];

			var i = l + 1;
			while (i < h)
			{
				var currentElement = _columnValueMemory[i];

				var j = i - 1;
				while (j>=l && _columnValueMemory[j].Column >= currentElement.Column)
				{
					j--;
				}
				j++;
				// now j is at the position where either
				//     * i==j:
				//               nothing to do, element was already at the right position)
				//     * j<i && _columnValueMemory[j].Column == currentElement.Column:
				//               merge
				//     * j<i && _columnValueMemory[j].Column > currentElement.Column
				//               move
				if (i == j)
				{
					i++;
				}
				else
				{
					if (_columnValueMemory[j].Column == currentElement.Column)
					{
						// merge nodes
						_columnValueMemory[j].Value += currentElement.Value;
						// move last entry to the front
						_columnValueMemory[i] = _columnValueMemory[h - 1]; //last element is always h-1
						h--;
						_rowColumnCountMemory[row]--;
					}
					else
					{
						// insertion at the right position and move every element
						j = i - 1;
						while (j >= l && _columnValueMemory[j].Column > currentElement.Column)
						{
							_columnValueMemory[j + 1] = _columnValueMemory[j];
							j--;
						}
						_columnValueMemory[j+1] = currentElement;
					}
				}
			}
		}

		internal void OptimizeAndSeal()
		{
			//Problem: Es kann sein, dass die States unsortiert hereinkommen. Also zuerst State 0, dann State 5, dann State 4
			// ... Frage: Ist ein sortierter Vektor effizienter, so das sich ein umkopieren noch lohnt ("defragmentieren")?!?
			// ... Frage: Zusätzlich müsste auch das Ende mit angegeben werden
			// Lässt sich evaluieren (sollte aber im Release Modus geschehen)
			// TODO: Merge multiple entries of a row having the same Column. Sort entries?!?
			CheckForInvalidRowEntries();
			//initialize fresh memory for optimized data
			// use actual number of rows and entries, not the pessimistic upper limits
			_rowBufferOptimized.Resize((long) (Rows + 1) * sizeof(int), zeroMemory: false);
			_columnValueBufferOptimized.Resize((long)TotalColumnValueEntries * sizeof(ColumnValue), zeroMemory: false);
			//Reason for +1 in the previous line: To be able to optimize we need space for one more state 
			var optimizedRowMemory = (int*)_rowBufferOptimized.Pointer;
			var optimizedColumnValueMemory = (ColumnValue*)_columnValueBufferOptimized.Pointer;

			//copy values
			var columnValueIndex = 0;
			for (var rowi=0; rowi < Rows; rowi++)
			{
				optimizedRowMemory[rowi] = columnValueIndex;
				var l = _rowMemory[rowi];
				var h = l + _rowColumnCountMemory[rowi];
				for (var j = l; j < h; j++)
				{
					optimizedColumnValueMemory[columnValueIndex]=_columnValueMemory[j];
					columnValueIndex++;
				}
			}
			optimizedRowMemory[Rows] = columnValueIndex; //last entry


			//commit changes
			_isSealed = true;
			_rowBufferUnoptimized.SafeDispose();
			_columnValueBufferUnoptimized.SafeDispose();
			_rowColumnCountBufferUnoptimized.SafeDispose();
			_rowMemory = optimizedRowMemory;
			_columnValueMemory = optimizedColumnValueMemory;
			_rowColumnCountMemory = null;
			CheckForInvalidRowEntries();
		}

		public void MultiplyWithVectorSealed(double[] b, double[] res)
		{
			//Implementation of algorithm 3.13
			for (var i = 0; i < Rows; i++)
			{
				res[i] = 0;
				var l = _rowMemory[i];
				var h = _rowMemory[i+1];
				for (var j = l; j < h; j++)
				{
					var colValEntry = _columnValueMemory[j];
					res[i] = res[i] + colValEntry.Value * b[colValEntry.Column];
				}
			}
		}

		public void MultiplyWithVectorUnsealed(double[] b, double[] res)
		{
			for (var i = 0; i < Rows; i++)
			{
				res[i] = 0;
				var l = _rowMemory[i];
				var h = l+_rowColumnCountMemory[i];
				for (var j = l; j < h; j++)
				{
					var colValEntry = _columnValueMemory[j];
					res[i] = res[i] + colValEntry.Value * b[colValEntry.Column];
				}
			}
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (disposing)
			{
				_rowBufferUnoptimized.SafeDispose();
				_columnValueBufferUnoptimized.SafeDispose();
				_rowColumnCountBufferUnoptimized.SafeDispose();
				_rowBufferOptimized.SafeDispose();
				_columnValueBufferOptimized.SafeDispose();
			}
		}

		[Conditional("DEBUG")]
		public void PrintMatrix(Action<string, object[]> printer=null)
		{
			if (printer == null)
				printer = Console.WriteLine;
			var enumerator = GetEnumerator();
			while (enumerator.MoveNextRow())
			{
				printer(enumerator.CurrentRow.ToString(),new object[0]);
				while (enumerator.MoveNextColumn())
				{
					if (enumerator.CurrentColumnValue != null) 
						printer($"\t-> {enumerator.CurrentColumnValue.Value.Column} {enumerator.CurrentColumnValue.Value.Value.ToString(CultureInfo.InvariantCulture)}", new object[0]);
					else
						throw new Exception("Entry must not be null");
				}
			}
		}

		internal SparseDoubleMatrixEnumerator GetEnumerator()
		{
			return new SparseDoubleMatrixEnumerator(this);
		}

		// a nested class can access private members
		internal class SparseDoubleMatrixEnumerator
		{
			private SparseDoubleMatrix _matrix;
			
			public int CurrentRow { get; private set; }

			private int _currentColumnValueL; //inclusive
			private int _currentColumnValueH; //exclusive
			private int _currentColumnValueIndex;

			public ColumnValue? CurrentColumnValue { get; private set; }

			public SparseDoubleMatrixEnumerator(SparseDoubleMatrix matrix)
			{
				_matrix = matrix;
				Reset();
			}

			/// <summary>
			/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
			/// </summary>
			public void Dispose()
			{
			}
			
			public bool MoveRow(int row)
			{
				CurrentRow=row;
				if (CurrentRow >= _matrix.Rows)
					return false;
				_currentColumnValueL = _matrix._rowMemory[CurrentRow];
				if (_matrix._isSealed)
				{
					_currentColumnValueH = _matrix._rowMemory[CurrentRow + 1];
				}
				else
				{
					_currentColumnValueH = _currentColumnValueL+_matrix._rowColumnCountMemory[CurrentRow];
				}
				_currentColumnValueIndex = _currentColumnValueL - 1;
				CurrentColumnValue = null;
				return true;
			}

			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns>
			/// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
			/// </returns>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			public bool MoveNextRow()
			{
				CurrentRow++;
				if (CurrentRow >= _matrix.Rows)
					return false;
				_currentColumnValueL = _matrix._rowMemory[CurrentRow];
				if (_matrix._isSealed)
				{
					_currentColumnValueH = _matrix._rowMemory[CurrentRow + 1];
				}
				else
				{
					_currentColumnValueH = _currentColumnValueL+_matrix._rowColumnCountMemory[CurrentRow];
				}
				_currentColumnValueIndex = _currentColumnValueL - 1;
				CurrentColumnValue = null;
				return true;
			}

			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns>
			/// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
			/// </returns>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			public bool MoveNextColumn()
			{
				_currentColumnValueIndex++;
				if (_currentColumnValueIndex>= _currentColumnValueH)
				{
					CurrentColumnValue = null;
					return false;
				}
				else
				{
					CurrentColumnValue = _matrix._columnValueMemory[_currentColumnValueIndex];
					return true;
				}
			}

			/// <summary>
			/// Sets the enumerator to its initial position, which is before the first element in the collection.
			/// </summary>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			public void Reset()
			{
				CurrentRow = -1;
				_currentColumnValueL = -1;
				_currentColumnValueH = -1;
				_currentColumnValueIndex = -1;
				CurrentColumnValue = null;
			}
		}

	}
}
