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

namespace SafetySharp.Runtime.Serialization.Serializers
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Reflection.Emit;
	using Modeling;

	// Currently Rewards are value types and not reference types.
	// They never get serialized. Thus, they do not contribute any data or meta-data.
	// During deserialization their fields are reseted to zero.

	/// <summary>
	///   Serializes <see cref="Reward" /> instances.
	/// </summary>
	internal sealed class RewardSerializer : Serializer
	{
		private static readonly ConstructorInfo _createReward;
		private static readonly FieldInfo _fieldInfoRewardPositive;
		private static readonly FieldInfo _fieldInfoRewardNegative;
		private static readonly FieldInfo _fieldInfoMightBeNegative;

		static RewardSerializer()
		{
			_createReward = typeof(Reward).GetConstructor(new []{typeof(bool)});

			_fieldInfoRewardPositive = typeof(Reward).GetField("_valuePositive", BindingFlags.Instance|BindingFlags.NonPublic);
			_fieldInfoRewardNegative = typeof(Reward).GetField("_valueNegative", BindingFlags.Instance|BindingFlags.NonPublic);
			_fieldInfoMightBeNegative = typeof(Reward).GetField(nameof(Reward.MightBeNegative));
		}


		/// <summary>
		///   Checks whether the serialize is able to serialize the <paramref name="obj" />.
		/// </summary>
		/// <param name="obj">The obj that should be checked.</param>
		protected internal override bool CanSerialize(object obj)
		{
			if (obj is Reward)
				Debugger.Break();
			return obj is Reward;
		}

		/// <summary>
		///   Generates the state slot metadata for the <paramref name="obj" />.
		/// </summary>
		/// <param name="obj">The object the state slot metadata should be generated for.</param>
		/// <param name="objectIdentifier">The identifier of the <paramref name="obj" />.</param>
		/// <param name="mode">The serialization mode that should be used to generate the metadata.</param>
		protected internal override IEnumerable<StateSlotMetadata> GetStateSlotMetadata(object obj, int objectIdentifier, SerializationMode mode)
		{
			// Nothing to do for Rewards
			yield break;
		}

		/// <summary>
		///   Serializes the information about <paramref name="obj" />'s type using the <paramref name="writer" />.
		/// </summary>
		/// <param name="obj">The object whose type information should be serialized.</param>
		/// <param name="writer">The writer the serialized information should be written to.</param>
		protected internal override void SerializeType(object obj, BinaryWriter writer)
		{
			// never called
			var reward = (Reward) obj;
			writer.Write(reward.MightBeNegative);
		}

		/// <summary>
		///   Creates an instance of the serialized type stored in the <paramref name="reader" /> without running
		///   any of the type's constructors.
		/// </summary>
		/// <param name="reader">The reader the serialized type information should be read from.</param>
		protected internal override object InstantiateType(BinaryReader reader)
		{
			// never called
			var mightBeNegative = reader.ReadBoolean();
			return new Reward(mightBeNegative);
		}

		/// <summary>
		///   Gets all objects referenced by <paramref name="obj" />, excluding <paramref name="obj" /> itself.
		/// </summary>
		/// <param name="obj">The object the referenced objects should be returned for.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		protected internal override IEnumerable<object> GetReferencedObjects(object obj, SerializationMode mode)
		{
			return Enumerable.Empty<object>();
		}

		internal static int GetElementSizeInBits()
		{
			return 0*8;
		}

		internal static bool IsReward(StateSlotMetadata stateSlotMetadata)
		{
			return typeof(Reward) == stateSlotMetadata.DataType;
		}

		/*
		/// <summary>
		///   Generates the code to serialize the <paramref name="field" /> of the object stored in the local variable.
		/// </summary>
		/// <param name="field">The field that should be serialized.</param>
		internal static void SerializeField(ILGenerator il,FieldInfo field)
		{
			// *posInStateVector = o.field.MightBeNegative

			il.Emit(OpCodes.Ldloc_0); // Ldloc_0 contains a pointer on the position in the stateVector (byte*) where the value should be saved (see constructor of SerializationGenerator)

			il.Emit(OpCodes.Ldloc_1); // Ldloc_1 contains the object which contains field
			il.Emit(OpCodes.Ldflda, field); //Note: Because Reward is a value type we need Ldflda instead of Ldfld
			il.Emit(OpCodes.Ldfld, _fieldInfoMightBeNegative);

			il.Emit(OpCodes.Stind_I1);
		}


		/// <summary>
		///   Generates the code to deserialize the <paramref name="field" /> of the object stored in the local variable.
		/// </summary>
		/// <param name="field">The field that should be serialized.</param>
		internal static void DeserializeField(ILGenerator il, FieldInfo field)
		{
			// o.field = new Probability(*posInStateVector)
			il.Emit(OpCodes.Ldloc_1); // Ldloc_1 contains the object which contains field

			il.Emit(OpCodes.Ldloc_0); // Ldloc_0 contains a pointer on the position in the stateVector (byte*) where the value to load is located (see constructor of SerializationGenerator)
			il.Emit(OpCodes.Ldind_I1); //boolean value is saved as integer
			il.Emit(OpCodes.Newobj, _createReward);

			// o.field = v
			il.Emit(OpCodes.Stfld, field);
		}
		*/

		/// <summary>
		///   Generates the code to serialize the <paramref name="field" /> of the object stored in the local variable.
		/// </summary>
		/// <param name="field">The field that should be serialized.</param>
		internal static void ResetFields(ILGenerator il, FieldInfo field)
		{
			//here we need to reset both Reward Fields
			// o.field.ValuePositive = 0
			// o.field.ValueNegative = 0
			il.Emit(OpCodes.Ldloc_1); // Ldloc_1 contains the object which contains field
			il.Emit(OpCodes.Ldflda, field); //Note: Because Reward is a value type we need Ldflda instead of Ldfld
			il.Emit(OpCodes.Dup); //We double the pointer towards Reward, because we need it two times
								  // now the location of Reward is on the stack two times
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Stfld, _fieldInfoRewardPositive);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Stfld, _fieldInfoRewardNegative);
		}
	}
}