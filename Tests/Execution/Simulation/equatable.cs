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

namespace Tests.Execution.Simulation
{
	using System;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class Equatable : TestObject
	{
		protected override void Check()
		{
			var simulator = new SafetySharpSimulator(TestModel.InitializeModel(new C()));
			var c = (C)simulator.Model.Roots[0];

			c.D.X.ShouldBe(44);
			c.E.X.ShouldBe(44);

			simulator.SimulateStep();
			c.D.X.ShouldBe(45);
			c.E.X.ShouldBe(46);
		}

		private class C : Component
		{
			public readonly D D = new D { X = 44 };
			public readonly D E = new D { X = 44 };

			public override void Update()
			{
				D.X += 1;
				E.X += 2;
			}
		}

		private class D : IEquatable<D>
		{
			public int X;

			public bool Equals(D other)
			{
				throw new InvalidOperationException("Equals should never be called by S#.");
			}

			public override bool Equals(object obj)
			{
				throw new InvalidOperationException("Equals should never be called by S#.");
			}

			public override int GetHashCode()
			{
				throw new InvalidOperationException("GetHashCode should never be called by S#.");
			}
		}
	}
}