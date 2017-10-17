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
	using System;
	using SafetySharp.Analysis;
	using SafetySharp.Odp;
	using Shouldly;
	using Utilities;

	internal class ResourceHandover : TestObject
	{
		private class TestResource : Resource
		{
			public TestResource(ITask task)
			{
				Task = task;
			}

			public void OnProduced()
			{
				OnCapabilityApplied(Task.RequiredCapabilities[0]);
			}
		}

		protected override void Check()
		{
			var produce = new ProduceCapability();
			var consume = new ConsumeCapability();

			var task = new Task(produce, consume);
			var resource = new TestResource(task);

			/* model setup */
			var agent1 = new Producer() { AvailableResource = resource };
			agent1.Capabilities.Add(produce);

			var agent2 = new Consumer();
			agent2.Capabilities.Add(consume);

			agent1.Connect(agent2);

			var producerRole = new Role(
					new Condition(task, 0),
					new Condition(task, 0, agent2)
				).WithCapability(produce);
			agent1.AllocateRoles(new[] { producerRole });

			var consumerRole = new Role(
					new Condition(task, producerRole.PostCondition.StateLength, agent1),
					new Condition(task, producerRole.PostCondition.StateLength)
				).WithCapability(consume);
			agent2.AllocateRoles(new [] { consumerRole });
			/* end model setup */

			var simulator = new Simulator(TestModel.InitializeModel(agent1, agent2));
			agent1 = simulator.Model.Roots[0] as Producer;
			agent2 = simulator.Model.Roots[1] as Consumer;
			resource = agent1.AvailableResource;

			Action<string, string, TestResource, TestResource> checkResourceTransfer =
				(expectedState1, expectedState2, expectedResource1, expectedResource2) =>
				{
					agent1.GetState().ShouldBe(expectedState1);
					agent2.GetState().ShouldBe(expectedState2);
					agent1.Resource.ShouldBe(expectedResource1);
					agent2.Resource.ShouldBe(expectedResource2);
				};

			simulator.SimulateStep();
			checkResourceTransfer("ChooseRole", "ChooseRole", null, null);

			simulator.SimulateStep();
			checkResourceTransfer("ExecuteRole", "Idle", null, null);

			simulator.SimulateStep();
			checkResourceTransfer("ExecuteRole", "ChooseRole", resource, null);

			simulator.SimulateStep();
			checkResourceTransfer("Output", "Idle", resource, null);

			simulator.SimulateStep();
			checkResourceTransfer("Output", "ChooseRole", resource, null);

			simulator.SimulateStep();
			checkResourceTransfer("Idle", "ExecuteRole", null, resource);

			agent1.LockRoles(agent1.AllocatedRoles); // don't produce further resources

			simulator.SimulateStep();
			checkResourceTransfer("ChooseRole", "ExecuteRole", null, null);

			simulator.SimulateStep();
			checkResourceTransfer("Idle", "Idle", null, null);
		}

		private class Producer : Agent, ICapabilityHandler<ProduceCapability>
		{
			public TestResource AvailableResource { get; set; }

			public void ApplyCapability(ProduceCapability capability)
			{
				Resource.ShouldBeNull();
				Resource = AvailableResource;
				AvailableResource.OnProduced();
			}
		}

		private class Consumer : Agent, ICapabilityHandler<ConsumeCapability>
		{
			public void ApplyCapability(ConsumeCapability capability)
			{
				Resource.ShouldNotBeNull();
				Resource = null;
			}
		}
	}
}