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

				var producerRole = new Role { PreCondition = { Task = task }, PostCondition = { Task = task, Port = agents[1] } };
				producerRole.AddCapability(new ProduceCapability());
				agents[0].AllocatedRoles.ShouldBe(new[] { producerRole }, ignoreOrder: false);

				var transportRole = new Role { PreCondition = { Task = task, Port = agents[0] }, PostCondition = { Task = task, Port = agents[2] } };
				transportRole.Initialize(producerRole.PostCondition);
				agents[1].AllocatedRoles.ShouldBe(new [] { transportRole });

				var processRoleA = new Role { PreCondition = { Task = task, Port = agents[1] }, PostCondition = { Task = task, Port = agents[3] } };
				processRoleA.Initialize(transportRole.PostCondition);
				processRoleA.AddCapability(new ProcessCapabilityA());
				agents[2].AllocatedRoles.ShouldBe(new [] { processRoleA });

				var processRoleB = new Role { PreCondition = { Task = task, Port = agents[2] }, PostCondition = { Task = task, Port = agents[4] } };
				processRoleB.Initialize(processRoleA.PostCondition);
				processRoleB.AddCapability(new ProcessCapabilityB());
				agents[3].AllocatedRoles.ShouldBe(new[] { processRoleB });

				var consumerRole = new Role { PreCondition = { Task = task, Port = agents[3] }, PostCondition = { Task = task } };
				consumerRole.Initialize(processRoleB.PostCondition);
				consumerRole.AddCapability(new ConsumeCapability());
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