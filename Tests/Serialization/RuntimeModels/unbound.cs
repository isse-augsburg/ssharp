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

namespace Tests.Serialization.RuntimeModels
{
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;

	internal class Unbound : TestModel
	{
		protected override void Check()
		{
			var d = new D();

			Should.Throw<UnboundPortException>(() => d.R());
			Should.Throw<UnboundPortException>(() => { var x = d.A; });
			Should.Throw<UnboundPortException>(() => d.B = 0);
			Should.Throw<UnboundPortException>(() => { var x = d.C; });
			Should.Throw<UnboundPortException>(() => d.C = 0);

			var m = TestModel.InitializeModel(d);
			Create(m);

			StateFormulas.ShouldBeEmpty();
			RootComponents.Length.ShouldBe(1);
			RuntimeModel.StateVectorLayout.Groups.ShouldBeEmpty();

			var root = RootComponents[0];
			root.ShouldBeOfType<D>();
			d = (D)root;

			Should.Throw<UnboundPortException>(() => d.R());
			Should.Throw<UnboundPortException>(() => { var x = d.A; });
			Should.Throw<UnboundPortException>(() => d.B = 0);
			Should.Throw<UnboundPortException>(() => { var x = d.C; });
			Should.Throw<UnboundPortException>(() => d.C = 0);
		}

		private class D : Component
		{
			public extern int A { get; }
			public extern int B { set; }
			public extern int C { get; set; }

			public extern int R();
		}
	}
}