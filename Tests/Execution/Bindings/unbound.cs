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

namespace Tests.Execution.Bindings
{
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;

	internal class Unbound : TestComponent
	{
		private extern int X1 { get; }
		private extern int X2 { set; get; }
		private extern int X3 { set; }
		private extern void N();

		public virtual extern int Y1 { get; }
		public virtual extern int Y2 { set; get; }
		public virtual extern int Y3 { set; }
		public virtual extern void M();

		protected override void Check()
		{
			var x = 0;

			Should.Throw<UnboundPortException>(() => N());
			Should.Throw<UnboundPortException>(() => x = X1);
			Should.Throw<UnboundPortException>(() => X2 = x);
			Should.Throw<UnboundPortException>(() => x = X2);
			Should.Throw<UnboundPortException>(() => X3 = x);

			Should.Throw<UnboundPortException>(() => M());
			Should.Throw<UnboundPortException>(() => x = Y1);
			Should.Throw<UnboundPortException>(() => Y2 = x);
			Should.Throw<UnboundPortException>(() => x = Y2);
			Should.Throw<UnboundPortException>(() => Y3 = x);
		}
	}
}