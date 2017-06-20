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

	public class AutoResizeVector<T> : IEnumerable<T>
		where T : struct
	{
		protected readonly List<T> _backingArray;

		public int Count => _backingArray.Count;

		public T DefaultValue = default(T);

		// auto resize
		public AutoResizeVector()
		{
			//_hasFixedSize = false;
			_backingArray = new List<T>();
		}

		// fixed size
		public AutoResizeVector(int initialCapacity)
		{
			//_hasFixedSize = true;
			_backingArray = new List<T>(initialCapacity);
			IncreaseSize(initialCapacity);
		}

		public void IncreaseSize(int size)
		{
			if (_backingArray.Count >= size)
				return;
			if (_backingArray.Capacity < size)
				_backingArray.Capacity = size*2;
			for (var i = _backingArray.Count; i < size; i++)
			{
				_backingArray.Add(DefaultValue);
			}
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
		public T this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				IncreaseSize(index + 1);
				return _backingArray[index];
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				IncreaseSize(index + 1);
				_backingArray[index] = value;
			}
		}

		public void Clear()
		{
			_backingArray.Clear();
		}

		// a nested class can access private members
		internal struct AutoResizeVectorEnumerator : IEnumerator<T>
		{
			private readonly AutoResizeVector<T> _vector;

			public int CurrentIndex { get; private set; }

			public T Current => _vector[CurrentIndex];

			object IEnumerator.Current => _vector[CurrentIndex];

			public AutoResizeVectorEnumerator(AutoResizeVector<T> vector)
			{
				_vector = vector;
				CurrentIndex = -1;
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
				CurrentIndex++;
				if (CurrentIndex >= _vector.Count)
					return false;
				return true;
			}
			

			/// <summary>
			/// Sets the enumerator to its initial position, which is before the first element in the collection.
			/// </summary>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			public void Reset()
			{
				CurrentIndex = -1;;
			}
		}
	}

	internal class DoubleVector : AutoResizeVector<double>
	{
		public DoubleVector()
			: base()
		{
		}
		
		public DoubleVector(int initialCapacity)
			: base(initialCapacity)
		{
		}
	}

	public class LabelVector : AutoResizeVector<StateFormulaSet>
	{
		public LabelVector()
			: base()
		{
		}
		
		public LabelVector(int initialCapacity)
			: base(initialCapacity)
		{
		}
	}

	internal class RewardVector : AutoResizeVector<Modeling.Reward>
	{
		public RewardVector()
			: base()
		{
		}
		
		public RewardVector(int initialCapacity)
			: base(initialCapacity)
		{
		}
	}
}
