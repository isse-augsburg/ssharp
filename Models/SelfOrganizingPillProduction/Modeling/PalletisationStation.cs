namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// A production station that removes containers from the conveyor belt, closes, labels and stores them on pallets.
    /// </summary>
    public class PalletisationStation : Station
    {
        public override Capability[] AvailableCapabilities { get; } = new[] { new ConsumeCapability() };

        protected override void ExecuteRole(Role role)
        {
            Container.Recipe.RemoveContainer(Container);
            if (Container.Recipe.ProcessingComplete)
            {
                RemoveRecipeConfigurations(Container.Recipe);
            }
            Container = null;
        }
    }
}
