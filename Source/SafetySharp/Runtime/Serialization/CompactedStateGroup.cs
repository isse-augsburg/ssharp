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
	using System.Linq;

	/// <summary>
	///   Represents a group of state values of the same compacted size.
	/// </summary>
	internal struct CompactedStateGroup
	{
		/// <summary>
		///   The state slots the group consists of.
		/// </summary>
		public StateSlotMetadata[] Slots;

		/// <summary>
		///   The number of padding bytes after the group. Each group is padded such that the next group starts at a location that is a
		///   multiple of 4.
		/// </summary>
		public int PaddingBytes
		{
			get
			{
				var remainder = GroupSizeInBytes % 4;
				return remainder == 0 ? 0 : 4 - remainder;
			}
		}

		/// <summary>
		///   Gets the size of the group in bytes.
		/// </summary>
		public int GroupSizeInBytes => Slots.Select(slot => slot.TotalSizeInBytes).Sum();

		/// <summary>
		///   The size of each element in bits.
		/// </summary>
		public int SizeInBits => Slots[0].ElementSizeInBits;
	}
}