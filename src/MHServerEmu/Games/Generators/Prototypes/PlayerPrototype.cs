using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class PlayerPrototype : EntityPrototype
    {
        public float CameraFacingDirX;
        public float CameraFacingDirY;
        public float CameraFacingDirZ;
        public float CameraFOV;
        public float CameraZoomDistance;
        public float ResurrectWaitTime;
        public int NumConsumableSlots;
        public AbilityAssignmentPrototype[] StartingEmotes;
        public EntityInventoryAssignmentPrototype[] StashInventories;
        public int MaxDroppedItems;
        public PlayerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PlayerPrototype), proto); }
    }

    public class CohortPrototype : Prototype
    {
        public int Weight;
        public CohortPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CohortPrototype), proto); }
    }

    public class CohortExperimentPrototype : Prototype
    {
        public CohortPrototype Cohorts;
        public CohortExperimentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CohortExperimentPrototype), proto); }
    }
}
