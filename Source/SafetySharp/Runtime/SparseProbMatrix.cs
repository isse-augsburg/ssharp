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

namespace SafetySharp.Runtime
{
	using System.Diagnostics;
	using Utilities;

	// See PhD thesis of David Anthony Parker (Implementation of symbolic model checking for probabilistic systems)
	// Chapter 3.6 Sparse Matrices
	internal sealed unsafe class SparseProbMatrix : DisposableObject
	{
		// Small difference: Mix column and value to improve caching
		internal struct ColumnValue //Contains Transition
		{
			public int Column; //No of outgoing state
			public double Value; //Probability of outgoing state
		}

		private readonly int _spaceLimitNumberOfRows; //pessimistic limit
		private readonly int _spaceLimitNumberOfEntries; //pessimistic limit

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
		private int _noOfColumnValues = 0;
		private int _currentColumnCount = 0;

		private int _maximalRow = 0;

		// The outgoing transitions of state s are stored between
		// _columnValueMemory[_rowMemory[s]] and _columnValueMemory[_rowMemory[s]+_rowColumnCountMemory[s]]
		// or when optimized and sealed between
		// _columnValueMemory[_rowMemory[s]] and _columnValueMemory[_rowMemory[s+1]]
		public SparseProbMatrix(int spaceLimitNumberOfRows, int spaceLimitNumberOfEntries)
		{
			Requires.InRange(spaceLimitNumberOfRows, nameof(spaceLimitNumberOfRows), 1024, Int32.MaxValue-1);
			Requires.InRange(spaceLimitNumberOfEntries, nameof(spaceLimitNumberOfEntries), 1024, Int32.MaxValue);

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
			for (var i = 0; i <= _spaceLimitNumberOfRows+1; i++)
			{
				_rowMemory[i] = -1;
			}
			for (var i = 0; i <= _spaceLimitNumberOfRows; i++)
			{
				_rowColumnCountMemory[i] = -1;
			}
		}

		[Conditional("DEBUG")]
		private void CheckForInvalidRowEntries()
		{
			for (var i = 0; i < _maximalRow; i++)
			{
				if (_rowMemory[i] == -1)
					throw new Exception("Entry should not be -1");
			}
			if (!_isSealed)
			{
				for (var i = 0; i <= _maximalRow; i++)
				{
					if (_rowColumnCountMemory[i] == -1)
						throw new Exception("Entry should not be -1");
				}
			}
			else
			{
				if (_rowMemory[_maximalRow + 1] == -1)
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
			Requires.InRange(row, nameof(row), 0, _spaceLimitNumberOfRows);
			AssertNotSealed();
			if (_rowMemory[row] == -1 || _rowColumnCountMemory[row] == -1)
			{
				throw new Exception("Row has already been selected. Every row might only be selected once.");
			}
			_currentRow = row;
			_currentColumnCount = 0;
			if (row > _maximalRow)
				_maximalRow =  row;
			_rowMemory[row] = _noOfColumnValues;
		}

		internal void FinishRow()
		{
			AssertNotSealed();
			if (_currentRow != -1)
			{
				//finish row
				_rowColumnCountMemory[_currentRow] = _currentColumnCount;
			}
			//TODO: Check if row has at least one entry (this is required and asserted by our application for probability matrices)
		}
		
		internal void AddColumnValueToCurrentRow(ColumnValue columnValue)
		{
			Requires.InRange(columnValue.Value, nameof(columnValue), 0, _spaceLimitNumberOfRows);
			Requires.InRange(_noOfColumnValues, nameof(_noOfColumnValues), 0, _spaceLimitNumberOfEntries);
			AssertNotSealed();
			var nextColumnValueIndex = _noOfColumnValues;
			_columnValueMemory[nextColumnValueIndex] = columnValue;
			_noOfColumnValues++;
			_currentColumnCount++;
		}

		internal void OptimizeAndSeal()
		{
			//Problem: Es kann sein, dass die States unsortiert hereinkommen. Also zuerst State 0, dann State 5, dann State 4
			// ... Frage: Ist ein sortierter Vektor effizienter, so das sich ein umkopieren noch lohnt ("defragmentieren")?!?
			// ... Frage: Zusätzlich müsste auch das Ende mit angegeben werden
			// Lässt sich evaluieren (sollte aber im Release Modus geschehen)
			CheckForInvalidRowEntries();
			//initialize fresh memory for optimized data
			// use actual number of rows and entries, not the pessimistic upper limits
			_rowBufferOptimized.Resize((long) (_maximalRow + 1) * sizeof(int), zeroMemory: false);
			_columnValueBufferOptimized.Resize((long)_noOfColumnValues * sizeof(ColumnValue), zeroMemory: false);
			//Reason for +1 in the previous line: To be able to optimize we need space for one more state 
			var optimizedRowMemory = (int*)_rowBufferOptimized.Pointer;
			var optimizedColumnValueMemory = (ColumnValue*)_columnValueBufferOptimized.Pointer;

			//copy values
			var columnValueIndex = 0;
			for (var rowi=0; rowi < _maximalRow; rowi++)
			{
				optimizedRowMemory[rowi] = columnValueIndex;
				var baseColumnIndexOfRow = _rowMemory[rowi];
				var columnEntries = _rowColumnCountMemory[rowi];
				for (var columnEntry=0; columnEntry< columnEntries; columnEntry++)
				{
					optimizedColumnValueMemory[columnValueIndex]=_columnValueMemory[baseColumnIndexOfRow+columnEntry];
					columnValueIndex++;
				}
			}
			optimizedRowMemory[_maximalRow] = columnValueIndex; //last entry


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
			for (var i = 0; i < _maximalRow; i++)
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
			for (var i = 0; i < _maximalRow; i++)
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
	}
}
