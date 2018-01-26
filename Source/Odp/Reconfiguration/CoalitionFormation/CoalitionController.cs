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

namespace SafetySharp.Odp.Reconfiguration.CoalitionFormation
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;
	using JetBrains.Annotations;

	/// <summary>
	///   ...
	/// </summary>
	public partial class CoalitionController : AbstractController
	{
		private readonly IConfigurationFinder _configurationFinder;

		private readonly Dictionary<InvariantPredicate, IRecruitingStrategy> _strategies =
			new Dictionary<InvariantPredicate, IRecruitingStrategy>();

		protected virtual IRecruitingStrategy NewTaskStrategy => CoalitionFormation.NewTaskStrategy.Instance;

		public CoalitionController([NotNull, ItemNotNull] BaseAgent[] agents, [NotNull] IConfigurationFinder configurationFinder)
			: base(agents)
		{
			if (configurationFinder == null)
				throw new ArgumentNullException(nameof(configurationFinder));

			_configurationFinder = configurationFinder;

			Register(Invariant.CapabilityConsistency, MissingCapabilitiesStrategy.Instance);
			Register(Invariant.IoConsistency, BrokenIoStrategy.Instance);
			Register(Invariant.NeighborsAliveGuarantee, DeadNeighbourStrategy.Instance);
		}

		public void Register(InvariantPredicate predicate, IRecruitingStrategy strategy)
		{
			_strategies[predicate] = strategy;
		}

		public override Task<ConfigurationUpdate> CalculateConfigurationsAsync(object context, ITask task)
		{
			var leader = (CoalitionReconfigurationAgent)context;
			var violatedPredicates = (leader.ReconfigurationReason as ReconfigurationReason.InvariantsViolated)?.ViolatedPredicates
									 ?? new InvariantPredicate[0];
			var isInitialReconfiguration = leader.ReconfigurationReason is ReconfigurationReason.InitialReconfiguration;

			var coalition = new Coalition(leader, task, violatedPredicates, isInitialReconfiguration);
			return CalculateConfigurations(coalition);
		}

		protected async Task<ConfigurationUpdate> CalculateConfigurations(Coalition coalition)
		{
			Debug.WriteLine("Begin coalition-based reconfiguration");
			try
			{
				ConfigurationUpdate config;

				Debug.WriteLine("Recruiting necessary agents");
				var fragmentComputations = new List<Task<TaskFragment>>();

				if (coalition.IsInitialConfiguration)
					fragmentComputations.Add(NewTaskStrategy.RecruitNecessaryAgents(coalition));
				fragmentComputations.AddRange(from predicate in coalition.ViolatedPredicates
											  select RecruitNecessaryAgents(coalition, predicate));

				var fragments = await Task.WhenAll(fragmentComputations);
				var minTfr = TaskFragment.Merge(coalition.Task, fragments);

				Debug.WriteLine("Inviting CTF agents");
				coalition.MergeCtf(minTfr);
				await coalition.InviteCtfAgents();

				do
				{
					Debug.WriteLine("Calculating solution");

					var fragment = coalition.CTF;
					var entry = coalition.RecoveredDistribution[fragment.Start];
					var exit = coalition.RecoveredDistribution[fragment.End];

					var solution = await _configurationFinder.Find(fragment, coalition.BaseAgents, x => x == entry, x => x == exit);
					if (solution.HasValue)
					{
						Debug.WriteLine("Computing role allocations");
						config = await ComputeRoleAllocations(fragment, solution.Value, coalition, minTfr);

						config.RecordInvolvement(coalition.BaseAgents);
						OnConfigurationsCalculated(coalition.Task, config);

						Debug.WriteLine("Reconfiguration complete");
						return config;
					}
					else if (coalition.HasNeighbours) // no solution found: recruit an arbitrary known agent, try again
					{
						Debug.WriteLine("Inviting arbitrary neighbour");
						await coalition.InviteNeighbour();
						await coalition.InviteCtfAgents(); // CTF might have grown - make sure to fill all gaps if it has
					}
					else // failed to find a solution; no more neighbours to recruit - give up
						break;
				} while (true);

				config = FailedReconfiguration(coalition);
				OnConfigurationsCalculated(coalition.Task, config);
				return config;
			}
			catch (OperationCanceledException)
			{
				// operation was canceled (e.g. because coalition was merged into another coalition), so produce no updates
				Debug.WriteLine("Controller for coalition with leader {0} cancelled", coalition.Leader.BaseAgent.Id);
				return new ConfigurationUpdate();
			}
			catch (RestartReconfigurationException)
			{
				// restart reconfiguration, e.g. because another coalition was just merged into the current one
				Debug.WriteLine("Controller for coalition with leader {0} restarted", coalition.Leader.BaseAgent.Id);
				return await CalculateConfigurations(coalition);
			}
		}

		private static ConfigurationUpdate FailedReconfiguration(Coalition coalition)
		{
			// At this point, all reachable agents will be in the coalition.
			// Otherwise the algorithm would try to recruit them instead of failing.

			var config = new ConfigurationUpdate();
			config.Fail();
			config.RemoveAllRoles(coalition.Task, coalition.BaseAgents.ToArray());
			return config;
		}

		/// <summary>
		/// Selects the strategy used to solve the occuring invariant violations based on the violated predicate.
		/// </summary>
		private Task<TaskFragment> RecruitNecessaryAgents(Coalition coalition, InvariantPredicate invariant)
		{
			if (!_strategies.ContainsKey(invariant))
				throw new InvalidOperationException("no recruiting strategy specified for invariant predicate");
			return _strategies[invariant].RecruitNecessaryAgents(coalition);
		}

		/// <summary>
		///   From a <paramref name="solution"/> returned by an <see cref="IConfigurationFinder"/>, computes role allocation changes for the <paramref name="coalition"/>'s agents.
		/// </summary>
		/// <param name="fragment">The task fragment covered by the <paramref name="solution"/>.</param>
		/// <param name="solution">The solution, i.e., distribution and resource flow.</param>
		/// <param name="coalition">The coalition for which updates are computed.</param>
		/// <param name="minimalReconfiguredFragment">The minimal fragment that must be reconfigured.</param>
		/// <returns></returns>
		private Task<ConfigurationUpdate> ComputeRoleAllocations(TaskFragment fragment, Configuration solution, Coalition coalition, TaskFragment minimalReconfiguredFragment)
		{
			return RoleAllocator.Allocate(fragment, solution, coalition, minimalReconfiguredFragment);
		}
	}
}
