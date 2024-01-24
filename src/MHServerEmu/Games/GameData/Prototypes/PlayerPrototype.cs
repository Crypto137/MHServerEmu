namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PlayerPrototype : EntityPrototype
    {
        public float CameraFacingDirX { get; protected set; }
        public float CameraFacingDirY { get; protected set; }
        public float CameraFacingDirZ { get; protected set; }
        public float CameraFOV { get; protected set; }
        public float CameraZoomDistance { get; protected set; }
        public float ResurrectWaitTime { get; protected set; }
        public int NumConsumableSlots { get; protected set; }
        public AbilityAssignmentPrototype[] StartingEmotes { get; protected set; }
        public EntityInventoryAssignmentPrototype[] StashInventories { get; protected set; }
        public int MaxDroppedItems { get; protected set; }
    }

    public class CohortPrototype : Prototype
    {
        public int Weight { get; protected set; }
    }

    public class CohortExperimentPrototype : Prototype
    {
        public PrototypeId[] Cohorts { get; protected set; }      // VectorPrototypeRefPtr CohortPrototype 
    }
}
