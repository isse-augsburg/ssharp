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
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;

	internal class RangesViaAttributes : TestModel
	{
		private static bool _hasConstructorRun;

		protected override void Check()
		{
			var c = new C { F1 = 99, F2 = 12, F3 = -1, F4 = 3 };
			var m = InitializeModel(c);

			_hasConstructorRun = false;
			Create(m);

			StateVectorSize.ShouldBe(4);
			ExecutableStateFormulas.ShouldBeEmpty();
			RootComponents.Length.ShouldBe(1);

			var root = RootComponents[0];
			root.ShouldBeOfType<C>();
			((C)root).F1.ShouldBe(5);
			((C)root).F2.ShouldBe(5);
			((C)root).F3.ShouldBe(0);
			((C)root).F4.ShouldBe(3);
			root.GetSubcomponents().ShouldBeEmpty();

			_hasConstructorRun.ShouldBe(false);
		}

		private class C : Component
		{
			[Range(0, 5, OverflowBehavior.Clamp)]
			public int F1;

			[Range(0, 5, OverflowBehavior.Clamp)]
			public int F2;

			[Range(0, 5, OverflowBehavior.Clamp)]
			public int F3;

			[Range(0, 5, OverflowBehavior.Clamp)]
			public int F4;

			public C()
			{
				_hasConstructorRun = true;
			}
		}
	}
}