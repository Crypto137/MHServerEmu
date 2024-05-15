using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Inventories
{
    public class Inventory
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private HashSet<(uint, InvEntry)> _entities = new();

        public Game Game { get; }
        public ulong OwnerId { get; private set; }
        public Entity Owner { get => Game.EntityManager.GetEntity<Entity>(OwnerId); }

        public InventoryPrototype Prototype { get; private set; }
        public PrototypeId PrototypeDataRef { get => Prototype != null ? Prototype.DataRef : PrototypeId.Invalid; }

        public InventoryCategory Category { get; private set; } = InventoryCategory.None;
        public InventoryConvenienceLabel ConvenienceLabel { get; private set; } = InventoryConvenienceLabel.None;
        public int MaxCapacity { get; private set; }

        public int Count { get => _entities.Count; }

        public Inventory(Game game)
        {
            Game = game;
        }

        public bool Initialize(PrototypeId prototypeRef, ulong ownerId)
        {
            var prototype = prototypeRef.As<InventoryPrototype>();
            if (prototype == null) return Logger.WarnReturn(false, "Initialize(): prototype == null");

            Prototype = prototype;
            OwnerId = ownerId;
            Category = prototype.Category;
            ConvenienceLabel = prototype.ConvenienceLabel;
            MaxCapacity = prototype.CapacityUnlimited ? int.MaxValue : prototype.Capacity;

            if (ownerId == EntityManager.InvalidEntityId) return Logger.WarnReturn(false, "Initialize(): ownerId == InvalidEntityId");
            return true;
        }

        public static InventoryResult ChangeEntityInventoryLocation(Entity entity, Inventory destination, uint slot, ref ulong stackEntityId, bool useStacking)
        {
            InventoryLocation invLoc = entity.InventoryLocation;

            if (destination != null)
            {
                if (invLoc.IsValid == false)
                    return destination.AddEntity(entity, ref stackEntityId, useStacking, slot, InventoryLocation.Invalid);

                Inventory prevInventory = entity.GetOwnerInventory();
                if (prevInventory == null)
                    return Logger.WarnReturn(InventoryResult.NotInInventory, $"ChangeEntityInventoryLocation(): Unable to get owner inventory for move with entity {entity.Id} at invLoc {invLoc}");

                return prevInventory.MoveEntityTo(entity, destination, ref stackEntityId, useStacking, slot);
            }
            else
            {
                if (invLoc.IsValid == false)
                    return Logger.WarnReturn(InventoryResult.NotInInventory, $"ChangeEntityInventoryLocation(): Trying to remove entity {entity.Id} from inventory, but it is not in any inventory");

                Inventory inventory = entity.GetOwnerInventory();

                if (inventory == null)
                    return Logger.WarnReturn(InventoryResult.NotInInventory, $"ChangeEntityInventoryLocation(): Unable to get owner inventory for remove with entity {entity.Id} at invLoc {invLoc}");

                return inventory.RemoveEntity(entity);
            }
        }

        public static bool IsPlayerStashInventory(PrototypeId inventoryRef)
        {
            if (inventoryRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, "IsPlayerStashInventory(): inventoryRef == PrototypeId.Invalid");

            var inventoryProto = GameDatabase.GetPrototype<InventoryPrototype>(inventoryRef);
            if (inventoryProto == null)
                return Logger.WarnReturn(false, "IsPlayerStashInventory(): inventoryProto == null");

            return inventoryProto.IsPlayerStashInventory();
        }

        private InventoryResult AddEntity(Entity entity, ref ulong stackEntityId, bool useStacking, uint slot, InventoryLocation prevInvLoc)
        {
            return InventoryResult.Invalid;
        }

        private InventoryResult MoveEntityTo(Entity entity, Inventory destination, ref ulong stackEntityId, bool useStacking, uint slot)
        {
            return InventoryResult.Invalid;
        }

        private InventoryResult RemoveEntity(Entity entity)
        {
            return InventoryResult.Invalid;
        }

        private readonly struct InvEntry : IComparable<InvEntry>
        {
            public ulong EntityId { get; }
            public PrototypeId PrototypeDataRef { get; }
            public InventoryMetaData MetaData { get; }

            public InvEntry()
            {
                EntityId = 0;
                PrototypeDataRef = PrototypeId.Invalid;
                MetaData = null;
            }

            public InvEntry(ulong entityId, PrototypeId prototypeDataRef, InventoryMetaData metaData)
            {
                EntityId = entityId;
                PrototypeDataRef = prototypeDataRef;
                MetaData = metaData;
            }

            public InvEntry(InvEntry other)
            {
                EntityId = other.EntityId;
                PrototypeDataRef = other.PrototypeDataRef;
                MetaData = other.MetaData;
            }

            public int CompareTo(InvEntry other) => EntityId.CompareTo(other.EntityId);
        }
    }
}
