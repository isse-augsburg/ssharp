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

namespace SafetySharp.Modeling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Analysis;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;

	/// <summary>
	///   Represents a model consisting of several root <see cref="IComponent" /> instances.
	/// </summary>
	public class Model
	{
		private Fault[] _faults;
		private IComponent[] _roots;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="components">The components the model should consist of.</param>
		internal Model(params IComponent[] components)
		{
			Requires.NotNull(components, nameof(components));
			Requires.That(components.Length > 0, nameof(components), "Expected at least one component.");

			Roots = components;
			ModelBinder.Bind(this);
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		protected Model()
		{
		}

		/// <summary>
		///   Gets the model's root components.
		/// </summary>
		public IComponent[] Roots
		{
			get
			{
				EnsureIsBound();
				return _roots;
			}
			internal set
			{
				Requires.That(_roots == null, "The roots have already been set.");
				_roots = value;
			}
		}

		/// <summary>
		///   Gets all of the faults contained in the model.
		/// </summary>
		public Fault[] Faults
		{
			get
			{
				EnsureIsBound();
				return _faults;
			}
			set
			{
				Requires.That(_faults == null, "The faults have already been set.");
				_faults = value;
			}
		}

		/// <summary>
		///   Gets the <see cref="IComponent" /> instances the model consists of.
		/// </summary>
		public IComponent[] GetComponents()
		{
			var components = new HashSet<IComponent>();
			VisitPreOrder(c => components.Add(c));

			return components.ToArray();
		}

		/// <summary>
		///   Visits the hierarchy of components in pre-order, executing the <paramref name="action" /> for each one.
		/// </summary>
		/// <param name="action">The action that should be executed for each component.</param>
		public void VisitPreOrder(Action<IComponent> action)
		{
			foreach (var component in Roots)
				component.VisitPreOrder(action);
		}

		/// <summary>
		///   Visits the hierarchy of components in post-order, executing the <paramref name="action" /> for each one.
		/// </summary>
		/// <param name="action">The action that should be executed for each component.</param>
		public void VisitPostOrder(Action<IComponent> action)
		{
			foreach (var component in Roots)
				component.VisitPostOrder(action);
		}

		/// <summary>
		///   Creates a <see cref="RuntimeModel" /> instance from the model and the <paramref name="formulas" />.
		/// </summary>
		/// <param name="formulas">The formulas the model should be able to check.</param>
		internal RuntimeModel ToRuntimeModel(params Formula[] formulas)
		{
			Requires.NotNull(formulas, nameof(formulas));

			var serializer = new RuntimeModelSerializer();
			serializer.Serialize(this, formulas);
			return serializer.Load();
		}

		/// <summary>
		///   Ensures that the model has been bound.
		/// </summary>
		private void EnsureIsBound()
		{
			if (_roots != null)
				return;

			ModelBinder.DiscoverRoots(this);
			ModelBinder.Bind(this);
		}
	}
}