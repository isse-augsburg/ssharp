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
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using JetBrains.Annotations;

	/// <summary>
	///   Represents a configuration change for several base agents, as produced by a reconfiguration.
	/// </summary>
	public class ConfigurationUpdate
	{
		private readonly Dictionary<BaseAgent, HashSet<Role>> _addedRoles = new Dictionary<BaseAgent, HashSet<Role>>();
		private readonly Dictionary<BaseAgent, HashSet<Role>> _removedRoles = new Dictionary<BaseAgent, HashSet<Role>>();

		/// <summary>
		///   The agents whose configuration will change through application of this instance.
		/// </summary>
		[NotNull, ItemNotNull]
		public BaseAgent[] AffectedAgents
			=> _addedRoles.Keys.Concat(_removedRoles.Keys).Distinct().ToArray();

		/// <summary>
		///   The set of agents involved in the reconfiguration that produced this update,
		///   as recorded by its producer. This is always a superset of <see cref="AffectedAgents"/>.
		/// </summary>
		[NotNull, ItemNotNull]
		public IEnumerable<BaseAgent> InvolvedAgents => _involvedAgents;
		private readonly HashSet<BaseAgent> _involvedAgents = new HashSet<BaseAgent>();

		/// <summary>
		///   Indicates if this update is the result of a failed reconfiguration.
		/// </summary>
		public bool Failed { get; private set; }

		/// <summary>
		///   Removes all roles for the given <paramref name="task"/> from the given <paramref name="agents"/>.
		/// </summary>
		public void RemoveAllRoles([NotNull] ITask task, [ItemNotNull] params BaseAgent[] agents)
		{
			if (task == null)
				throw new ArgumentNullException(nameof(task));

			foreach (var agent in agents)
				RemoveRoles(agent, agent.AllocatedRoles.Where(role => role.Task == task).ToArray());
		}

		/// <summary>
		///   Marks the reconfiguration producing this update as <see cref="Failed"/>.
		/// </summary>
		public void Fail()
		{
			Failed = true;
		}

		/// <summary>
		///   Removes the given <paramref name="rolesToRemove"/> from the given <paramref name="agent"/>
		///   and records its involvement in the reconfiguration.
		/// </summary>
		public void RemoveRoles([NotNull] BaseAgent agent, params Role[] rolesToRemove)
		{
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));

			if (!_removedRoles.ContainsKey(agent))
				_removedRoles[agent] = new HashSet<Role>();
			if (!_involvedAgents.Contains(agent))
				_involvedAgents.Add(agent);
			_removedRoles[agent].UnionWith(rolesToRemove);
		}

		/// <summary>
		///   Adds the given <paramref name="rolesToAdd"/> to the given <paramref name="agent"/>
		///   and records its involvement in the reconfiguration.
		/// </summary>
		public void AddRoles([NotNull] BaseAgent agent, params Role[] rolesToAdd)
		{
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));

			if (!_addedRoles.ContainsKey(agent))
				_addedRoles[agent] = new HashSet<Role>();
			if (!_involvedAgents.Contains(agent))
				_involvedAgents.Add(agent);
			_addedRoles[agent].UnionWith(rolesToAdd);
		}

		/// <summary>
		///   Records the involvement of the given <paramref name="involvedAgents"/> in the reconfiguration producing this update.
		/// </summary>
		public void RecordInvolvement([NotNull, ItemNotNull] IEnumerable<BaseAgent> involvedAgents)
		{
			if (involvedAgents == null)
				throw new ArgumentNullException(nameof(involvedAgents));

			_involvedAgents.UnionWith(involvedAgents);
		}

		/// <summary>
		///   Locks all roles that will be added to agents when applying this update.
		/// </summary>
		public void LockAddedRoles()
		{
			foreach (var id in _addedRoles.Keys.ToArray())
				_addedRoles[id] = new HashSet<Role>(_addedRoles[id].Select(role => role.Lock()));
		}

		/// <summary>
		///   Applies the role changes recorded for the given <paramref name="agents"/> to those agents.
		/// </summary>
		public void Apply([ItemNotNull] params BaseAgent[] agents)
		{
			foreach (var agent in agents)
			{
				if (_removedRoles.ContainsKey(agent))
					agent.RemoveAllocatedRoles(_removedRoles[agent]);
				if (_addedRoles.ContainsKey(agent))
					agent.AllocateRoles(_addedRoles[agent]);
			}
		}

		/// <summary>
		///   Retrieves the changes recorded for a specific given <paramref name="agent"/>.
		/// </summary>
		/// <returns>A tuple containing the removed and added roles.</returns>
		[Pure]
		public Tuple<Role[], Role[]> GetChanges([NotNull] BaseAgent agent)
		{
			return Tuple.Create(
			  _removedRoles.ContainsKey(agent) ? _removedRoles[agent].ToArray() : new Role[0],
			  _addedRoles.ContainsKey(agent) ? _addedRoles[agent].ToArray() : new Role[0]
			);
		}
	}
}