using MHServerEmu.Core.VectorMath;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public enum EntitySettingsOptionFlags
    {
        None                        = 0,
        HasOverrideSnapToFloor      = 1 << 0,
        OverrideSnapToFloorValue    = 1 << 1,
        EnterGame                   = 1 << 2,   // Entity enters game as soon as its created
        SuspendDBOpsWhileCreating   = 1 << 3,   // Sets DisableDBOps status during entity creation
        Flag4                       = 1 << 4,
        IsNewOnServer               = 1 << 5,
        PopulateInventories         = 1 << 6,   // Initialize inventory instances on creation
        Flag7                       = 1 << 7,
        ClientOnly                  = 1 << 8,   // Entity is client-only
        LogInventoryErrors          = 1 << 9,
        Flag10                      = 1 << 10,
        IsPacked                    = 1 << 11,
        IsClientEntityHidden        = 1 << 12,  // Hide avatar during swapping
        DeferAdapterChanges         = 1 << 13,  // Used for interaction with UE3
        DoNotAllowStackingOnCreate  = 1 << 14,  // Used as an argument in Inventory::ChangeEntityInventoryLocationOnCreate()

        DefaultOptions = EnterGame | PopulateInventories | LogInventoryErrors
    }

    /// <summary>
    /// Contains parameters for <see cref="Entity"/> creation.
    /// </summary>
    public class EntitySettings
    {
        public EntityCreateResults Results;

        public ulong Id { get; set; }
        public ulong DbGuid { get; set; }
        public PrototypeId EntityRef { get; set; }
        public ulong RegionId { get; set; }
        public Vector3 Position { get; set; }
        public Orientation Orientation { get; set; }
        public byte[] ArchiveData { get; set; }
        public DBAccount DBAccount { get; set; }    // TODO: Remove this when we have persistent archives

        public ulong SourceEntityId { get; set; }
        public Vector3 SourcePosition { get; set; }
        public float BoundsScaleOverride { get; set; } = 1f;
        public bool IgnoreNavi { get; set; }

        public InventoryLocation InventoryLocation { get; set; }
        public InventoryLocation InventoryLocationPrevious { get; set; } = InventoryLocation.Invalid;

        public EntitySettingsOptionFlags OptionFlags { get; set; } = EntitySettingsOptionFlags.DefaultOptions;

        public bool HotspotSkipCollide { get; set; }
        public PropertyCollection Properties { get; set; }
        public Cell Cell { get; set; }
        public List<EntitySelectorActionPrototype> Actions { get; set; }
        public PrototypeId ActionsTarget { get; set; }
        public SpawnSpec SpawnSpec { get; set; }
        public float LocomotorHeightOverride { get; set; }

        // Class-specific
        public PlayerConnection PlayerConnection { get; set; }      // For Player
        public ItemSpec ItemSpec { get; set; }                      // For Item
        public TimeSpan Lifespan { get; set; }
        public uint VariationSeed { get; set; }
    }

    public struct EntityCreateResults
    {
        public InventoryResult InventoryResult = InventoryResult.NotAttempted;
        public Entity Entity = null;

        public EntityCreateResults() { }
    }
}
