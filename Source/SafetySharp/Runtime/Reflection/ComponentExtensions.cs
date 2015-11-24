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

namespace SafetySharp.Runtime.Reflection
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using Modeling;
	using Serialization;
	using Utilities;

	/// <summary>
	///   Allows access to the metadata of <see cref="Component" /> and <see cref="IComponent" /> instances and provides convenience
	///   methods for working with the metadata.
	/// </summary>
	public static class ComponentExtensions
	{
		/// <summary>
		///   Gets the <paramref name="component" />'s subcomponents.
		/// </summary>
		/// <param name="component">The component the subcomponents should be returned for.</param>
		public static IEnumerable<IComponent> GetSubcomponents(this IComponent component)
		{
			Requires.NotNull(component, nameof(component));

			var subcomponents = new HashSet<IComponent>();
			GetSubcomponents(subcomponents, component);

			// Some objects may have backward references to the component itself, so we have to make sure that we
			// don't include the component in its set of subcomponents
			subcomponents.Remove(component);
			return subcomponents;
		}

		/// <summary>
		///   Gets the <paramref name="component" />'s state fields, i.e., those fields that actually contribute to the state space of a
		///   model and whose values are preserved between different system steps.
		/// </summary>
		/// <param name="component">The component the state fields should be returned for.</param>
		public static IEnumerable<FieldInfo> GetStateFields(this IComponent component)
		{
			Requires.NotNull(component, nameof(component));

			return from field in component.GetType().GetFields(typeof(Component))
				   where !field.IsStatic && !field.IsLiteral && !field.IsHidden(SerializationMode.Optimized, false)
						 && !typeof(IComponent).IsAssignableFrom(field.FieldType)
				   select field;
		}

		/// <summary>
		///   Gets the <see cref="FieldInfo" /> storing the <see cref="Fault" /> the <paramref name="faultEffect" /> belongs to.
		/// </summary>
		/// <param name="faultEffect">The fault effect whose <see cref="Fault" /> <see cref="FieldInfo" /> should be returned.</param>
		public static FieldInfo GetFaultField(this IComponent faultEffect)
		{
			Requires.NotNull(faultEffect, nameof(faultEffect));
			Requires.That(faultEffect.GetType().HasAttribute<FaultEffectAttribute>(), nameof(faultEffect), "Expected a fault effect.");

			var faultField = faultEffect.GetType().GetField("__fault__", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.NotNull(faultField, $"Unable to determine fault field of fault effect '{faultEffect.GetType().FullName}'.");

			return faultField;
		}

		/// <summary>
		///   Gets the <see cref="Fault" /> the <paramref name="faultEffect" /> belongs to.
		/// </summary>
		/// <param name="faultEffect">The fault effect whose <see cref="Fault" /> should be returned.</param>
		public static Fault GetFault(this IComponent faultEffect)
		{
			return (Fault)faultEffect.GetFaultField().GetValue(faultEffect);
		}

		/// <summary>
		///   Sets the <see cref="Fault" /> the <paramref name="faultEffect" /> belongs to.
		/// </summary>
		/// <param name="faultEffect">The fault effect whose <see cref="Fault" /> should be set.</param>
		/// <param name="fault">The fault the fault effect should belong to.</param>
		public static void SetFault(this IComponent faultEffect, Fault fault)
		{
			faultEffect.GetFaultField().SetValue(faultEffect, fault);
		}

		/// <summary>
		///   Gets the <paramref name="component" />'s runtime type.
		/// </summary>
		/// <param name="component">The component the runtime type should be returned for.</param>
		public static Type GetRuntimeType(this IComponent component)
		{
			var runtimeTypeField = component.GetType().GetField("__runtimeType__", BindingFlags.Static | BindingFlags.NonPublic);
			Assert.NotNull(runtimeTypeField, $"Unable to determine runtime type of component '{component.GetType().FullName}'.");

			return (Type)runtimeTypeField.GetValue(null);
		}

		/// <summary>
		///   Starting with <paramref name="component" />, visits the hierarchy of components in pre-order, executing the
		///   <paramref name="action" /> for each one.
		/// </summary>
		/// <param name="component">The root component that should be visited.</param>
		/// <param name="action">The action that should be executed for each component.</param>
		public static void VisitPreOrder(this IComponent component, Action<IComponent> action)
		{
			Requires.NotNull(component, nameof(component));
			Requires.NotNull(action, nameof(action));

			var visitedComponents = new HashSet<IComponent>();
			VisitPreOrder(visitedComponents, component, action);
		}

		/// <summary>
		///   Starting with <paramref name="component" />, visits the hierarchy of components in post-order, executing the
		///   <paramref name="action" /> for each one.
		/// </summary>
		/// <param name="component">The root component that should be visited.</param>
		/// <param name="action">The action that should be executed for each component.</param>
		public static void VisitPostOrder(this IComponent component, Action<IComponent> action)
		{
			Requires.NotNull(component, nameof(component));
			Requires.NotNull(action, nameof(action));

			var visitedComponents = new HashSet<IComponent>();
			VisitPostOrder(visitedComponents, component, action);
		}

		/// <summary>
		///   Visits the <paramref name="component" /> and all of its subcomponents in pre-order, executing the
		///   <paramref name="action" /> for each component.
		/// </summary>
		/// <param name="visitedComponents">The components that have already been visited, in case the hierarchy contains any cycles.</param>
		/// <param name="component">The component that should be visited.</param>
		/// <param name="action">The action that should be executed for each component.</param>
		private static void VisitPreOrder(HashSet<IComponent> visitedComponents, IComponent component, Action<IComponent> action)
		{
			if (!visitedComponents.Add(component))
				return;

			action(component);

			foreach (var subcomponent in component.GetSubcomponents())
				VisitPreOrder(visitedComponents, subcomponent, action);
		}

		/// <summary>
		///   Visits the <paramref name="component" /> and all of its subcomponents in post-order, executing the
		///   <paramref name="action" /> for each component.
		/// </summary>
		/// <param name="visitedComponents">The components that have already been visited, in case the hierarchy contains any cycles.</param>
		/// <param name="component">The component that should be visited.</param>
		/// <param name="action">The action that should be executed for each component.</param>
		private static void VisitPostOrder(HashSet<IComponent> visitedComponents, IComponent component, Action<IComponent> action)
		{
			if (!visitedComponents.Add(component))
				return;

			foreach (var subcomponent in component.GetSubcomponents())
				VisitPostOrder(visitedComponents, subcomponent, action);

			action(component);
		}

		/// <summary>
		///   Adds all subcomponents referenced by <paramref name="obj" />, excluding <paramref name="obj" /> itself, to the set of
		///   <paramref name="subcomponents" />.
		/// </summary>
		/// <param name="subcomponents">The set of referenced objects.</param>
		/// <param name="obj">The object the referenced objects should be returned for.</param>
		private static void GetSubcomponents(HashSet<IComponent> subcomponents, object obj)
		{
			foreach (var referencedObject in SerializationRegistry.Default.GetSerializer(obj).GetReferencedObjects(obj, SerializationMode.Full))
			{
				if (referencedObject == null)
					continue;

				var component = referencedObject as IComponent;
				if (component != null)
				{
					if (!component.GetType().HasAttribute<FaultEffectAttribute>())
						subcomponents.Add(component);
				}
				else
					GetSubcomponents(subcomponents, referencedObject);
			}
		}
	}
}