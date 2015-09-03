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

namespace SafetySharp.Runtime.Serialization
{
	using System;
	using System.Reflection;
	using System.Reflection.Emit;
	using System.Runtime.InteropServices;
	using Utilities;

	/// <summary>
	///   Provides methods to dynamically generate state serialization and deserialization methods.
	/// </summary>
	public sealed class SerializationGenerator
	{
		/// <summary>
		///   The reflection information of the <see cref="ObjectTable.GetObjectIdentifier" /> method.
		/// </summary>
		private readonly MethodInfo _getObjectIdentifierMethod = typeof(ObjectTable).GetMethod(nameof(ObjectTable.GetObjectIdentifier));

		/// <summary>
		///   The reflection information of the <see cref="ObjectTable.GetObject" /> method.
		/// </summary>
		private readonly MethodInfo _getObjectMethod = typeof(ObjectTable).GetMethod(nameof(ObjectTable.GetObject));

		/// <summary>
		///   The IL generator of the serialization method.
		/// </summary>
		private readonly ILGenerator _il;

		/// <summary>
		///   The method that is being generated.
		/// </summary>
		private readonly DynamicMethod _method;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="name">The name of the generated method.</param>
		internal SerializationGenerator(string name)
		{
			Requires.NotNullOrWhitespace(name, nameof(name));

			_method = new DynamicMethod(
				name: name,
				returnType: typeof(void),
				parameterTypes: new[] { typeof(ObjectTable), typeof(int*) },
				m: typeof(object).Assembly.ManifestModule,
				skipVisibility: true);

			_il = _method.GetILGenerator();

			// Store the state vector in a local variable
			_il.DeclareLocal(typeof(int*));
			_il.Emit(OpCodes.Ldarg_1);
			_il.Emit(OpCodes.Stloc_0);
		}

		/// <summary>
		///   Compiles the dynamic method, returning a delegate that can be used to invoke it.
		/// </summary>
		/// <param name="objects">The known objects that can be serialized and deserialized.</param>
		internal SerializationDelegate Compile(ObjectTable objects)
		{
			Requires.NotNull(objects, nameof(objects));

			_il.Emit(OpCodes.Ret);
			return (SerializationDelegate)_method.CreateDelegate(typeof(SerializationDelegate), objects);
		}

		/// <summary>
		///   Gets the number of state slots required for serialization of the <paramref name="type" />.
		/// </summary>
		/// <param name="type">The type that should be checked.</param>
		internal static int GetStateSlotCount(Type type)
		{
			if (IsPrimitiveTypeWithAtMostFourBytes(type))
				return 1;

			if (IsPrimitiveTypeWithAtMostEightBytes(type))
				return 2;

			if (IsReferenceType(type))
				return 1;

			throw new NotSupportedException($"Field type '{type.FullName}' is unsupported.");
		}

		/// <summary>
		///   Generates the code to deserialize the <paramref name="field" /> of the object identified by
		///   <paramref name="objectIdentifier" />.
		/// </summary>
		/// <param name="objectIdentifier">The identifier of the object that declares the <paramref name="field" />.</param>
		/// <param name="field">The field that should be deserialized.</param>
		public void DeserializeField(int objectIdentifier, FieldInfo field)
		{
			if (IsPrimitiveTypeWithAtMostFourBytes(field.FieldType))
				DeserializeFourByteField(objectIdentifier, field);
			else if (IsPrimitiveTypeWithAtMostEightBytes(field.FieldType))
				DeserializeEightByteField(objectIdentifier, field);
			else if (IsReferenceType(field.FieldType))
				DeserializeReferenceField(objectIdentifier, field);
			else
				throw new NotSupportedException($"Field type '{field.FieldType.FullName}' is unsupported.");
		}

		/// <summary>
		///   Generates the code to serialize the <paramref name="field" /> of the object identified by
		///   <paramref name="objectIdentifier" />.
		/// </summary>
		/// <param name="objectIdentifier">The identifier of the object that declares the <paramref name="field" />.</param>
		/// <param name="field">The field that should be serialized.</param>
		public void SerializeField(int objectIdentifier, FieldInfo field)
		{
			if (IsPrimitiveTypeWithAtMostFourBytes(field.FieldType))
				SerializeFourByteField(objectIdentifier, field);
			else if (IsPrimitiveTypeWithAtMostEightBytes(field.FieldType))
				SerializeEightByteField(objectIdentifier, field);
			else if (IsReferenceType(field.FieldType))
				SerializeReferenceField(objectIdentifier, field);
			else
				throw new NotSupportedException($"Field type '{field.FieldType.FullName}' is unsupported.");
		}

		/// <summary>
		///   Generates the code to deserialize the <paramref name="field" /> of the object identified by
		///   <paramref name="objectIdentifier" />.
		/// </summary>
		/// <param name="objectIdentifier">The identifier of the object that declares the <paramref name="field" />.</param>
		/// <param name="field">The field that should be deserialized.</param>
		private void DeserializeFourByteField(int objectIdentifier, FieldInfo field)
		{
			// o = objs.GetObject(identifier)
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Ldc_I4, objectIdentifier);
			_il.Emit(OpCodes.Call, _getObjectMethod);

			// v = *state
			_il.Emit(OpCodes.Ldloc_0);
			_il.Emit(OpCodes.Ldind_I4);

			// o.field = v
			_il.Emit(OpCodes.Stfld, field);

			AdvanceToNextSlot();
		}

		/// <summary>
		///   Generates the code to serialize the <paramref name="field" /> of the object identified by
		///   <paramref name="objectIdentifier" />.
		/// </summary>
		/// <param name="objectIdentifier">The identifier of the object that declares the <paramref name="field" />.</param>
		/// <param name="field">The field that should be serialized.</param>
		private void SerializeFourByteField(int objectIdentifier, FieldInfo field)
		{
			// s = state
			_il.Emit(OpCodes.Ldloc_0);

			// o = objs.GetObject(identifier)
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Ldc_I4, objectIdentifier);
			_il.Emit(OpCodes.Call, _getObjectMethod);

			// *s = o.field
			_il.Emit(OpCodes.Ldfld, field);
			_il.Emit(OpCodes.Stind_I4);

			AdvanceToNextSlot();
		}

		/// <summary>
		///   Generates the code to deserialize the <paramref name="field" /> of the object identified by
		///   <paramref name="objectIdentifier" />.
		/// </summary>
		/// <param name="objectIdentifier">The identifier of the object that declares the <paramref name="field" />.</param>
		/// <param name="field">The field that should be deserialized.</param>
		private void DeserializeEightByteField(int objectIdentifier, FieldInfo field)
		{
			// o = objs.GetObject(identifier)
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Ldc_I4, objectIdentifier);
			_il.Emit(OpCodes.Call, _getObjectMethod);

			// v = *state
			_il.Emit(OpCodes.Ldloc_0);
			_il.Emit(OpCodes.Ldind_I8);

			// o.field = v
			_il.Emit(OpCodes.Stfld, field);

			AdvanceToNextSlot();
			AdvanceToNextSlot();
		}

		/// <summary>
		///   Generates the code to serialize the <paramref name="field" /> of the object identified by
		///   <paramref name="objectIdentifier" />.
		/// </summary>
		/// <param name="objectIdentifier">The identifier of the object that declares the <paramref name="field" />.</param>
		/// <param name="field">The field that should be serialized.</param>
		private void SerializeEightByteField(int objectIdentifier, FieldInfo field)
		{
			// s = state
			_il.Emit(OpCodes.Ldloc_0);

			// o = objs.GetObject(identifier)
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Ldc_I4, objectIdentifier);
			_il.Emit(OpCodes.Call, _getObjectMethod);

			// *s = o.field
			_il.Emit(OpCodes.Ldfld, field);
			_il.Emit(OpCodes.Stind_I8);

			AdvanceToNextSlot();
			AdvanceToNextSlot();
		}

		/// <summary>
		///   Generates the code to deserialize the <paramref name="field" /> of the object identified by
		///   <paramref name="objectIdentifier" />.
		/// </summary>
		/// <param name="objectIdentifier">The identifier of the object that declares the <paramref name="field" />.</param>
		/// <param name="field">The field that should be deserialized.</param>
		private void DeserializeReferenceField(int objectIdentifier, FieldInfo field)
		{
			// o = objs.GetObject(identifier)
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Ldc_I4, objectIdentifier);
			_il.Emit(OpCodes.Call, _getObjectMethod);

			// v = objs.GetObject(*state)
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Ldloc_0);
			_il.Emit(OpCodes.Ldind_I4);
			_il.Emit(OpCodes.Call, _getObjectMethod);

			// o.field = v
			_il.Emit(OpCodes.Stfld, field);

			AdvanceToNextSlot();
		}

		/// <summary>
		///   Generates the code to serialize the <paramref name="field" /> of the object identified by
		///   <paramref name="objectIdentifier" />.
		/// </summary>
		/// <param name="objectIdentifier">The identifier of the object that declares the <paramref name="field" />.</param>
		/// <param name="field">The field that should be serialized.</param>
		private void SerializeReferenceField(int objectIdentifier, FieldInfo field)
		{
			// s = state
			_il.Emit(OpCodes.Ldloc_0);

			// o = objs.GetObject(identifier)
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Dup);
			_il.Emit(OpCodes.Ldc_I4, objectIdentifier);
			_il.Emit(OpCodes.Call, _getObjectMethod);

			// *s = objs.GetObjectIdentifier(o.field)
			_il.Emit(OpCodes.Ldfld, field);
			_il.Emit(OpCodes.Call, _getObjectIdentifierMethod);
			_il.Emit(OpCodes.Stind_I4);

			AdvanceToNextSlot();
		}

		/// <summary>
		///   Advances the local state variable to point to the next state slot.
		/// </summary>
		private void AdvanceToNextSlot()
		{
			// state = state + 4;
			_il.Emit(OpCodes.Ldloc_0);
			_il.Emit(OpCodes.Ldc_I4, 4);
			_il.Emit(OpCodes.Add);
			_il.Emit(OpCodes.Stloc_0);
		}

		/// <summary>
		///   Checks whether <paramref name="type" /> is a reference type, i.e., a class or interface.
		/// </summary>
		private static bool IsReferenceType(Type type)
		{
			if (type.IsSubclassOf(typeof(Delegate)))
				throw new NotSupportedException($"Delegate types such as '{type.FullName}' are not supported.");

			return type.IsReferenceType();
		}

		/// <summary>
		///   Checks whether <paramref name="type" /> is a primitive type with at most four bytes.
		/// </summary>
		private static bool IsPrimitiveTypeWithAtMostFourBytes(Type type)
		{
			type = type.IsEnum ? type.GetEnumUnderlyingType() : type;
			return type.IsPrimitive && Marshal.SizeOf(type) <= 4;
		}

		/// <summary>
		///   Checks whether <paramref name="type" /> is a primitive type with at most eight bytes.
		/// </summary>
		private static bool IsPrimitiveTypeWithAtMostEightBytes(Type type)
		{
			type = type.IsEnum ? type.GetEnumUnderlyingType() : type;
			return type.IsPrimitive && Marshal.SizeOf(type) <= 8;
		}
	}
}