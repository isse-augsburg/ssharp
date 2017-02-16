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
	using System.Collections.Generic;
	using System.Linq;
	using ISSE.SafetyChecking.Modeling;
	using ISSE.SafetyChecking.Utilities;
	using Utilities;

	/// <summary>
	///   Provides helper methods for working with <see cref="Fault" /> instances.
	/// </summary>
	public static class FaultExtensions
	{
		/// <summary>
		///   Suppresses all activations of the <paramref name="faults" />.
		/// </summary>
		public static void SuppressActivations(this IEnumerable<Fault> faults)
		{
			Requires.NotNull(faults, nameof(faults));

			foreach (var fault in faults)
				fault.Activation = Activation.Suppressed;
		}

		/// <summary>
		///   Forces the activations of the <paramref name="faults" />, i.e., whenever the faults can be activated, they are activated.
		/// </summary>
		public static void ForceActivations(this IEnumerable<Fault> faults)
		{
			Requires.NotNull(faults, nameof(faults));

			foreach (var fault in faults)
				fault.Activation = Activation.Forced;
		}

		/// <summary>
		///   Makes the activations of the <paramref name="faults" /> nondeterministic.
		/// </summary>
		public static void MakeNondeterministic(this IEnumerable<Fault> faults)
		{
			Requires.NotNull(faults, nameof(faults));

			foreach (var fault in faults)
				fault.Activation = Activation.Nondeterministic;
		}

		/// <summary>
		///   Suppresses all activations of the <paramref name="fault" />.
		/// </summary>
		public static void SuppressActivation(this Fault fault)
		{
			Requires.NotNull(fault, nameof(fault));
			fault.Activation = Activation.Suppressed;
		}

		/// <summary>
		///   Forces the activations of the <paramref name="fault" />, i.e., whenever the fault can be activated, it is indeed
		///   activated.
		/// </summary>
		public static void ForceActivation(this Fault fault)
		{
			Requires.NotNull(fault, nameof(fault));
			fault.Activation = Activation.Forced;
		}

		/// <summary>
		///   Makes the activation of the <paramref name="fault" /> nondeterministic.
		/// </summary>
		public static void MakeNondeterministic(this Fault fault)
		{
			Requires.NotNull(fault, nameof(fault));
			fault.Activation = Activation.Nondeterministic;
		}

		/// <summary>
		///   Toggles the <paramref name="fault" />'s <see cref="Activation" /> between <see cref="Activation.Forced" /> and
		///   <see cref="Activation.Suppressed" />, with <see cref="Activation.Nondeterministic" /> being
		///   treated as <see cref="Activation.Suppressed" />. This method should not be used while model checking.
		/// </summary>
		public static void ToggleActivationMode(this Fault fault)
		{
			fault.Activation = fault.Activation == Activation.Forced ? Activation.Suppressed : Activation.Forced;
		}

		/// <summary>
		///   Adds fault effects to <paramref name="fault" /> for the <paramref name="components" /> that are enabled when the fault is activated.
		/// </summary>
		/// <typeparam name="TFaultEffect">The type of the fault effect that should be added.</typeparam>
		/// <param name="components">The components the fault effects are added for.</param>
		/// <param name="fault">The fault.</param>
		public static void AddEffects<TFaultEffect>(this Fault fault, params IComponent[] components)
			where TFaultEffect : Component, new()
		{
			fault.AddEffects<TFaultEffect>((IEnumerable<IComponent>)components);
		}

		/// <summary>
		///   Adds fault effects to <paramref name="fault" /> for the <paramref name="components" /> that are enabled when the fault is activated.
		/// </summary>
		/// <typeparam name="TFaultEffect">The type of the fault effect that should be added.</typeparam>
		/// <param name="components">The components the fault effects are added for.</param>
		/// <param name="fault">The fault.</param>
		public static void AddEffects<TFaultEffect>(this Fault fault, IEnumerable<IComponent> components)
			where TFaultEffect : Component, new()
		{
			foreach (var component in components)
				fault.AddEffect<TFaultEffect>(component);
		}

		/// <summary>
		///   Adds a fault effect to <paramref name="fault" /> for the <paramref name="component" /> that is enabled when the fault is activated. Returns the fault
		///   effect instance that was added.
		/// </summary>
		/// <typeparam name="TFaultEffect">The type of the fault effect that should be added.</typeparam>
		/// <param name="component">The component the fault effect is added for.</param>
		/// <param name="fault">The fault.</param>
		public static TFaultEffect AddEffect<TFaultEffect>(this Fault fault, IComponent component)
			where TFaultEffect : Component, new()
		{
			return (TFaultEffect)fault.AddEffect(component, typeof(TFaultEffect));
		}

		/// <summary>
		///   Adds a fault effect to <paramref name="fault" /> for the <paramref name="component" /> that is enabled when the fault is activated. Returns the fault
		///   effect instance that was added.
		/// </summary>
		/// <param name="component">The component the fault effect is added for.</param>
		/// <param name="faultEffectType">The type of the fault effect that should be added.</param>
		/// <param name="fault">The fault.</param>
		public static IComponent AddEffect(this Fault fault, IComponent component, System.Type faultEffectType)
		{
			Requires.NotNull(component, nameof(component));
			Requires.That(faultEffectType.HasAttribute<FaultEffectAttribute>(),
				$"Expected fault effect '{faultEffectType.FullName}' to be marked with '{typeof(FaultEffectAttribute).FullName}'.");
			Requires.That(((Component)component).FaultEffectTypes.SingleOrDefault(type => type == faultEffectType) == null,
				$"A fault effect of type '{faultEffectType.FullName}' has already been added.");

			var faultEffect = (Component)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(component.GetRuntimeType());

			faultEffect.FaultEffectType = faultEffectType;
			faultEffect.SetFault(fault);
			((Component)component).FaultEffects.Add(faultEffect);
			((Component)component).FaultEffectTypes.Add(faultEffectType);

			return faultEffect;
		}
	}
}