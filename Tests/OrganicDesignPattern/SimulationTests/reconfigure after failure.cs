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
	using System.Collections.Generic;
	using System.Diagnostics;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Odp;
	using SafetySharp.Odp.Reconfiguration;
	using Shouldly;
	using Utilities;

	class ReconfigureAfterFailure : OdpTestObject
	{
		private static readonly ProduceCapability _produce = new ProduceCapability();
		private static readonly ConsumeCapability _consume = new ConsumeCapability();
	    private static readonly ITask _task = new Task(_produce, _consume);

		protected override void Check()
		{
			// initialize the model
			var monitor = new ReconfigurationMonitor(1);
			var producer = new Producer(monitor);
			var consumer = new Consumer(monitor);
			producer.Connect(consumer);

			var controller = new FastController(new BaseAgent[] { producer, consumer });
			monitor.Controller = controller;
			producer.ReconfigurationStrategy = new CentralReconfiguration(controller);
			consumer.ReconfigurationStrategy = new CentralReconfiguration(controller);

			// setup simulation
			Debugger.Break();
			var simulator = new Simulator(TestModel.InitializeModel(producer, consumer));
			producer = (Producer)simulator.Model.Roots[0];
			consumer = (Consumer)simulator.Model.Roots[1];

			// fault-free simulation produces resources
			producer.ProduceFailure.Activation = Activation.Suppressed;
			simulator.FastForward(7);
			monitor.ReconfigurationFailure.ShouldBe(false);
			consumer.ConsumedResources.ShouldBe(1);
			
			// no possible configurations -> no more resources
			producer.ProduceFailure.Activation = Activation.Forced;
			simulator.FastForward(10);
			consumer.ConsumedResources.ShouldBe(1);

			// configuration possible again -> production resumes
			producer.ProduceFailure.Activation = Activation.Suppressed;
		    producer.Restore();

			// HACK: FastForward deserializes the last state, eliminating the changes mady by Restore() above
			// To avoid this, use SimulateStep() repeatedly here.
			// In general, interference in the model should be avoided by calling Restore() in the model.
			// To achieve this, the model must observe activation and deeactivation of its faults.

			//simulator.FastForward(20);
			for (int i = 0; i < 20; ++i)
				simulator.SimulateStep();

			// production continues!
			consumer.ConsumedResources.ShouldBeGreaterThan(1);
		}

		private class Producer : BaseAgent, ICapabilityHandler<ProduceCapability>
		{
		    private readonly ITask _task = ReconfigureAfterFailure._task;
            private readonly Resource[] _resources = { new TestResource(), new TestResource(), new TestResource(), new TestResource(), new TestResource(), new TestResource() };

			private int i = 0;
		    private bool _justRestored;
		    private bool _initialized;

			public override IEnumerable<ICapability> AvailableCapabilities => new[] { _produce };

			public readonly Fault ProduceFailure = new PermanentFault();
			private readonly ReconfigurationMonitor _monitor;

			public Producer(ReconfigurationMonitor monitor)
			{
				monitor.AddAgent(this);
				_monitor = monitor;
			}

		    protected override async System.Threading.Tasks.Task UpdateAsync()
		    {
		        if (!_initialized)
		        {
		            _initialized = true;
		            await PerformReconfiguration(new[] { Tuple.Create(_task, new State(this)) });
                }
		        else if (_justRestored)
		        {
		            _justRestored = false;
		            foreach (var task in _monitor.GetTasksToContinue())
		                await PerformReconfiguration(new[] { Tuple.Create(task, new State(this)) });
		        }
		        await base.UpdateAsync();
		    }

			public void ApplyCapability(ProduceCapability capability)
			{
				Resource = _resources[i++];
				Resource.OnCapabilityApplied(capability);
			}

			public void Restore()
			{
			    _justRestored = true;
			}

			[FaultEffect(Fault = nameof(ProduceFailure))]
			public class ProduceFailureEffect : Producer
			{
				public ProduceFailureEffect() : base(null) { }

				public override IEnumerable<SafetySharp.Odp.ICapability> AvailableCapabilities => new SafetySharp.Odp.ICapability[0];
			}
		}

		private class Consumer : BaseAgent, ICapabilityHandler<ConsumeCapability>
		{
			public Consumer(ReconfigurationMonitor monitor)
			{
				monitor.AddAgent(this);
			}

			public override IEnumerable<SafetySharp.Odp.ICapability> AvailableCapabilities => new[] { _consume };

			public int ConsumedResources { get; private set; }

			public void ApplyCapability(ConsumeCapability capability)
			{
				Resource.OnCapabilityApplied(capability);
				Resource = null;
				ConsumedResources++;
			}
		}

		private class TestResource : Resource
		{
			public TestResource()
			{
				Task = _task;
			}
		}
	}
}
