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
	using System.Runtime.Serialization;
	using Utilities;

	/// <summary>
	///   Represents a base class for all faults affecting the behavior of <see cref="Component" />s.
	/// </summary>
	public abstract class Fault
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		protected Fault()
		{
		}

		/// <summary>
		///   Gets or sets a value indicating whether the fault is currently occurring.
		/// </summary>
		public bool IsOccurring { get; protected set; }

		/// <summary>
		///   Gets or sets a value indicating whether the fault is ignored for a simulation or model checking run.
		/// </summary>
		internal bool IsIgnored { get; set; }

		/// <summary>
		///   Gets the <see cref="Choice" /> instance that can be used to determine whether the fault occurs.
		/// </summary>
		protected Choice Choice { get; } = new Choice();

		/// <summary>
		///   Adds a fault effect for the <paramref name="component" /> that is enabled when the fault occurs.
		/// </summary>
		/// <typeparam name="TFaultEffect">The type of the fault effect that is added.</typeparam>
		/// <param name="component">The component the fault effect is added for.</param>
		public TFaultEffect AddEffect<TFaultEffect>(IComponent component)
			where TFaultEffect : class, new()
		{
			Requires.NotNull(component, nameof(component));
			Requires.That(typeof(TFaultEffect).HasAttribute<FaultEffectAttribute>(),
				$"Expected fault effect to be marked with '{typeof(FaultEffectAttribute)}'.");

			var faultEffect = (TFaultEffect)FormatterServices.GetUninitializedObject(typeof(TFaultEffect));
			var effect = ((IFaultEffect)faultEffect);

			Requires.That(effect.Component == null, nameof(faultEffect), "Fault effects cannot be used with multiple components at the same time.");

			effect.Component = (Component)component;
			effect.Fault = this;
			effect.Component.FaultEffects.Add(effect);

			return faultEffect;
		}

		/// <summary>
		///   Removes the <paramref name="faultEffect" /> from the fault.
		/// </summary>
		/// <param name="faultEffect">The fault effect that should be removed.</param>
		public void RemoveEffect(object faultEffect)
		{
			Requires.NotNull(faultEffect, nameof(faultEffect));

			var effect = faultEffect as IFaultEffect;
			if (effect?.Component == null || !effect.Component.FaultEffects.Remove(effect))
				return;

			effect.Component = null;
			effect.Fault = null;
		}

		/// <summary>
		///   Updates the state of the fault.
		/// </summary>
		public virtual void Update()
		{
		}
	}
}