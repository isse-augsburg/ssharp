namespace SafetySharp.Odp
{
	using System;
	using System.Linq;

	public static class Invariant
	{
		public static Predicate<Role<TAgent, TTask, TResource>> CapabilitiesAvailable<TAgent, TTask, TResource>(this BaseAgent<TAgent, TTask, TResource> agent)
			where TAgent : BaseAgent<TAgent,TTask,TResource>
			where TTask : class, ITask
		{
			return (role) => role.CapabilitiesToApply.All(cap => agent.AvailableCapabilities.Contains(cap));
		}

		public static Predicate<Role<TAgent,TTask,TResource>> ResourceFlowPossible<TAgent,TTask,TResource>(this BaseAgent<TAgent, TTask, TResource> agent)
			where TAgent : BaseAgent<TAgent, TTask, TResource>
			where TTask : class, ITask
		{
			return (role) => (role.PreCondition.Port == null || agent.Inputs.Contains(role.PreCondition.Port))
				&& (role.PostCondition.Port == null || agent.Outputs.Contains(role.PostCondition.Port));
		}

		public static Predicate<Role<TAgent, TTask, TResource>> ResourceFlowConsistent<TAgent, TTask, TResource>(this BaseAgent<TAgent, TTask, TResource> agent)
			where TAgent : BaseAgent<TAgent, TTask, TResource>
			where TTask : class, ITask
		{
			return (role) =>
				// consistent input:
				(role.PreCondition.Port == null // no input...
				|| role.PreCondition.Port.AllocatedRoles.Exists( // ... or there's a matching role at the input
					sendingRole => sendingRole.PostCondition.StateMatches(role.PreCondition)
						&& sendingRole.PostCondition.Port == agent
					)
				)
				// consistent output:
				&& (role.PostCondition.Port == null // no output...
				|| role.PostCondition.Port.AllocatedRoles.Exists( // ... or there's a matching role at the output
					receivingRole => receivingRole.PreCondition.StateMatches(role.PostCondition)
						&& receivingRole.PreCondition.Port == agent
					)
				);
		}
	}
}
