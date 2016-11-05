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

namespace Tests.OrganicDesignPattern
{
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Odp;
	using Shouldly;
	using Utilities;

	internal class RoleExecution : TestObject
	{
		protected override void Check()
		{
			var capabilities = new[] {
				new C() { Action = 12 },
				new C() { Action = 4 },
				new C() { Action = -3 }
			};

			var t = new Task(capabilities);
			var r = new Role() { PreCondition = { Task = t }, PostCondition = { Task = t } };

			foreach (var c in capabilities)
				r.AddCapability(c);

			var a = new A();
			a.Capabilities.AddRange(capabilities);
			a.AllocatedRoles.Add(r);

			var simulator = new Simulator(TestModel.InitializeModel(a));
			simulator.Model.Faults.SuppressActivations();
			a = (A)simulator.Model.Roots[0];

			simulator.FastForward(steps: 2); // role selection
			a.LastAction.ShouldBe(0);

			simulator.SimulateStep();
			a.LastAction.ShouldBe(capabilities[0].Action);

			simulator.SimulateStep();
			a.LastAction.ShouldBe(capabilities[1].Action);

			simulator.SimulateStep();
			a.LastAction.ShouldBe(capabilities[2].Action);
		}

		private class A : Agent, ICapabilityHandler<C>
		{
			public int LastAction = 0;

			public void ApplyCapability(C capability)
			{
				LastAction = capability.Action;
			}
		}

		private class C : Capability<C>
		{
			public int Action;
			public override CapabilityType CapabilityType => CapabilityType.Process;
		}
	}
}