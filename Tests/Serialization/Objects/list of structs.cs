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
	using System.Collections.Generic;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class ListOfStructs : SerializationObject
	{
		protected override void Check()
		{
			var o1 = new object();
			var o2 = new object();
			var o3 = new object();
			var l = new List<S> { new S(o1, o2), new S(o3, o2), new S(o1, o3) };

			GenerateCode(SerializationMode.Optimized, l, o1, o2, o3);

			Serialize();
			l[0] = new S();
			l[1] = new S();
			l[2] = new S();

			Deserialize();
			l[0].O1.ShouldBe(o1);
			l[0].O2.ShouldBe(o2);
			l[1].O1.ShouldBe(o3);
			l[1].O2.ShouldBe(o2);
			l[2].O1.ShouldBe(o1);
			l[2].O2.ShouldBe(o3);

			l.Remove(new S(o3, o2));

			Serialize();
			l[0] = new S();
			l[1] = new S();

			Deserialize();
			l[0].O1.ShouldBe(o1);
			l[0].O2.ShouldBe(o2);
			l[1].O1.ShouldBe(o1);
			l[1].O2.ShouldBe(o3);
		}

		private struct S
		{
			public S(object o1, object o2)
			{
				O1 = o1;
				O2 = o2;
			}

			public object O1 { get; }
			public object O2 { get; }
		}
	}
}