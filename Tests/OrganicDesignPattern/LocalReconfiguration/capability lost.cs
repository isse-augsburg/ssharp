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

namespace Tests.OrganicDesignPattern.LocalReconfiguration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using SafetySharp.Analysis;
	using SafetySharp.Odp;
	using SafetySharp.Odp.Reconfiguration;
	using SafetySharp.Odp.Reconfiguration.CoalitionFormation;
	using Shouldly;
	using Utilities;

	class CapabilityLost : OdpTestObject
	{
		private readonly ProduceCapability produce = new ProduceCapability();
		private readonly ProcessCapability process = new ProcessCapability();
		private readonly ConsumeCapability consume = new ConsumeCapability();

		protected override void Check()
		{
			var task = new Task(produce, process, consume);

			var producer = new Producer() { _res = new TestResource(task) };
			producer.Capabilities.Add(produce);

			var processor = new Agent();
			processor.Capabilities.Add(process);

			var consumer = new Agent();
			consumer.Capabilities.Add(process);
			consumer.Capabilities.Add(consume);

			var controller = new CoalitionFormationController(new [] { producer, processor, consumer });
			CreateReconfAgentHandler(producer, controller);
			CreateReconfAgentHandler(processor, controller);
			CreateReconfAgentHandler(consumer, controller);

			producer.Connect(processor);
			processor.Connect(consumer);

			var producerRole = new Role() { PreCondition = { Task = task }, PostCondition = { Port = processor, Task = task } };
			producerRole.AddCapability(produce);
			producer.AllocateRoles(new[] { producerRole });

			var processorRole = new Role() { PreCondition = { Task = task, Port = producer }, PostCondition = { Port = consumer, Task = task } };
			processorRole.Initialize(producerRole.PostCondition);
			processorRole.AddCapability(process);
			processor.AllocateRoles(new[] { processorRole });

			var consumerRole = new Role() { PreCondition = { Task = task, Port = processor }, PostCondition = { Task = task } };
			consumerRole.Initialize(processorRole.PostCondition);
			consumerRole.AddCapability(consume);
			consumer.AllocateRoles(new[] { consumerRole });

			var model = TestModel.InitializeModel(producer, processor, consumer);
			var simulator = new Simulator(model);

			simulator.FastForward(steps: 6);
			processor = (Agent)simulator.Model.Roots[1];
			processor.Capabilities.Remove(process);
			simulator.SimulateStep();

			producer = (Producer)simulator.Model.Roots[0];
			processor = (Agent)simulator.Model.Roots[1];
			consumer = (Agent)simulator.Model.Roots[2];
			task = producer.AllocatedRoles.First().Task as Task;

			producerRole = new Role() { PreCondition = { Task = task }, PostCondition = { Port = processor, Task = task } };
			producerRole.AddCapability(produce);

			var transportRole = new Role() { PreCondition = { Port = producer, Task = task }, PostCondition = { Port = consumer, Task = task } };
			transportRole.Initialize(producerRole.PostCondition);

			var processConsumerRole = new Role() { PreCondition = { Port = processor, Task = task }, PostCondition = { Task = task } };
			processConsumerRole.Initialize(transportRole.PostCondition);
			processConsumerRole.AddCapability(process);
			processConsumerRole.AddCapability(consume);
	
			producer.AllocatedRoles.ShouldBe(new[] { producerRole });
			processor.AllocatedRoles.ShouldBe(new[] { transportRole });
			consumer.AllocatedRoles.ShouldBe(new[] { processConsumerRole });
		}

		private void CreateReconfAgentHandler(Agent agent, IController controller)
		{
			agent.ReconfigurationStrategy = new ReconfigurationAgentHandler(agent,
				(ag, handler, task) => new CoalitionReconfigurationAgent(ag, handler, controller));
		}

		private class Agent : BaseAgent
		{
			public readonly List<ICapability> Capabilities = new List<ICapability>(20);
			public override IEnumerable<ICapability> AvailableCapabilities => Capabilities;
		}

		private class Producer : Agent, ICapabilityHandler<ProduceCapability>
		{
			public Resource _res;

			public void ApplyCapability(ProduceCapability capability)
			{
				Resource = _res;
				Resource.OnCapabilityApplied(capability);
			}
		}

		private class ProcessCapability : Capability<ProcessCapability>
		{
			public override CapabilityType CapabilityType => CapabilityType.Process;

			public override bool Equals(object obj) => obj is ProcessCapability;

			public override int GetHashCode() => 0;
		}

		private class TestResource : Resource
		{
			public TestResource(ITask task)
			{
				Task = task;
			}
		}
	}
}