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
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Reflection.Emit;
	using Modeling;
	using Utilities;
	// Currently Rewards are value types and not reference types.
	// They never get serialized. Thus, they do not contribute any data or meta-data.
	// During deserialization their fields are reseted to zero.

	/// <summary>
	///   Serializes <see cref="Reward" /> instances.
	/// </summary>
	internal sealed class ResetRewardGenerator
	{

		/// <summary>
		///   The IL generator of the serialization method.
		/// </summary>
		private readonly ILGenerator _il;

		/// <summary>
		///   The reflection information of the <see cref="ObjectTable.GetObject" /> method.
		/// </summary>
		private readonly MethodInfo _getObjectMethod;

		private readonly ConstructorInfo _createReward;
		private readonly FieldInfo _fieldInfoRewardValue;
		private readonly FieldInfo _fieldInfoRewardNegative;
		private readonly FieldInfo _fieldInfoMightBeNegative;

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
		internal ResetRewardGenerator(string methodName)
		{
			Requires.NotNullOrWhitespace(methodName, nameof(methodName));


			_createReward = typeof(Reward).GetConstructor(new[] { typeof(bool) });

			_getObjectMethod = typeof(ObjectTable).GetMethod(nameof(ObjectTable.GetObject));

			_fieldInfoRewardValue = typeof(Reward).GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);
			_fieldInfoRewardNegative = typeof(Reward).GetField("_valueNegative", BindingFlags.Instance | BindingFlags.NonPublic);
			_fieldInfoMightBeNegative = typeof(Reward).GetField(nameof(Reward.MightBeNegative));

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
		internal Action Compile(ObjectTable objects)
		{
			_il.Emit(OpCodes.Ret);

			//This is the reason ObjectTable is arg_0
			return (Action)_method.CreateDelegate(typeof(Action), objects);
		}

		/// <summary>
		///   Generates the code for the reset reward method.
		/// </summary>
		internal void GenerateCode(ObjectTable objectTable)
		{
			Requires.NotNull(objectTable, nameof(objectTable));

			foreach (var obj in objectTable)
			{
				foreach (var fieldInfo in obj.GetType().GetFields(typeof(object)))
				{
					if (fieldInfo.FieldType == typeof(Reward))
					{
						LoadObject(objectTable.GetObjectIdentifier(obj));
						ResetRewardFields(fieldInfo);
					}
				}
			}
		}
		
		/// <summary>
		///   Generates the code to serialize the <paramref name="field" /> of the object stored in the local variable.
		/// </summary>
		/// <param name="field">The field that should be serialized.</param>
		internal void ResetRewardFields(FieldInfo field)
		{
			//here we need to reset both Reward Fields
			// o.field.ValuePositive = 0
			// o.field.ValueNegative = 0
			_il.Emit(OpCodes.Ldloc_0); // Ldloc_0 contains the object which contains field
			_il.Emit(OpCodes.Ldflda, field); //Note: Because Reward is a value type we need Ldflda instead of Ldfld
			_il.Emit(OpCodes.Dup); //We double the pointer towards Reward, because we need it two times
								  // now the location of Reward is on the stack two times
			_il.Emit(OpCodes.Ldc_I4_0);
			_il.Emit(OpCodes.Stfld, _fieldInfoRewardValue);
			_il.Emit(OpCodes.Ldc_I4_0);
			_il.Emit(OpCodes.Stfld, _fieldInfoRewardNegative);
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

		/*
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
		*/

		/// <summary>
		///   Dynamically generates a delegate that can be used to restrict state ranges.
		/// </summary>
		/// <param name="objects">The objects whose data is stored in the state vector.</param>
		internal static Action CreateRangeRestrictor(ObjectTable objects)
		{
			var generator = new ResetRewardGenerator(methodName: "RestrictRanges");
			generator.GenerateCode(objects);
			return generator.Compile(objects);
		}
	}
}