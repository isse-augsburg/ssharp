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
		public Model(params IComponent[] components)
			: this((IEnumerable<IComponent>)components)
		{
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="components">The components the model should consist of.</param>
		public Model(IEnumerable<IComponent> components)
		{
			Requires.NotNull(components, nameof(components));

			AddRange(components);
			Assert.That(Count != 0, "Expected at least one component.");
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

			CollectComponents(components, obj.GetType().GetFields(bindingFlags), info => info.FieldType, info => info.GetValue(obj));
			CollectComponents(components, obj.GetType().GetProperties(bindingFlags), info => info.PropertyType, info => info.GetValue(obj));
			CollectComponents(components, obj.GetType().GetMethods(bindingFlags), info => info.ReturnType, info =>
			{
				if (info.GetParameters().Length == 0)
					return info.Invoke(obj, new object[0]);

				return null;
			});

			return new Model(components);
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
		///   Validates the model, checking for anything unsupported.
		/// </summary>
		internal void Validate()
		{
			if (this.GetFaults().Any(fault => !fault.IndependentOccurrence))
				throw new NotSupportedException("Faults that cannot occur independently are currently unsupported.");
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
						break;

					var faultEffects = ((Component)component).FaultEffects;
					if (!String.IsNullOrWhiteSpace(attribute.Fault) && faultEffects.All(f => f.GetType() != type))
					{
						const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
						var fault =
							component.GetType().GetField(attribute.Fault, flags)?.GetValue(component) as Fault ??
							component.GetType().GetProperty(attribute.Fault, flags)?.GetMethod?.Invoke(component, null) as Fault;

						if (fault == null)
						{
							throw new InvalidOperationException(
								$"'{component.GetType().FullName}' does not declare a field or property " +
								$"called '{attribute.Fault}' of type '{typeof(Fault).FullName}' (or a type derived from it) " +
								$"that contains a valid fault instance as expected by '{type.FullName}'.");
						}

						fault.AddEffect(component, type);
					}

					type = type.BaseType;
				}
			});
		}

		/// <summary>
		///   Helper method that collects <see cref="IComponent" /> instances.
		/// </summary>
		private static void CollectComponents<T>(HashSet<IComponent> components, IEnumerable<T> members, Func<T, Type> getMemberType,
												 Func<T, object> getValue)
		{
			foreach (var member in members)
			{
				var memberType = getMemberType(member);
				if (typeof(IComponent).IsAssignableFrom(memberType))
				{
					var value = (IComponent)getValue(member);
					if (value != null)
						components.Add(value);
				}

				if (!typeof(IEnumerable<IComponent>).IsAssignableFrom(memberType))
					continue;

				var values = (IEnumerable<IComponent>)getValue(member);
				if (values == null)
					continue;

				foreach (var value in values.Where(value => value != null))
					components.Add(value);
			}
		}
	}
}