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
	using System.Collections.Generic;
	using System.Linq;
	using Controllers;
	using Plants;
	using SafetySharp.Modeling;

	internal class Model : ModelBase
	{
		public const int MaxRoleCapabilities = 2;
		public const int MaxRoleCount = 20;
		public const int MaxTaskCount = 3;

		public Model()
		{
			Tasks = new List<Task> { new Task(Capability.Drill, Capability.Insert, Capability.Tighten, Capability.Polish) };

			CreateWorkpieces(Tasks[0], 3);

			CreateRobot(Capability.Drill);
			CreateRobot(Capability.Insert);
			CreateRobot(Capability.Tighten);
			CreateRobot(Capability.Polish);

			CreateCart(Robots[0], new Route(Robots[0], Robots[1]));
			CreateCart(Robots[1], new Route(Robots[1], Robots[2]));
			CreateCart(Robots[2], new Route(Robots[2], Robots[3]));

			ObserverController = new MiniZincObserverController(RobotAgents.Cast<Agent>().Concat(CartAgents));
		}

		private List<Task> Tasks { get; }

		[Root(RootKind.Plant)]
		public List<Workpiece> Workpieces { get; } = new List<Workpiece>();

		[Root(RootKind.Plant)]
		public List<Robot> Robots { get; } = new List<Robot>();

		[Root(RootKind.Plant)]
		public List<Cart> Carts { get; } = new List<Cart>();

		[Root(RootKind.Controller)]
		public List<RobotAgent> RobotAgents { get; } = new List<RobotAgent>();

		[Root(RootKind.Controller)]
		public List<CartAgent> CartAgents { get; } = new List<CartAgent>();

		[Root(RootKind.Controller)]
		public ObserverController ObserverController { get; }

		private void CreateWorkpieces(Task task, int count)
		{
			for (var i = 0; i < count; ++i)
				Workpieces.Add(new Workpiece(task.Capabilities.Select(c => c.ProductionAction).ToArray()));
		}

		private void CreateRobot(params Capability[] capabilities)
		{
			var robot = new Robot(capabilities);
			var agent = new RobotAgent(capabilities, robot);

			Robots.Add(robot);
			RobotAgents.Add(agent);

			robot.SetNames(Robots.Count);
		}

		private void CreateCart(Robot startPosition, params Route[] routes)
		{
			var cart = new Cart(startPosition, routes);
			var agent = new CartAgent(cart);

			Carts.Add(cart);
			CartAgents.Add(agent);

			cart.SetNames(Robots, Carts.Count);

			foreach (var route in routes)
			{
				Agent.Connect(from: RobotAgents.Single(a => route.From == a.Robot), to: agent);
				Agent.Connect(from: agent, to: RobotAgents.Single(a => route.To == a.Robot));
			}
		}
	}
}