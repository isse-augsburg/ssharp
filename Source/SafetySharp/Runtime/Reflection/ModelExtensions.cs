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

namespace SafetySharp.Runtime.Reflection
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Analysis;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Allows access to the metadata of <see cref="Model" /> instances and provides convenience
	///   methods for working with the metadata.
	/// </summary>
	public static class ModelExtensions
	{
		/// <summary>
		///   Gets the <see cref="IComponent" /> instances the <paramref name="model" /> consists of.
		/// </summary>
		/// <param name="model">The model the components should be returned for.</param>
		public static IComponent[] GetComponents(this Model model)
		{
			Requires.NotNull(model, nameof(model));

			var components = new HashSet<IComponent>();
			model.VisitPreOrder(c => components.Add(c));

			return components.ToArray();
		}

		/// <summary>
		///   Gets the <see cref="Fault" /> instances contained in the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model the faults should be returned for.</param>
		public static Fault[] GetFaults(this Model model)
		{
			Requires.NotNull(model, nameof(model));

			model.BindFaultEffects();

			var faults = new HashSet<Fault>();
			model.VisitPreOrder(c =>
			{
				foreach (var faultEffect in ((Component)c).FaultEffects)
					faults.Add(faultEffect.GetFault());
			});

			return faults.ToArray();
		}

		/// <summary>
		///   Suppresses all potential fault activations for the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model the fault activations should be suppressed for.</param>
		public static void SuppressAllFaultActivations(this Model model)
		{
			Requires.NotNull(model, nameof(model));

			foreach (var fault in model.GetFaults())
				fault.Activation = Activation.Suppressed;
		}

		/// <summary>
		///   Visits the hierarchy of components in pre-order, executing the <paramref name="action" /> for each one.
		/// </summary>
		/// <param name="model">The model whose components should be visited.</param>
		/// <param name="action">The action that should be executed for each component.</param>
		public static void VisitPreOrder(this Model model, Action<IComponent> action)
		{
			Requires.NotNull(model, nameof(model));

			foreach (var component in model)
				component.VisitPreOrder(action);
		}

		/// <summary>
		///   Visits the hierarchy of components in post-order, executing the <paramref name="action" /> for each one.
		/// </summary>
		/// <param name="model">The model whose components should be visited.</param>
		/// <param name="action">The action that should be executed for each component.</param>
		public static void VisitPostOrder(this Model model, Action<IComponent> action)
		{
			Requires.NotNull(model, nameof(model));

			foreach (var component in model)
				component.VisitPostOrder(action);
		}
	}
}