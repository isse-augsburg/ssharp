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

namespace SafetySharp.Modeling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using ISSE.SafetyChecking.Utilities;
	using Utilities;

	/// <summary>
	///   Pools objects of type <typeparamref name="T" /> in order to mimic object creation during simulations and model checking.
	///   Instead of allocating a new object of type <typeparamref name="T" /> whenever one is needed, the pool's
	///   <see cref="Allocate" /> method should be used to retrieve a previously allocated instance. Once the object is no longer
	///   being used, it must be returned to the pool so that it can be reused later on.
	/// </summary>
	/// <typeparam name="T">The type of the pooled objects.</typeparam>
	[Hidden]
	public sealed class ObjectPool<T>
		where T : class
	{
		/// <summary>
		///   The objects managed by the pool.
		/// </summary>
		[Hidden]
		private readonly ObjectInfo[] _pooledObjects;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="objs">The objects that can be allocated from the pool.</param>
		public ObjectPool(IEnumerable<T> objs)
		{
			Requires.NotNull(objs, nameof(objs));
			_pooledObjects = objs.Select(obj => new ObjectInfo { Object = obj }).ToArray();
		}

		/// <summary>
		///   Initializes a new instance, initializing <paramref name="capacity" />-many instances using the
		///   <paramref name="constructor" /> function.
		///   If <paramref name="constructor" /> is <c>null</c>, <typeparamref name="T" />'s default constructor is used to initialize
		///   the objects.
		/// </summary>
		/// <param name="capacity">The number of instances that can be allocated from the pool.</param>
		/// <param name="constructor">
		///   The function that should be used to initialize the objects or <c>null</c> if
		///   <typeparamref name="T" />'s default constructor should be used.
		/// </param>
		public ObjectPool(int capacity, Func<T> constructor = null)
		{
			Requires.That(capacity >= 0, nameof(capacity), "Invalid capacity.");

			_pooledObjects = new ObjectInfo[capacity];

			for (var i = 0; i < capacity; ++i)
			{
				T obj;
				if (constructor != null)
				{
					obj = constructor();
					if (obj == null)
						throw new InvalidOperationException("The constructor function returned a null value.");
				}
				else
				{
					try
					{
						obj = Activator.CreateInstance<T>();
					}
					catch (MissingMethodException)
					{
						throw new InvalidOperationException(
							$"Type '{typeof(T).FullName}' does not declare a default constructor. Provide a constructor function instead.");
					}
				}

				_pooledObjects[i].Object = obj;
			}
		}

		/// <summary>
		///   Gets a pooled object or allocates a new instance if none are currently pooled.
		/// </summary>
		public T Allocate()
		{
			for (var i = 0; i < _pooledObjects.Length; ++i)
			{
				if (_pooledObjects[i].Used)
					continue;

				_pooledObjects[i].Used = true;
				return _pooledObjects[i].Object;
			}

			throw new OutOfMemoryException($"Object pool ran out of instances of type '{typeof(T).FullName}'.");
		}

		/// <summary>
		///   Returns <paramref name="obj" /> to the pool so that it can be reused later.
		/// </summary>
		/// <param name="obj">The object that should be returned to the pool.</param>
		public void Return(T obj)
		{
			if (obj == null)
				return;

			var index = IndexOf(obj);
			Requires.That(index != -1, "The object is not managed by the pool.");
			Requires.That(_pooledObjects[index].Used, "The object has already been returned to the pool.");

			_pooledObjects[index].Used = false;
		}

		/// <summary>
		///   Returns <paramref name="objs" /> to the pool so that they can be reused later.
		/// </summary>
		/// <param name="objs">The objects that should be returned to the pool.</param>
		public void Return(IEnumerable<T> objs)
		{
			if (objs == null)
				return;

			foreach (var obj in objs)
				Return(obj);
		}

		/// <summary>
		///   Resets the object pool, marking all pooled objects as unused.
		/// </summary>
		public void Reset()
		{
			for (var i = 0; i < _pooledObjects.Length; ++i)
				_pooledObjects[i].Used = false;
		}

		/// <summary>
		///   Returns the zero-based index of <paramref name="obj" /> in <see cref="_pooledObjects" /> or <c>-1</c> otherwise.
		/// </summary>
		private int IndexOf(T obj)
		{
			for (var i = 0; i < _pooledObjects.Length; ++i)
			{
				if (ReferenceEquals(_pooledObjects[i].Object, obj))
					return i;
			}

			return -1;
		}

		/// <summary>
		///   Indicates whether a pooled object is currently in use.
		/// </summary>
		private struct ObjectInfo
		{
			/// <summary>
			///   The pooled object.
			/// </summary>
			public T Object;

			/// <summary>
			///   A value indicating whether the item is currently in use.
			/// </summary>
			public bool Used;
		}
	}
}