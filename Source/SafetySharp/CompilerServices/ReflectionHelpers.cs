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
	///   Provides helper methods for reflection scenarios.
	/// </summary>
	public static class ReflectionHelpers
	{
		private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

		/// <summary>
		///   Gets the instance field called <paramref name="fieldName" /> of type <paramref name="fieldType" /> declared by the
		///   <paramref name="declaringType" />.
		/// </summary>
		/// <param name="declaringType">The type that declares the field.</param>
		/// <param name="fieldType">The type of the field.</param>
		/// <param name="fieldName">The name of the field.</param>
		public static FieldInfo GetField(Type declaringType, Type fieldType, string fieldName)
		{
			Requires.NotNull(declaringType, nameof(declaringType));
			Requires.NotNull(fieldType, nameof(fieldType));
			Requires.NotNullOrWhitespace(fieldName, nameof(fieldName));

			var field = declaringType
				.GetFields(Flags)
				.SingleOrDefault(f => f.Name == fieldName && f.FieldType == fieldType);

			Requires.That(field != null, nameof(fieldName),
				$"'{declaringType.FullName}' does not declare an instance field called '{fieldName}' of type '{fieldType}'.");

			return field;
		}

		/// <summary>
		///   Gets the instance property called <paramref name="propertyName" /> of type <paramref name="propertyType" /> declared by
		///   the <paramref name="declaringType" />.
		/// </summary>
		/// <param name="declaringType">The type that declares the property.</param>
		/// <param name="propertyType">The type of the property.</param>
		/// <param name="propertyName">The name of the property.</param>
		public static PropertyInfo GetProperty(Type declaringType, Type propertyType, string propertyName)
		{
			Requires.NotNull(declaringType, nameof(declaringType));
			Requires.NotNull(propertyType, nameof(propertyType));
			Requires.NotNullOrWhitespace(propertyName, nameof(propertyName));

			var property = declaringType
				.GetProperties(Flags)
				.SingleOrDefault(p => p.Name == propertyName && p.PropertyType == propertyType);

			Requires.That(property != null, nameof(propertyName),
				$"'{declaringType.FullName}' does not declare an instance property called '{propertyName}' of type '{propertyType}'.");

			return property;
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
				.GetMethods(Flags)
				.SingleOrDefault(m =>
					m.Name == methodName &&
					m.ReturnType == returnType &&
					m.GetParameters().Select(p => p.ParameterType).SequenceEqual(argumentTypes));

			Requires.That(method != null,
				"'{declaringType.FullName}' does not declare an instance method called '{methodName}' with the given signature.");

			return method;
		}
	}
}