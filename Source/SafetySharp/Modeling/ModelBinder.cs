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
	using System.Reflection;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;

	/// <summary>
	///   A helper type that discovers the <see cref="IComponent" /> instances within a <see cref="ModelBase" /> that are
	///   marked with <see cref="RootAttribute" /> and binds the model.
	/// </summary>
	internal static class ModelBinder
	{
		/// <summary>
		///   Analyzes the <paramref name="model" /> via reflection to discover all root <see cref="IComponent" /> instances.
		/// </summary>
		/// <param name="model">The model the roots should be discovered for.</param>
		internal static void DiscoverRoots(ModelBase model)
		{
			Requires.NotNull(model, nameof(model));

			const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var components = new HashSet<IComponent>(ReferenceEqualityComparer<IComponent>.Default);
			var kinds = new Dictionary<IComponent, RootKind>(ReferenceEqualityComparer<IComponent>.Default);

			CollectRoots(components, kinds, model.GetType().GetFields(bindingFlags), info => info.FieldType,
				info => info.GetValue(model));
			CollectRoots(components, kinds, model.GetType().GetProperties(bindingFlags), info => info.PropertyType,
				info => info.CanRead ? info.GetValue(model) : null);
			CollectRoots(components, kinds, model.GetType().GetMethods(bindingFlags), info => info.ReturnType, info =>
			{
				if (info.GetParameters().Length == 0)
					return info.Invoke(model, new object[0]);

				return null;
			});

			if (components.Count == 0)
			{
				throw new InvalidOperationException(
					$"At least one property, field, or method of the model must be marked with '{typeof(RootAttribute).FullName}'.");
			}

			model.Roots = components.OrderByDescending(component => kinds[component]).ToArray();
		}

		/// <summary>
		///   A helper method that collects root <see cref="IComponent" /> instances.
		/// </summary>
		private static void CollectRoots<T>(HashSet<IComponent> components, Dictionary<IComponent, RootKind> kinds, IEnumerable<T> members,
											Func<T, Type> getMemberType, Func<T, object> getValue)
			where T : MemberInfo
		{
			foreach (var member in members)
			{
				var rootAttribute = member.GetCustomAttribute<RootAttribute>();
				if (rootAttribute == null)
					continue;

				var memberType = getMemberType(member);
				if (typeof(IComponent).IsAssignableFrom(memberType))
				{
					var value = (IComponent)getValue(member);
					if (value != null && components.Add(value))
						kinds.Add(value, rootAttribute.Kind);
				}
				else if (typeof(IEnumerable<IComponent>).IsAssignableFrom(memberType))
				{
					var values = (IEnumerable<IComponent>)getValue(member);
					if (values == null)
						continue;

					foreach (var value in values.Where(value => value != null))
					{
						if (components.Add(value))
							kinds.Add(value, rootAttribute.Kind);
					}
				}
				else
				{
					throw new InvalidOperationException(
						$"'{member.DeclaringType.FullName}.{member.Name}' is marked with '{typeof(RootAttribute).FullName}' but is not of type " +
						$"'{typeof(IComponent)}' or '{typeof(IEnumerable<IComponent>).FullName}'.");
				}
			}
		}

		/// <summary>
		///   Initializes the faults and bindings of the model.
		/// </summary>
		/// <param name="model">The model that should be bound.</param>
		internal static void Bind(ModelBase model)
		{
			Requires.NotNull(model, nameof(model));

			model.ReferencedObjects = SerializationRegistry.Default.GetReferencedObjects(model.Roots, SerializationMode.Optimized).ToArray();
			model.Components = model.ReferencedObjects.OfType<IComponent>().Distinct(ReferenceEqualityComparer<IComponent>.Default).ToArray();

			BindFaultEffects(model);
			DiscoverFaults(model);
			AssignFaultIdentifiers(model);

			Range.CopyMetadata(model);
		}

		/// <summary>
		///   Binds all automatically bound fault effects to their respective faults.
		/// </summary>
		private static void BindFaultEffects(ModelBase model)
		{
			foreach (var component in model.Components)
			{
				var type = component.GetRuntimeType();

				while (type != typeof(Component))
				{
					var attribute = type.GetCustomAttribute<FaultEffectAttribute>();
					if (attribute == null)
					{
						type = type.BaseType;
						continue;
					}

					var faultEffects = ((Component)component).FaultEffects;
					if (!String.IsNullOrWhiteSpace(attribute.Fault) && faultEffects.All(f => f.FaultEffectType != type))
					{
						var baseType = type.BaseType;
						while (baseType.HasAttribute<FaultEffectAttribute>())
							baseType = baseType.BaseType;

						var field = baseType.GetFields(typeof(object)).SingleOrDefault(f => !f.IsStatic && f.Name == attribute.Fault);
						var property = baseType.GetProperties(typeof(object))
											   .SingleOrDefault(p => p.GetMethod != null && !p.GetMethod.IsStatic && p.Name == attribute.Fault);

						if (field == null && property == null)
						{
							throw new InvalidOperationException(
								$"'{baseType.FullName}' does not declare a field or property " +
								$"called '{attribute.Fault}' of type '{typeof(Fault).FullName}' (or a type derived from it) " +
								$"that contains a valid fault instance as expected by '{type.FullName}'.");
						}

						var fault = field == null ? property.GetMethod.Invoke(component, null) as Fault : field.GetValue(component) as Fault;
						fault.AddEffect(component, type);
					}

					type = type.BaseType;
				}
			}
		}

		/// <summary>
		///   Discovers all faults contained in the model.
		/// </summary>
		private static void DiscoverFaults(ModelBase model)
		{
			var faults = new HashSet<Fault>(ReferenceEqualityComparer<Fault>.Default);
			foreach (var component in model.Components)
			{
				foreach (var faultEffect in ((Component)component).FaultEffects)
					faults.Add(faultEffect.GetFault());
			}

			model.Faults = faults.OrderBy(fault => fault.Identifier).ToArray();
		}

		/// <summary>
		///   Assigns the fault identifiers to each fault.
		/// </summary>
		private static void AssignFaultIdentifiers(ModelBase model)
		{
			for (var i = 0; i < model.Faults.Length; ++i)
				model.Faults[i].Identifier = i;
		}
	}
}