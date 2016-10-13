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
	using Odp;

	using IController = Odp.IController<Controllers.Agent, Controllers.Task>;

	internal class Model : ModelBase
	{
		public const int MaxRoleCount = 8;
		public const int MaxAgentRequests = 2;
		public const int MaxProductionSteps = 8;

		public Model(string name = "")
		{
			Name = name;
		}

		public string Name { get; }

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
		public CentralRobotReconfiguration ReconfigurationStrategy { get; set; }

		private ICapability Produce => new ProduceCapability(Resources, Tasks);
		private static ICapability Insert => new ProcessCapability(ProductionAction.Insert);
		private static ICapability Drill => new ProcessCapability(ProductionAction.Drill);
		private static ICapability Tighten => new ProcessCapability(ProductionAction.Tighten);
		private static ICapability Polish => new ProcessCapability(ProductionAction.Polish);
		private static ICapability Consume => new ConsumeCapability();

		public void InitializeDefaultInstance()
		{
			Ictss1();
		}

		public void Ictss1()
		{
			CreateWorkpieces(5, Produce, Drill, Insert, Tighten, Polish, Consume);

			CreateRobot(Produce, Drill, Insert);
			CreateRobot(Insert, Drill);
			CreateRobot(Tighten, Polish, Tighten, Drill);
			CreateRobot(Polish, Consume);

			CreateCart(new Route(Robots[0], Robots[1]), new Route(Robots[0], Robots[2]), new Route(Robots[0], Robots[3]));
			CreateCart(new Route(Robots[1], Robots[2]), new Route(Robots[0], Robots[1]));
			CreateCart(new Route(Robots[2], Robots[3]));
		}

		public void Ictss2()
		{
			CreateWorkpieces(5, Produce, Drill, Insert, Tighten, Consume);

			CreateRobot(Produce, Insert);
			CreateRobot(Tighten);
			CreateRobot(Drill, Consume);

			CreateCart(new Route(Robots[0], Robots[1]), new Route(Robots[0], Robots[2]));
			CreateCart(new Route(Robots[1], Robots[2]), new Route(Robots[0], Robots[1]));
		}

		public void Ictss3()
		{
			CreateWorkpieces(5, Produce, Drill, Insert, Tighten, Consume);

			CreateRobot(Produce, Insert, Drill, Insert);
			CreateRobot(Insert, Tighten, Drill);
			CreateRobot(Tighten, Insert, Consume, Drill);

			CreateCart(new Route(Robots[0], Robots[1]), new Route(Robots[0], Robots[2]));
			CreateCart(new Route(Robots[1], Robots[2]), new Route(Robots[0], Robots[1]));
		}

		public void Ictss4()
		{
			CreateWorkpieces(5, Produce, Drill, Insert, Tighten, Consume);

			CreateRobot(Produce, Insert);
			CreateRobot(Tighten);
			CreateRobot(Drill, Consume);

			CreateCart(new Route(Robots[0], Robots[1]), new Route(Robots[0], Robots[2]), new Route(Robots[1], Robots[2]));
			CreateCart(new Route(Robots[1], Robots[2]), new Route(Robots[0], Robots[1]), new Route(Robots[0], Robots[2]));
		}

		public void Ictss5()
		{
			CreateWorkpieces(5, Produce, Drill, Insert, Tighten, Drill, Insert, Tighten, Consume);

			CreateRobot(Produce, Insert);
			CreateRobot(Tighten);
			CreateRobot(Drill, Consume);

			CreateCart(new Route(Robots[0], Robots[1]), new Route(Robots[0], Robots[2]));
			CreateCart(new Route(Robots[1], Robots[2]), new Route(Robots[0], Robots[1]));
		}

		public void Ictss6()
		{
			CreateWorkpieces(5, Produce, Drill, Insert, Tighten, Consume);

			CreateRobot(Produce, Insert);
			CreateRobot(Insert);
			CreateRobot(Drill, Tighten);
			CreateRobot(Tighten);
			CreateRobot(Drill, Consume);

			CreateCart(new Route(Robots[0], Robots[1]), new Route(Robots[0], Robots[2]));
			CreateCart(new Route(Robots[1], Robots[3]), new Route(Robots[1], Robots[4]), new Route(Robots[1], Robots[2]));
		}

		public void Ictss7()
		{
			CreateWorkpieces(5, Produce, Drill, Insert, Tighten, Consume);

			CreateRobot(Produce, Insert);
			CreateRobot(Tighten);
			CreateRobot(Drill, Consume);

			CreateCart(new Route(Robots[0], Robots[1]));
			CreateCart(new Route(Robots[1], Robots[2]), new Route(Robots[0], Robots[1]));
			CreateCart(new Route(Robots[0], Robots[1]));
			CreateCart(new Route(Robots[1], Robots[2]));
		}

		public void CreateController<T>()
			where T : IController
		{
			var agents = RobotAgents.Cast<Agent>().Concat(CartAgents);
			ReconfigurationStrategy = new CentralRobotReconfiguration(
				(IController)Activator.CreateInstance(typeof(T), agents)
			);
			foreach (var agent in agents)
				agent.ReconfigurationStrategy = ReconfigurationStrategy;
		}

		public void SetAnalysisMode(AnalysisMode mode)
		{
			ReconfigurationStrategy.Mode = mode;
			var agents = RobotAgents.Cast<Agent>().Concat(CartAgents);

			switch (mode)
			{
				case AnalysisMode.TolerableFaults:
					agents.Select(agent => agent.ConfigurationUpdateFailed).SuppressActivations();
					Workpieces.Select(workpiece => workpiece.IncorrectlyPositionedFault).SuppressActivations();
					Workpieces.Select(workpiece => workpiece.ToolApplicationFailed).SuppressActivations();
					Robots.Select(robot => robot.SwitchToWrongToolFault).SuppressActivations();
					Carts.Select(cart => cart.Lost).SuppressActivations();
					ReconfigurationStrategy.ReconfigurationFailure.SuppressActivation();
					break;
				case AnalysisMode.IntolerableFaults:
					Faults.SuppressActivations();
					agents.Select(agent => agent.ConfigurationUpdateFailed).MakeNondeterministic();
					Workpieces.Select(workpiece => workpiece.IncorrectlyPositionedFault).MakeNondeterministic();
					Workpieces.Select(workpiece => workpiece.ToolApplicationFailed).MakeNondeterministic();
					Robots.Select(robot => robot.SwitchToWrongToolFault).MakeNondeterministic();
					Carts.Select(cart => cart.Lost).MakeNondeterministic();
					ReconfigurationStrategy.ReconfigurationFailure.MakeNondeterministic();
					break;
				case AnalysisMode.AllFaults:
					return;
				default:
					throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
			}
		}

		public static IEnumerable<Model> CreateConfigurations<T>(AnalysisMode mode)
			where T : IController
		{
			yield return CreateConfiguration<T>(m => m.Ictss1(), nameof(Ictss1), mode);
			yield return CreateConfiguration<T>(m => m.Ictss2(), nameof(Ictss2), mode);
			yield return CreateConfiguration<T>(m => m.Ictss3(), nameof(Ictss3), mode);
			yield return CreateConfiguration<T>(m => m.Ictss4(), nameof(Ictss4), mode);
			yield return CreateConfiguration<T>(m => m.Ictss5(), nameof(Ictss5), mode);
			yield return CreateConfiguration<T>(m => m.Ictss6(), nameof(Ictss6), mode);
			yield return CreateConfiguration<T>(m => m.Ictss7(), nameof(Ictss7), mode);
		}

		private static Model CreateConfiguration<T>(Action<Model> initializer, string name, AnalysisMode mode)
			where T : IController
		{
			var model = new Model(name);
			initializer(model);
			model.CreateController<T>();
			model.SetAnalysisMode(mode);

			return model;
		}

		private void CreateWorkpieces(int count, params ICapability[] capabilities)
		{
			if (capabilities.Length > MaxProductionSteps)
				throw new InvalidOperationException($"Too many production steps; increase '{MaxProductionSteps}'.");

			var task = new Task(capabilities);
			Tasks.Add(task);

			for (var i = 0; i < count; ++i)
			{
				var workpiece = new Workpiece(capabilities.OfType<ProcessCapability>().Select(c => c.ProductionAction).ToArray())
				{
					Name = $"W{Workpieces.Count}",
					IncorrectlyPositionedFault = { Name = $"W{Workpieces.Count}.IncorrectlyPositioned" },
					ToolApplicationFailed = { Name = $"W{Workpieces.Count}.ToolApplicationFailed" },
				};

				Workpieces.Add(workpiece);
				Resources.Add(new Resource(task, workpiece));
			}
		}

		private void CreateRobot(params ICapability[] capabilities)
		{
			var robot = new Robot(capabilities.OfType<ProcessCapability>().ToArray());
			var agent = new RobotAgent(capabilities.Distinct().ToArray(), robot);

			Robots.Add(robot);
			RobotAgents.Add(agent);

			robot.SetNames(Robots.Count - 1);
			agent.Name = $"R{Robots.Count - 1}";
			agent.ConfigurationUpdateFailed.Name = agent.Name + ".ConfigUpdateFailed";
		}

		private void CreateCart(params Route[] routes)
		{
			// compute the transitive closure of the routes
			routes =
				routes.SelectMany(
					route =>
						Closure(route.Robot1, robot => { return routes.Where(r => r.Robot1 == robot).Select(r => r.Robot2); })
							.Select(target => new Route(route.Robot1, target))).ToArray();

			// make sure we don't have duplicate routes
			routes = routes.Distinct(new RouteComparer()).ToArray();

			// for efficiency (less faults), remove reflexive routes
			routes = routes.Where(route => route.Robot1 != route.Robot2).ToArray();

			var cart = new Cart(routes[0].Robot1, routes);
			var agent = new CartAgent(cart);

			Carts.Add(cart);
			CartAgents.Add(agent);

			cart.SetNames(Carts.Count - 1);
			agent.Name = $"C{Carts.Count - 1}";
			agent.ConfigurationUpdateFailed.Name = agent.Name + ".ConfigUpdateFailed";

			foreach (var route in routes)
			{
				RobotAgents.Single(a => route.Robot1 == a.Robot).BidirectionallyConnect(agent);
				RobotAgents.Single(a => route.Robot2 == a.Robot).BidirectionallyConnect(agent);
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