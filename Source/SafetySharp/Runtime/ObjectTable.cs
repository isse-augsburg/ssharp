// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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

namespace SafetySharp.Runtime
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Maps objects to unique identifiers and vice versa.
	/// </summary>
	internal sealed class ObjectTable
	{
		/// <summary>
		///   Maps each object to its corresponding identifier.
		/// </summary>
		private readonly Dictionary<object, int> _objectToIdentifier = new Dictionary<object, int>();

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model containing the objects that should be mapped by the table.</param>
		public ObjectTable(Model model)
		{
			Requires.NotNull(model, nameof(model));
			CollectObjects(model);
		}

		/// <summary>
		///   Gets the object corresponding to the <paramref name="identifier" />.
		/// </summary>
		public object this[int identifier]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { return Objects[identifier]; }
		}

		/// <summary>
		///   Gets the identifier that has been assigned to <paramref name="obj" />.
		/// </summary>
		public int this[object obj]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { return _objectToIdentifier[obj]; }
		}

		/// <summary>
		///   Gets the objects contained in the table.
		/// </summary>
		public object[] Objects { get; private set; }

		/// <summary>
		///   Collects all objects contained in the <paramref name="model" />.
		/// </summary>
		// TODO: Collect other kinds of objects such as list fields, arrays, etc.
		private void CollectObjects(Model model)
		{
			var objects = new List<object>();

//			model.Metadata.RootComponent.VisitPreOrder(component =>
//			{
//				objects.Add(component.Component);
//
//				foreach (var fault in component.Faults)
//				{
//					objects.Add(fault.Fault);
//					objects.Add(fault.OccurrencePattern.OccurrencePattern);
//				}
//			});

			Objects = objects.OrderBy(o => o.GetType().FullName).ToArray();

			for (var i = 0; i < Objects.Length; ++i)
				_objectToIdentifier.Add(Objects[i], i);
		}
	}
}