using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
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
    public sealed class EntitySettings : IPoolable, IDisposable
    {
        public EntityCreateResults Results;

        public ulong Id { get; set; }
        public ulong DbGuid { get; set; }
        public PrototypeId EntityRef { get; set; }
        public ulong RegionId { get; set; }
        public Vector3 Position { get; set; }
        public Orientation Orientation { get; set; }

        public ArchiveSerializeType ArchiveSerializeType { get; set; } = ArchiveSerializeType.Invalid;
        public byte[] ArchiveData { get; set; }

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
        public SpawnSpec SpawnSpec { get; set; }
        public float LocomotorHeightOverride { get; set; }

        // Class-specific
        public PlayerConnection PlayerConnection { get; set; }      // For Player
        public string PlayerName { get; set; }

        public ItemSpec ItemSpec { get; set; }                      // For Item
        public TimeSpan Lifespan { get; set; }
        public int VariationSeed { get; set; }
        public bool IsPopulation { get; set; }

        public bool IsInPool { get; set; }

        public EntitySettings() { }     // Use pooling instead of calling this directly

        public void ResetForPool()
        {
            Results = default;

            Id = 0;
            DbGuid = 0;
            EntityRef = 0;
            RegionId = 0;
            Position = default;
            Orientation = default;

            ArchiveSerializeType = ArchiveSerializeType.Invalid;
            ArchiveData = null;

            SourceEntityId = 0;
            SourcePosition = default;
            BoundsScaleOverride = 1f;
            IgnoreNavi = false;

            InventoryLocation = null;
            InventoryLocationPrevious = InventoryLocation.Invalid;

            OptionFlags = EntitySettingsOptionFlags.DefaultOptions;

            HotspotSkipCollide = false;
            Properties = null;
            Cell = null;
            Actions = null;
            SpawnSpec = null;
            LocomotorHeightOverride = 0f;

            PlayerConnection = null;
            PlayerName = null;

            ItemSpec = null;
            Lifespan = default;
            VariationSeed = 0;
            IsPopulation = default;
        }

        public void Dispose()
        {
            ObjectPoolManager.Instance.Return(this);
        }
    }

    public struct EntityCreateResults
    {
        public InventoryResult InventoryResult = InventoryResult.NotAttempted;
        public Entity Entity = null;

        public EntityCreateResults() { }
    }
}
