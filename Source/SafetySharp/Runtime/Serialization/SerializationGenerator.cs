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

namespace SafetySharp.Runtime.Serialization
{
	using System;
	using System.Linq;
	using System.Reflection;
	using System.Reflection.Emit;
	using ISSE.SafetyChecking.Utilities;
	using Utilities;

	/// <summary>
	///   Dynamically generates state serialization and deserialization methods.
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
		///   The index used to read or write a bit.
		/// </summary>
		private int _bitIndex;

		/// <summary>
		///   Indicates whether individual elements of the state vector are addressed by bit instead of by byte.
		/// </summary>
		private bool _bitLevelAddressing;

		/// <summary>
		///   The object that is currently stored in the local variable.
		/// </summary>
		private int _loadedObject;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="methodName">The name of the generated method.</param>
		internal SerializationGenerator(string methodName)
		{
			Requires.NotNullOrWhitespace(methodName, nameof(methodName));

			_method = new DynamicMethod(
				name: methodName,
				returnType: typeof(void),
				parameterTypes: new[] { typeof(ObjectTable), typeof(byte*) },
				m: typeof(object).Assembly.ManifestModule,
				skipVisibility: true);

			_il = _method.GetILGenerator();

			// Store the state vector in a local variable
			_il.DeclareLocal(typeof(byte*));
			_il.DeclareLocal(typeof(object));
			_il.Emit(OpCodes.Ldarg_1);
			_il.Emit(OpCodes.Stloc_0);
		}

		/// <summary>
		///   Compiles the dynamic method, returning a delegate that can be used to invoke it.
		/// </summary>
		/// <param name="objects">The known objects that can be serialized and deserialized.</param>
		internal T Compile<T>(ObjectTable objects = null)
		{
			_il.Emit(OpCodes.Ret);
			return (T)(object)_method.CreateDelegate(typeof(T), objects);
		}

		/// <summary>
		///   Generates the code for the serialization method.
		/// </summary>
		/// <param name="stateGroups">The state groups the code should be generated for.</param>
		internal void GenerateSerializationCode(CompactedStateGroup[] stateGroups)
		{
			Requires.NotNull(stateGroups, nameof(stateGroups));

			foreach (var group in stateGroups)
			{
				_bitLevelAddressing = group.ElementSizeInBits == 1;
				_bitIndex = 0;

				foreach (var slot in group.Slots)
				{
					if (slot.Field != null)
					{
						SerializeField(slot);
						Advance(slot.ElementSizeInBits / 8);
					}
					else
						SerializeArray(slot);
				}

				if (_bitLevelAddressing)
				{
					_bitLevelAddressing = false;
					if (_bitIndex != 0)
						Advance(1);
				}

				Advance(group.PaddingBytes);
			}
		}

		/// <summary>
		///   Generates the code for the deserialization method.
		/// </summary>
		/// <param name="stateGroups">The state groups the code should be generated for.</param>
		internal void GenerateDeserializationCode(CompactedStateGroup[] stateGroups)
		{
			Requires.NotNull(stateGroups, nameof(stateGroups));

			foreach (var group in stateGroups)
			{
				_bitLevelAddressing = group.ElementSizeInBits == 1;
				_bitIndex = 0;

				foreach (var slot in group.Slots)
				{
					if (slot.Field != null)
					{
						DeserializeField(slot);
						Advance(slot.ElementSizeInBits / 8);
					}
					else
						DeserializeArray(slot);
				}

				if (_bitLevelAddressing)
				{
					_bitLevelAddressing = false;
					if (_bitIndex != 0)
						Advance(1);
				}

				Advance(group.PaddingBytes);
			}
		}

		/// <summary>
		///   Generates the code to deserialize the state slot described by the <paramref name="metadata" />.
		/// </summary>
		/// <param name="metadata">The metadata of the state slot the code should be generated for.</param>
		private void DeserializeField(StateSlotMetadata metadata)
		{
			LoadObject(metadata.ObjectIdentifier);

			if (IsReferenceType(metadata.DataType))
				DeserializeReferenceField(metadata);
			else
				DeserializePrimitiveTypeField(metadata);
		}

		/// <summary>
		///   Generates the code to serialize the state slot described by the <paramref name="metadata" />.
		/// </summary>
		/// <param name="metadata">The metadata of the state slot the code should be generated for.</param>
		private void SerializeField(StateSlotMetadata metadata)
		{
			LoadObject(metadata.ObjectIdentifier);

			if (IsReferenceType(metadata.DataType))
				SerializeReferenceField(metadata);
			else
				SerializePrimitiveTypeField(metadata);
		}

		/// <summary>
		///   Generates the code to deserialize the array state slot described by the <paramref name="metadata" />.
		/// </summary>
		/// <param name="metadata">The metadata of the state slot the code should be generated for.</param>
		private void DeserializeArray(StateSlotMetadata metadata)
		{
			var isReferenceType = metadata.DataType.IsReferenceType();
			var loadCode = _bitLevelAddressing
				? default(OpCode)
				: GetLoadElementOpCode(metadata.ElementSizeInBits / 8, metadata.EffectiveType.IsUnsignedNumericType());
			var storeCode = isReferenceType ? OpCodes.Stelem_Ref : GetStoreArrayElementOpCode(GetUnmanagedSize(metadata.DataType));

			LoadObject(metadata.ObjectIdentifier);

			for (var i = 0; i < metadata.ElementCount; ++i)
			{
				// o = &objs.GetObject(identifier)[i]
				_il.Emit(OpCodes.Ldloc_1);
				PrepareElementAccess(metadata, i);

				if (isReferenceType)
				{
					// v = objs.GetObject(*state)
					_il.Emit(OpCodes.Ldarg_0);
					_il.Emit(OpCodes.Ldloc_0);
					_il.Emit(OpCodes.Ldind_I2);
					_il.Emit(OpCodes.Call, _getObjectMethod);
				}
				else if (_bitLevelAddressing)
					LoadBooleanValue();
				else
				{
					// v = *state
					_il.Emit(OpCodes.Ldloc_0);
					_il.Emit(loadCode);
				}

				// *o = v
				if (metadata.ContainedInStruct)
					AccessField(metadata, OpCodes.Stfld);
				else
					_il.Emit(storeCode);

				Advance(metadata.ElementSizeInBits / 8);
			}
		}

		/// <summary>
		///   Generates the code to serialize the array state slot described by the <paramref name="metadata" />.
		/// </summary>
		/// <param name="metadata">The metadata of the state slot the code should be generated for.</param>
		private void SerializeArray(StateSlotMetadata metadata)
		{
			var isReferenceType = metadata.DataType.IsReferenceType();
			var storeCode = _bitLevelAddressing ? default(OpCode) : GetStoreElementOpCode(metadata.ElementSizeInBits / 8);
			var loadCode = isReferenceType
				? OpCodes.Ldelem_Ref
				: GetLoadArrayElementOpCode(GetUnmanagedSize(metadata.DataType), metadata.EffectiveType.IsUnsignedNumericType());

			LoadObject(metadata.ObjectIdentifier);

			for (var i = 0; i < metadata.ElementCount; ++i)
			{
				if (_bitLevelAddressing)
				{
					StoreBooleanValue(() =>
					{
						// o = objs.GetObject(identifier)
						_il.Emit(OpCodes.Ldloc_1);

						// v = o[i]
						PrepareElementAccess(metadata, i);
						if (metadata.ContainedInStruct)
							AccessField(metadata, OpCodes.Ldfld);
						else
							_il.Emit(loadCode);
					});
				}
				else
				{
					// s = state
					_il.Emit(OpCodes.Ldloc_0);

					// o = objs.GetObject(identifier)
					if (isReferenceType)
						_il.Emit(OpCodes.Ldarg_0);

					_il.Emit(OpCodes.Ldloc_1);

					// v = o[i]
					PrepareElementAccess(metadata, i);
					if (metadata.ContainedInStruct)
						AccessField(metadata, OpCodes.Ldfld);
					else
						_il.Emit(loadCode);

					// v = objs.GetObjectIdentifier(o[i])
					if (isReferenceType)
						_il.Emit(OpCodes.Call, _getObjectIdentifierMethod);

					// *s = v
					_il.Emit(storeCode);
				}

				Advance(metadata.ElementSizeInBits / 8);
			}
		}

		/// <summary>
		///   Gets the IL instruction that can be used to store an element of the <paramref name="sizeInBytes" /> into an array.
		/// </summary>
		private static OpCode GetStoreArrayElementOpCode(int sizeInBytes)
		{
			switch (sizeInBytes)
			{
				case 1:
					return OpCodes.Stelem_I1;
				case 2:
					return OpCodes.Stelem_I2;
				case 4:
					return OpCodes.Stelem_I4;
				case 8:
					return OpCodes.Stelem_I8;
				default:
					return Assert.NotReached<OpCode>($"Unsupported element size: {sizeInBytes}.");
			}
		}

		/// <summary>
		///   Gets the IL instruction that can be used to load an element of the <paramref name="sizeInBytes" /> from an array.
		/// </summary>
		private static OpCode GetLoadArrayElementOpCode(int sizeInBytes, bool isUnsigned)
		{
			switch (sizeInBytes)
			{
				case 1:
					return isUnsigned ? OpCodes.Ldelem_U1 : OpCodes.Ldelem_I1;
				case 2:
					return isUnsigned ? OpCodes.Ldelem_U2 : OpCodes.Ldelem_I2;
				case 4:
					return isUnsigned ? OpCodes.Ldelem_U4 : OpCodes.Ldelem_I4;
				case 8:
					return OpCodes.Ldelem_I8;
				default:
					return Assert.NotReached<OpCode>($"Unsupported element size: {sizeInBytes}.");
			}
		}

		/// <summary>
		///   Gets the IL instruction that can be used to store an element of the <paramref name="sizeInBytes" /> into the state vector.
		/// </summary>
		private static OpCode GetStoreElementOpCode(int sizeInBytes)
		{
			switch (sizeInBytes)
			{
				case 1:
					return OpCodes.Stind_I1;
				case 2:
					return OpCodes.Stind_I2;
				case 4:
					return OpCodes.Stind_I4;
				case 8:
					return OpCodes.Stind_I8;
				default:
					return Assert.NotReached<OpCode>($"Unsupported element size: {sizeInBytes}.");
			}
		}

		/// <summary>
		///   Gets the IL instruction that can be used to load an element of the <paramref name="sizeInBytes" /> from the state vector.
		/// </summary>
		private static OpCode GetLoadElementOpCode(int sizeInBytes, bool isUnsigned)
		{
			switch (sizeInBytes)
			{
				case 1:
					return isUnsigned ? OpCodes.Ldind_U1 : OpCodes.Ldind_I1;
				case 2:
					return isUnsigned ? OpCodes.Ldind_U2 : OpCodes.Ldind_I2;
				case 4:
					return isUnsigned ? OpCodes.Ldind_U4 : OpCodes.Ldind_I4;
				case 8:
					return OpCodes.Ldind_I8;
				default:
					return Assert.NotReached<OpCode>($"Unsupported element size: {sizeInBytes}.");
			}
		}

		/// <summary>
		///   Generates the code to deserialize the state slot described by the <paramref name="metadata" /> of the object stored in the
		///   local variable.
		/// </summary>
		/// <param name="metadata">The metadata of the state slot the code should be generated for.</param>
		private void DeserializePrimitiveTypeField(StateSlotMetadata metadata)
		{
			// o = objs.GetObject(identifier)
			PrepareFieldAccess(metadata);

			if (_bitLevelAddressing)
				LoadBooleanValue();
			else
			{
				// v = *state
				_il.Emit(OpCodes.Ldloc_0);
				_il.Emit(GetLoadElementOpCode(metadata.ElementSizeInBits / 8, metadata.EffectiveType.IsUnsignedNumericType()));
			}

			// o.field = v
			AccessField(metadata, OpCodes.Stfld);
		}

		/// <summary>
		///   Generates the code to serialize the state slot described by the <paramref name="metadata" /> of the object stored in the
		///   local variable.
		/// </summary>
		/// <param name="metadata">The metadata of the state slot the code should be generated for.</param>
		private void SerializePrimitiveTypeField(StateSlotMetadata metadata)
		{
			if (_bitLevelAddressing)
			{
				StoreBooleanValue(() =>
				{
					PrepareFieldAccess(metadata);
					AccessField(metadata, OpCodes.Ldfld);
				});
				return;
			}

			// s = state
			_il.Emit(OpCodes.Ldloc_0);

			// v = o.field
			PrepareFieldAccess(metadata);
			AccessField(metadata, OpCodes.Ldfld);

			// *s = v
			_il.Emit(GetStoreElementOpCode(metadata.ElementSizeInBits / 8));
		}

		/// <summary>
		///   Prepares the access to the field referenced by the <paramref name="metadata" /> of an element contained in an array.
		/// </summary>
		private void PrepareElementAccess(StateSlotMetadata metadata, int elementIndex)
		{
			_il.Emit(OpCodes.Ldc_I4, elementIndex);

			if (!metadata.ContainedInStruct)
				return;

			_il.Emit(OpCodes.Ldelema, metadata.ObjectType.GetElementType());

			for (var i = 0; i < metadata.FieldChain.Length - 1; ++i)
				_il.Emit(OpCodes.Ldflda, metadata.FieldChain[i]);
		}

		/// <summary>
		///   Prepares the access to the field referenced by the <paramref name="metadata" /> of an object.
		/// </summary>
		private void PrepareFieldAccess(StateSlotMetadata metadata)
		{
			_il.Emit(OpCodes.Ldloc_1);

			if (!metadata.ContainedInStruct)
				return;

			_il.Emit(OpCodes.Ldflda, metadata.Field);

			for (var i = 0; i < metadata.FieldChain.Length - 1; ++i)
				_il.Emit(OpCodes.Ldflda, metadata.FieldChain[i]);
		}

		/// <summary>
		///   Accesses the field on the object currently on the stack.
		/// </summary>
		private void AccessField(StateSlotMetadata metadata, OpCode accessCode)
		{
			var field = metadata.ContainedInStruct ? metadata.FieldChain.Last() : metadata.Field;
			_il.Emit(accessCode, field);
		}

		/// <summary>
		///   Loads a bit-compressed Boolean value onto the stack.
		/// </summary>
		private void LoadBooleanValue()
		{
			// v = (*state >> _bitIndex) & 0x01 == 1
			_il.Emit(OpCodes.Ldloc_0);
			_il.Emit(OpCodes.Ldind_U1);
			_il.Emit(OpCodes.Ldc_I4, _bitIndex);
			_il.Emit(OpCodes.Shr_Un);
			_il.Emit(OpCodes.Ldc_I4_1);
			_il.Emit(OpCodes.And);
		}

		/// <summary>
		///   Stores a bit-compressed Boolean value loaded using the <paramref name="valueLoader" />.
		/// </summary>
		private void StoreBooleanValue(Action valueLoader)
		{
			// *s |= o.field << _bitIndex;
			_il.Emit(OpCodes.Ldloc_0);

			// If we write the first bit, use the constant 0 instead of loading the previous value
			// as otherwise a bit that is true would never be able to become false again
			if (_bitIndex == 0)
				_il.Emit(OpCodes.Ldc_I4_0);
			else
			{
				_il.Emit(OpCodes.Dup);
				_il.Emit(OpCodes.Ldind_U1);
			}

			valueLoader();

			_il.Emit(OpCodes.Ldc_I4, _bitIndex);
			_il.Emit(OpCodes.Shl);
			_il.Emit(OpCodes.Or);

			_il.Emit(OpCodes.Stind_I1);
		}

		/// <summary>
		///   Generates the code to deserialize a reference field.
		/// </summary>
		private void DeserializeReferenceField(StateSlotMetadata metadata)
		{
			// o = objs.GetObject(identifier)
			PrepareFieldAccess(metadata);

			// v = objs.GetObject(*state)
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Ldloc_0);
			_il.Emit(OpCodes.Ldind_I2);
			_il.Emit(OpCodes.Call, _getObjectMethod);

			// o.field = v
			AccessField(metadata, OpCodes.Stfld);
		}

		/// <summary>
		///   Generates the code to serialize a reference field.
		/// </summary>
		private void SerializeReferenceField(StateSlotMetadata metadata)
		{
			// s = state
			_il.Emit(OpCodes.Ldloc_0);

			// *s = objs.GetObjectIdentifier(o.field)
			_il.Emit(OpCodes.Ldarg_0);
			PrepareFieldAccess(metadata);
			AccessField(metadata, OpCodes.Ldfld);
			_il.Emit(OpCodes.Call, _getObjectIdentifierMethod);
			_il.Emit(OpCodes.Stind_I2);
		}

		/// <summary>
		///   Advances the local state variable by <paramref name="byteCount" /> bytes.
		/// </summary>
		private void Advance(int byteCount)
		{
			if (_bitLevelAddressing)
			{
				_bitIndex = (_bitIndex + 1) % 8;
				if (_bitIndex != 0)
					return;

				byteCount = 1;
			}
			else if (byteCount == 0)
				return;

			// state = state + byteCount;
			_il.Emit(OpCodes.Ldloc_0);
			_il.Emit(OpCodes.Ldc_I4, byteCount);
			_il.Emit(OpCodes.Add);
			_il.Emit(OpCodes.Stloc_0);
		}

		/// <summary>
		///   Loads the object with the <paramref name="objectIdentifier" /> into the local variable.
		/// </summary>
		private void LoadObject(int objectIdentifier)
		{
			if (_loadedObject == objectIdentifier)
				return;

			// o = objs.GetObject(objectIdentifier)
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Ldc_I4, objectIdentifier);
			_il.Emit(OpCodes.Call, _getObjectMethod);
			_il.Emit(OpCodes.Stloc_1);

			_loadedObject = objectIdentifier;
		}

		/// <summary>
		///   Checks whether <paramref name="type" /> is a reference type, i.e., a class or interface.
		/// </summary>
		private static bool IsReferenceType(Type type)
		{
			return type.IsReferenceType();
		}

		/// <summary>
		///   Gets the unmanaged size in bytes required to store value of the given <paramref name="type" />.
		/// </summary>
		private static int GetUnmanagedSize(Type type)
		{
			if (type.IsReferenceType())
				return 2;

			return type.GetUnmanagedSize();
		}
	}
}