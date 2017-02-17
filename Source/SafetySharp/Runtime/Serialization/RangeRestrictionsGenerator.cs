// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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
	using Modeling;
	using Utilities;

	/// <summary>
	///   Dynamically generates a method that checks range violations or clamps ranges.
	/// </summary>
	public sealed class RangeRestrictionsGenerator
	{
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
		///   The object that is currently stored in the local variable.
		/// </summary>
		private int _loadedObject;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="methodName">The name of the generated method.</param>
		internal RangeRestrictionsGenerator(string methodName)
		{
			Requires.NotNullOrWhitespace(methodName, nameof(methodName));

			_method = new DynamicMethod(
				name: methodName,
				returnType: typeof(void),
				parameterTypes: new[] { typeof(ObjectTable) },
				m: typeof(object).Assembly.ManifestModule,
				skipVisibility: true);

			_il = _method.GetILGenerator();
			_il.DeclareLocal(typeof(object));
		}

		/// <summary>
		///   Compiles the dynamic method, returning a delegate that can be used to invoke it.
		/// </summary>
		/// <param name="objects">The known objects that can be serialized and deserialized.</param>
		internal Action Compile(ObjectTable objects = null)
		{
			_il.Emit(OpCodes.Ret);
			return (Action)_method.CreateDelegate(typeof(Action), objects);
		}

		/// <summary>
		///   Generates the code for the range restriction method.
		/// </summary>
		/// <param name="stateGroups">The state groups the code should be generated for.</param>
		internal void GenerateCode(CompactedStateGroup[] stateGroups)
		{
			Requires.NotNull(stateGroups, nameof(stateGroups));

			foreach (var group in stateGroups.SelectMany(g => g.Slots).GroupBy(slot => slot.Object, ReferenceEqualityComparer<object>.Default))
			{
				var rangedSlots = group.Where(slot => slot.Range != null && slot.DataType.IsPrimitiveType() && !slot.DataType.IsEnum).ToArray();
				if (rangedSlots.Length == 0)
					continue;

				LoadObject(rangedSlots[0].ObjectIdentifier);
				foreach (var slot in rangedSlots)
				{
					if (slot.ObjectType.IsArray)
						RestrictFields(slot);
					else
						RestrictField(slot);
				}
			}
		}

		/// <summary>
		///   Generates the code to restrict the values contained in ranged array fields.
		/// </summary>
		private void RestrictFields(StateSlotMetadata metadata)
		{
			for (var i = 0; i < metadata.ElementCount; ++i)
				RestrictField(metadata, i);
		}

		/// <summary>
		///   Generates the code to restrict the values contained in ranged object fields.
		/// </summary>
		private void RestrictField(StateSlotMetadata metadata, int elementIndex = 0)
		{
			var exceedsFourBytes = metadata.ElementSizeInBits > 32;
			var continueLabel = _il.DefineLabel();

			switch (metadata.Range.OverflowBehavior)
			{
				case OverflowBehavior.Error:
					// if (v < lower | v > upper) throw
					PrepareAccess(metadata, elementIndex);
					AccessField(metadata, OpCodes.Ldfld);
					LoadConstant(metadata.Range.LowerBound, exceedsFourBytes);
					_il.Emit(metadata.DataType.IsUnsignedNumericType() ? OpCodes.Clt_Un : OpCodes.Clt);

					PrepareAccess(metadata, elementIndex);
					AccessField(metadata, OpCodes.Ldfld);
					LoadConstant(metadata.Range.UpperBound, exceedsFourBytes);
					_il.Emit(metadata.DataType.IsUnsignedNumericType() ? OpCodes.Cgt_Un : OpCodes.Cgt);

					_il.Emit(OpCodes.Or);
					_il.Emit(OpCodes.Brfalse, continueLabel);

					// throw new RangeViolationException(obj, field)
					_il.Emit(OpCodes.Ldloc_0);
					var field = metadata.ContainedInStruct ? metadata.FieldChain.Last() : metadata.Field;
					_il.Emit(OpCodes.Ldtoken, field);
					_il.Emit(OpCodes.Ldtoken, field.DeclaringType);
					var parameters = new[] { typeof(RuntimeFieldHandle), typeof(RuntimeTypeHandle) };
					_il.Emit(OpCodes.Call, typeof(FieldInfo).GetMethod("GetFieldFromHandle", parameters));
					LoadConstant(metadata.Range.LowerBound, exceedsFourBytes);
					_il.Emit(OpCodes.Box, metadata.EffectiveType);
					LoadConstant(metadata.Range.UpperBound, exceedsFourBytes);
					_il.Emit(OpCodes.Box, metadata.EffectiveType);
					_il.Emit(OpCodes.Ldc_I4, (int)OverflowBehavior.Error);
					_il.Emit(OpCodes.Newobj, typeof(RangeAttribute).GetConstructors().Single());
					_il.Emit(OpCodes.Newobj, typeof(RangeViolationException).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single());
					_il.Emit(OpCodes.Throw);
					break;
				case OverflowBehavior.Clamp:
					var clampLabel = _il.DefineLabel();

					PrepareAccess(metadata, elementIndex);
					AccessField(metadata, OpCodes.Ldfld);
					_il.Emit(OpCodes.Dup);

					// if (v < lower) v = lower
					LoadConstant(metadata.Range.LowerBound, exceedsFourBytes);
					_il.Emit(metadata.DataType.IsUnsignedNumericType() ? OpCodes.Bge_Un_S : OpCodes.Bge_S, clampLabel);
					_il.Emit(OpCodes.Pop);
					PrepareAccess(metadata, elementIndex);
					LoadConstant(metadata.Range.LowerBound, exceedsFourBytes);
					AccessField(metadata, OpCodes.Stfld);
					_il.Emit(OpCodes.Br, continueLabel);

					// else if (v > upper) v = upper
					_il.MarkLabel(clampLabel);
					LoadConstant(metadata.Range.UpperBound, exceedsFourBytes);
					_il.Emit(metadata.DataType.IsUnsignedNumericType() ? OpCodes.Ble_Un_S : OpCodes.Ble_S, continueLabel);
					PrepareAccess(metadata, elementIndex);
					LoadConstant(metadata.Range.UpperBound, exceedsFourBytes);
					AccessField(metadata, OpCodes.Stfld);
					break;
				case OverflowBehavior.WrapClamp:
					var wrapLabel = _il.DefineLabel();

					PrepareAccess(metadata, elementIndex);
					AccessField(metadata, OpCodes.Ldfld);
					_il.Emit(OpCodes.Dup);

					// if (v < lower) v = upper
					LoadConstant(metadata.Range.LowerBound, exceedsFourBytes);
					_il.Emit(metadata.DataType.IsUnsignedNumericType() ? OpCodes.Bge_Un_S : OpCodes.Bge_S, wrapLabel);
					_il.Emit(OpCodes.Pop);
					PrepareAccess(metadata, elementIndex);
					LoadConstant(metadata.Range.UpperBound, exceedsFourBytes);
					AccessField(metadata, OpCodes.Stfld);
					_il.Emit(OpCodes.Br, continueLabel);

					// else if (v > upper) v = lower
					_il.MarkLabel(wrapLabel);
					LoadConstant(metadata.Range.UpperBound, exceedsFourBytes);
					_il.Emit(metadata.DataType.IsUnsignedNumericType() ? OpCodes.Ble_Un_S : OpCodes.Ble_S, continueLabel);
					PrepareAccess(metadata, elementIndex);
					LoadConstant(metadata.Range.LowerBound, exceedsFourBytes);
					AccessField(metadata, OpCodes.Stfld);
					break;
				default:
					Assert.NotReached("Unknown overflow behavior.");
					break;
			}

			_il.MarkLabel(continueLabel);
		}

		/// <summary>
		///   Loads the <paramref name="constant" /> onto the stack.
		/// </summary>
		private void LoadConstant(object constant, bool exceedsFourBytes)
		{
			if (exceedsFourBytes)
			{
				if (constant is double)
					_il.Emit(OpCodes.Ldc_I8, (double)constant);
				else
					_il.Emit(OpCodes.Ldc_I8, NormalizeToInt64(constant));
			}
			else
			{
				if (constant is float)
					_il.Emit(OpCodes.Ldc_I4, (float)constant);
				else
					_il.Emit(OpCodes.Ldc_I4, NormalizeToInt32(constant));
			}
		}

		/// <summary>
		///   Normalizes the type of the <paramref name="constant" /> value.
		/// </summary>
		private static int NormalizeToInt32(object constant)
		{
			switch (Type.GetTypeCode(constant.GetType()))
			{
				case TypeCode.Char:
					return (char)constant;
				case TypeCode.SByte:
					return (sbyte)constant;
				case TypeCode.Byte:
					return (byte)constant;
				case TypeCode.Int16:
					return (short)constant;
				case TypeCode.UInt16:
					return (ushort)constant;
				case TypeCode.Int32:
					return (int)constant;
				case TypeCode.UInt32:
					return (int)(uint)constant;
				case TypeCode.Int64:
					return checked((int)(long)constant);
				case TypeCode.UInt64:
					return checked((int)(ulong)constant);
				default:
					return Assert.NotReached<int>($"Cannot normalize value of type {constant.GetType().FullName}.");
			}
		}

		/// <summary>
		///   Normalizes the type of the <paramref name="constant" /> value.
		/// </summary>
		private static long NormalizeToInt64(object constant)
		{
			switch (Type.GetTypeCode(constant.GetType()))
			{
				case TypeCode.Char:
					return (char)constant;
				case TypeCode.SByte:
					return (sbyte)constant;
				case TypeCode.Byte:
					return (byte)constant;
				case TypeCode.Int16:
					return (short)constant;
				case TypeCode.UInt16:
					return (ushort)constant;
				case TypeCode.Int32:
					return (int)constant;
				case TypeCode.UInt32:
					return (int)(uint)constant;
				case TypeCode.Int64:
					return (long)constant;
				case TypeCode.UInt64:
					return (long)(ulong)constant;
				default:
					return Assert.NotReached<long>($"Cannot normalize value of type {constant.GetType().FullName}.");
			}
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
			_il.Emit(OpCodes.Stloc_0);

			_loadedObject = objectIdentifier;
		}

		/// <summary>
		///   Prepares the access to the field referenced by the <paramref name="metadata" />.
		/// </summary>
		private void PrepareAccess(StateSlotMetadata metadata, int elementIndex)
		{
			_il.Emit(OpCodes.Ldloc_0);

			if (!metadata.ContainedInStruct)
				return;

			if (metadata.ObjectType.IsArray)
			{
				_il.Emit(OpCodes.Ldc_I4, elementIndex);
				_il.Emit(OpCodes.Ldelema, metadata.ObjectType.GetElementType());
			}
			else
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
	}
}