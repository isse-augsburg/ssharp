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
	using System.Runtime.Serialization;
	using CompilerServices;
	using Runtime.Reflection;
	using Utilities;

	/// <summary>
	///   Represents a base class for all faults affecting the behavior of <see cref="Component" />s.
	/// </summary>
	public abstract class Fault
	{
		private readonly Choice _choice = new Choice();

		[Hidden]
		private Activation _activation = Activation.Nondeterministic;

		[NonSerializable]
		private bool _activationIsUnknown;

		[NonSerializable]
		private bool _canUndoActivation;

		[NonSerializable]
		private int _choiceIndex;

		private bool _isActivated;

		[NonSerializable]
		private string _name = "<Unnamed>";

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		internal Fault()
		{
		}

		/// <summary>
		///   Gets a value indicating whether the fault is activated and has some effect on the state of the system, therefore inducing
		///   an error or possibly a failure.
		/// </summary>
		// ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
		public bool IsActivated => _isActivated;

		/// <summary>
		///   Gets or sets the fault's name.
		/// </summary>
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		/// <summary>
		///   Gets or sets the fault's forced occurrence kind.
		/// </summary>
		public Activation Activation
		{
			get { return _activation; }
			set
			{
				_activation = value;

				if (value == Activation.Nondeterministic)
					_isActivated = false;
				else
					_isActivated = value == Activation.Forced;
			}
		}

		/// <summary>
		///   Toggles the fault's <see cref="Activation" /> between <see cref="Modeling.Activation.Forced" /> and
		///   <see cref="Modeling.Activation.Suppressed" />, with <see cref="Modeling.Activation.Nondeterministic" /> being
		///   treated as <see cref="Modeling.Activation.Suppressed" />.
		/// </summary>
		public void ToggleActivationMode()
		{
			Activation = Activation == Activation.Forced ? Activation.Suppressed : Activation.Forced;
		}

		/// <summary>
		///   Adds a fault effect for the <paramref name="component" /> that is enabled while the fault is activated.
		/// </summary>
		/// <typeparam name="TFaultEffect">The type of the fault effect that should be added.</typeparam>
		/// <param name="component">The component the fault effect is added for.</param>
		internal TFaultEffect AddEffect<TFaultEffect>(IComponent component)
			where TFaultEffect : Component, new()
		{
			return (TFaultEffect)AddEffect(component, typeof(TFaultEffect));
		}

		/// <summary>
		///   Adds a fault effect for the <paramref name="component" /> that is enabled while the fault is activated.
		/// </summary>
		/// <param name="component">The component the fault effect is added for.</param>
		/// <param name="faultEffectType">The type of the fault effect that should be added.</param>
		internal IComponent AddEffect(IComponent component, Type faultEffectType)
		{
			Requires.NotNull(component, nameof(component));
			Requires.That(faultEffectType.HasAttribute<FaultEffectAttribute>(),
				$"Expected fault effect to be marked with '{typeof(FaultEffectAttribute)}'.");

			var faultEffect = (Component)FormatterServices.GetUninitializedObject(component.GetRuntimeType());

			faultEffect.FaultEffectType = faultEffectType;
			faultEffect.SetFault(this);
			((Component)component).FaultEffects.Add(faultEffect);

			return faultEffect;
		}

		/// <summary>
		///   Undoes the activation of the fault when the activation is known to have no observable effect and fault activation was
		///   nondeterministic in the current step.
		/// </summary>
		/// <remarks>
		///   This method is internal to simplify the public API of the class. The method is publically exposed via
		///   <see cref="FaultHelper.UndoActivation" /> for use by the S# compiler.
		/// </remarks>
		internal void UndoActivation()
		{
			if (!_canUndoActivation)
				return;

			_canUndoActivation = false;
			_activationIsUnknown = true;
			_choice.Resolver.Undo(_choiceIndex);
		}

		/// <summary>
		///   Tries to activate the fault.
		/// </summary>
		/// <remarks>
		///   This method is internal to simplify the public API of the class. The method is publically exposed via
		///   <see cref="FaultHelper.Activate" /> for use by the S# compiler.
		/// </remarks>
		internal void TryActivate()
		{
			if (!_activationIsUnknown)
				_canUndoActivation = false;
			else
			{
				switch (GetUpdatedActivationState())
				{
					case Activation.Forced:
						_isActivated = true;
						_canUndoActivation = false;
						break;
					case Activation.Suppressed:
						_isActivated = false;
						_canUndoActivation = false;
						break;
					case Activation.Nondeterministic:
						_isActivated = _choice.Choose(false, true);
						_choiceIndex = _choice.Resolver.LastChoiceIndex;
						_canUndoActivation = true;
						break;
					default:
						throw new InvalidOperationException("Unsupported fault activation.");
				}

				_activationIsUnknown = false;
			}
		}

		/// <summary>
		///   Resetse the fault's activation state for the current step.
		/// </summary>
		internal void Reset()
		{
			_activationIsUnknown = true;
			_canUndoActivation = false;
		}

		/// <summary>
		///   Gets the updated activation state of the fault.
		/// </summary>
		protected abstract Activation GetUpdatedActivationState();
	}
}