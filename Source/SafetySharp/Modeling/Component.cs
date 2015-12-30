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
	using Utilities;

	/// <summary>
	///   Represents a S# component.
	/// </summary>
	public abstract partial class Component : IComponent
	{
		[Hidden, NonDiscoverable, DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly List<Component> _faultEffects = new List<Component>();

		[Hidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Type _faultEffectType;

		/// <summary>
		///   Gets the actual type of the fault effect.
		/// </summary>
#if !DEBUG
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
#endif
		internal Type FaultEffectType
		{
			get { return _faultEffectType; }
			set { _faultEffectType = value; }
		}

		/// <summary>
		///   Gets the fault effects that affect the component.
		/// </summary>
#if !DEBUG
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
#endif
		internal List<Component> FaultEffects => _faultEffects;

		/// <summary>
		///   Updates the state of the component.
		/// </summary>
		public virtual void Update()
		{
		}

		/// <summary>
		///   Invoked when the component should initialize bindings between itself and its subcomponents.
		/// </summary>
		protected internal virtual void CreateBindings()
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
	}
}