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

namespace SafetySharp.CompilerServices
{
	using System;
	using System.Linq;
	using System.Reflection;
	using Modeling;
	using Utilities;
	using ISSE.SafetyChecking.Utilities;

	/// <summary>
	///   Represents a reference to a <see cref="TargetObject" /> port.
	/// </summary>
	[Hidden, NonDiscoverable]
	public sealed class PortReference
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="targetObject">The target object that declares the referenced port.</param>
		/// <param name="declaringType">The type that declares the referenced port.</param>
		/// <param name="portName">The name of the referenced port.</param>
		/// <param name="argumentTypes">The argument types of the referenced port.</param>
		/// <param name="returnType">The return type of the referenced port.</param>
		/// <param name="isVirtualCall">Indicates whether the referenced port is invoked virtually or non-virtually.</param>
		public PortReference(object targetObject, Type declaringType, string portName, Type[] argumentTypes, Type returnType, bool isVirtualCall)
		{
			Requires.NotNull(targetObject, nameof(targetObject));
			Requires.NotNull(declaringType, nameof(declaringType));
			Requires.NotNullOrWhitespace(portName, nameof(portName));
			Requires.NotNull(argumentTypes, nameof(argumentTypes));
			Requires.NotNull(returnType, nameof(returnType));

			TargetObject = targetObject;
			DeclaringType = declaringType;
			PortName = portName;
			ArgumentTypes = argumentTypes;
			ReturnType = returnType;
			IsVirtualCall = isVirtualCall;
		}

		/// <summary>
		///   Gets the target object that declares the referenced port.
		/// </summary>
		public object TargetObject { get; }

		/// <summary>
		///   Gets the type that declares the referenced port.
		/// </summary>
		public Type DeclaringType { get; }

		/// <summary>
		///   Gets the name of the referenced port.
		/// </summary>
		public string PortName { get; }

		/// <summary>
		///   Gets the argument types of the referenced port.
		/// </summary>
		public Type[] ArgumentTypes { get; }

		/// <summary>
		///   Gets the return type of the referenced port.
		/// </summary>
		public Type ReturnType { get; }

		/// <summary>
		///   Gets a value indicating whether the referenced port is invoked virtually or non-virtually.
		/// </summary>
		public bool IsVirtualCall { get; }

		/// <summary>
		///   Gets the <see cref="MethodInfo" /> of the referenced port.
		/// </summary>
		internal MethodInfo GetMethod()
		{
			var method = DeclaringType
				.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
				.SingleOrDefault(m =>
					m.Name == PortName &&
					m.ReturnType == ReturnType &&
					m.GetParameters().Select(p => p.ParameterType).SequenceEqual(ArgumentTypes));

			Requires.That(method != null,
				$"'{DeclaringType.FullName}' does not declare an instance method called '{PortName}' with the given signature.");

			// If the method is an interface method, we have to determine the actual method on the target object
			// that will be invoked, otherwise we wouldn't be able to find the binding field of required ports, for instance
			if (method.DeclaringType.IsInterface)
				return TargetObject.GetType().ResolveImplementingMethod(method);

			return method;
		}

		/// <summary>
		///   Creates a <see cref="Delegate" /> of the given <paramref name="delegateType" /> that can be used to invoke
		///   the referenced port.
		/// </summary>
		/// <param name="delegateType">The type of the delegate that should be created.</param>
		internal Delegate CreateDelegate(Type delegateType)
		{
			Requires.NotNull(delegateType, nameof(delegateType));
			return delegateType.CreateDelegateInstance(TargetObject, GetMethod(), IsVirtualCall);
		}
	}
}