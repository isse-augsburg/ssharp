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
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Describes the layout of a state vector.
	/// </summary>
	public class StateVectorLayout : IEnumerable<StateSlotMetadata>
	{
		/// <summary>
		///   The objects whose data is stored in the state vector.
		/// </summary>
		private readonly ObjectTable _objects;

		/// <summary>
		///   Provides the metadata of the individual slots of the state vector.
		/// </summary>
		private readonly List<StateSlotMetadata> _slots = new List<StateSlotMetadata>();

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="objects">The objects whose data is stored in the state vector.</param>
		internal StateVectorLayout(ObjectTable objects)
		{
			Requires.NotNull(objects, nameof(objects));
			_objects = objects;
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="slotMetadata">The slot metadata that should be added to the state layout..</param>
		internal StateVectorLayout(StateSlotMetadata[] slotMetadata)
		{
			Requires.NotNull(slotMetadata, nameof(slotMetadata));
			_slots.AddRange(slotMetadata);
		}

		/// <summary>
		///   Gets the description of the compacted state vector layout.
		/// </summary>
		internal CompactedStateGroup[] Groups { get; private set; }

		/// <summary>
		///   Gets the number of slots the vector consists of.
		/// </summary>
		public int SlotCount => _slots.Count;

		/// <summary>
		///   Gets the number of bytes at the beginning of the state vector that are used to store fault activation states.
		/// </summary>
		public int FaultBytes { get; private set; }

		/// <summary>
		///   Gets the metadata of the data stored at the <paramref name="index" />.
		/// </summary>
		/// <param name="index">The zero-based index the slot metadata should be returned for.</param>
		public StateSlotMetadata this[int index]
		{
			get
			{
				Requires.InRange(index, nameof(index), _slots);
				return _slots[index];
			}
		}

		/// <summary>
		///   Gets the size in bytes required by the state vector. The size is always a multiple of 4.
		/// </summary>
		internal int SizeInBytes { get; private set; }

		/// <summary>
		///   Returns an enumerator that iterates through the collection.
		/// </summary>
		public IEnumerator<StateSlotMetadata> GetEnumerator()
		{
			return _slots.GetEnumerator();
		}

		/// <summary>
		///   Returns an enumerator that iterates through a collection.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		///   Adds the <paramref name="slotMetadata" /> to the state vector.
		/// </summary>
		/// <param name="slotMetadata">The slot metadata that should be added.</param>
		internal void Add(StateSlotMetadata slotMetadata)
		{
			_slots.Add(slotMetadata);
		}

		/// <summary>
		///   Compacts the state vector.
		/// </summary>
		internal void Compact()
		{
			// Check if we can use the range to compress the data even further
			foreach (var slot in _slots)
			{
				var range = slot.Field != null
					? Range.GetMetadata(slot.Object, slot.Field)
					: Range.CreateDefaultRange(slot.DataType);

				if (range != null)
					slot.CompressedDataType = range.GetRangeType();
			}

			// Organize all slots with the same element size into groups
			Groups = _slots
				.Where(slot => !slot.IsFaultActivationField)
				.GroupBy(slot => slot.ElementSizeInBits)
				.OrderBy(group => group.Key)
				.Select(group => new CompactedStateGroup
				{
					Slots = group
						.OrderBy(slot => slot.ObjectIdentifier)
						.ThenBy(slot => slot.Field?.Name ?? "array")
						.ThenBy(slot => slot.Field?.DeclaringType?.FullName ?? "array")
						.ToArray()
				})
				.ToArray();

			// Fault activation states always occupy the first bytes
			var faultSlots = _slots.Where(slot => slot.IsFaultActivationField).ToArray();
			FaultBytes = faultSlots.Length / 8 + (faultSlots.Length % 8 == 0 ? 0 : 1);
			if (faultSlots.Length > 0)
				Groups = new[] { new CompactedStateGroup { Slots = faultSlots } }.Concat(Groups).ToArray();

			// Compute the total state vector size and ensure alignment of the individual groups
			// Ensure that the state vector size is a multiple of 4 for correct alignment in state vector arrays
			for (var i = 0; i < Groups.Length; ++i)
			{
				Groups[i].OffsetInBytes = SizeInBytes;
				SizeInBytes += Groups[i].GroupSizeInBytes;

				var alignment = i + 1 >= Groups.Length ? 4 : Math.Min(4, Math.Max(1, Groups[i + 1].ElementSizeInBits / 8));
				var remainder = SizeInBytes % alignment;

				Groups[i].PaddingBytes = remainder == 0 ? 0 : alignment - remainder;
				SizeInBytes += Groups[i].PaddingBytes;
			}
		}

		/// <summary>
		///   Dynamically generates a delegate that can be used to serialize the state vector.
		/// </summary>
		internal unsafe SerializationDelegate CreateSerializer()
		{
			var generator = new SerializationGenerator(methodName: "Serialize");
			generator.GenerateSerializationCode(Groups);
			return generator.Compile(_objects);
		}

		/// <summary>
		///   Dynamically generates a delegate that can be used to deserialize the state vector.
		/// </summary>
		internal unsafe SerializationDelegate CreateDeserializer()
		{
			var generator = new SerializationGenerator(methodName: "Deserialize");
			generator.GenerateDeserializationCode(Groups);
			return generator.Compile(_objects);
		}

		/// <summary>
		///   Returns a string that represents the current object.
		/// </summary>
		public override string ToString()
		{
			var builder = new StringBuilder();

			if (Groups == null)
				return base.ToString();

			builder.AppendLine($"state vector size: {SizeInBytes} bytes");
			foreach (var group in Groups)
			{
				var elementCount = group.Slots.Sum(slot => slot.ElementCount);
				builder.AppendLine($"group of {group.ElementSizeInBits} bit values, {elementCount} elements at offset {group.OffsetInBytes}");

				foreach (var slot in group.Slots)
				{
					if (slot.Field == null)
						builder.AppendLine($"\tobj#{slot.ObjectIdentifier}: {slot.EffectiveType.FullName}[{slot.ElementCount}]");
					else
						builder.AppendLine(
							$"\tobj#{slot.ObjectIdentifier}: {slot.Field.DeclaringType.FullName}.{slot.Field.Name}");
				}

				if (group.PaddingBytes > 0)
					builder.AppendLine($"padding: {group.PaddingBytes} bytes");
			}

			return builder.ToString();
		}
	}
}