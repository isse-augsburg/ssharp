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

namespace Tests.Execution.Models.Faults
{
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class MultipleGeneric : TestObject
	{
		protected override void Check()
		{
			var d = new D { };
			var m = TestModel.New(d);

			m.Faults.ShouldBe(new[] { d.F2, d.C1.F, d.C3.F, d.C2.F }, ignoreOrder: true);
		}

		private class D : Component
		{
			public readonly Fault F1 = new TransientFault(); // Not used - shouldn't be returned
			public readonly Fault F2 = new TransientFault();
			public C1<int> C1;
			public readonly C2 C2;
			public C1<bool> C3;

			public D()
			{
				C1 = new C1<int>();
				C2 = new C2();
				C3 = new C1<bool>();
			}

			[FaultEffect(Fault = nameof(F2))]
			private class E : D
			{
			}
		}

		private class C1<T> : Component
		{
			public readonly Fault F = new TransientFault();

			[FaultEffect(Fault = nameof(F))]
			private class E : C1<T>
			{
			}
		}

		private class C2 : Component
		{
			public readonly Fault F = new PermanentFault();

			[FaultEffect(Fault = nameof(F))]
			private class E : C2
			{
			}
		}
	}
}