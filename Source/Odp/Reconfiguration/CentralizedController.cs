// The MIT License (MIT)
//
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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
	using System.Diagnostics;
	using System.Threading.Tasks;

	/// <summary>
	///   A centralized controller that uses all currently functioning agents in the system.
	/// </summary>
	public class CentralizedController : AbstractController
	{
		private readonly IConfigurationFinder _configurationFinder;

		/// <summary>
		///   Creates a new instance.
		/// </summary>
		/// <param name="agents">All agents that exist in the system.</param>
		/// <param name="configurationFinder">A strategy to find configurations for given parameters.</param>
		public CentralizedController(BaseAgent[] agents, IConfigurationFinder configurationFinder)
			: base(agents)
		{
			_configurationFinder = configurationFinder;
		}

		// synchronous implementation
		public override async Task<ConfigurationUpdate> CalculateConfigurationsAsync(object context, ITask task)
		{
			var availableAgents = GetAvailableAgents();

			var configs = new ConfigurationUpdate();
			configs.RecordInvolvement(availableAgents); // central controller uses all available agents!
			configs.RemoveAllRoles(task, Agents);

			var result = await _configurationFinder.Find(new HashSet<BaseAgent>(availableAgents), task.RequiredCapabilities);
			if (result == null)
				configs.Fail();
			else
				ExtractConfigurations(configs, task, result.Item1, result.Item2);

			OnConfigurationsCalculated(task, configs);
			return configs;
		}

		private void ExtractConfigurations(ConfigurationUpdate config, ITask task, BaseAgent[] distribution, BaseAgent[] resourceFlow)
		{
			BaseAgent lastAgent = null;
			Role? lastRole = null;
			var currentState = 0;

			for (var i = 0; i < resourceFlow.Length; ++i)
			{
				var nextAgent = i + 1 < resourceFlow.Length ? resourceFlow[i + 1] : null;
				var agent = resourceFlow[i];

				var initialRole = GetRole(task, lastAgent, lastRole?.PostCondition);
				var role = CollectCapabilities(ref currentState, distribution, agent, initialRole, task.RequiredCapabilities).WithOutput(nextAgent);
				config.AddRoles(agent, role);

				lastRole = role;
				lastAgent = agent;
			}
		}

		private static Role CollectCapabilities(ref int currentState, BaseAgent[] distribution, BaseAgent agent, Role initialRole, ICapability[] capabilities)
		{
			Debug.Assert(initialRole.PostCondition.StateLength == currentState);

			var role = initialRole;
			while (currentState < capabilities.Length && distribution[currentState] == agent)
				role = role.WithCapability(capabilities[currentState++]);
			return role;
		}
	}
}