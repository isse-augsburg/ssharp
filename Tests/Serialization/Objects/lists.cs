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

namespace Tests.Serialization.Objects
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class Lists : SerializationObject
	{
		protected override void Check()
		{
			var o1 = new object();
			var o2 = new object();
			var c = new C
			{
				I = new List<int> { -17, 2, 12 },
				D = new List<long> { Int64.MaxValue, Int64.MinValue },
				B = new List<bool> { true, false, true },
				O = new List<object> { o1, o2 },
			};

			GenerateCode(SerializationMode.Optimized, c, c.I, o1, o2, c.D, c.B, c.O);

			Serialize();
			c.I[1] = 33;
			c.D[0] = Int64.MinValue;
			c.B[2] = false;
			c.O[1] = null;
			c.I = null;
			c.D = null;
			c.B = null;
			c.O = null;
			Deserialize();

			c.I.ShouldBe(new[] { -17, 2, 12 });
			c.D.ShouldBe(new[] { Int64.MaxValue, Int64.MinValue });
			c.B.ShouldBe(new[] { true, false, true });
			c.O.ShouldBe(new[] { o1, o2 });

			c.I.RemoveAt(2);
			c.D.RemoveAt(0);

			Serialize();
			c.I[1] = 33;
			c.D[0] = Int64.MinValue;
			c.B[2] = false;
			c.O[1] = null;
			c.I = null;
			c.D = null;
			c.B = null;
			c.O = null;
			Deserialize();

			c.I.ShouldBe(new[] { -17, 2 });
			c.D.ShouldBe(new[] { Int64.MinValue });
			c.B.ShouldBe(new[] { true, false, true });
			c.O.ShouldBe(new[] { o1, o2 });

			c.I.Insert(0, 666);
			c.D.Add(0);

			Serialize();
			c.I[1] = 33;
			c.D[0] = Int64.MinValue;
			c.B[2] = false;
			c.O[1] = null;
			Deserialize();

			c.I.ShouldBe(new[] { 666, -17, 2 });
			c.D.ShouldBe(new[] { Int64.MinValue, 0 });
			c.B.ShouldBe(new[] { true, false, true });
			c.O.ShouldBe(new[] { o1, o2 });

			// List resizing is not supported but should result in a helpful error message
			var capacity = c.I.Capacity;
			for (var i = 0; i < capacity; ++i)
				c.I.Add(3);

			var exception = Should.Throw<RangeViolationException>(() => Serialize());
			exception.Field.ShouldBe(typeof(List<int>).GetField("_size", BindingFlags.Instance | BindingFlags.NonPublic));
			exception.FieldValue.ShouldBe(c.I.Count);
			exception.Object.ShouldBe(c.I);
			exception.Range.LowerBound.ShouldBe(0);
			exception.Range.UpperBound.ShouldBe(capacity);
		}

		private class C : Component
		{
			public List<bool> B;
			public List<long> D;
			public List<int> I;
			public List<object> O;
		}
	}
}