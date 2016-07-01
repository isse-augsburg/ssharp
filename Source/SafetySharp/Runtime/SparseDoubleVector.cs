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
	using System.Collections;
	using System.Runtime.CompilerServices;

	public class SparseDoubleVector : IEnumerable<SparseDoubleVector.IndexDoubleTuple>
	{
		private readonly List<IndexDoubleTuple> _backingArray;
		
		public SparseDoubleVector()
		{
			_backingArray = new List<IndexDoubleTuple>();
		}		

		public void Append(int index, double value)
		{
			_backingArray.Add(new IndexDoubleTuple {Index=index,Value=value});
		}

		internal void OptimizeAndSeal()
		{
			_backingArray.Sort((element1,element2)=>element1.Index.CompareTo(element2.Index));
		}

		public struct IndexDoubleTuple
		{
			public int Index;
			public double Value;
		}

		public IEnumerator<IndexDoubleTuple> GetEnumerator()
		{
			return _backingArray.GetEnumerator();
		} 

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _backingArray.GetEnumerator();
		}
	}
}
