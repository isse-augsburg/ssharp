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

namespace SafetySharp.Modeling
{
	using System;
	using System.Collections.Generic;
	using Utilities;

	/// <summary>
	///   Represents a S# component.
	/// </summary>
	public abstract partial class Component : IComponent
	{
		[Hidden, NonDiscoverable]
		private readonly List<IFaultEffect> _faultEffects = new List<IFaultEffect>();

		[Hidden, NonDiscoverable]
		private readonly List<Component> _subcomponents = new List<Component>();

		/// <summary>
		///   Gets the fault effects that affect the component.
		/// </summary>
		internal List<IFaultEffect> FaultEffects => _faultEffects;

		/// <summary>
		///   Gets the component's subcomponents.
		/// </summary>
		internal List<Component> Subcomponents => _subcomponents;

		/// <summary>
		///   Updates the state of the component.
		/// </summary>
		public virtual void Update()
		{
		}

		/// <summary>
		///   Visits the hierarchy of components in pre-order, executing the <paramref name="action" /> for each component.
		/// </summary>
		/// <param name="action">The action that should be executed for each component.</param>
		internal void VisitPreOrder(Action<Component> action)
		{
			Requires.NotNull(action, nameof(action));

			var visitedComponents = new HashSet<Component>();
			VisitPreOrder(visitedComponents, this, action);
		}

		/// <summary>
		///   Visits the <paramref name="component" /> and all of its subcomponents in pre-order, executing the
		///   <paramref name="action" /> for each component.
		/// </summary>
		/// <param name="visitedComponents">The components that have already been visited, in case the hierarchy contains any cycles.</param>
		/// <param name="component">The component that should be visited.</param>
		/// <param name="action">The action that should be executed for each component.</param>
		private static void VisitPreOrder(HashSet<Component> visitedComponents, Component component, Action<Component> action)
		{
			if (!visitedComponents.Add(component))
				return;

			action(component);

			foreach (var subcomponent in component.Subcomponents)
				VisitPreOrder(visitedComponents, subcomponent, action);
		}
	}
}