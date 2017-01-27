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
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;

	/// <summary>
	/// A decentralized, local reconfiguration algorithm based on coalition formation.
	/// This class is only meant to be used by the <see cref="CoalitionReconfigurationAgent"/> class
	/// (and possibly subclasses). It is implemented as an <see cref="IController"/> nevertheless in
	/// order to allow compositional usage within other controllers.
	/// 
	/// (cf. Konstruktion selbst-organisierender Softwaresysteme, chapter 8)
	/// </summary>
	public class CoalitionFormationController : AbstractController
	{
		public CoalitionFormationController(BaseAgent[] agents) : base(agents) { }

		public override async Task<ConfigurationUpdate> CalculateConfigurations(object context, params ITask[] tasks)
		{
			Debug.Assert(tasks.Length == 1);
			var task = tasks[0];

			var leader = (CoalitionReconfigurationAgent)context;
			var config = new ConfigurationUpdate();
			var coalition = new Coalition(leader, task);

			try
			{
				foreach (var predicate in leader.BaseAgentState.ViolatedPredicates)
					await SolveInvariantViolation(coalition, predicate, task, config);
			}
			catch (OperationCanceledException)
			{
				// operation was canceled (e.g. because coalition was merged into another coalition), so produce no updates
				return null;
			}

			return config;
		}

		/// <summary>
		/// Selects the strategy used to solve the occuring invariant violations based on the violated predicate.
		/// </summary>
		protected virtual Task SolveInvariantViolation(Coalition coalition, InvariantPredicate invariant,
																			 ITask task, ConfigurationUpdate config)
		{
			if (invariant == Invariant.CapabilityConsistency)
				return RestoreCapabilityConsistency(coalition, task, config);

			throw new NotImplementedException(); // TODO: strategies for other invariant predicates
		}

		/// <summary>
		/// Reconfiguration strategy for violations of the <see cref="Invariant.CapabilityConsistency"/> invariant predicate.
		/// </summary>
		protected async Task RestoreCapabilityConsistency(Coalition coalition, ITask task, ConfigurationUpdate config)
		{
			while (true)
			{
				await EnlargeCoalitionUntil(coalition.CapabilitiesSatisfied, coalition, RecrutableMembers(coalition));

				foreach (var distribution in CalculateCapabilityDistributions(coalition))
				{
					var resourceFlow = CalculateResourceFlow(coalition, distribution);
					if (resourceFlow != null)
					{
						await CalculateRoleAllocations(coalition, distribution, resourceFlow, config);
						return;
					}
				}

				// TODO: enlarge coalition to allow resource flow (?)
			}
		}

		private IEnumerable<BaseAgent> RecrutableMembers(Coalition coalition)
		{
			var memberBaseAgents = coalition.Members.Select(member => member.BaseAgent);

			// TODO: prioritize already participating agents (inputs/outputs of member roles for task)
			var queueIn = new Queue<BaseAgent>(coalition.Members.SelectMany(member => member.BaseAgentState.Inputs).Except(memberBaseAgents));
			var queueOut = new Queue<BaseAgent>(coalition.Members.SelectMany(member => member.BaseAgentState.Outputs).Except(memberBaseAgents));

			while (queueOut.Count > 0 || queueIn.Count > 0)
			{
				// TODO: update BOTH queues (remove nextMember, add nextMember's neighbours, prioritize participating agents)
				if (queueIn.Count > 0)
					yield return queueIn.Dequeue();
				if (queueOut.Count > 0)
					yield return queueOut.Dequeue();
			}
		}

		private async Task EnlargeCoalitionUntil(Func<bool> condition, Coalition coalition, IEnumerable<BaseAgent> possibleMembers)
		{
			foreach (var agent in possibleMembers.TakeWhile(a => !condition()))
				await coalition.Invite(agent);
		}

		/// <summary>
		/// Lazily calculates all possible distributions of the capabilities in CTF between the members of the coalition.
		/// </summary>
		protected IEnumerable<BaseAgent[]> CalculateCapabilityDistributions(Coalition coalition)
		{
			var distribution = new BaseAgent[coalition.CTF.Length];
			return CalculateCapabilityDistributions(coalition, distribution, 0);
		}

		// enumerate all paths, but lazily! (depth-first search)
		private IEnumerable<BaseAgent[]> CalculateCapabilityDistributions(Coalition coalition, BaseAgent[] distribution, int prefixLength)
		{
			// termination case: copy distribution and return it
			if (prefixLength == distribution.Length)
			{
				var result = new BaseAgent[distribution.Length];
				Array.Copy(distribution, result, distribution.Length);
				yield return result;
				yield break;
			}

			// recursive case: iterate through all possible next agents, recurse, forward results
			var eligibleAgents = coalition.Members
										  .Select(member => member.BaseAgent)
										  .Where(agent => CanSatisfyNext(agent, coalition, distribution, prefixLength));
			foreach (var agent in eligibleAgents)
			{
				distribution[prefixLength] = agent;
				foreach (var result in CalculateCapabilityDistributions(coalition, distribution, prefixLength + 1))
					yield return result;
			}
		}

		// TODO: override for pill production
		protected virtual bool CanSatisfyNext(BaseAgent agent, Coalition coalition, BaseAgent[] distribution, int prefixLength)
		{
			var capability = coalition.Task.RequiredCapabilities[coalition.CTF.Start + prefixLength];
			return agent.AvailableCapabilities.Contains(capability);
		}

		protected BaseAgent[] CalculateResourceFlow(Coalition coalition, BaseAgent[] distribution)
		{
			throw new NotImplementedException();
		}

		protected async Task CalculateRoleAllocations(Coalition coalition, BaseAgent[] distribution, BaseAgent[] resourceFlow,
												ConfigurationUpdate config)
		{
			await Task.Yield();
			throw new NotImplementedException();
		}
	}
}