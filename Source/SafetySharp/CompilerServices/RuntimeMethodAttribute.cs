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
	using System.Reflection;
	using Utilities;

	/// <summary>
	///   When applied to a method, indicates the field that stores the dynamically generated implementation of the method as well
	///   as the method representing the default implementation, if any.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	internal sealed class RuntimeMethodAttribute : Attribute
	{
		/// <summary>
		///   The name of the marked method's default implementation.
		/// </summary>
		private readonly string _defaultImplementation;

		/// <summary>
		///   The name of the marked component method's backing field.
		/// </summary>
		private readonly string _fieldName;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="fieldName">The name of the marked method's backing field.</param>
		/// <param name="defaultImplementation">The name of the marked method's default implementation.</param>
		internal RuntimeMethodAttribute(string fieldName, string defaultImplementation)
		{
			_fieldName = fieldName;
			_defaultImplementation = defaultImplementation;
		}

		/// <summary>
		///   Gets the <see cref="FieldInfo" /> object representing the marked method's backing field.
		/// </summary>
		/// <param name="type">The type that declares the marked method.</param>
		public FieldInfo GetBackingField(Type type)
		{
			Requires.NotNull(type, nameof(type));

			var field = type.GetField(_fieldName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
			Requires.That(field != null, $"Unable to find backing field '{type.FullName}.{_fieldName}'.");

			return field;
		}

		/// <summary>
		///   Gets the <see cref="MethodInfo" /> object representing the marked method's default implementation.
		/// </summary>
		/// <param name="type">The type that declares the marked method.</param>
		public MethodInfo GetDefaultImplementation(Type type)
		{
			Requires.NotNull(type, nameof(type));

			var method = type.GetMethod(_defaultImplementation, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
			Requires.That(method != null, $"Unable to find default implementation '{type.FullName}.{_defaultImplementation}'.");

			return method;
		}
	}
}