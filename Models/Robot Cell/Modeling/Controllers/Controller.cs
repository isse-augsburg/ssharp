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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Odp;

	using Role = Odp.Role<Agent, Task, Resource>;

	internal abstract class Controller : AbstractController<Agent, Task, Resource>
	{
		protected Controller(IEnumerable<Agent> agents) : base(agents.ToArray()) { }

		protected void ExtractConfigurations(Dictionary<Agent, IEnumerable<Role>> configs, Task task, Tuple<Agent, ICapability[]>[] roleAllocations)
		{
			Action<Agent, Role> addRole = (agent, role) => {
				if (!configs.ContainsKey(agent))
					configs.Add(agent, new HashSet<Role>());
			};

			Role lastRole = default(Role);

			for (var i = 0; i < roleAllocations.Length; i++)
			{
				var agent = roleAllocations[i].Item1;
				var capabilities = roleAllocations[i].Item2;

				var preAgent = i == 0 ? null : roleAllocations[i - 1].Item1;
				var postAgent = i == roleAllocations.Length - 1 ? null : roleAllocations[i + 1].Item1;

				var role = GetRole(task, preAgent, lastRole.PostCondition);
				role.PostCondition.Port = postAgent;

				foreach (var capability in capabilities)
				{
					role.AddCapability(capability);
					role.PostCondition.AppendToState(capability);
				}

				addRole(agent, role);
				lastRole = role;
			}
		}
	}
}