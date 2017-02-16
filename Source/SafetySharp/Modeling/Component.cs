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
	using System.Diagnostics;
	using CompilerServices;
	using ISSE.SafetyChecking.Utilities;
	using Utilities;

	/// <summary>
	///   Represents a S# component.
	/// </summary>
	public abstract partial class Component : IComponent, IInitializable
	{
		/// <summary>
		///   Gets the actual type of the fault effect.
		/// </summary>
#if !DEBUG
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
#endif
		[Hidden]
		internal Type FaultEffectType { get; set; }

		/// <summary>
		///   Gets the fault effects that affect the component.
		/// </summary>
#if !DEBUG
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
#endif
		[Hidden, NonDiscoverable]
		internal List<Component> FaultEffects { get; } = new List<Component>();

		/// <summary>
		///   Gets the state constraints defined over the component.
		/// </summary>
#if !DEBUG
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
#endif
		[Hidden, NonDiscoverable]
		internal List<Func<bool>> StateConstraints { get; } = new List<Func<bool>>();

		/// <summary>
		///   Gets the original types of the fault effects that affect the component.
		/// </summary>
#if !DEBUG
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
#endif
		[NonSerializable]
		internal List<Type> FaultEffectTypes { get; } = new List<Type>();

		/// <summary>
		///   Updates the state of the component.
		/// </summary>
		public virtual void Update()
		{
		}

		/// <summary>
		///   Performs the runtime initialization.
		/// </summary>
		void IInitializable.Initialize()
		{
			Initialize();
		}

		/// <summary>
		///   Invoked when the component should initialize itself, possibly nondeterministically.
		/// </summary>
		protected internal virtual void Initialize()
		{
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
		///   Adds the state <paramref name="constraint" /> to the model. All states in which the constraint is violated, i.e.,
		///   <c>false</c>, are not considered during model checking. State constraints can lead to deadlock states with no outgoing
		///   transitions; such states are reported as errors during model checking.
		/// </summary>
		/// <param name="constraint">The state constraint that should be added.</param>
		protected void AddStateConstraint(Func<bool> constraint)
		{
			Requires.NotNull(constraint, nameof(constraint));
			StateConstraints.Add(constraint);
		}
	}
}