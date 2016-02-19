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

namespace SafetySharp.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Reflection.Emit;
	using System.Runtime.InteropServices;
	using Modeling;
	using Runtime.Serialization;

	/// <summary>
	///   Provides extension methods for reflection scenarios.
	/// </summary>
	internal static class ReflectionExtensions
	{
		/// <summary>
		///   The binding flags that are used to look up members.
		/// </summary>
		private const BindingFlags Flags =
			BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

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
		private static IEnumerable<MemberInfo> GetMembers(this Type type, Type inheritanceRoot, Func<Type, IEnumerable<MemberInfo>> selector)
		{
			if (type.BaseType != null && type.BaseType != inheritanceRoot)
			{
				foreach (var member in GetMembers(type.BaseType, inheritanceRoot, selector))
					yield return member;
			}

			foreach (var member in selector(type))
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

			return type.GetMembers(inheritanceRoot, t => t.GetFields(Flags)).Cast<FieldInfo>();
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

			return type.GetMembers(inheritanceRoot, t => t.GetProperties(Flags)).Cast<PropertyInfo>();
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

			return type.GetMembers(inheritanceRoot, t => t.GetMethods(Flags)).Cast<MethodInfo>();
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

		/// <summary>
		///   Checks whether <paramref name="type" /> is a primitive type.
		/// </summary>
		public static bool IsPrimitiveType(this Type type)
		{
			Requires.NotNull(type, nameof(type));
			return type.IsPrimitive || type.IsPointer || type.IsEnum;
		}

		/// <summary>
		///   Checks whether <paramref name="type" /> is a reference type, i.e., a class, delegate, or interface.
		/// </summary>
		public static bool IsReferenceType(this Type type)
		{
			Requires.NotNull(type, nameof(type));

			// We don't treat pointers as reference types; in particular, why is IsClass true for pointers?
			if (type.IsPointer)
				return false;

			return type.IsClass || type.IsInterface || type.IsSubclassOf(typeof(Delegate));
		}

		/// <summary>
		///   Checks wheter <paramref name="type" /> is a numeric type.
		/// </summary>
		internal static bool IsNumericType(this Type type)
		{
			if (type.IsEnum)
				return false;

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Char:
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		///   Checks wheter <paramref name="type" /> is an unsigned numeric type.
		/// </summary>
		internal static bool IsUnsignedNumericType(this Type type)
		{
			if (type.IsEnum)
				return false;

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Char:
				case TypeCode.Byte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return true;
				case TypeCode.Single:
				case TypeCode.Int64:
				case TypeCode.Int32:
				case TypeCode.Int16:
				case TypeCode.SByte:
				case TypeCode.Double:
					return false;
				default:
					return false;
			}
		}

		/// <summary>
		///   Gets the global C# name of <paramref name="type" />, for instance <c>global::System.Int32</c>.
		/// </summary>
		/// <param name="type">The type the global name should be returned for.</param>
		public static string GetGlobalName(this Type type)
		{
			Requires.NotNull(type, nameof(type));
			return $"global::{type.FullName}";
		}

		/// <summary>
		///   Gets the unmanaged size in bytes of the primitive or enum <paramref name="type" />.
		/// </summary>
		/// <param name="type">The type the unmanaged size should be returned for.</param>
		public static int GetUnmanagedSize(this Type type)
		{
			Requires.NotNull(type, nameof(type));
			Requires.That(type.IsEnum || type.IsPrimitive || type.IsPointer, nameof(type),
				$"Expected an enum or primitive type instead of '{type.FullName}'.");

			type = type.IsEnum ? type.GetEnumUnderlyingType() : type;
			if (type == typeof(bool))
				return 1;

			if (type == typeof(char))
				return 2;

			return Marshal.SizeOf(type);
		}

		/// <summary>
		///   Checks whether the <paramref name="member" /> is hidden in the serialization <paramref name="mode" />, depending on
		///   whether <paramref name="discoveringObjects" /> is <c>true</c>.
		/// </summary>
		/// <param name="member">The member whose visibility should be determined.</param>
		/// <param name="mode">The serialization mode the visibility should be determined for.</param>
		/// <param name="discoveringObjects">Indicates whether objects are being discovered.</param>
		public static bool IsHidden(this MemberInfo member, SerializationMode mode, bool discoveringObjects)
		{
			// Don't try to serialize members that are explicitly marked as non-serializable
			if (member.HasAttribute<NonSerializableAttribute>() || member.HasAttribute<NonSerializedAttribute>())
				return true;

			// If we're discovering objects in optimized mode and the member is explicitly marked as non-discoverable, it is hidden
			if (mode == SerializationMode.Optimized && discoveringObjects && member.HasAttribute<NonDiscoverableAttribute>())
				return true;

			// Read-only fields are implicitly marked with [Hidden]
			var fieldInfo = member as FieldInfo;
			var hiddenAttribute = member.GetCustomAttribute<HiddenAttribute>();
			var isHidden = hiddenAttribute != null || (fieldInfo != null && fieldInfo.IsInitOnly);

			// If the member is hidden, only ignore it in optimized serializations when we're not discovering objects
			if (mode == SerializationMode.Optimized && !discoveringObjects && isHidden)
				return true;

			return false;
		}

		/// <summary>
		///   Creates a delegate of type <paramref name="delegateType" /> that invokes the <paramref name="targetMethod" /> on the
		///   <paramref name="targetObject" /> either virtually or non-virtually, depending on the value of the
		///   <paramref name="virtualInvocation" /> parameter.
		/// </summary>
		/// <param name="delegateType">The type of the delegate that should be created.</param>
		/// <param name="targetObject">The target object the delegate should be created for.</param>
		/// <param name="targetMethod">The target method the delegate should be created for.</param>
		/// <param name="virtualInvocation">Indicates whether the target method should be invoked virtually.</param>
		public static Delegate CreateDelegateInstance(this Type delegateType, object targetObject, MethodInfo targetMethod, bool virtualInvocation)
		{
			Requires.NotNull(delegateType, nameof(delegateType));
			Requires.NotNull(targetObject, nameof(targetObject));
			Requires.NotNull(targetMethod, nameof(targetMethod));

			// Virtual invocations are .NET's defaults, hence we can simply use the method provided by the framework.
			// For non-virtual invocations however, we have to generate a dynamic method ourself that non-virtually
			// invokes the target method.
			if (virtualInvocation)
				return Delegate.CreateDelegate(delegateType, targetObject, targetMethod);

			var parameters = new[] { targetMethod.DeclaringType }
				.Concat(targetMethod.GetParameters().Select(parameter => parameter.ParameterType)).ToArray();

			var dynamicMethod = new DynamicMethod(
				name: targetMethod.Name + "NonVirtual",
				returnType: targetMethod.ReturnType,
				parameterTypes: parameters,
				m: typeof(object).Assembly.ManifestModule,
				skipVisibility: true);

			var il = dynamicMethod.GetILGenerator();

			for (var i = 0; i < parameters.Length; ++i)
				il.Emit(OpCodes.Ldarg, i);

			il.Emit(OpCodes.Call, targetMethod);
			il.Emit(OpCodes.Ret);

			return dynamicMethod.CreateDelegate(delegateType, targetObject);
		}

		/// <summary>
		///   Resolves the <paramref name="targetType" />'s implementing method for the <paramref name="interfaceMethod" />.
		/// </summary>
		/// <param name="targetType">The target type the implementing method should be resolved for.</param>
		/// <param name="interfaceMethod">The interface method the implementing method should be resolved for.</param>
		public static MethodInfo ResolveImplementingMethod(this Type targetType, MethodInfo interfaceMethod)
		{
			Requires.NotNull(targetType, nameof(targetType));
			Requires.NotNull(interfaceMethod, nameof(interfaceMethod));
			Requires.That(interfaceMethod.DeclaringType.IsInterface, nameof(interfaceMethod), "Expected a method declared by an interface.");
			Requires.That(targetType.GetInterfaces().Contains(interfaceMethod.DeclaringType), nameof(targetType),
				"The target type does not implement the interface.");

			var interfaceMap = targetType.GetInterfaceMap(interfaceMethod.DeclaringType);
			var index = Array.IndexOf(interfaceMap.InterfaceMethods, interfaceMethod);
			Requires.That(index != -1, "Unable to find the interface method in the type's interface map.");

			return interfaceMap.TargetMethods[index];
		}
	}
}