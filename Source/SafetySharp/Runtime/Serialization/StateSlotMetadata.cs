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
	using System.Linq;
	using System.Reflection;
	using Utilities;

	/// <summary>
	///   Describes the contents stored in a state slot of a state vector.
	/// </summary>
	[Serializable]
	public class StateSlotMetadata : IEquatable<StateSlotMetadata>
	{
		/// <summary>
		///   The compressed type of the data stored in the slot.
		/// </summary>
		public Type CompressedDataType;

		/// <summary>
		///   The uncompressed type of the data stored in the slot.
		/// </summary>
		public Type DataType;

		/// <summary>
		///   The number of elements the data consists of.
		/// </summary>
		public int ElementCount;

		/// <summary>
		///   The field stored in the slot, if any.
		/// </summary>
		public FieldInfo Field;

		/// <summary>
		///   The chain of fields if the data is stored in possibly nested structs.
		/// </summary>
		public FieldInfo[] FieldChain;

		/// <summary>
		///   The object whose data is stored in the slot.
		/// </summary>
		[NonSerialized]
		public object Object;

		/// <summary>
		///   The identifier of the object whose data is stored in the slot.
		/// </summary>
		public int ObjectIdentifier;

		/// <summary>
		///   The type of the object whose data is stored in the slot.
		/// </summary>
		public Type ObjectType;

		/// <summary>
		///   The range metadata of the slot, if any.
		/// </summary>
		[NonSerialized]
		public RangeMetadata Range;

		/// <summary>
		///   Gets a value indicating whether the data is stored in a struct.
		/// </summary>
		public bool ContainedInStruct => (FieldChain?.Length ?? 0) != 0;

		/// <summary>
		///   Gets the effective type of the data stored in the slot.
		/// </summary>
		public Type EffectiveType => CompressedDataType ?? DataType;

		/// <summary>
		///   Gets the total size in bits required to store the data in the state vector.
		/// </summary>
		public int TotalSizeInBits => ElementSizeInBits * ElementCount;

		/// <summary>
		///   Gets the size in bits required to store each individual element in the state vector.
		/// </summary>
		public int ElementSizeInBits
		{
			get
			{
				if (DataType.IsReferenceType())
					return sizeof(ushort) * 8;

				if (DataType == typeof(bool))
					return 1;

				if (DataType == typeof(Probability))
					return Serializers.ProbabilitySerializer.GetElementSizeInBits();

				if (DataType == typeof(Reward))
					return Serializers.RewardSerializer.GetElementSizeInBits();

				return EffectiveType.GetUnmanagedSize() * 8;
			}
		}

		/// <summary>
		///   Indicates whether the current object is equal to <paramref name="other" />.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		public bool Equals(StateSlotMetadata other)
		{
			return ObjectIdentifier == other.ObjectIdentifier &&
				   ObjectType == other.ObjectType &&
				   ElementCount == other.ElementCount &&
				   Equals(Field, other.Field) &&
				   DataType == other.DataType &&
				   CompressedDataType == other.CompressedDataType &&
				   (FieldChain?.SequenceEqual(other.FieldChain) ?? true);
		}

		/// <summary>
		///   Indicates whether this instance and <paramref name="obj" /> are equal.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance.</param>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;

			return obj is StateSlotMetadata && Equals((StateSlotMetadata)obj);
		}

		/// <summary>
		///   Returns the hash code for this instance.
		/// </summary>
		public override int GetHashCode()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///   Checks whether <paramref name="left" /> and <paramref name="right" /> are equal.
		/// </summary>
		/// <param name="left">The first instance that should be compared.</param>
		/// <param name="right">The second instance that should be compared.</param>
		public static bool operator ==(StateSlotMetadata left, StateSlotMetadata right)
		{
			return left?.Equals(right) ?? right == null;
		}

		/// <summary>
		///   Checks whether <paramref name="left" /> and <paramref name="right" /> are not equal.
		/// </summary>
		/// <param name="left">The first instance that should be compared.</param>
		/// <param name="right">The second instance that should be compared.</param>
		public static bool operator !=(StateSlotMetadata left, StateSlotMetadata right)
		{
			return !(left == right);
		}

		/// <summary>
		///   Creates the metadata required to serialize the <paramref name="structType" />.
		/// </summary>
		/// <param name="structType">The type of the struct the metadata should be created for.</param>
		public static IEnumerable<StateSlotMetadata> FromStruct(Type structType)
		{
			Requires.NotNull(structType, nameof(structType));
			Requires.That(structType.IsStructType(), "Expected a value type.");

			return FromStruct(structType, new FieldInfo[0]);
		}

		/// <summary>
		///   Creates the metadata required to serialize the <paramref name="structType" /> with the <paramref name="fieldChain" />.
		/// </summary>
		public static IEnumerable<StateSlotMetadata> FromStruct(Type structType, FieldInfo[] fieldChain)
		{
			var fields = structType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var field in fields)
			{
				var chain = fieldChain.Concat(new[] { field }).ToArray();

				if (field.FieldType.IsStructType())
				{
					foreach (var metadataSlot in FromStruct(field.FieldType, chain))
						yield return metadataSlot;
				}
				else
				{
					yield return new StateSlotMetadata
					{
						DataType = field.FieldType,
						FieldChain = chain
					};
				}
			}
		}
	}
}