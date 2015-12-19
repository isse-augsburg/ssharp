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

namespace Tests.Serialization.Ranges
{
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class Char : SerializationObject
	{
		protected override void Check()
		{
			var c = new C { F = 'y', G = 'y' };

			GenerateCode(SerializationMode.Full, c);

			Serialize();
			c.F = 'z';
			c.G = 'z';
			Deserialize();
			c.F.ShouldBe('q');
			c.G.ShouldBe('q');

			c.F = System.Char.MaxValue;
			c.Q = System.Char.MaxValue;

			Serialize();
			Deserialize();

			c.F.ShouldBe('q');
			c.Q.ShouldBe(System.Char.MaxValue);

			c.F = System.Char.MinValue;
			c.Q = System.Char.MinValue;

			Serialize();
			Deserialize();

			c.F.ShouldBe('a');
			c.Q.ShouldBe(System.Char.MinValue);
		}

		internal class C
		{
			public char F;
			public char Q;

			[Range('a', 'q', OverflowBehavior.Clamp)]
			public char G;

			public C()
			{
				Range.Restrict(F, 'a', 'q', OverflowBehavior.Clamp);
			}
		}
	}
}