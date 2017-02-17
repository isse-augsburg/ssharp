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
	using ISSE.SafetyChecking.Modeling;
	using ISSE.SafetyChecking.Utilities;
	using Runtime;
	using Utilities;

	/// <summary>
	///   A base class for S# models consisting of several root <see cref="IComponent" /> instances
	///   that can be simulated and model checked.
	/// </summary>
	public abstract class ModelBase
	{
		[Hidden, NonDiscoverable]
		private IComponent[] _components;

		[Hidden, NonDiscoverable]
		private Fault[] _faults;

		[Hidden, NonDiscoverable]
		private object[] _referencedObjects;

		[Hidden, NonDiscoverable]
		private IComponent[] _roots;

		/// <summary>
		///   Gets the range metadata for the model's objects.
		/// </summary>
		[Hidden, NonDiscoverable]
		internal List<RangeMetadata> RangeMetadata { get; } = new List<RangeMetadata>();

		/// <summary>
		///   Gets the components contained in the model.
		/// </summary>
		public IComponent[] Components
		{
			get
			{
				EnsureIsBound();
				return _components;
			}
			internal set
			{
				Requires.That(_components == null, "The components have already been set.");
				Requires.NotNull(value, nameof(value));
				Requires.That(value.Length > 0, nameof(value), "Expected at least one component.");

				_components = value;
			}
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
				Requires.NotNull(value, nameof(value));
				Requires.That(value.Length > 0, nameof(value), "Expected at least one root component.");

				_roots = value;
			}
		}

		/// <summary>
		///   Gets or sets the objects referenced by the model that have to be serialized during simulation and model checking.
		/// </summary>
		internal object[] ReferencedObjects
		{
			get
			{
				EnsureIsBound();
				return _referencedObjects;
			}
			set
			{
				Requires.That(_referencedObjects == null, "The referenced objects have already been set.");
				_referencedObjects = value;
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
			internal set
			{
				Requires.That(_faults == null, "The faults have already been set.");
				_faults = value;
			}
		}

		/// <summary>
		///   Establishes a port binding between the <paramref name="requiredPort" /> and the <paramref name="providedPort" />.
		/// </summary>
		/// <param name="requiredPort">The required port that should be bound to the <paramref name="providedPort" />.</param>
		/// <param name="providedPort">The provided port that should be bound to the <paramref name="requiredPort" />.</param>
		protected static void Bind(string requiredPort, string providedPort)
		{
			Requires.CompilationTransformation();
		}

		/// <summary>
		///   Establishes a port binding between the <paramref name="requiredPort" /> and the <paramref name="providedPort" /> where the
		///   actual ports that should be bound are disambiguated by the delegate <typeparamref name="T" />.
		/// </summary>
		/// <typeparam name="T">A delegate type that disambiguates the ports.</typeparam>
		/// <param name="requiredPort">The required port that should be bound to the <paramref name="providedPort" />.</param>
		/// <param name="providedPort">The provided port that should be bound to the <paramref name="requiredPort" />.</param>
		protected static void Bind<T>(string requiredPort, string providedPort)
			where T : class
		{
			Requires.CompilationTransformation();
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
		///   Ensures that the model has been bound.
		/// </summary>
		internal void EnsureIsBound()
		{
			if (_roots != null)
				return;

			ModelBinder.DiscoverRoots(this);
			ModelBinder.Bind(this);
		}
	}
}