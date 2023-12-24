namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PlayerPrototype : EntityPrototype
    {
        public float CameraFacingDirX { get; private set; }
        public float CameraFacingDirY { get; private set; }
        public float CameraFacingDirZ { get; private set; }
        public float CameraFOV { get; private set; }
        public float CameraZoomDistance { get; private set; }
        public float ResurrectWaitTime { get; private set; }
        public int NumConsumableSlots { get; private set; }
        public AbilityAssignmentPrototype[] StartingEmotes { get; private set; }
        public EntityInventoryAssignmentPrototype[] StashInventories { get; private set; }
        public int MaxDroppedItems { get; private set; }
    }

    public class CohortPrototype : Prototype
    {
        public int Weight { get; private set; }
    }

    public class CohortExperimentPrototype : Prototype
    {
        public CohortPrototype Cohorts { get; private set; }
    }
}
