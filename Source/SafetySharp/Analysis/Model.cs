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

namespace SafetySharp.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using Modeling;
	using Runtime;
	using Runtime.Reflection;
	using Runtime.Serialization;
	using Utilities;

	/// <summary>
	///   Represents a model consisting of several root <see cref="IComponent" /> instances.
	/// </summary>
	public sealed class Model : List<IComponent>
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="components">The components the model should consist of.</param>
		internal Model(params IComponent[] components)
			: this((IEnumerable<IComponent>)components)
		{
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="components">The components the model should consist of.</param>
		internal Model(IEnumerable<IComponent> components)
		{
			Requires.NotNull(components, nameof(components));

			AddRange(components);
			Requires.That(Count != 0, "Expected at least one component.");
		}

		/// <summary>
		///   Creates a <see cref="Model" /> instance for the <paramref name="obj" />. The object must have at least one member marked
		///   with <see cref="RootAttribute" />.
		/// </summary>
		/// <param name="obj">The object the model should be created for.</param>
		public static Model Create(object obj)
		{
			Requires.NotNull(obj, nameof(obj));

			const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var components = new HashSet<IComponent>(ReferenceEqualityComparer<IComponent>.Default);
			var roles = new Dictionary<IComponent, Role>(ReferenceEqualityComparer<IComponent>.Default);

			CollectComponents(components, roles, obj.GetType().GetFields(bindingFlags), info => info.FieldType, info => info.GetValue(obj));
			CollectComponents(components, roles, obj.GetType().GetProperties(bindingFlags), info => info.PropertyType, info => info.GetValue(obj));
			CollectComponents(components, roles, obj.GetType().GetMethods(bindingFlags), info => info.ReturnType, info =>
			{
				if (info.GetParameters().Length == 0)
					return info.Invoke(obj, new object[0]);

				return null;
			});

			if (components.Count == 0)
			{
				throw new InvalidOperationException(
					$"Unable to determine root blocks. At least one property, field, or method must be marked with {typeof(RootAttribute).FullName}");
			}

			return new Model(components.OrderByDescending(component => roles[component]));
		}

		/// <summary>
		///   Creates a <see cref="RuntimeModel" /> instance from the model and the <paramref name="formulas" />.
		/// </summary>
		/// <param name="formulas">The formulas the model should be able to check.</param>
		internal RuntimeModel ToRuntimeModel(params Formula[] formulas)
		{
			Requires.NotNull(formulas, nameof(formulas));

			using (var memoryStream = new MemoryStream())
			{
				RuntimeModelSerializer.Save(memoryStream, this, formulas);

				memoryStream.Seek(0, SeekOrigin.Begin);
				return RuntimeModelSerializer.Load(memoryStream);
			}
		}

		/// <summary>
		///   Binds all automatically bound fault effects to their respective faults.
		/// </summary>
		internal void BindFaultEffects()
		{
			this.VisitPostOrder(component =>
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
			});
		}

		/// <summary>
		///   Initializes all bindings.
		/// </summary>
		internal void CreateBindings()
		{
			this.VisitPostOrder(component => ((Component)component).CreateBindings());
		}

		/// <summary>
		///   Helper method that collects <see cref="IComponent" /> instances.
		/// </summary>
		private static void CollectComponents<T>(HashSet<IComponent> components, Dictionary<IComponent, Role> roles, IEnumerable<T> members,
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
						roles.Add(value, rootAttribute.Role);
				}
				else if (typeof(IEnumerable<IComponent>).IsAssignableFrom(memberType))
				{
					var values = (IEnumerable<IComponent>)getValue(member);
					if (values == null)
						continue;

					foreach (var value in values.Where(value => value != null))
					{
						if (components.Add(value))
							roles.Add(value, rootAttribute.Role);
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
	}
}