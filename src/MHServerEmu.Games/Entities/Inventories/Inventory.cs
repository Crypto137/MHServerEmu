using System.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities.Inventories
{
    public class Inventory
    {
        public const uint InvalidSlot = uint.MaxValue;      // 0xFFFFFFFF / -1

        // The client uses a set of std::pair here, the main requirement is that this has to be sorted by key (slot)
        private readonly SortedDictionary<uint, InvEntry> _entities = new();

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
        public int CapacityRemaining { get => GetCapacity() - Count; }

        public bool VisibleToOwner { get; set; }    // For AOI

        public Inventory(Game game)
        {
            Game = game;
        }

        public override string ToString()
        {
            return PrototypeDataRef.GetName();
        }

        public bool Initialize(PrototypeId prototypeRef, ulong ownerId)
        {
            InventoryPrototype inventoryPrototype = prototypeRef.As<InventoryPrototype>();
            if (!Verify.IsNotNull(inventoryPrototype)) return false;

            Prototype = inventoryPrototype;
            OwnerId = ownerId;
            Category = inventoryPrototype.Category;
            ConvenienceLabel = inventoryPrototype.ConvenienceLabel;
            MaxCapacity = inventoryPrototype.CapacityUnlimited ? int.MaxValue : inventoryPrototype.Capacity;

            if (!Verify.IsTrue(ownerId != Entity.InvalidId)) return false;
            return true;
        }

        public ulong GetEntityInSlot(uint slot)
        {
            foreach (var entry in this)
            {
                if (entry.Slot == slot)
                    return entry.Id;
            }

            return Entity.InvalidId;
        }

        public ulong GetAnyEntity()
        {
            if (_entities.Count == 0)
                return 0;

            SortedDictionary<uint, InvEntry>.Enumerator enumerator = _entities.GetEnumerator();
            if (enumerator.MoveNext() == false)
                return 0;

            return enumerator.Current.Value.EntityId;
        }

        public Entity GetMatchingEntity(PrototypeId entityRef)
        {
            if (!Verify.IsTrue(entityRef != PrototypeId.Invalid)) return null;

            foreach (var entry in this)
            {
                if (entry.ProtoRef == entityRef)
                    return Game.EntityManager.GetEntity<Entity>(entry.Id);
            }

            return null;
        }

        public int GetMatchingEntities(PrototypeId entityRef, List<ulong> matchList = null)
        {
            if (!Verify.IsTrue(entityRef != PrototypeId.Invalid)) return 0;

            int numMatches = 0;

            EntityManager entityManager = Game.EntityManager;

            foreach (var entry in this)
            {
                if (entry.ProtoRef == entityRef)
                {
                    Entity entity = entityManager.GetEntity<Entity>(entry.Id);
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
            if (!Verify.IsTrue(entityRef != PrototypeId.Invalid)) return false;

            foreach (var entry in this)
            {
                if (entry.ProtoRef == entityRef)
                    return true;
            }

            return false;
        }

        public bool DestroyContained()
        {
            Game game = Game;
            if (!Verify.IsNotNull(game)) return false;

            // NOTE: We store contained entity ids in a list to be able to remove entries while we iterate.
            // The original implementation uses a custom iterator here that restarts after every removed item.
            using var containedIdsHandle = ListPool<ulong>.Instance.Get(out List<ulong> containedIds);
            foreach (InvEntry entry in _entities.Values)
                containedIds.Add(entry.EntityId);

            EntityManager entityManager = game.EntityManager;

            foreach (ulong containedId in containedIds)
            {
                Entity contained = entityManager.GetEntity<Entity>(containedId);
                if (!Verify.IsNotNull(contained))
                    continue;

                // Skipping entityManager verify from the client here because it doesn't make sense.
                bool isDestroyingAllEntities = entityManager.IsDestroyingAllEntities;

                // Entities that have the DetachOnContainerDestroyed are not destroyed (unless the EntityManager is currently cleaning up all entities)
                if (contained.Properties[PropertyEnum.DetachOnContainerDestroyed] && isDestroyingAllEntities == false)
                {
                    contained.ChangeInventoryLocation(null);
                    contained.OnDetachedFromDestroyedContainer();
                    continue;
                }

                contained.Destroy();
            }

            // If things are added as a result of destroying other things, we need to do this in a loop like the client.
            Verify.IsTrue(_entities.Count == 0);

            return true;
        }

        public void TriggerCleanupEvent(InventoryEvent inventoryEvent)
        {
            InventoryPrototype inventoryProto = Prototype;
            if (!Verify.IsNotNull(inventoryProto)) return;

            InventoryEvent destroyOnEvent = inventoryProto.DestroyContainedOnEvent;
            if (destroyOnEvent == InventoryEvent.Invalid || destroyOnEvent != inventoryEvent)
                return;

            TimeSpan expirationTime = TimeSpan.FromSeconds(inventoryProto.DestroyContainedAfterSecs);

            EntityManager entityManager = Game.EntityManager;
            using var entitiesToDestroyHandle = ListPool<ulong>.Instance.Get(out List<ulong> entitiesToDestroy);

            foreach (var entry in this)
            {
                Entity entity = entityManager.GetEntity<Entity>(entry.Id);
                if (!Verify.IsNotNull(entity))
                    continue;

                if (entity.Properties.HasProperty(PropertyEnum.InventoryAddTime) == false)
                    continue;

                TimeSpan currentTime = Game.Current.CurrentTime;
                TimeSpan elapsedTime = currentTime - entity.Properties[PropertyEnum.InventoryAddTime];

                if (elapsedTime < expirationTime)
                    continue;

                entitiesToDestroy.Add(entity.Id);
            }

            foreach (ulong entityId in entitiesToDestroy)
            {
                // Retrieve the entity from id again in case this game's spaghetti code somehow destroyed it in a callback.
                Entity entity = entityManager.GetEntity<Entity>(entityId);
                if (entity == null)
                    continue;

                entity.Destroy();
            }
        }

        public int GetCapacity()
        {
            InventoryPrototype inventoryPrototype = Prototype;
            if (!Verify.IsNotNull(inventoryPrototype)) return 0;

            int nSoftCap = inventoryPrototype.GetSoftCapacityDefaultSlots();
            if (nSoftCap < 0)
                return MaxCapacity;

            if (!Verify.IsNotNull(Owner)) return MaxCapacity;

            foreach (PrototypeId slotGroupRef in inventoryPrototype.GetSoftCapacitySlotGroups())
            {
                var slotGroup = slotGroupRef.As<InventoryExtraSlotsGroupPrototype>();
                int extraSlots = Owner.Properties[PropertyEnum.InventoryExtraSlotsAvailable, slotGroup.DataRef];

                if (slotGroup.MaxExtraSlotCount > 0)
                    extraSlots = Math.Min(extraSlots, slotGroup.MaxExtraSlotCount);

                nSoftCap += extraSlots;
            }

            Verify.IsTrue(nSoftCap <= MaxCapacity, $"Inventory softcap over max inventory limit. INVENTORY={this} OWNER={Owner}");
            return Math.Min(nSoftCap, MaxCapacity);
        }

        public bool IsSlotFree(uint slot)
        {
            if (!Verify.IsTrue(slot != InvalidSlot, $"IsSlotFree() called with the invalid slot id for inventory:\n[{this}]"))
                return false;

            Game game = Game;
            if (!Verify.IsNotNull(game)) return false;

            if (Count >= GetCapacity())
                return false;

            if (slot >= GetCapacity())
                return false;

            if (GetEntityInSlot(slot) != Entity.InvalidId)
                return false;

            Entity inventoryOwner = game.EntityManager.GetEntity<Entity>(OwnerId);
            if (!Verify.IsNotNull(inventoryOwner)) return false;

            if (inventoryOwner.ValidateInventorySlot(this, slot) == false)
                return false;

            return true;
        }

        public uint GetFreeSlot(Entity entity, bool allowStacking, bool isAdding = false)
        {
            Game game = Game;
            if (!Verify.IsNotNull(game)) return InvalidSlot;

            if (entity != null && allowStacking)
            {
                // NOTE: GetAutoStackSlot() is part of GetFreeSlot() in the client
                uint stackSlot = GetAutoStackSlot(entity, isAdding);
                if (stackSlot != InvalidSlot) return stackSlot;
            }

            Entity inventoryOwner = game.EntityManager.GetEntity<Entity>(OwnerId);
            if (!Verify.IsNotNull(inventoryOwner)) return InvalidSlot;

            // Make sure we actually have free slots
            if (Count >= GetCapacity())
                return InvalidSlot;

            // Look for a free slot between occupied ones
            // NOTE: This requires the slot / InvEntry collection to be sorted by slot
            uint slot = 0;
            foreach (var entry in this)
            {
                if (entry.Slot != slot && inventoryOwner.ValidateInventorySlot(this, slot))
                    return slot;

                slot++;
            }

            // If there are no free spaces in between occupied slots, get a free slot from the end
            slot = (uint)Count;
            if (inventoryOwner.ValidateInventorySlot(this, slot))
                return slot;

            return InvalidSlot;
        }

        public bool IsSlotAvailableForEntity(Entity entity, bool allowStacking)
        {
            return GetFreeSlot(entity, allowStacking, true) != InvalidSlot;
        }

        public uint GetAutoStackSlot(Entity entity, bool isAdding = false)
        {
            if (entity.CanStack() == false || entity.IsAutoStackedWhenAddedToInventory() == false)
                return InvalidSlot;

            EntityManager entityManager = Game.EntityManager;

            foreach (var entry in this)
            {
                // Stacking with itself sure sounds like a potential dupe
                if (entry.Id == entity.Id)
                    continue;

                Entity existingEntity = entityManager.GetEntity<Entity>(entry.Id);
                if (!Verify.IsNotNull(existingEntity, $"Missing entity found while iterating inventory.  Id = {entry.Id}"))
                    continue;

                if (entity.CanStackOnto(existingEntity, isAdding))
                    return entry.Slot;
            }

            return InvalidSlot;
        }

        public InventoryResult PassesContainmentFilter(PrototypeId entityProtoRef)
        {
            InventoryPrototype inventoryPrototype = Prototype;
            if (!Verify.IsNotNull(inventoryPrototype)) return InventoryResult.Invalid;
            
            EntityPrototype entityProto = entityProtoRef.As<EntityPrototype>();
            if (!Verify.IsNotNull(entityProto)) return InventoryResult.Invalid;

            if (inventoryPrototype.AllowEntity(entityProto) == false)
                return InventoryResult.InvalidDestInvContainmentFilters;

            return InventoryResult.Success;
        }

        public InventoryResult PassesEquipmentRestrictions(Entity entity, out PropertyEnum propertyRestriction)
        {
            propertyRestriction = PropertyEnum.Invalid;

            InventoryResult result = InventoryResult.Success;
            if (IsEquipment == false) return result;

            Entity inventoryOwner = Game.EntityManager.GetEntity<Entity>(OwnerId);
            if (!Verify.IsNotNull(inventoryOwner)) return InventoryResult.Invalid;

            Agent inventoryAgentOwner = inventoryOwner.GetSelfOrOwnerOfType<Agent>();
            if (!Verify.IsNotNull(inventoryAgentOwner, "Found an equipment inventory belonging to a non-agent.")) return InventoryResult.Invalid;

            Item item = entity as Item;
            if (!Verify.IsNotNull(item)) return InventoryResult.InvalidNotAnItem;

            result = inventoryAgentOwner.CanEquip(item, out propertyRestriction);
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
            ref InventoryLocation invLoc = ref entity.InventoryLocation;

            if (destInventory != null)
            {
                // If we have a valid destination, it means we are either adding this entity for the first time,
                // or it is already present in the destination inventory, and we are moving it to another slot.
                
                if (invLoc.IsValid == false)
                {
                    InventoryLocation prevInvLoc = InventoryLocation.Invalid;
                    return destInventory.AddEntity(entity, ref stackEntityId, allowStacking, destSlot, ref prevInvLoc);
                }

                Inventory prevInventory = entity.GetOwnerInventory();
                if (!Verify.IsNotNull(prevInventory, $"Unable to get owner inventory for move with entity {entity} at invLoc {invLoc}"))
                    return InventoryResult.NotInInventory;

                return prevInventory.MoveEntityTo(entity, destInventory, ref stackEntityId, allowStacking, destSlot);
            }
            else
            {
                // If no valid destination is specified, it means we are removing an entity from the inventory it is currently in

                if (!Verify.IsTrue(invLoc.IsValid, $"Trying to remove entity {entity} from inventory, but it is not in any inventory"))
                    return InventoryResult.NotInInventory;

                Inventory inventory = entity.GetOwnerInventory();
                if (!Verify.IsNotNull(inventory, $"Unable to get owner inventory for remove with entity {entity} at invLoc {invLoc}"))
                    return InventoryResult.NotInInventory;

                return inventory.RemoveEntity(entity);
            }
        }

        public static InventoryResult ChangeEntityInventoryLocationOnCreate(Entity entity, Inventory destInventory, uint destSlot, bool isPacked,
            bool allowStacking, ref InventoryLocation prevInvLoc)
        {
            if (!Verify.IsTrue(entity.InventoryLocation.IsValid == false)) return InventoryResult.SourceEntityAlreadyInAnInventory;

            if (isPacked)
                return destInventory.UnpackArchivedEntity(entity, destSlot);

            ulong? stackEntityId = null;
            return destInventory.AddEntity(entity, ref stackEntityId, allowStacking, destSlot, ref prevInvLoc);
        }

        public static bool IsPlayerStashInventory(PrototypeId inventoryRef)
        {
            if (!Verify.IsTrue(inventoryRef != PrototypeId.Invalid)) return false;

            InventoryPrototype inventoryProto = inventoryRef.As<InventoryPrototype>();
            if (!Verify.IsNotNull(inventoryProto)) return false;

            return inventoryProto.IsPlayerStashInventory;
        }

        private InventoryResult AddEntity(Entity entity, ref ulong? stackEntityId, bool allowStacking, uint destSlot, ref InventoryLocation prevInvLoc)
        {
            // NOTE: The entity is actually added at the very end in DoAddEntity(). Everything before it is validation.

            if (!Verify.IsNotNull(entity)) return InventoryResult.InvalidSourceEntity;
            if (!Verify.IsTrue(entity.IsRootOwner)) return InventoryResult.NotRootOwner;

            // Look for a free slot of no slot if specified
            if (destSlot == InvalidSlot)
                destSlot = GetFreeSlot(entity, allowStacking, true);

            // If we still don't have a slot, it means we have nowhere to put our item
            if (destSlot == InvalidSlot)
                return InventoryResult.InventoryFull;

            ulong existingEntityId = GetEntityInSlot(destSlot);
            if (existingEntityId != Entity.InvalidId)
            {
                Entity existingEntity = Game.EntityManager.GetEntity<Entity>(existingEntityId);
                if (!Verify.IsNotNull(existingEntity)) return InventoryResult.InvalidExistingEntityAtDest;

                if (allowStacking && entity.CanStackOnto(existingEntity))
                    return DoStacking(entity, existingEntity, ref stackEntityId);
            }

            InventoryResult result = CheckAddEntity(entity, destSlot);
            if (result != InventoryResult.Success) return result;

            return DoAddEntity(entity, destSlot, ref prevInvLoc);
        }

        private InventoryResult CheckAddEntity(Entity entity, uint destSlot)
        {
            if (!Verify.IsNotNull(entity)) return InventoryResult.InvalidSourceEntity;
            if (!Verify.IsTrue(destSlot != InvalidSlot)) return InventoryResult.InvalidSlotParam;

            InventoryResult canChangeInvResult = entity.CanChangeInventoryLocation(this);
            if (!Verify.IsTrue(canChangeInvResult == InventoryResult.Success)) return canChangeInvResult;

            if (IsSlotFree(destSlot) == false)
                return InventoryResult.SlotAlreadyOccupied;

            return InventoryResult.Success;
        }

        private InventoryResult DoAddEntity(Entity entity, uint slot, ref InventoryLocation prevInvLoc)
        {
            if (!Verify.IsNotNull(entity)) return InventoryResult.InvalidSourceEntity;

            Entity inventoryOwner = Game.EntityManager.GetEntity<Entity>(OwnerId);
            if (!Verify.IsNotNull(inventoryOwner)) return InventoryResult.InventoryHasNoOwner;

            if (!Verify.IsTrue(slot < GetCapacity())) return InventoryResult.SlotExceedsCapacity;

            if (GetEntityInSlot(slot) != Entity.InvalidId)
                return InventoryResult.SlotAlreadyOccupied;

            ref InventoryLocation existingInvLoc = ref entity.InventoryLocation;
            if (!Verify.IsTrue(existingInvLoc.IsValid == false, $"Entity {entity} not expected in inventory, but is located at {existingInvLoc}"))
                return InventoryResult.SourceEntityAlreadyInAnInventory;

            PreAdd(entity);

            _entities.Add(slot, new InvEntry(entity.Id, entity.PrototypeDataRef, null));
            entity.InventoryLocation.Set(OwnerId, PrototypeDataRef, slot);
            ref InventoryLocation invLoc = ref entity.InventoryLocation;

            PostAdd(entity, ref prevInvLoc, ref invLoc);
            PostFinalMove(entity, ref prevInvLoc, ref invLoc);
            inventoryOwner.OnOtherEntityAddedToMyInventory(entity, ref invLoc, false);

            return InventoryResult.Success;
        }

        private InventoryResult RemoveEntity(Entity entity)
        {
            return DoRemoveEntity(entity, true, false);
        }

        private InventoryResult DoRemoveEntity(Entity entity, bool finalMove, bool withinSameInventory)
        {
            if (!Verify.IsNotNull(entity)) return InventoryResult.InvalidSourceEntity;
            if (!Verify.IsTrue(entity.IsRootOwner == false)) return InventoryResult.IsRootOwner;

            Entity inventoryOwner = Owner;
            if (!Verify.IsNotNull(inventoryOwner)) return InventoryResult.InventoryHasNoOwner;

            InventoryLocation invLoc = entity.InventoryLocation;    // copy

            if (!Verify.IsTrue(entity.GetOwnerInventory() == this, $"Entity {entity} expected to be in {this} inventory but is instead located at {invLoc} inventory location"))
                return InventoryResult.NotInInventory;

            uint slot = invLoc.Slot;
            if (!Verify.IsTrue(slot != InvalidSlot)) return InventoryResult.UnknownFailure;

            bool hasEntry = _entities.TryGetValue(slot, out var entry) && entry.EntityId == entity.Id && entry.PrototypeDataRef == entity.PrototypeDataRef;
            if (!Verify.IsTrue(hasEntry, $"Entity not in expected slot.  Slot contains entityId {entry.EntityId}, and entity {entity} is instead at invLoc {invLoc}"))
                return InventoryResult.NotFoundInThisInventory;

            PreRemove(entity);

            _entities.Remove(slot);
            InventoryLocation prevInvLoc = entity.InventoryLocation;    // copy
            entity.InventoryLocation.Clear();

            PostRemove(entity, ref prevInvLoc, withinSameInventory);

            if (finalMove)
                PostFinalMove(entity, ref prevInvLoc, ref entity.InventoryLocation);

            inventoryOwner.OnOtherEntityRemovedFromMyInventory(entity, ref invLoc);

            return InventoryResult.Success;
        }

        private InventoryResult MoveEntityTo(Entity entity, Inventory destInventory, ref ulong? stackEntityId, bool allowStacking, uint destSlot)
        {
            // NOTE: This is probably the most complicated and potentially error-prone inventory operation, so there is a lot of validation here
            if (!Verify.IsNotNull(entity)) return InventoryResult.InvalidSourceEntity;

            Entity myOwner = Owner;
            Entity destOwner = destInventory.Owner;
            if (!Verify.IsNotNull(myOwner)) return InventoryResult.InventoryHasNoOwner;
            if (!Verify.IsNotNull(destOwner)) return InventoryResult.InventoryHasNoOwner;

            if (entity.GetOwnerInventory() != this)
                return InventoryResult.NotInInventory;

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
                if (!Verify.IsNotNull(existingEntityAtDest)) return InventoryResult.InvalidExistingEntityAtDest;

                // If possible, stack our entity with the entity at the destination
                if (allowStacking && entity.CanStackOnto(existingEntityAtDest))
                    return DoStacking(entity, existingEntityAtDest, ref stackEntityId);

                // One more CanChangeInventoryLocation check, this time to make sure that existing entity can take
                // the place of the entity we are trying to move.
                canChangeInvResult = existingEntityAtDest.CanChangeInventoryLocation(destInventory);
                if (canChangeInvResult != InventoryResult.Success)
                    return canChangeInvResult;
            }

            // Remember previous inventory location of the entity we are moving
            InventoryLocation prevInvLoc = entity.InventoryLocation;    // copy

            // Check if we are moving entity within the same inventory (same owner and prototype)
            bool withinSameInventory = myOwner == destOwner && prevInvLoc.InventoryRef == destInventory.PrototypeDataRef;

            // Start moving things around
            InventoryResult result;

            InventoryLocation existingEntityAtDestPrevInvLoc;
            if (existingEntityAtDest != null)
            {
                // Remember previous inventory location of the entity that is already present at our destination's slot
                existingEntityAtDestPrevInvLoc = existingEntityAtDest.InventoryLocation;    // copy

                // Remove it
                result = destInventory.DoRemoveEntity(existingEntityAtDest, false, withinSameInventory);
                if (!Verify.IsTrue(result == InventoryResult.Success)) return result;
            }
            else
            {
                existingEntityAtDestPrevInvLoc = InventoryLocation.Invalid;
            }

            // Remove the entity we are moving from its place
            result = DoRemoveEntity(entity, false, withinSameInventory);
            if (!Verify.IsTrue(result == InventoryResult.Success)) return result;

            // Add the entity we are moving to its destination
            result = destInventory.DoAddEntity(entity, destSlot, ref prevInvLoc);
            if (!Verify.IsTrue(result == InventoryResult.Success)) return result;

            // Add the entity that was present at our destination's slot to where the entity we moved used to be
            if (existingEntityAtDest != null)
            {
                result = DoAddEntity(existingEntityAtDest, prevInvLoc.Slot, ref existingEntityAtDestPrevInvLoc);
                if (!Verify.IsTrue(result == InventoryResult.Success)) return result;
            }

            return InventoryResult.Success;
        }

        private InventoryResult DoStacking(Entity entityToStack, Entity entityToStackWith, ref ulong? stackEntityId)
        {
            if (!Verify.IsNotNull(entityToStack)) return InventoryResult.InvalidSourceEntity;
            if (!Verify.IsNotNull(entityToStackWith)) return InventoryResult.InvalidStackEntity;
            if (!Verify.IsTrue(entityToStack.CanStackOnto(entityToStackWith))) return InventoryResult.StackTypeMismatch;

            int stackSize = entityToStackWith.CurrentStackSize + entityToStack.CurrentStackSize;
            int stackSizeMax = entityToStackWith.Properties[PropertyEnum.InventoryStackSizeMax];
            int remaining = stackSize - stackSizeMax;   // If > 0, it means entityToStack can't fit into entityToStackWith completely

            if (remaining > 0 && !Verify.IsTrue(entityToStack.InventoryLocation.IsValid))
                return InventoryResult.StackCombinePartial;

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
            if (entity is WorldEntity worldEntity && Prototype.ExitWorldOnAdd && worldEntity.IsInWorld)
                worldEntity.ExitWorld();

            if (ConvenienceLabel == InventoryConvenienceLabel.Trade)
            {
                Player player = Owner?.GetSelfOrOwnerOfType<Player>();
                player?.OnPlayerTradeInventoryChanged();
            }
        }

        private bool PostAdd(Entity entity, ref InventoryLocation prevInvLoc, ref InventoryLocation invLoc)
        {
            if (!Verify.IsNotNull(entity)) return false;

            entity.OnSelfAddedToOtherInventory();

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.InventoryLocationPrevious = prevInvLoc;

            if (prevInvLoc.InventoryConvenienceLabel == InventoryConvenienceLabel.AvatarLibrary
                && invLoc.InventoryConvenienceLabel == InventoryConvenienceLabel.AvatarInPlay)
            {
                settings.OptionFlags = EntitySettingsOptionFlags.IsClientEntityHidden;
            }

            entity.UpdateInterestPolicies(true, settings);

            // Timestamp entities in expirable inventories.
            InventoryPrototype inventoryProto = Prototype;
            if (!Verify.IsNotNull(inventoryProto)) return false;

            if (inventoryProto.DestroyContainedOnEvent != InventoryEvent.Invalid &&
                entity.Properties.HasProperty(PropertyEnum.InventoryAddTime) == false)
            {
                TimeSpan timestamp = Game.Current.CurrentTime;
                entity.Properties[PropertyEnum.InventoryAddTime] = timestamp;
            }

            return true;
        }

        private void PreRemove(Entity entity)
        {
            if (entity is WorldEntity worldEntity && Prototype.ExitWorldOnRemove && worldEntity.IsInWorld)
                worldEntity.ExitWorld();

            if (ConvenienceLabel == InventoryConvenienceLabel.Trade)
            {
                Player player = Owner?.GetSelfOrOwnerOfType<Player>();
                player?.OnPlayerTradeInventoryChanged();
            }
        }

        private bool PostRemove(Entity entity, ref InventoryLocation prevInvLoc, bool withinSameInventory)
        {
            if (!Verify.IsNotNull(entity)) return false;

            entity.OnSelfRemovedFromOtherInventory(ref prevInvLoc);
            entity.UpdateInterestPolicies(true);

            if (withinSameInventory == false)
                entity.Properties.RemoveProperty(PropertyEnum.InventoryAddTime);

            return true;
        }

        private void PostFinalMove(Entity entity, ref InventoryLocation prevInvLoc, ref InventoryLocation invLoc)
        {
            if (!Verify.IsNotNull(entity)) return;

            Game game = Game;
            if (!Verify.IsNotNull(game)) return;

            Entity oldOwner = game.EntityManager.GetEntity<Entity>(prevInvLoc.ContainerId);
            Entity newOwner = game.EntityManager.GetEntity<Entity>(invLoc.ContainerId);
            if (!Verify.IsTrue(oldOwner != null || newOwner != null)) return;

            // InventorySlotChanged event not implemented

            if (oldOwner != null && oldOwner != newOwner)
                oldOwner.EntityInventoryChangedEvent.Invoke(new(entity));

            newOwner?.EntityInventoryChangedEvent.Invoke(new(entity));
        }

        private InventoryResult UnpackArchivedEntity(Entity entity, uint destSlot)
        {
            Verify.IsTrue(false);
            return InventoryResult.Invalid;
        }

        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        public struct Enumerator : IEnumerator<Enumerator.Entry>
        {
            private readonly SortedDictionary<uint, InvEntry> _entities;
            private SortedDictionary<uint, InvEntry>.Enumerator _enumerator;

            public Entry Current { get; private set; }
            object IEnumerator.Current { get => Current; }

            public Enumerator(Inventory inventory)
            {
                _entities = inventory._entities;
                _enumerator = _entities.GetEnumerator();
            }

            public bool MoveNext()
            {
                while (_enumerator.MoveNext())
                {
                    Current = new(_enumerator.Current);
                    return true;
                }

                Current = default;
                return false;
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            public void Reset()
            {
                _enumerator.Dispose();
                _enumerator = _entities.GetEnumerator();
            }

            public readonly struct Entry
            {
                private readonly KeyValuePair<uint, InvEntry> _kvp;

                public uint Slot { get => _kvp.Key; }
                public ulong Id { get => _kvp.Value.EntityId; }
                public PrototypeId ProtoRef { get => _kvp.Value.PrototypeDataRef; }
                public InventoryMetaData MetaData { get => _kvp.Value.MetaData; }

                public Entry(KeyValuePair<uint, InvEntry> kvp)
                {
                    _kvp = kvp;
                }
            }
        }

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
