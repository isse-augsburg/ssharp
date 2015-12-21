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

namespace Tests.Serialization.Compaction
{
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Reflection;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class StateMachineRange : SerializationObject
	{
		protected override void Check()
		{
			var s1 = new StateMachine<E>(E.B);
			var s2 = new StateMachine<E>(E.C);
			var s3 = new StateMachine<E>(E.G);

			GenerateCode(SerializationMode.Optimized, s1, s2, s3);
			StateSlotCount.ShouldBe(1);

			Serialize();
			s1.ChangeState(E.A);
			s2.ChangeState(E.A);
			s3.ChangeState(E.A);

			Deserialize();
			s1.State.ShouldBe(E.B);
			s2.State.ShouldBe(E.C);
			s3.State.ShouldBe(E.G);
		}

		private enum E
		{
			A,
			B,
			C,
			D,
			F,
			G
		}
	}
}