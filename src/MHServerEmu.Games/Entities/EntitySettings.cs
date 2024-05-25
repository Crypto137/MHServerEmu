using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public enum EntitySettingsOptionFlags
    {
        None                        = 0,
        HasOverrideSnapToFloor      = 1 << 0,
        OverrideSnapToFloorValue    = 1 << 1,
        EnterGameWorld              = 1 << 2,
        Flag3                       = 1 << 3,
        Flag4                       = 1 << 4,
        IsNewOnServer               = 1 << 5,
        PopulateInventories         = 1 << 6,
        Flag7                       = 1 << 7,
        Flag8                       = 1 << 8,
        Flag9                       = 1 << 9,
        Flag10                      = 1 << 10,
        Flag11                      = 1 << 11,
        IsClientEntityHidden        = 1 << 12,
        Flag13                      = 1 << 13,
        DoNotAllowStackingOnCreate  = 1 << 14,   // Used in EntityManager::finalizeEntity() for Inventory::ChangeEntityInventoryLocationOnCreate()

        DefaultOptions = EnterGameWorld | PopulateInventories | Flag9
    }

    /// <summary>
    /// Contains parameters for <see cref="Entity"/> creation.
    /// </summary>
    public class EntitySettings
    {
        public ulong Id { get; set; }
        public PrototypeId EntityRef { get; set; }
        public ulong RegionId { get; set; }
        public Vector3 Position { get; set; }
        public Orientation Orientation { get; set; }

        public EntitySettingsOptionFlags OptionFlags { get; set; }

        public bool HotspotSkipCollide { get; set; }
        public PropertyCollection Properties { get; set; }
        public Cell Cell { get; set; }
        public List<EntitySelectorActionPrototype> Actions { get; set; }
        public PrototypeId ActionsTarget { get; set; }
        public SpawnSpec SpawnSpec { get; set; }
        public float LocomotorHeightOverride { get; set; }

        public ItemSpec ItemSpec { get; set; }
    }
}
