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

// ReSharper disable SuspiciousTypeConversion.Global
namespace Tests.Serialization.Objects
{
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class FaultEffect : SerializationObject
	{
		protected override void Check()
		{
			var c = new C { I = 1 };
			c.F.AddEffect<E>(c).R = 3;
			
			GenerateCode(SerializationMode.Optimized, c);
			StateSlotCount.ShouldBe(2); // original fault effect should not be serialized

			Serialize();
			c.I = 0;
			((E)c.FaultEffects[0]).R = -1;
			((E)c.FaultEffects[0]).I = -2;
			Deserialize();

			c.I.ShouldBe(1);
			((E)c.FaultEffects[0]).R.ShouldBe(-1);
			((E)c.FaultEffects[0]).I.ShouldBe(-2);
		}

		private class C : Component
		{
			public int I;
			public readonly Fault F = new TransientFault();
		}

		[FaultEffect]
		private class E : C
		{
			public int R;
		}
	}
}