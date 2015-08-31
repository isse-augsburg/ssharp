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

namespace SafetySharp.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	/// <summary>
	///   Provides extension methods for reflection scenarios.
	/// </summary>
	internal static class ReflectionExtensions
	{
		/// <summary>
		///   Gets all members of the <paramref name="type" /> recursively, going up the inheritance chain.
		/// </summary>
		/// <remarks>
		///   Unfortunately, the reflection APIs do not return private members of base classes, even with
		///   <see cref="BindingFlags.FlattenHierarchy" />, hence this helper method.
		/// </remarks>
		/// <param name="type">The type the members should be retrieved from.</param>
		/// <param name="inheritanceRoot">The first base type of <paramref name="type" /> whose members should be ignored.</param>
		/// <param name="selector">The selector that should be used to select the members from the <paramref name="type" />.</param>
		private static IEnumerable<MemberInfo> GetMembers(this Type type, Type inheritanceRoot,
														  Func<Type, BindingFlags, IEnumerable<MemberInfo>> selector)
		{
			if (type.BaseType != null && type.BaseType != inheritanceRoot)
			{
				foreach (var member in GetMembers(type.BaseType, inheritanceRoot, selector))
					yield return member;
			}

			var flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
			foreach (var member in selector(type, flags))
				yield return member;
		}

		/// <summary>
		///   Gets all fields declared by <paramref name="type" /> or one of its base types up to <paramref name="inheritanceRoot" />.
		/// </summary>
		/// <param name="type">The type the fields should be retrieved from.</param>
		/// <param name="inheritanceRoot">The first base type of <paramref name="type" /> whose fields should be ignored.</param>
		public static IEnumerable<FieldInfo> GetFields(this Type type, Type inheritanceRoot)
		{
			Requires.NotNull(type, nameof(type));
			Requires.NotNull(inheritanceRoot, nameof(inheritanceRoot));

			return type.GetMembers(inheritanceRoot, (t, b) => t.GetFields(b)).Cast<FieldInfo>();
		}

		/// <summary>
		///   Gets all properties declared by <paramref name="type" /> or one of its base types up to
		///   <paramref name="inheritanceRoot" />.
		/// </summary>
		/// <param name="type">The type the properties should be retrieved from.</param>
		/// <param name="inheritanceRoot">The first base type of <paramref name="type" /> whose properties should be ignored.</param>
		public static IEnumerable<PropertyInfo> GetProperties(this Type type, Type inheritanceRoot)
		{
			Requires.NotNull(type, nameof(type));
			Requires.NotNull(inheritanceRoot, nameof(inheritanceRoot));

			return type.GetMembers(inheritanceRoot, (t, b) => t.GetProperties(b)).Cast<PropertyInfo>();
		}

		/// <summary>
		///   Gets all methods declared by <paramref name="type" /> or one of its base types up to
		///   <paramref name="inheritanceRoot" />.
		/// </summary>
		/// <param name="type">The type the methods should be retrieved from.</param>
		/// <param name="inheritanceRoot">The first base type of <paramref name="type" /> whose methods should be ignored.</param>
		public static IEnumerable<MethodInfo> GetMethods(this Type type, Type inheritanceRoot)
		{
			Requires.NotNull(type, nameof(type));
			Requires.NotNull(inheritanceRoot, nameof(inheritanceRoot));

			return type.GetMembers(inheritanceRoot, (t, b) => t.GetMethods(b)).Cast<MethodInfo>();
		}

		/// <summary>
		///   Gets a value indicating whether the <paramref name="member" /> is marked with an instance an attribute of type
		///   <typeparamref name="T" />.
		/// </summary>
		/// <typeparam name="T">The type of the attribute that should be checked for.</typeparam>
		/// <param name="member">The member that should be checked.</param>
		public static bool HasAttribute<T>(this MemberInfo member)
			where T : Attribute
		{
			Requires.NotNull(member, nameof(member));
			return member.GetCustomAttribute<T>() != null;
		}
	}
}