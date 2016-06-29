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
	using System.Runtime.CompilerServices;

	public class DoubleVector
	{
		//private bool _hasFixedSize;

		private List<double> _backingArray;

		// auto resize
		public DoubleVector()
		{
			//_hasFixedSize = false;
			_backingArray = new List<double>();
		}

		// fixed size
		public DoubleVector(int fixedSize)
		{
			//_hasFixedSize = true;
			_backingArray = new List<double>(fixedSize);
			IncreaseSize(fixedSize);
		}

		public void IncreaseSize(int size)
		{
			if (_backingArray.Count >= size)
				return;
			if (_backingArray.Capacity<size)
				_backingArray.Capacity = size;
			for (var i = _backingArray.Count; i < size; i++)
			{
				_backingArray[i] = 0.0;
			}
		}

		//public void SealSize()
		//{
		//}

		/// <summary>
		///   Gets the element at <paramref name="index" /> from the vector.
		/// </summary>
		public double this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IncreaseSize(index+1);
				return _backingArray[index];
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				IncreaseSize(index+1);
				_backingArray[index] = value;
			}
		}
	}
}
