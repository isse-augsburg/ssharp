using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// A production station that removes containers from the conveyor belt, closes, labels and stores them on pallets.
    /// </summary>
    public class PalletisationStation : Station
    {
        public readonly Fault PalletisationDefect = new PermanentFault();

        public override Capability[] AvailableCapabilities { get; } = new[] { new ConsumeCapability() };

        public PalletisationStation()
        {
            CompleteStationFailure.Subsumes(PalletisationDefect);
        }

        protected override void ExecuteRole(Role role)
        {
            // unless role is transport only, it will always be { ConsumeCapability }
            if (role.CapabilitiesToApply.Count > 0)
            {
                Container.Recipe.RemoveContainer(Container);
                if (Container.Recipe.ProcessingComplete)
                {
                    RemoveRecipeConfigurations(Container.Recipe);
                }
                currentRole = null;
                Container = null;
            }
        }

        [FaultEffect(Fault = nameof(PalletisationDefect))]
        public class PalletisationDefectEffect : PalletisationStation
        {
            public override Capability[] AvailableCapabilities => new Capability[0];
        }

        [FaultEffect(Fault = nameof(CompleteStationFailure))]
        public class CompleteStationFailureEffect : PalletisationStation
        {
            public override bool IsAlive => false;

            public override void Update() { }
        }
    }
}
