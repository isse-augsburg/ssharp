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

namespace SafetySharp.Odp
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Modeling;

	public class CentralReconfiguration : Component, IReconfigurationStrategy
	{
		protected readonly IController _controller;

		public CentralReconfiguration(IController controller)
		{
			_controller = controller;
		}

		public virtual void Reconfigure(IEnumerable<Tuple<ITask, BaseAgent.State>> reconfigurations)
		{
			var tasks = reconfigurations.Select(tuple => tuple.Item1).ToArray();

			var configs = _controller.CalculateConfigurations(tasks);
			if (configs != null)
			{
				RemoveConfigurations(tasks);
				ApplyConfigurations(configs);
			}
		}

		protected virtual void RemoveConfigurations(params ITask[] tasks)
		{
			foreach (var agent in _controller.Agents)
				foreach (var task in tasks)
					agent.RemoveAllocatedRoles(task);
		}

		protected virtual void ApplyConfigurations(Dictionary<BaseAgent, IEnumerable<Role>> configurations)
		{
			foreach (var agent in configurations.Keys)
				agent.AllocateRoles(configurations[agent].ToArray());
		}

		public override void Update()
		{
			_controller.Update();
		}
	}
}