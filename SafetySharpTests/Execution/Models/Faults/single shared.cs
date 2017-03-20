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

namespace Tests.Execution.Models.Faults
{
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class SharedSingleFaultInSubcomponent : TestObject
	{
		protected override void Check()
		{
			var d = new D { };
			var m = TestModel.InitializeModel(d);

			m.Faults.ShouldBe(new[] { d.F });
		}

		private class D : Component
		{
			public C1 C1;
			public C2 C2;
			public C1 C3;

			public readonly Fault F = new TransientFault();

			public D()
			{
				C1 = new C1(F);
				C2 = new C2(F);
				C3 = new C1(F);
			}
		}

		private class C1 : Component
		{
			public C1(Fault f = null)
			{
				f.AddEffect<E>(this);
			}

			[FaultEffect]
			private class E : C1
			{
			}
		}

		private class C2 : Component
		{
			public C2(Fault f = null)
			{
				f.AddEffect<E>(this);
			}

			[FaultEffect]
			private class E : C2
			{
			}
		}
	}
}