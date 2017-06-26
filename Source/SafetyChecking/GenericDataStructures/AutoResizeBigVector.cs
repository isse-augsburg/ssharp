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
	using AnalysisModel;
	using System.Collections;
	using System.Runtime.CompilerServices;
	using Utilities;

	public class AutoResizeBigVector<T> : IEnumerable<T>
		where T : struct
	{
		private long _currentOffset;

		protected readonly List<T> _backingArray;

		private int _internalSize = 0;

		public long Count => _internalSize + _currentOffset;
		
		public T DefaultValue = default(T);

		// auto resize
		public AutoResizeBigVector()
		{
			//_hasFixedSize = false;
			_backingArray = new List<T>();
			_currentOffset = 0;
		}

		// fixed size
		public AutoResizeBigVector(int initialCapacity)
		{
			//_hasFixedSize = true;
			_backingArray = new List<T>(initialCapacity);
			IncreaseSize(initialCapacity);
		}

		public void IncreaseSize(long size)
		{
			var internalSizeL = size - _currentOffset;
			Assert.That(internalSizeL < int.MaxValue, "too many elements");
			Assert.That(internalSizeL >= 0, "offset is too small");
			var internalSize = (int)internalSizeL;

			if (_internalSize >= internalSize)
				return;
			if (_backingArray.Capacity < internalSize)
				_backingArray.Capacity = internalSize * 2;
			for (var i = _backingArray.Count; i < internalSize; i++)
			{
				_backingArray.Add(DefaultValue);
			}
			_internalSize = internalSize;
		}

		private int GetInternalIndex(long publicIndex)
		{
			var internalIndex = publicIndex - _currentOffset;
			Assert.That(internalIndex < int.MaxValue, "too many elements");
			return (int)internalIndex;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new AutoResizeVectorEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new AutoResizeVectorEnumerator(this);
		}

		//public void SealSize()
		//{
		//}

		/// <summary>
		///   Gets the element at <paramref name="index" /> from the vector.
		/// </summary>
		public T this[long index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IncreaseSize(index + 1);
				var internalIndex = GetInternalIndex(index);
				return _backingArray[internalIndex];
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				IncreaseSize(index + 1);
				var internalIndex = GetInternalIndex(index);
				_backingArray[internalIndex] = value;
			}
		}

		public void Clear(long offset)
		{
			_currentOffset = offset;
			
			for (var i = 0; i < _internalSize; i++)
			{
				_backingArray[i] = DefaultValue;
			}
			_internalSize = 0;
		}

		// a nested class can access private members
		internal struct AutoResizeVectorEnumerator : IEnumerator<T>
		{
			private readonly AutoResizeBigVector<T> _vector;
			
			public int CurrentIndex => _vector.GetInternalIndex(InternalIndex);
			
			private int InternalIndex { get; set; }

			public T Current => _vector[CurrentIndex];

			object IEnumerator.Current => _vector[CurrentIndex];

			public AutoResizeVectorEnumerator(AutoResizeBigVector<T> vector)
			{
				_vector = vector;
				InternalIndex = -1;
			}

			/// <summary>
			/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
			/// </summary>
			public void Dispose()
			{
			}
			

			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns>
			/// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
			/// </returns>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			public bool MoveNext()
			{
				InternalIndex++;
				if (InternalIndex >= _vector._internalSize)
					return false;
				return true;
			}
			

			/// <summary>
			/// Sets the enumerator to its initial position, which is before the first element in the collection.
			/// </summary>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			public void Reset()
			{
				InternalIndex = -1;;
			}
		}
	}
}
