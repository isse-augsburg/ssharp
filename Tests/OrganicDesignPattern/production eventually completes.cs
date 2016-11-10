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
	using SafetySharp.Analysis;
	using SafetySharp.Odp;
	using SafetySharp.Odp.Reconfiguration;
	using Shouldly;
	using Utilities;

	using static SafetySharp.Analysis.Operators;
	using System.Collections.Generic;
	using System;

	internal class ProductionEventuallyCompletes : OdpTestObject
	{
		protected override void Check()
		{
			var agents = new Agent[12];

			var ctx = new Context();
			agents[0] = new Producer() { ctx = ctx };
			agents[0].Capabilities.Add(new ProduceCapability());
			for (var i = 1; i < agents.Length - 1; ++i)
			{
				agents[i] = new Agent();
				agents[i-1].Connect(agents[i]);
			}
			agents[agents.Length - 1] = new Consumer() { ctx = ctx };
			agents[agents.Length - 1].Capabilities.Add(new ConsumeCapability());
			agents[agents.Length - 2].Connect(agents[agents.Length - 1]);

			var controllers = new[] { new FastController(agents), new OptimalController(agents) };
			foreach (var controller in controllers)
			{
				foreach (var agent in agents)
					agent.ReconfigurationStrategy = new CentralReconfiguration(controller);
				var model = TestModel.InitializeModel(agents);

				var modelChecker = new SSharpChecker() { Configuration = { StateCapacity = 1024 } };
				var result = modelChecker.CheckInvariant(model, ctx.Completed != ctx.Total);
				result.FormulaHolds.ShouldBeFalse();

				var result2 = ModelChecker.Check(model, F(ctx.Completed == ctx.Total));
				result2.FormulaHolds.ShouldBeTrue();
			}
		}

		private class Context
		{
			public int InProduction = 0;
			public int Completed = 0;
			public int Total;
		}

		private class R : Resource { }

		internal class Agent : BaseAgent
		{
			public const int MaxCapabilityCount = 20;
			public readonly List<ICapability> Capabilities = new List<ICapability>(20);

			public override IEnumerable<ICapability> AvailableCapabilities => Capabilities;

			internal void ConfigureTask(ITask task)
			{
				PerformReconfiguration(new[] { Tuple.Create(task, new State(this)) });
			}
		}

		private class Producer : Agent, ICapabilityHandler<ProduceCapability>
		{
			public Context ctx;

			public void ApplyCapability(ProduceCapability capability)
			{
				Resource = new R();
				ctx.InProduction++;
			}

			public override bool CanExecute(Role role)
			{
				if (role.CapabilitiesToApply.FirstOrDefault() is ProduceCapability)
					return ctx.InProduction + ctx.Completed < ctx.Total && base.CanExecute(role);
				return base.CanExecute(role);
			}
		}
		private class Consumer : Agent, ICapabilityHandler<ConsumeCapability>
		{
			public Context ctx;

			public void ApplyCapability(ConsumeCapability capability)
			{
				Resource = null;
				ctx.InProduction--;
				ctx.Completed++;
			}
		}
	}
}