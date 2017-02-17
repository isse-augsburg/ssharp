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

namespace Tests.Serialization.RuntimeModels
{
	using System;
	using SafetySharp.CompilerServices;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class ManualBinding : TestModel
	{
		protected override void Check()
		{
			var d = new D { A = 7, C = 3 };
			var r = new PortReference(d, typeof(D), "M", new[] { typeof(bool), typeof(int) }, typeof(bool), false);
			var p = new PortReference(d, typeof(D), "Q", new[] { typeof(bool), typeof(int) }, typeof(bool), true);
			d.B = new PortBinding(r, p);
			var m = InitializeModel(d);

			Create(m);

			ExecutableStateFormulas.ShouldBeEmpty();
			RootComponents.Length.ShouldBe(1);
			StateSlotCount.ShouldBe(2);

			var root = RootComponents[0];
			root.ShouldBeOfType<D>();

			r = ((D)root).B.RequiredPort;
			p = ((D)root).B.ProvidedPort;

			r.TargetObject.ShouldBe(root);
			r.DeclaringType.ShouldBe(typeof(D));
			r.PortName.ShouldBe("M");
			r.ArgumentTypes.ShouldBe(new[] { typeof(bool), typeof(int) });
			r.ReturnType.ShouldBe(typeof(bool));
			r.IsVirtualCall.ShouldBe(false);

			p.TargetObject.ShouldBe(root);
			p.DeclaringType.ShouldBe(typeof(D));
			p.PortName.ShouldBe("Q");
			p.ArgumentTypes.ShouldBe(new[] { typeof(bool), typeof(int) });
			p.ReturnType.ShouldBe(typeof(bool));
			p.IsVirtualCall.ShouldBe(true);

			((D)root).A.ShouldBe(7);
			((D)root).C.ShouldBe(3);
		}

		private class D : Component
		{
			[NonSerializable]
			private Func<bool, int, bool> _x = null;

			[NonSerializable]
			private PortBinding _y = null;

			public int A;
			public PortBinding B;
			public int C;

			[BindingMetadata("_x", "_y", "f")]
			private bool M(bool b, int i)
			{
				return b;
			}

			private bool Q(bool b, int i)
			{
				return b;
			}

			private void f()
			{
			}
		}
	}
}