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
	using System.Reflection;
	using Utilities;

	/// <summary>
	///   When applied to a required port, indicates the fields that the S# compiler used to implement the port.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public sealed class BindingMetadataAttribute : Attribute
	{
		/// <summary>
		///   The name of the marked required port's binding field.
		/// </summary>
		private readonly string _bindingField;

		/// <summary>
		///   The name of the marked required port's delegate field.
		/// </summary>
		private readonly string _delegateField;

		/// <summary>
		///   The type that declares the marked required port.
		/// </summary>
		private Type _type;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="delegateField">The name of the marked required port's delegate field.</param>
		/// <param name="bindingField">The name of the marked required port's binding field.</param>
		public BindingMetadataAttribute(string delegateField, string bindingField)
		{
			Requires.NotNullOrWhitespace(delegateField, nameof(delegateField));
			Requires.NotNullOrWhitespace(bindingField, nameof(bindingField));

			_delegateField = delegateField;
			_bindingField = bindingField;
		}

		/// <summary>
		///   Gets the <see cref="FieldInfo" /> object representing the marked required port's delegate field.
		/// </summary>
		public FieldInfo DelegateField
		{
			get
			{
				var field = _type.GetField(_delegateField, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
				Requires.That(field != null, $"Unable to find binding delegate field '{_type.FullName}.{_delegateField}'.");

				return field;
			}
		}

		/// <summary>
		///   Gets the <see cref="FieldInfo" /> object representing the marked required port's binding field.
		/// </summary>
		public FieldInfo BindingField
		{
			get
			{
				var field = _type.GetField(_bindingField, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
				Requires.That(field != null, $"Unable to find binding field '{_type.FullName}.{_bindingField}'.");

				return field;
			}
		}

		/// <summary>
		///   Gets the <see cref="BindingMetadataAttribute" /> for the <paramref name="requiredPort" />.
		/// </summary>
		/// <param name="requiredPort">The required port he metadata should be returned for.</param>
		public static BindingMetadataAttribute Get(MethodInfo requiredPort)
		{
			Requires.NotNull(requiredPort, nameof(requiredPort));

			var metadataAttribute = requiredPort.GetCustomAttribute<BindingMetadataAttribute>();
			Requires.NotNull(metadataAttribute,
				$"Expected required port '{requiredPort}' to be marked with '{typeof(BindingMetadataAttribute).FullName}'.");

			metadataAttribute._type = requiredPort.DeclaringType;
			return metadataAttribute;
		}
	}
}