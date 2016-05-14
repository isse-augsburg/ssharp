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

namespace SafetySharp.CaseStudies.ProductionCell.Modeling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Controllers;
	using Plants;
	using SafetySharp.Modeling;

	internal class Model : ModelBase
	{
		public const int MaxRoleCapabilities = 5;
		public const int MaxRoleCount = 20;
		public const int MaxAgentRequests = 2;
		public const int MaxProductionSteps = 6;

		public Model()
		{
			var produce = new ProduceCapability(Resources, Tasks);
			var insert = new ProcessCapability(ProductionAction.Insert);
			var drill = new ProcessCapability(ProductionAction.Drill);
			var tighten = new ProcessCapability(ProductionAction.Tighten);
			var polish = new ProcessCapability(ProductionAction.Polish);
			var consume = new ConsumeCapability(Resources);

			CreateWorkpieces(5, produce, drill, insert, tighten, polish, consume);

			CreateRobot(produce, drill);
			CreateRobot(insert);
			CreateRobot(tighten);
			CreateRobot(polish, consume);

			CreateCart(Robots[0], new Route(Robots[0], Robots[1]));
			CreateCart(Robots[1], new Route(Robots[1], Robots[2]));
			CreateCart(Robots[2], new Route(Robots[2], Robots[3]));

			ObserverController = new MiniZincObserverController(RobotAgents.Cast<Agent>().Concat(CartAgents), Tasks);
		}

		private List<Task> Tasks { get; } = new List<Task>();

		[Root(RootKind.Plant)]
		public List<Workpiece> Workpieces { get; } = new List<Workpiece>();

		[Root(RootKind.Plant)]
		public List<Robot> Robots { get; } = new List<Robot>();

		[Root(RootKind.Plant)]
		public List<Cart> Carts { get; } = new List<Cart>();

		public List<RobotAgent> RobotAgents { get; } = new List<RobotAgent>();
		public List<CartAgent> CartAgents { get; } = new List<CartAgent>();

		[Root(RootKind.Controller)]
		public List<Resource> Resources { get; } = new List<Resource>();

		[Root(RootKind.Controller)]
		public ObserverController ObserverController { get; }

		private void CreateWorkpieces(int count, params Capability[] capabilities)
		{
			if (capabilities.Length > MaxProductionSteps)
				throw new InvalidOperationException($"Too many production steps; increase '{MaxProductionSteps}'.");

			var task = new Task(capabilities);
			Tasks.Add(task);

			for (var i = 0; i < count; ++i)
			{
				var workpiece = new Workpiece(capabilities.OfType<ProcessCapability>().Select(c => c.ProductionAction).ToArray());
				workpiece.Name = $"W{Workpieces.Count + 1}";

				Workpieces.Add(workpiece);
				Resources.Add(new Resource(task, workpiece));
			}
		}

		private void CreateRobot(params Capability[] capabilities)
		{
			var robot = new Robot(capabilities.OfType<ProcessCapability>().ToArray());
			var agent = new RobotAgent(capabilities, robot);

			Robots.Add(robot);
			RobotAgents.Add(agent);

			robot.SetNames(Robots.Count);
			agent.Name = $"R{Robots.Count}";
		}

		private void CreateCart(Robot startPosition, params Route[] routes)
		{
			var cart = new Cart(startPosition, routes);
			var agent = new CartAgent(cart);

			Carts.Add(cart);
			CartAgents.Add(agent);

			cart.SetNames(Robots, Carts.Count);
			agent.Name = $"C{Carts.Count}";

			foreach (var route in routes)
			{
				Agent.Connect(from: RobotAgents.Single(a => route.From == a.Robot), to: agent);
				Agent.Connect(from: agent, to: RobotAgents.Single(a => route.To == a.Robot));
			}
		}
	}
}