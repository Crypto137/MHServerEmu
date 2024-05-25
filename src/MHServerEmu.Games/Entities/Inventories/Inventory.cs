﻿using System.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities.Inventories
{
    public class Inventory : IEnumerable<KeyValuePair<uint, Inventory.InvEntry>>
    {
        public const uint InvalidSlot = uint.MaxValue;      // 0xFFFFFFFF / -1

        private static readonly Logger Logger = LogManager.CreateLogger();

        // The client uses a set of std::pair here, the main requirement is that this has to be sorted by key (slot)
        private SortedDictionary<uint, InvEntry> _entities = new();

        public Game Game { get; }
        public ulong OwnerId { get; private set; }
        public Entity Owner { get => Game.EntityManager.GetEntity<Entity>(OwnerId); }

        public InventoryPrototype Prototype { get; private set; }
        public PrototypeId PrototypeDataRef { get => Prototype != null ? Prototype.DataRef : PrototypeId.Invalid; }

        public InventoryCategory Category { get; private set; } = InventoryCategory.None;
        public InventoryConvenienceLabel ConvenienceLabel { get; private set; } = InventoryConvenienceLabel.None;
        public bool IsEquipment { get => Prototype != null && Prototype.IsEquipmentInventory; }
        public int MaxCapacity { get; private set; }

        public int Count { get => _entities.Count; }

        public Inventory(Game game)
        {
            Game = game;
        }

        public override string ToString()
        {
            return $"{GameDatabase.GetPrototypeName(PrototypeDataRef)}";
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

            if (ownerId == Entity.InvalidId) return Logger.WarnReturn(false, "Initialize(): ownerId == Entity.InvalidId");
            return true;
        }

        public ulong GetEntityInSlot(uint slot)
        {
            foreach (var kvp in this)
            {
                if (kvp.Key == slot)
                    return kvp.Value.EntityId;
            }

            return Entity.InvalidId;
        }

        public ulong GetAnyEntity()
        {
            if (_entities.Any())
                return _entities.First().Value.EntityId;

            return 0;
        }

        public Entity GetMatchingEntity(PrototypeId entityRef)
        {
            if (entityRef == PrototypeId.Invalid) return Logger.WarnReturn<Entity>(null, "GetMatchingEntity(): entityRef == PrototypeId.Invalid");

            foreach (var kvp in this)
            {
                if (kvp.Value.PrototypeDataRef == entityRef)
                    return Game.EntityManager.GetEntity<Entity>(kvp.Value.EntityId);
            }

            return null;
        }

        public int GetMatchingEntities(PrototypeId entityRef, List<ulong> matchList = null)
        {
            // NOTE: This is probably used for things like checking if a player has enough of something (e.g. crafting materials)
            if (entityRef == PrototypeId.Invalid) return Logger.WarnReturn(0, "GetMatchingEntities(): entityRef == PrototypeId.Invalid");

            int numMatches = 0;

            foreach (var kvp in this)
            {
                if (kvp.Value.PrototypeDataRef == entityRef)
                {
                    Entity entity = Game.EntityManager.GetEntity<Entity>(kvp.Value.EntityId);
                    if (entity != null)
                    {
                        numMatches += entity.CurrentStackSize;
                        matchList?.Add(entity.Id);
                    }
                }
            }

            return numMatches;
        }

        public bool ContainsMatchingEntity(PrototypeId entityRef)
        {
            if (entityRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "ContainsMatchingEntity(): entityRef == PrototypeId.Invalid");

            foreach (var kvp in this)
            {
                if (kvp.Value.PrototypeDataRef == entityRef)
                    return true;
            }

            return false;
        }

        public bool DestroyContained()
        {
            if (Game == null) return Logger.WarnReturn(false, "DestroyContained(): Game == null");

            // NOTE: We convert entry collection to list to be able to remove entries while we iterate.
            // The original implementation uses a custom iterator here that restarts after every removed item.
            foreach (var kvp in _entities.ToList())
            {
                Entity contained = Game.EntityManager.GetEntity<Entity>(kvp.Value.EntityId);
                if (contained == null)
                {
                    Logger.Warn("DestroyContained(): contained == null");
                    continue;
                }

                bool isDestroyingAllEntities = false;
                if (Game.EntityManager == null)
                    Logger.Warn("DestroyContained(): Game.EntityManager == null");
                else
                    isDestroyingAllEntities = Game.EntityManager.IsDestroyingAllEntities;

                // Entities that have the DetachOnContainerDestroyed are not destroyed (unless the EntityManager is currently cleaning up all entities)
                if (contained.Properties[PropertyEnum.DetachOnContainerDestroyed] && isDestroyingAllEntities == false)
                {
                    contained.ChangeInventoryLocation(null);
                    contained.OnDetachedFromDestroyedContainer();
                    continue;
                }

                contained.Destroy();
            }

            return true;
        }

        public int GetCapacity()
        {
            if (Prototype == null) return Logger.WarnReturn(0, "GetCapacity(): Prototype == null");

            int nSoftCap = Prototype.GetSoftCapacityDefaultSlots();
            if (nSoftCap < 0) return MaxCapacity;

            if (Owner == null) return Logger.WarnReturn(MaxCapacity, "GetCapacity(): Owner == null");

            foreach (PrototypeId slotGroupRef in Prototype.GetSoftCapacitySlotGroups())
            {
                var slotGroup = slotGroupRef.As<InventoryExtraSlotsGroupPrototype>();
                int extraSlots = Owner.Properties[PropertyEnum.InventoryExtraSlotsAvailable, slotGroup.DataRef];

                if (slotGroup.MaxExtraSlotCount > 0)
                    extraSlots = Math.Min(extraSlots, slotGroup.MaxExtraSlotCount);

                nSoftCap += extraSlots;
            }

            if (nSoftCap > MaxCapacity) Logger.Warn($"GetCapacity(): Inventory softcap over max inventory limit. INVENTORY={this} OWNER={Owner}");

            return Math.Min(nSoftCap, MaxCapacity);
        }

        public bool IsSlotFree(uint slot)
        {
            if (slot == InvalidSlot) return Logger.WarnReturn(false, $"IsSlotFree(): Called with the invalid slot id for inventory {this}");
            if (Game == null) return Logger.WarnReturn(false, "IsSlotFree(): Game == null");

            if (Count >= GetCapacity()) return false;
            if (slot >= GetCapacity()) return false;
            if (GetEntityInSlot(slot) != Entity.InvalidId) return false;

            Entity inventoryOwner = Game.EntityManager.GetEntity<Entity>(OwnerId);
            if (inventoryOwner == null) return Logger.WarnReturn(false, "IsSlotFree(): inventoryOwner == null");

            if (inventoryOwner.ValidateInventorySlot(this, slot) == false) return false;

            return true;
        }

        public uint GetFreeSlot(Entity entity, bool allowStacking, bool isAdding = false)
        {
            if (Game == null) return Logger.WarnReturn(InvalidSlot, "GetFreeSlot(): Game == null");

            if (entity != null && allowStacking)
            {
                // NOTE: GetAutoStackSlot() is part of GetFreeSlot() in the client
                uint stackSlot = GetAutoStackSlot(entity, isAdding);
                if (stackSlot != InvalidSlot) return stackSlot;
            }

            Entity inventoryOwner = Game.EntityManager.GetEntity<Entity>(OwnerId);
            if (inventoryOwner == null) return Logger.WarnReturn(InvalidSlot, "GetFreeSlot(): inventoryOwner == null");

            // Make sure we actually have free slots
            if (Count >= GetCapacity()) return InvalidSlot;

            // Look for a free slot between occupied ones
            // NOTE: This requires the slot / InvEntry collection to be sorted by slot
            uint slot = 0;
            foreach (var kvp in this)
            {
                if (kvp.Key != slot && inventoryOwner.ValidateInventorySlot(this, slot))
                    return slot;

                slot++;
            }

            // If there are no free spaces in between occupied slots, get a free slot from the end
            slot = (uint)Count;
            if (inventoryOwner.ValidateInventorySlot(this, slot))
                return slot;

            return InvalidSlot;
        }

        public uint GetAutoStackSlot(Entity entity, bool isAdding = false)
        {
            if (entity.CanStack() == false || entity.IsAutoStackedWhenAddedToInventory() == false)
                return InvalidSlot;
            
            foreach (var kvp in this)
            {
                if (kvp.Key == entity.Id) continue;     // Stacking with itself sure sounds like a potential dupe
                Entity existingEntity = Game.EntityManager.GetEntity<Entity>(kvp.Value.EntityId);
                
                if (existingEntity == null)
                {
                    Logger.Warn($"GetAutoStackSlot(): Missing entity found while iterating inventory. Id={kvp.Value.EntityId}");
                    continue;
                }

                if (entity.CanStackOnto(existingEntity, isAdding))
                    return kvp.Key;
            }

            return InvalidSlot;
        }

        public InventoryResult PassesContainmentFilter(PrototypeId entityProtoRef)
        {
            if (Prototype == null) return Logger.WarnReturn(InventoryResult.Invalid, "PassesContainmentFilter(): Prototype == null");
            
            var entityProto = entityProtoRef.As<EntityPrototype>();
            if (entityProto == null) return Logger.WarnReturn(InventoryResult.Invalid, "PassesContainmentFilter(): entityProto == null");

            if (Prototype.AllowEntity(entityProto) == false)
                return InventoryResult.InvalidDestInvContainmentFilters;

            return InventoryResult.Success;
        }

        public InventoryResult PassesEquipmentRestrictions(Entity entity, ref PropertyEnum propertyRestriction)
        {
            InventoryResult result = InventoryResult.Success;
            if (IsEquipment == false) return result;

            Entity inventoryOwner = Game.EntityManager.GetEntity<Entity>(OwnerId);
            if (inventoryOwner == null) return Logger.WarnReturn(InventoryResult.Invalid, "PassesEquipmentRestrictions(): inventoryOwner == null");

            Agent inventoryAgentOwner = inventoryOwner.GetSelfOrOwnerOfType<Agent>();
            if (inventoryAgentOwner == null) return Logger.WarnReturn(InventoryResult.Invalid, "PassesEquipmentRestrictions(): Found an equipment inventory belonging to a non-agent");

            Item item = entity as Item;
            if (item == null) return Logger.WarnReturn(InventoryResult.InvalidNotAnItem, "PassesEquipmentRestrictions(): item == null");

            result = inventoryAgentOwner.CanEquip(item, ref propertyRestriction);
            if (result == InventoryResult.Success)
            {
                Avatar inventoryAvatarOwner = inventoryOwner.GetSelfOrOwnerOfType<Avatar>();
                if (inventoryAvatarOwner != null)
                    result = inventoryAvatarOwner.GetEquipmentInventoryAvailableStatus(PrototypeDataRef);
            }

            return result;
        }

        public static InventoryResult ChangeEntityInventoryLocation(Entity entity, Inventory destInventory, uint destSlot, ref ulong? stackEntityId, bool allowStacking)
        {
            InventoryLocation invLoc = entity.InventoryLocation;

            if (destInventory != null)
            {
                // If we have a valid destination, it means we are either adding this entity for the first time,
                // or it is already present in the destination inventory, and we are moving it to another slot.
                
                if (invLoc.IsValid == false)
                    return destInventory.AddEntity(entity, ref stackEntityId, allowStacking, destSlot, InventoryLocation.Invalid);

                Inventory prevInventory = entity.GetOwnerInventory();

                if (prevInventory == null)
                    return Logger.WarnReturn(InventoryResult.NotInInventory,
                        $"ChangeEntityInventoryLocation(): Unable to get owner inventory for move with entity {entity} at invLoc {invLoc}");

                return prevInventory.MoveEntityTo(entity, destInventory, ref stackEntityId, allowStacking, destSlot);
            }
            else
            {
                // If no valid destination is specified, it means we are removing an entity from the inventory it is currently in

                if (invLoc.IsValid == false)
                    return Logger.WarnReturn(InventoryResult.NotInInventory,
                        $"ChangeEntityInventoryLocation(): Trying to remove entity {entity} from inventory, but it is not in any inventory");

                Inventory inventory = entity.GetOwnerInventory();

                if (inventory == null)
                    return Logger.WarnReturn(InventoryResult.NotInInventory,
                        $"ChangeEntityInventoryLocation(): Unable to get owner inventory for remove with entity {entity} at invLoc {invLoc}");

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

            return inventoryProto.IsPlayerStashInventory;
        }

        private InventoryResult AddEntity(Entity entity, ref ulong? stackEntityId, bool allowStacking, uint destSlot, InventoryLocation prevInvLoc)
        {
            // NOTE: The entity is actually added at the very end in DoAddEntity(). Everything before it is validation.

            if (entity == null) return Logger.WarnReturn(InventoryResult.InvalidSourceEntity, "AddEntity(): entity == null");
            if (entity.IsRootOwner == false) return Logger.WarnReturn(InventoryResult.NotRootOwner, "AddEntity(): entity.IsRootOwner == false");

            // Look for a free slot of no slot if specified
            if (destSlot == InvalidSlot)
                destSlot = GetFreeSlot(entity, allowStacking, true);

            // If we still don't have a slot, it means we have nowhere to put our item
            if (destSlot == InvalidSlot)
                return InventoryResult.InventoryFull;

            ulong existingEntityId = GetEntityInSlot(destSlot);
            if (existingEntityId != Entity.InvalidId)
            {
                var existingEntity = Game.EntityManager.GetEntity<Entity>(existingEntityId);
                if (existingEntity == null) return Logger.WarnReturn(InventoryResult.InvalidExistingEntityAtDest, "AddEntity(): existingEntity == null");

                if (allowStacking && entity.CanStackOnto(existingEntity))
                    return DoStacking(entity, existingEntity, ref stackEntityId);
            }

            InventoryResult result = CheckAddEntity(entity, destSlot);
            if (result != InventoryResult.Success) return result;

            return DoAddEntity(entity, destSlot, prevInvLoc);
        }

        private InventoryResult CheckAddEntity(Entity entity, uint destSlot)
        {
            if (entity == null) return Logger.WarnReturn(InventoryResult.InvalidSourceEntity, "CheckAddEntity(): entity == null");
            if (destSlot == InvalidSlot) return Logger.WarnReturn(InventoryResult.InvalidSlotParam, "CheckAddEntity(): destSlot == InvalidSlot");

            InventoryResult canChangeInvResult = entity.CanChangeInventoryLocation(this);
            if (canChangeInvResult != InventoryResult.Success)
                return Logger.WarnReturn(canChangeInvResult, "CheckAddEntity(): canChangeInvResult != InventoryResult.Success");

            if (IsSlotFree(destSlot) == false) return InventoryResult.SlotAlreadyOccupied;

            return InventoryResult.Success;
        }

        private InventoryResult DoAddEntity(Entity entity, uint destSlot, InventoryLocation prevInvLoc)
        {
            if (entity == null) return Logger.WarnReturn(InventoryResult.InvalidSourceEntity, "DoAddEntity(): entity == null");

            Entity inventoryOwner = Game.EntityManager.GetEntity<Entity>(OwnerId);
            if (inventoryOwner == null) return Logger.WarnReturn(InventoryResult.InventoryHasNoOwner, "DoAddEntity(): owner == null");

            if (destSlot >= GetCapacity()) return Logger.WarnReturn(InventoryResult.SlotExceedsCapacity, "DoAddEntity(): destSlot >= GetCapacity()");

            if (GetEntityInSlot(destSlot) != Entity.InvalidId) return InventoryResult.SlotAlreadyOccupied;

            InventoryLocation existingInvLoc = entity.InventoryLocation;

            if (existingInvLoc.IsValid)
                return Logger.WarnReturn(InventoryResult.SourceEntityAlreadyInAnInventory,
                    $"DoAddEntity(): Entity {entity} not expected in inventory, but is located at {existingInvLoc}");

            PreAdd(entity);

            _entities.Add(destSlot, new InvEntry(entity.Id, entity.PrototypeDataRef, null));
            entity.InventoryLocation.Set(OwnerId, PrototypeDataRef, destSlot);
            InventoryLocation invLoc = entity.InventoryLocation;

            PostAdd(entity, prevInvLoc, invLoc);
            PostFinalMove(entity, prevInvLoc, invLoc);
            inventoryOwner.OnOtherEntityAddedToMyInventory(entity, invLoc, false);

            return InventoryResult.Success;
        }

        private InventoryResult RemoveEntity(Entity entity)
        {
            return DoRemoveEntity(entity, true, false);
        }

        private InventoryResult DoRemoveEntity(Entity entity, bool finalMove, bool withinSameInventory)
        {
            if (entity == null) return Logger.WarnReturn(InventoryResult.InvalidSourceEntity, "DoRemoveEntity(): entity == null");
            if (entity.IsRootOwner) return Logger.WarnReturn(InventoryResult.IsRootOwner, "DoRemoveEntity(): entity.IsRootOwner");

            Entity inventoryOwner = Owner;
            if (inventoryOwner == null) return Logger.WarnReturn(InventoryResult.InventoryHasNoOwner, "DoRemoveEntity(): inventoryOwner == null");

            InventoryLocation invLoc = new(entity.InventoryLocation);

            if (entity.GetOwnerInventory() != this)
                return Logger.WarnReturn(InventoryResult.NotInInventory, 
                    $"DoRemoveEntity(): Entity {entity} expected to be in {this} inventory but is instead located at {invLoc} inventory location");

            uint slot = invLoc.Slot;
            if (slot == InvalidSlot)
                return Logger.WarnReturn(InventoryResult.UnknownFailure, "DoRemoveEntity(): slot == InvalidSlot");

            // NOTE: This is a bit simplified compared to the original, but should probably be fine. Original verify message below.
            // Entity not in expected slot.  Slot contains entityId %lld, and entity %s is instead at invLoc %s
            if (_entities.ContainsKey(slot) == false)
                return Logger.WarnReturn(InventoryResult.NotFoundInThisInventory, "DoRemoveEntity(): Entity not in expected slot");

            PreRemove(entity);

            _entities.Remove(slot);
            InventoryLocation prevInvLoc = new(entity.InventoryLocation);
            entity.InventoryLocation.Clear();

            PostRemove(entity, prevInvLoc, withinSameInventory);

            if (finalMove)
                PostFinalMove(entity, prevInvLoc, entity.InventoryLocation);

            inventoryOwner.OnOtherEntityRemovedFromMyInventory(entity, invLoc);

            return InventoryResult.Success;
        }

        private InventoryResult MoveEntityTo(Entity entity, Inventory destInventory, ref ulong? stackEntityId, bool allowStacking, uint destSlot)
        {
            // NOTE: This is probably the most complicated and potentially error-prone inventory operation, so there is a lot of validation here

            if (entity == null) return Logger.WarnReturn(InventoryResult.InvalidSourceEntity, "MoveEntityTo(): entity == null");
            if (Owner == null) return Logger.WarnReturn(InventoryResult.InventoryHasNoOwner, "MoveEntityTo(): Owner == null");
            if (destInventory.Owner == null) return Logger.WarnReturn(InventoryResult.InventoryHasNoOwner, "MoveEntityTo(): destInventory.Owner == null");

            if (entity.GetOwnerInventory() != this) return InventoryResult.NotInInventory;

            // Same as AddEntity(), we look for a free slot if no specific slot is specified
            if (destSlot == InvalidSlot)
                destSlot = destInventory.GetFreeSlot(entity, allowStacking);

            // Bail out if no free slots
            if (destSlot == InvalidSlot)
                return InventoryResult.InventoryFull;

            // This part is a bit weird, why are we if the slot is free if we know there is no entity in it?
            ulong existingEntityAtDestId = destInventory.GetEntityInSlot(destSlot);
            if (existingEntityAtDestId == Entity.InvalidId && destInventory.IsSlotFree(destSlot) == false)
                return InventoryResult.SlotAlreadyOccupied;

            // No need to move if the entity is already where it should be
            if (entity.Id == existingEntityAtDestId)
                return InventoryResult.Success;

            // Similar to CheckAddEntity(), but without a verify (is this intended?)
            InventoryResult canChangeInvResult = entity.CanChangeInventoryLocation(destInventory);
            if (canChangeInvResult != InventoryResult.Success)
                return canChangeInvResult;

            // Handle the entity that is already present at our destination's slot (if any)
            Entity existingEntityAtDest = null;
            if (existingEntityAtDestId != Entity.InvalidId)
            {
                existingEntityAtDest = Game.EntityManager.GetEntity<Entity>(existingEntityAtDestId);
                if (existingEntityAtDest == null) return Logger.WarnReturn(InventoryResult.InvalidExistingEntityAtDest,
                    "MoveEntityTo(): existingEntityAtDest == null");

                // If possible, stack our entity with the entity at the destination
                if (allowStacking && entity.CanStackOnto(existingEntityAtDest))
                    return DoStacking(entity, existingEntityAtDest, ref stackEntityId);

                // One more CanChangeInventoryLocation check, this time to make sure that existing entity can take
                // the place of the entity we are trying to move.
                canChangeInvResult = existingEntityAtDest.CanChangeInventoryLocation(destInventory);
                if (canChangeInvResult != InventoryResult.Success)
                    return canChangeInvResult;
            }

            // Check if we are moving entity within the same inventory (same owner and prototype)
            bool withinSameInventory = Owner == destInventory.Owner && entity.InventoryLocation.InventoryRef == destInventory.PrototypeDataRef;

            // Remember previous inventory location of the entity we are moving
            InventoryLocation prevInvLoc = new(entity.InventoryLocation);

            // Start moving things around
            InventoryResult result;

            InventoryLocation existingEntityAtDestPrevInvLoc = null;
            if (existingEntityAtDest != null)
            {
                // Remember previous inventory location of the entity that is already present at our destination's slot
                existingEntityAtDestPrevInvLoc = new(existingEntityAtDest.InventoryLocation);

                // Remove it
                result = destInventory.DoRemoveEntity(existingEntityAtDest, false, withinSameInventory);
                if (result != InventoryResult.Success) return Logger.WarnReturn(result, "MoveEntityTo(): Failed to remove existing entity at destination");
            }

            // Remove the entity we are moving from its place
            result = DoRemoveEntity(entity, false, withinSameInventory);
            if (result != InventoryResult.Success) return Logger.WarnReturn(result, "MoveEntityTo(): Failed to remove entity from its original location");

            // Add the entity we are moving to its destination
            result = destInventory.DoAddEntity(entity, destSlot, prevInvLoc);
            if (result != InventoryResult.Success) return Logger.WarnReturn(result, "MoveEntityTo(): Failed to add entity to its destination");

            // Add the entity that was present at our destination's slot to where the entity we moved used to be
            if (existingEntityAtDest != null)
            {
                result = DoAddEntity(existingEntityAtDest, prevInvLoc.Slot, existingEntityAtDestPrevInvLoc);
                if (result != InventoryResult.Success) return Logger.WarnReturn(result, "MoveEntityTo(): Failed to add existing entity to the location of the original entity");
            }

            return InventoryResult.Success;
        }

        private InventoryResult DoStacking(Entity entityToStack, Entity entityToStackWith, ref ulong? stackEntityId)
        {
            if (entityToStack == null) return Logger.WarnReturn(InventoryResult.InvalidSourceEntity, "DoStacking(): entityToStack == null");
            if (entityToStackWith == null) return Logger.WarnReturn(InventoryResult.InvalidStackEntity, "DoStacking(): entityToStackWith == null");
            if (entityToStack.CanStackOnto(entityToStackWith) == false) return Logger.WarnReturn(InventoryResult.StackTypeMismatch,
                "DoStacking(): entityToStack.CanStackOnto(entityToStackWith) == false");

            int stackSize = entityToStackWith.CurrentStackSize + entityToStack.CurrentStackSize;
            int stackSizeMax = entityToStackWith.Properties[PropertyEnum.InventoryStackSizeMax];
            int remaining = stackSize - stackSizeMax;   // If > 0, it means entityToStack can't fit into entityToStackWith completely

            if (remaining > 0 && entityToStack.InventoryLocation.IsValid == false)
                return Logger.WarnReturn(InventoryResult.StackCombinePartial,
                    "DoStacking(): remaining > 0 && entityToStack.InventoryLocation.IsValid == false");

            // Prevent the target stack from overflowing
            stackSize = Math.Min(stackSize, stackSizeMax);

            // No idea what this is for
            if (entityToStack.MissionPrototype != entityToStackWith.MissionPrototype)
            {
                entityToStack.Properties.RemoveProperty(PropertyEnum.MissionPrototype);
                entityToStackWith.Properties.RemoveProperty(PropertyEnum.MissionPrototype);
            }

            // Update stack sizes
            entityToStackWith.Properties[PropertyEnum.InventoryStackCount] = stackSize;

            if (remaining > 0)
                entityToStack.Properties[PropertyEnum.InventoryStackCount] = remaining;
            else
                entityToStack.Destroy();    // Destroy empty stacks

            if (stackEntityId != null) stackEntityId = entityToStackWith.Id;

            return InventoryResult.Success;
        }

        private void PreAdd(Entity entity)
        {

        }

        private void PostAdd(Entity entity, InventoryLocation prevInvLoc, InventoryLocation invLoc)
        {

        }

        private void PreRemove(Entity entity)
        {

        }

        private void PostRemove(Entity entity, InventoryLocation prevInvLoc, bool withinSameInventory)
        {

        }

        private void PostFinalMove(Entity entity, InventoryLocation prevInvLoc, InventoryLocation invLoc)
        {

        }

        // Inventory::Iterator
        public IEnumerator<KeyValuePair<uint, InvEntry>> GetEnumerator() => _entities.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public readonly struct InvEntry : IComparable<InvEntry>
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
