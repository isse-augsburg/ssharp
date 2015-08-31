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

namespace SafetySharp.Runtime
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
		///   The IL generator of the serialization method.
		/// </summary>
		private readonly ILGenerator _il;

		/// <summary>
		///   The method that is being generated.
		/// </summary>
		private readonly DynamicMethod _method;

		/// <summary>
		///   The objects that are serialized.
		/// </summary>
		private readonly ObjectTable _objects;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="objects">The known objects that can be serialized and deserialized.</param>
		/// <param name="name">The name of the generated method.</param>
		internal SerializationGenerator(ObjectTable objects, string name)
		{
			Requires.NotNull(objects, nameof(objects));
			Requires.NotNullOrWhitespace(name, nameof(name));

			_method = new DynamicMethod(
				name: name,
				returnType: typeof(void),
				parameterTypes: new[] { typeof(int*), typeof(object[]) },
				m: typeof(object).Assembly.ManifestModule,
				skipVisibility: true);

			_il = _method.GetILGenerator();
			_objects = objects;

			// Store state vector in a local variable
			_il.DeclareLocal(typeof(int*));
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Stloc_0);
		}

		/// <summary>
		///   Gets the unique serialization identifier for <paramref name="obj" />.
		/// </summary>
		/// <param name="obj">The object the identifier should be returned for.</param>
		public int GetObjectIdentifier(object obj)
		{
			return _objects[obj];
		}

		/// <summary>
		///   Compiles the dynamic method, returning a delegate that can be used to invoke it.
		/// </summary>
		internal SerializationDelegate Compile()
		{
			// The method must end with a ret instruction, even though no value is returned
			_il.Emit(OpCodes.Ret);

			return (SerializationDelegate)_method.CreateDelegate(typeof(SerializationDelegate));
		}

		/// <summary>
		///   Generates the code to deserialize the <paramref name="field" /> of the object identified by
		///   <paramref name="objectIdentifier" />.
		/// </summary>
		/// <param name="objectIdentifier">The identifier of the object that declares the <paramref name="field" />.</param>
		/// <param name="field">The field that should be deserialized.</param>
		public void DeserializeField(int objectIdentifier, FieldInfo field)
		{
			CheckIsSupportedType(field.FieldType);

			// o = objs[identifier]
			_il.Emit(OpCodes.Ldarg_1);
			_il.Emit(OpCodes.Ldc_I4, objectIdentifier);
			_il.Emit(OpCodes.Ldelem_Ref);

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
		public void SerializeField(int objectIdentifier, FieldInfo field)
		{
			CheckIsSupportedType(field.FieldType);

			// s = state
			_il.Emit(OpCodes.Ldloc_0);

			// o = objs[identifier]
			_il.Emit(OpCodes.Ldarg_1);
			_il.Emit(OpCodes.Ldc_I4, objectIdentifier);
			_il.Emit(OpCodes.Ldelem_Ref);

			// *s = o.field
			_il.Emit(OpCodes.Ldfld, field);
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
		///   Checks whether the serializer supports the serialization of the <paramref name="type" />.
		/// </summary>
		/// <param name="type">The type that should be checked.</param>
		private static void CheckIsSupportedType(Type type)
		{
			type = type.IsEnum ? type.GetEnumUnderlyingType() : type;

			Assert.That(type.IsPrimitive, $"Unsupported type '{type.FullName}'; only primitive types are supported.");
			Assert.That(Marshal.SizeOf(type) <= 4, "Field types with more than 4 bytes are not supported.");
		}
	}
}