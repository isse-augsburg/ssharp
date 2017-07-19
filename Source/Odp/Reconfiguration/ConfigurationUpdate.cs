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

namespace SafetySharp.Odp.Reconfiguration
{
	using System.Collections.Generic;
	using System.Linq;

	public class ConfigurationUpdate
	{
		private readonly Dictionary<BaseAgent, HashSet<Role>> _addedRoles = new Dictionary<BaseAgent, HashSet<Role>>();
		private readonly Dictionary<BaseAgent, HashSet<Role>> _removedRoles = new Dictionary<BaseAgent, HashSet<Role>>();

		public BaseAgent[] AffectedAgents
			=> _addedRoles.Keys.Concat(_removedRoles.Keys).Distinct().ToArray();

	    public BaseAgent[] InvolvedAgents { get; private set; }

	    public bool Failed { get; private set; }

		public void RemoveAllRoles(ITask task, params BaseAgent[] agents)
		{
			foreach (var agent in agents)
				RemoveRoles(agent, agent.AllocatedRoles.Where(role => role.Task == task).ToArray());
		}

		public void Fail()
		{
			Failed = true;
		}

		public void RemoveRoles(BaseAgent agent, params Role[] rolesToRemove)
		{
			if (!_removedRoles.ContainsKey(agent))
				_removedRoles[agent] = new HashSet<Role>();
			_removedRoles[agent].UnionWith(rolesToRemove);
		}

		public void AddRoles(BaseAgent agent, params Role[] rolesToAdd)
		{
			if (!_addedRoles.ContainsKey(agent))
				_addedRoles[agent] = new HashSet<Role>();
			_addedRoles[agent].UnionWith(rolesToAdd);
		}

		public void LockAddedRoles()
		{
			foreach (var id in _addedRoles.Keys.ToArray())
				_addedRoles[id] = new HashSet<Role>(_addedRoles[id].Select(role => role.Lock()));
		}

		public void Apply(params BaseAgent[] agents)
		{
			foreach (var agent in agents)
			{
				if (_removedRoles.ContainsKey(agent))
					agent.RemoveAllocatedRoles(_removedRoles[agent]);
				if (_addedRoles.ContainsKey(agent))
					agent.AllocateRoles(_addedRoles[agent]);
			}
		}
	}
}