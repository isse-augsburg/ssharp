namespace SelfOrganizingPillProduction
{
    /// <summary>
    /// A production station that removes containers from the conveyor belt, closes, labels and stores them on pallets.
    /// </summary>
    public class PalletisationStation : Station
    {
        public override Capability[] AvailableCapabilities { get; } = new[] { ConsumeCapability.Instance };

        protected override void ExecuteRole(Role role)
        {
            Container.Recipe.ActiveContainers.Remove(Container);
            if (Container.Recipe.ActiveContainers.Count == 0)
            {
                RemoveRecipeConfigurations(Container.Recipe);
            }
            Container = null;
        }
    }
}
