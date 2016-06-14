namespace SafetySharp.Analysis
{
	/// <summary>
	/// Defines how faults are activated during analysis.
	/// </summary>
	public enum FaultActivationBehaviour
	{
		/// <summary>
		/// Faults are activated nondeterministically (the default).
		/// </summary>
		Nondeterministic,

		/// <summary>
		/// First analyze with forced fault activation. If the hazard does not occur,
		/// test nondeterministically to make sure the fault set is safe.
		/// </summary>
		ForceThenFallback,

		/// <summary>
		/// Only analyze with forced fault activation.
		/// </summary>
		ForceOnly
	}
}
