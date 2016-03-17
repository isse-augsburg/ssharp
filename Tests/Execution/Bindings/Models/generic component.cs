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

namespace Tests.Execution.Bindings.Models
{
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class X4 : TestObject
	{
		protected override void Check()
		{
			var m = new M();
			m.X.M(33).ShouldBe(33);
		}

		private class M : ModelBase
		{
			public readonly X<int> X = new X<int>();

			public M()
			{
				Bind(nameof(X.M), nameof(X.N));
			}
		}

		private class X<T> : Component
		{
			public X()
			{
				Bind(nameof(M), nameof(N));
			}

			public extern T M(T i);

			public T N(T i)
			{
				return i;
			}
		}
	}
}