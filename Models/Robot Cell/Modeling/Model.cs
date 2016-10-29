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
	using System.Collections.Generic;

	using SafetySharp.Modeling;
	using Odp;

	using Controllers;
	using Plants;

	using Resource = Controllers.Resource;

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

		/* PLANTS */

		[Root(RootKind.Plant), Hidden(HideElements = true)]
		public List<Workpiece> Workpieces { get; } = new List<Workpiece>();

		[Root(RootKind.Plant), Hidden(HideElements = true)]
		public List<Robot> Robots { get; } = new List<Robot>();

		[Root(RootKind.Plant), Hidden(HideElements = true)]
		public List<Cart> Carts { get; } = new List<Cart>();

		/* CONTROLLERS */

		[Root(RootKind.Controller), Hidden(HideElements = true)]
		public List<RobotAgent> RobotAgents { get; } = new List<RobotAgent>();

		[Root(RootKind.Controller), Hidden(HideElements = true)]
		public List<CartAgent> CartAgents { get; } = new List<CartAgent>();

		[Root(RootKind.Controller), Hidden(HideElements = true)]
		public readonly List<IComponent> AdditionaComponents = new List<IComponent>();

		// TODO: move to ModelBuilder?
		public List<Resource> Resources { get; } = new List<Resource>();

		[Hidden(HideElements = true)]
		public List<Task> Tasks { get; } = new List<Task>();

		[Hidden]
		public IController Controller { get; set; }
	}
}