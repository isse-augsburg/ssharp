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
	using System.Threading.Tasks;
	using Modeling;

	public abstract class AbstractController : IController
	{
		[Hidden(HideElements = true)]
		public BaseAgent[] Agents { get; }

		public event Action<ITask, ConfigurationUpdate> ConfigurationsCalculated;

		protected AbstractController(BaseAgent[] agents)
		{
			Agents = agents;
		}

	    protected virtual BaseAgent[] GetAvailableAgents()
	    {
	        return Array.FindAll(Agents, agent => agent.IsAlive);
	    }

		public abstract Task<ConfigurationUpdate> CalculateConfigurations(object context, ITask task);

		protected Role GetRole(ITask recipe, BaseAgent input, Condition? previous)
		{
			var role = new Role()
			{
				PreCondition = { Task = recipe, Port = input },
				PostCondition = { Task = recipe, Port = null }
			};

			if (previous != null)
				role.Initialize(previous.Value);

			return role;
		}

		protected void OnConfigurationsCalculated(ITask task, ConfigurationUpdate config)
		{
			ConfigurationsCalculated?.Invoke(task, config);
		}
	}
}