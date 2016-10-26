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
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class Events : SerializationObject
	{
		protected override void Check()
		{
			var c = new C();
			c.O += c.M;
			c.P += c.M;

			GenerateCode(SerializationMode.Full, c);

			Serialize();
			c.O -= c.M;
			c.P -= c.M;

			Deserialize();

			c.RaiseO(4);
			c.F.ShouldBe(4);
			c.RaiseP(2);
			c.F.ShouldBe(6);

			GenerateCode(SerializationMode.Optimized, c);

			Serialize();
			c.O -= c.M;

			Deserialize();
			c.RaiseO(4);
			c.F.ShouldBe(10);

			c.O += c.N;
			Should.Throw<InvalidOperationException>(() => Serialize());

			c = new C();
			c.O += c.M;
			c.O += c.N;

			GenerateCode(SerializationMode.Full, c);

			Serialize();
			c.O -= c.M;

			Deserialize();

			c.RaiseO(4);
			c.F.ShouldBe(12);

			c = new C();
			c.P += c.M;

			GenerateCode(SerializationMode.Optimized, c);
			Serialize();

			c.P -= c.M;

			Deserialize();
			Should.Throw<NullReferenceException>(() => c.RaiseP(1));
		}

		private class C
		{
			public event Action<int> O;

			[Hidden]
			public event Action<int> P;

			public int F { get; private set; }

			public void M(int f)
			{
				F += f;
			}

			public void N(int f)
			{
				F += 2 * f;
			}

			public void RaiseO(int f)
			{
				O(f);
			}

			public void RaiseP(int f)
			{
				P(f);
			}
		}

		
	}
}