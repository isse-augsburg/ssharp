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

namespace SafetySharp.CompilerServices
{
	using System;
	using System.Linq;
	using System.Reflection;
	using Utilities;

	/// <summary>
	///   Used by the S# compiler to establish port bindings.
	/// </summary>
	public static class Binder
	{
		/// <summary>
		///   Binds the port methods of the port target objects either virtually or non-virtually.
		/// </summary>
		/// <param name="requiredPortTarget">The target object of the required port that should be bound.</param>
		/// <param name="providedPortTarget">The target object of the provided port that should be bound.</param>
		/// <param name="requiredPortMethod">The target method of the required port that should be bound.</param>
		/// <param name="providedPortMethod">The target method of the provided port that should be bound.</param>
		/// <param name="providedPortVirtual">Indicates whether the provided port method should be invoked virtually or non-virtually.</param>
		public static void Bind(object requiredPortTarget, object providedPortTarget, MethodInfo requiredPortMethod, MethodInfo providedPortMethod,
								bool providedPortVirtual)
		{
			Requires.NotNull(requiredPortTarget, nameof(requiredPortTarget));
			Requires.NotNull(requiredPortMethod, nameof(requiredPortMethod));
			Requires.NotNull(providedPortTarget, nameof(providedPortTarget));
			Requires.NotNull(providedPortMethod, nameof(providedPortMethod));

			// If the required port is an interface method, we have to determine the actual method on the target object
			// that will be invoked, otherwise we wouldn't be able to find the binding field
			if (requiredPortMethod.DeclaringType.IsInterface)
				requiredPortMethod = requiredPortTarget.GetType().ResolveImplementingMethod(requiredPortMethod);

			var bindingFieldAttribute = requiredPortMethod.GetCustomAttribute<BindingFieldAttribute>();
			Assert.NotNull(bindingFieldAttribute,
				$"Expected required port '{requiredPortMethod}' to be marked with '{typeof(BindingFieldAttribute).FullName}'.");

			var bindingField = bindingFieldAttribute.GetFieldInfo(requiredPortMethod.DeclaringType);
			var providedPortDelegate = bindingField.FieldType.CreateDelegateInstance(providedPortTarget, providedPortMethod, providedPortVirtual);

			bindingField.SetValue(requiredPortTarget, providedPortDelegate);
		}

		/// <summary>
		///   Gets the instance method called <paramref name="methodName" /> declared by the <paramref name="declaringType" />,
		///   with the signature of the method defined by the <paramref name="argumentTypes" /> and <paramref name="returnType" />.
		/// </summary>
		/// <param name="declaringType">The type that declares the method.</param>
		/// <param name="methodName">The name of the method.</param>
		/// <param name="argumentTypes">The argument types of the method.</param>
		/// <param name="returnType">The return type of the method.</param>
		public static MethodInfo GetMethod(Type declaringType, string methodName, Type[] argumentTypes, Type returnType)
		{
			Requires.NotNull(declaringType, nameof(declaringType));
			Requires.NotNullOrWhitespace(methodName, nameof(methodName));
			Requires.NotNull(argumentTypes, nameof(argumentTypes));
			Requires.NotNull(returnType, nameof(returnType));

			var method = declaringType
				.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
				.SingleOrDefault(m =>
					m.Name == methodName &&
					m.ReturnType == returnType &&
					m.GetParameters().Select(p => p.ParameterType).SequenceEqual(argumentTypes));

			Requires.That(method != null,
				$"'{declaringType.FullName}' does not declare an instance method called '{methodName}' with the given signature.");

			return method;
		}
	}
}