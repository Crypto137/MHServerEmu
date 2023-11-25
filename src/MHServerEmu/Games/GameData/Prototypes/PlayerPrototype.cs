namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PlayerPrototype : EntityPrototype
    {
        public float CameraFacingDirX { get; set; }
        public float CameraFacingDirY { get; set; }
        public float CameraFacingDirZ { get; set; }
        public float CameraFOV { get; set; }
        public float CameraZoomDistance { get; set; }
        public float ResurrectWaitTime { get; set; }
        public int NumConsumableSlots { get; set; }
        public AbilityAssignmentPrototype[] StartingEmotes { get; set; }
        public EntityInventoryAssignmentPrototype[] StashInventories { get; set; }
        public int MaxDroppedItems { get; set; }
    }

    public class CohortPrototype : Prototype
    {
        public int Weight { get; set; }
    }

    public class CohortExperimentPrototype : Prototype
    {
        public CohortPrototype Cohorts { get; set; }
    }
}
