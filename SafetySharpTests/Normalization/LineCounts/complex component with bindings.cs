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

namespace Tests.Normalization.LineCounts
{
	using System.Collections.Generic;
	using SafetySharp.Modeling;

	public class ComplexComponentWithBindings : LineCountTestObject
	{
		public class C<T1, T2> : Component
			where T1 : class, new()
			where T2 : class, new()
		{
			public List<int> P { get; set; }
			private readonly D<T1, T2> _d = new D<T1, T2>();

			public C()
			{
				P = new List<int>();
			}

			private void CreateBindings()
			{
				Bind(nameof(_d.Required1), nameof(Provided1));
				Bind(nameof(_d.Required2), nameof(Provided2));
			}

			public extern void Required1(T1 t);

			public T1 Provided1()
			{
				Required1(_d.Required1());
				return default(T1);
			}

			public extern void Required2(T2 t);

			private T2 Provided2()
			{
				Required2(_d.Required2());
				return default(T2);
			}
		}

		class D<T1, T2> : Component
			where T1 : class, new()
			where T2 : class, new()
		{
			public extern T1 Required1();
			public extern T2 Required2();
		}

		protected override void CheckLines()
		{
			CheckProperty("P", expectedLine: 34, occurrence: 0);
			CheckField("_d", expectedLine: 35, occurrence: 0);

			CheckConstructor("C", expectedLine: 37, occurrence: 0);
			CheckMethod("CreateBindings", expectedLine: 42, occurrence: 0);
			CheckMethod("Required1", expectedLine: 48, occurrence: 0);
			CheckMethod("Required2", expectedLine: 56, occurrence: 0);
			CheckMethod("Required1", expectedLine: 69, occurrence: 1);
			CheckMethod("Required2", expectedLine: 70, occurrence: 1);
			CheckMethod("Provided1", expectedLine: 50, occurrence: 0);
			CheckMethod("Provided2", expectedLine: 58, occurrence: 0);

			CheckClass("ComplexComponentWithBindings", expectedLine: 28, occurrence: 0);
			CheckClass("C", expectedLine: 30, occurrence: 0);
			CheckClass("D", expectedLine: 65, occurrence: 0);
		}
	}
}