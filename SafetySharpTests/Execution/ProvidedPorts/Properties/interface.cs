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

namespace Tests.Execution.ProvidedPorts.Properties
{
	using Shouldly;
	using Utilities;

	interface I
	{
		int M { get; set; }
		int N { get; }
		int Q {  set; }
	}

	internal abstract class BaseInterface : TestComponent, I
	{
		public int M { get; set; }
		int I.N { get; } = 99;
		virtual public int Q { get; set; }
	}

	internal class DerivedInterface : BaseInterface
	{
		public override int Q { get; set; }

		protected override void Check()
		{
			M = 17;
			M.ShouldBe(17);

			Q = 33;
			Q.ShouldBe(33);
			base.Q.ShouldBe(0);

			((I)this).N.ShouldBe(99);

			I x = this;

			x.M = 171;
			x.M.ShouldBe(171);

			x.Q = 331;
			Q.ShouldBe(331);

			x.N.ShouldBe(99);
		}
	}
}