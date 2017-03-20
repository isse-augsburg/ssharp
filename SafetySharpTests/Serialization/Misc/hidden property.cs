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

namespace Tests.Serialization.Misc
{
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Serialization;
	using SafetySharp.Utilities;
	using Shouldly;

	internal class HiddenProperty : SerializationObject
	{
		protected override void Check()
		{
			var c = new C { X = 1 };

			GenerateCode(SerializationMode.Full, c);
			StateSlotCount.ShouldBe(2);

			Serialize();
			c.X = 2;

			typeof(C).GetProperty("Y").GetBackingField().SetValue(c, 102);
			typeof(C).GetProperty("Z").GetBackingField().SetValue(c, 101);

			c.Y.ShouldBe(102);
			c.Z.ShouldBe(101);

			Deserialize();
			c.X.ShouldBe(1);
			c.Y.ShouldBe(102);
			c.Z.ShouldBe(11);

			GenerateCode(SerializationMode.Optimized, c);
			StateSlotCount.ShouldBe(1);

			Serialize();
			c.X = 2;

			typeof(C).GetProperty("Y").GetBackingField().SetValue(c, 1102);
			typeof(C).GetProperty("Z").GetBackingField().SetValue(c, 1101);

			c.Y.ShouldBe(1102);
			c.Z.ShouldBe(1101);

			Deserialize();
			c.X.ShouldBe(2);
			c.Y.ShouldBe(1102);
			c.Z.ShouldBe(1101);
		}

		internal class C
		{
			[Hidden]
			public int X { get; set; }

			[NonSerializable]
			public int Y { get; } = 99;

			public int Z { get; } = 11;
		}

		internal class D
		{
			public int X;
		}
	}
}