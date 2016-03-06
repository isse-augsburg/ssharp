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

namespace Tests.FaultActivation.Invariants
{
	using SafetySharp.Modeling;
	using Shouldly;

	internal class InitialState : FaultActivationTestObject
	{
		protected override void Check()
		{
			GenerateStateSpace(new C1());

			StateCount.ShouldBe(3);
			TransitionCount.ShouldBe(6);
			ComputedTransitionCount.ShouldBe(6);

			GenerateStateSpace(new C2());

			StateCount.ShouldBe(1);
			TransitionCount.ShouldBe(2);
			ComputedTransitionCount.ShouldBe(2);

			GenerateStateSpace(new C3());

			StateCount.ShouldBe(1);
			TransitionCount.ShouldBe(2);
			ComputedTransitionCount.ShouldBe(3);

			GenerateStateSpace(new C4());

			StateCount.ShouldBe(2);
			TransitionCount.ShouldBe(4);
			ComputedTransitionCount.ShouldBe(5);
		}

		private class C1 : Component, IInitializable
		{
			private readonly Fault _f = new TransientFault();

			private int _x;

			public virtual int X => 1;

			public void Initialize()
			{
				_x = X;
			}

			[FaultEffect(Fault = nameof(_f))]
			public class E : C1
			{
				public override int X => Choose(0, 2);
			}
		}

		private class C2 : Component, IInitializable
		{
			private readonly Fault _f = new TransientFault();

			private int _x;

			public virtual int X => 1;

			public void Initialize()
			{
				_x = X;
			}

			[FaultEffect(Fault = nameof(_f))]
			public class E : C2
			{
				public override int X => 1;
			}
		}

		private class C3 : Component, IInitializable
		{
			private readonly Fault _f = new TransientFault();

			private int _x;

			public virtual int X => 1;

			public void Initialize()
			{
				_x = X;
			}

			[FaultEffect(Fault = nameof(_f))]
			public class E : C3
			{
				public override int X => Choose(1);
			}
		}

		private class C4 : Component, IInitializable
		{
			private readonly Fault _f = new TransientFault();

			private int _x;

			public virtual int X => 1;

			public void Initialize()
			{
				_x = X;
			}

			[FaultEffect(Fault = nameof(_f))]
			public class E : C4
			{
				public override int X => Choose(1, 2);
			}
		}
	}
}