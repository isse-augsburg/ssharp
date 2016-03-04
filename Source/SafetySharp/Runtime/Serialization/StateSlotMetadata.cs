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
	using System.Reflection;
	using Modeling;
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
		public RangeAttribute Range;

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
				   CompressedDataType == other.CompressedDataType;
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
			return left.Equals(right);
		}

		/// <summary>
		///   Checks whether <paramref name="left" /> and <paramref name="right" /> are not equal.
		/// </summary>
		/// <param name="left">The first instance that should be compared.</param>
		/// <param name="right">The second instance that should be compared.</param>
		public static bool operator !=(StateSlotMetadata left, StateSlotMetadata right)
		{
			return !left.Equals(right);
		}
	}
}