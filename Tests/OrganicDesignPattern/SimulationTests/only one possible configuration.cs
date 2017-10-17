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

namespace Tests.OrganicDesignPattern.SimulationTests
{
	using System.Linq;
	using SafetySharp.Odp;
	using SafetySharp.Odp.Reconfiguration;
	using Shouldly;

	class OnlyOneConfiguration : OdpTestObject
	{
		protected override void Check()
		{
			// create model
			var agents = Enumerable.Range(0, 6).Select(i => new Agent()).ToArray();

			agents[0].Connect(agents[1]);
			agents[1].Connect(agents[2]);
			agents[1].Connect(agents[3]);
			agents[2].Connect(agents[3]);
			agents[3].Connect(agents[4]);
			agents[5].Connect(agents[4]);

			agents[0].Capabilities.Add(new ProduceCapability());
			agents[2].Capabilities.Add(new ProcessCapabilityA());
			agents[3].Capabilities.Add(new ProcessCapabilityB());
			agents[4].Capabilities.Add(new ConsumeCapability());

			var task = new Task(new ProduceCapability(), new ProcessCapabilityA(), new ProcessCapabilityB(), new ConsumeCapability());

			// create controllers
			var fast = new FastController(agents);
			var optimal = new OptimalController(agents);
			var controllers = new[] { fast, optimal };

			foreach (var controller in controllers)
			{
				foreach (var agent in agents)
					agent.ReconfigurationStrategy = new CentralReconfiguration(controller);
				agents[0].ConfigureTask(task);

				// verify configuration

				var producerRole = new Role(new Condition(task, 0), new Condition(task, 0, agents[1]))
					.WithCapability(new ProduceCapability());
				agents[0].AllocatedRoles.ShouldBe(new[] { producerRole }, ignoreOrder: false);

				var transportRole = new Role(
						new Condition(task, producerRole.PostCondition.StateLength, agents[0]),
						new Condition(task, producerRole.PostCondition.StateLength, agents[2])
					);
				agents[1].AllocatedRoles.ShouldBe(new [] { transportRole });

				var processRoleA = new Role(
						new Condition(task, transportRole.PostCondition.StateLength, agents[1]),
						new Condition(task, transportRole.PostCondition.StateLength, agents[3])
					).WithCapability(new ProcessCapabilityA());
				agents[2].AllocatedRoles.ShouldBe(new [] { processRoleA });

				var processRoleB = new Role(
						new Condition(task, processRoleA.PostCondition.StateLength, agents[2]),
						new Condition(task, processRoleA.PostCondition.StateLength, agents[4])
					).WithCapability(new ProcessCapabilityB());
				agents[3].AllocatedRoles.ShouldBe(new[] { processRoleB });

				var consumerRole = new Role(
						new Condition(task, processRoleB.PostCondition.StateLength, agents[3]),
						new Condition(task, processRoleB.PostCondition.StateLength)
					).WithCapability(new ConsumeCapability());
				agents[4].AllocatedRoles.ShouldBe(new[] { consumerRole });

				agents[5].AllocatedRoles.ShouldBeEmpty();

			}
		}

		private class ProcessCapabilityA : Capability<ProcessCapabilityA>
		{
			public override CapabilityType CapabilityType => CapabilityType.Process;

			public override bool Equals(object a) => a is ProcessCapabilityA;
		}
		private class ProcessCapabilityB : Capability<ProcessCapabilityB>
		{
			public override CapabilityType CapabilityType => CapabilityType.Process;

			public override bool Equals(object b) => b is ProcessCapabilityB;
		}
	}
}