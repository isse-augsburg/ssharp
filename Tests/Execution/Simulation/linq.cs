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

namespace Tests.Execution.Simulation
{
	using System.Linq;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class LinqQuery : TestObject
	{
		protected override void Check()
		{
			var simulator = new Simulator(TestModel.InitializeModel(new C()));
			var c = (C)simulator.Model.Roots[0];

			c.X.ShouldBe(0);
			c.Y.ShouldBe(0);

			simulator.SimulateStep();
			c.X.ShouldBe(3320);
			c.Y.ShouldBe(2480);
		}

		private class C : Component
		{
			public int X;
			public int Y;

			public C()
			{
				Bind(nameof(Q), nameof(P));
			}

			private extern decimal Q();
			private decimal P() => 33.3m;

			public override void Update()
			{
				var abc = new { Z = P(), W = "test" };

				X = Enumerable
					.Range(2, 100)
					.Where(x => x > 10 && x < 51)
					.Select((x, y) => new { X = x, Y = y })
					.Sum(t => t.X + t.Y + (int)abc.Z);

				Y = (from x in Enumerable.Range(2, 100)
					  where x > 10 && x < 51
					  select new { X = x, Y = x + 1 }).
					Sum(t => t.X + t.Y);
			}
		}
	}
}