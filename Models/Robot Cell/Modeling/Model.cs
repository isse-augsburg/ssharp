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

namespace SafetySharp.CaseStudies.RobotCell.Modeling
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
		public const int MaxAllocatedRoles = 5;

		public Model()
		{
			var produce = (Func<ProduceCapability>)(() => new ProduceCapability(Resources, Tasks));
			var insert = (Func<ProcessCapability>)(() => new ProcessCapability(ProductionAction.Insert));
			var drill = (Func<ProcessCapability>)(() => new ProcessCapability(ProductionAction.Drill));
			var tighten = (Func<ProcessCapability>)(() => new ProcessCapability(ProductionAction.Tighten));
			var polish = (Func<ProcessCapability>)(() => new ProcessCapability(ProductionAction.Polish));
			var consume = (Func<ConsumeCapability>)(() => new ConsumeCapability());

			CreateWorkpieces(5, produce(), drill(), insert(), tighten(), polish(), consume());

			CreateRobot(produce(), drill(), insert());
			CreateRobot(insert(), drill());
			CreateRobot(tighten(), polish(), tighten(), drill());
			CreateRobot(polish(), consume());

			CreateCart(Robots[0], new Route(Robots[0], Robots[1]), new Route(Robots[0], Robots[2]), new Route(Robots[0], Robots[3]));
			CreateCart(Robots[1], new Route(Robots[1], Robots[2]), new Route(Robots[0], Robots[1]));
			CreateCart(Robots[2], new Route(Robots[2], Robots[3]));

			ObserverController = new MiniZincObserverController(RobotAgents.Cast<Agent>().Concat(CartAgents), Tasks);
		}

		public List<Task> Tasks { get; } = new List<Task>();

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
				var workpiece = new Workpiece(capabilities.OfType<ProcessCapability>().Select(c => c.ProductionAction).ToArray())
				{
					Name = $"W{Workpieces.Count}"
				};

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

			robot.SetNames(Robots.Count - 1);
			agent.Name = $"R{Robots.Count - 1}";
		}

		private void CreateCart(Robot startPosition, params Route[] routes)
		{
			// compute the transitive closure of the routes
			routes = routes.SelectMany(route => Closure(route.Robot1, robot =>
			{
				return routes.Where(r => r.Robot1 == robot).Select(r => r.Robot2);
			}).Select(target => new Route(route.Robot1, target))).ToArray();

			// make sure we don't have duplicate routes
			routes = routes.Distinct(new RouteComparer()).ToArray();

			// for efficiency (less faults), remove reflexive routes
			routes = routes.Where(route => route.Robot1 != route.Robot2).ToArray();

			var cart = new Cart(startPosition, routes);
			var agent = new CartAgent(cart);

			Carts.Add(cart);
			CartAgents.Add(agent);

			cart.SetNames(Carts.Count - 1);
			agent.Name = $"C{Carts.Count - 1}";

			foreach (var route in routes)
			{
				Agent.Connect(from: RobotAgents.Single(a => route.Robot1 == a.Robot), to: agent);
				Agent.Connect(from: agent, to: RobotAgents.Single(a => route.Robot2 == a.Robot));
				Agent.Connect(from: RobotAgents.Single(a => route.Robot2 == a.Robot), to: agent);
				Agent.Connect(from: agent, to: RobotAgents.Single(a => route.Robot1 == a.Robot));
			}
		}

		private static IEnumerable<T> Closure<T>(T root, Func<T, IEnumerable<T>> children)
		{
			var seen = new HashSet<T>();
			var stack = new Stack<T>();
			stack.Push(root);

			while (stack.Count != 0)
			{
				var item = stack.Pop();
				if (seen.Contains(item))
					continue;

				seen.Add(item);
				yield return item;

				foreach (var child in children(item))
					stack.Push(child);
			}
		}

		private class RouteComparer : IEqualityComparer<Route>
		{
			public bool Equals(Route x, Route y)
			{
				return x.Robot1 == y.Robot1 && x.Robot2 == y.Robot2;
			}

			public int GetHashCode(Route obj)
			{
				return obj.Robot1.GetHashCode() + obj.Robot2.GetHashCode();
			}
		}
	}
}