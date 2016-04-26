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
	///   A base class for S# models consisting of several root <see cref="IComponent" /> instances
	///   that can be simulator and model checked.
	/// </summary>
	public abstract class ModelBase
	{
		private IComponent[] _components;
		private Fault[] _faults;
		private IComponent[] _roots;

		/// <summary>
		///   Gets the components contained in the model.
		/// </summary>
		public IComponent[] Components
		{
			get
			{
				EnsureIsBound();
				return _components ?? (_components = GetComponents());
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
		public static void Bind(string requiredPort, string providedPort)
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
		public static void Bind<T>(string requiredPort, string providedPort)
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
		///   Invoked when the model should initialize bindings between its components.
		/// </summary>
		protected internal virtual void CreateBindings()
		{
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
		///   Gets the <see cref="IComponent" /> instances referenced by the model.
		/// </summary>
		private IComponent[] GetComponents()
		{
			var components = new HashSet<IComponent>(ReferenceEqualityComparer<IComponent>.Default);
			VisitPreOrder(c => components.Add(c));

			return components.ToArray();
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