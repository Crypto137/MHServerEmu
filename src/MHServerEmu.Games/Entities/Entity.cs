using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities
{
    #region Enums

    [Flags]
    public enum EntityFlags : ulong
    {
        Dormant                         = 1ul << 0,
        IsDead                          = 1ul << 1,
        HasMovementPreventionStatus     = 1ul << 2,
        AIMasterAvatar                  = 1ul << 3,
        Confused                        = 1ul << 4,
        Mesmerized                      = 1ul << 5,
        MissionXEncounterHostilityOk    = 1ul << 6,
        IgnoreMissionOwnerForTargeting  = 1ul << 7,
        IsSimulated                     = 1ul << 8,
        Untargetable                    = 1ul << 9,
        Unaffectable                    = 1ul << 10,
        IsNeverAffectedByPowers         = 1ul << 11,
        AITargetableOverride            = 1ul << 12,
        AIControlPowerLock              = 1ul << 13,
        Knockback                       = 1ul << 14,
        Knockdown                       = 1ul << 15,
        Knockup                         = 1ul << 16,
        Immobilized                     = 1ul << 17,
        ImmobilizedParam                = 1ul << 18,
        ImmobilizedByHitReact           = 1ul << 19,
        SystemImmobilized               = 1ul << 20,
        Stunned                         = 1ul << 21,
        StunnedByHitReact               = 1ul << 22,
        NPCAmbientLock                  = 1ul << 23,
        PowerLock                       = 1ul << 24,
        NoCollide                       = 1ul << 25,
        HasNoCollideException           = 1ul << 26,
        Intangible                      = 1ul << 27,
        PowerUserOverrideId             = 1ul << 28,
        MissileOwnedByPlayer            = 1ul << 29,
        HasMissionPrototype             = 1ul << 30,
        Flag31                          = 1ul << 31,
        Flag32                          = 1ul << 32,
        Flag33                          = 1ul << 33,
        AttachedToEntityId              = 1ul << 34,
        IsHotspot                       = 1ul << 35,
        IsCollidableHotspot             = 1ul << 36,
        IsReflectingHotspot             = 1ul << 37,
        ImmuneToPower                   = 1ul << 38,
        ClusterPrototype                = 1ul << 39,
        EncounterResource               = 1ul << 40,
        IgnoreNavi                      = 1ul << 41,
        TutorialImmobilized             = 1ul << 42,
        TutorialInvulnerable            = 1ul << 43,
        TutorialPowerLock               = 1ul << 44,
    }

    [Flags]
    public enum EntityStatus
    {
        PendingDestroy  = 1 << 0,
        Destroyed       = 1 << 1,
        ToTransform     = 1 << 2,
        InGame          = 1 << 3,
        ExitWorld       = 1 << 10,
        // TODO etc
    }

    public enum SimulateResult
    {
        None,
        Set,
        Clear
    }

    #endregion

    public class Entity : ISerialize
    {
        public const ulong InvalidId = 0;

        private static readonly Logger Logger = LogManager.CreateLogger();

        protected EntityFlags _flags;
        private InvasiveListNodeCollection<Entity> _entityListNodes = new(3);

        public EntityBaseData BaseData { get; set; } = new();
        public ulong Id { get => BaseData.EntityId; set => BaseData.EntityId = value; }
        public ulong DatabaseUniqueId { get => BaseData.DbId; set => BaseData.DbId = value; }
        public AOINetworkPolicyValues ReplicationPolicy { get; set; }

        public ulong RegionId { get; set; } = 0;
        public Game Game { get; set; } 
        public EntityStatus Status { get; set; }

        public ReplicatedPropertyCollection Properties { get; set; } = new();

        public virtual ulong PartyId
        {
            get
            {
                var ownerPlayer = GetOwnerOfType<Player>();
                return ownerPlayer != null ? ownerPlayer.PartyId : 0;
            }
        }

        public DateTime DeathTime { get; private set; }
        public EntityPrototype Prototype { get; private set; }
        public string PrototypeName { get => GameDatabase.GetFormattedPrototypeName(BaseData.EntityPrototypeRef); }
        public PrototypeId PrototypeDataRef { get => BaseData.EntityPrototypeRef; private set => BaseData.EntityPrototypeRef = value; }

        public InventoryCollection InventoryCollection { get; } = new();
        public InventoryLocation InventoryLocation { get => BaseData.InvLoc; set => BaseData.InvLoc = value; }
        public ulong OwnerId { get => InventoryLocation.ContainerId; }

        #region Flag Properties

        public virtual bool IsDormant { get => _flags.HasFlag(EntityFlags.Dormant); }
        public bool IsDead { get => _flags.HasFlag(EntityFlags.IsDead); }
        public bool HasMovementPreventionStatus { get => _flags.HasFlag(EntityFlags.HasMovementPreventionStatus); }
        public bool IsControlledEntity { get => _flags.HasFlag(EntityFlags.AIMasterAvatar); }
        public bool IsConfused { get => _flags.HasFlag(EntityFlags.Confused); }
        public bool IsMesmerized { get => _flags.HasFlag(EntityFlags.Mesmerized); }
        public bool IsMissionCrossEncounterHostilityOk { get => _flags.HasFlag(EntityFlags.MissionXEncounterHostilityOk); }
        public bool IgnoreMissionOwnerForTargeting { get => _flags.HasFlag(EntityFlags.IgnoreMissionOwnerForTargeting); }
        public bool IsSimulated { get => _flags.HasFlag(EntityFlags.IsSimulated); }
        public bool IsUntargetable { get => _flags.HasFlag(EntityFlags.Untargetable); }
        public bool IsUnaffectable { get => _flags.HasFlag(EntityFlags.Unaffectable) || _flags.HasFlag(EntityFlags.TutorialInvulnerable); }
        public bool IsNeverAffectedByPowers { get => _flags.HasFlag(EntityFlags.IsNeverAffectedByPowers); }
        public bool HasAITargetableOverride { get => _flags.HasFlag(EntityFlags.AITargetableOverride); }
        public bool HasAIControlPowerLock { get => _flags.HasFlag(EntityFlags.AIControlPowerLock); }
        public bool IsInKnockback { get => _flags.HasFlag(EntityFlags.Knockback); }
        public bool IsInKnockdown { get => _flags.HasFlag(EntityFlags.Knockdown); }
        public bool IsInKnockup { get => _flags.HasFlag(EntityFlags.Knockup); }
        public bool IsImmobilized { get => _flags.HasFlag(EntityFlags.Immobilized) || _flags.HasFlag(EntityFlags.ImmobilizedParam); }
        public bool IsImmobilizedByHitReact { get => _flags.HasFlag(EntityFlags.ImmobilizedByHitReact); }
        public bool IsSystemImmobilized { get => _flags.HasFlag(EntityFlags.SystemImmobilized) || _flags.HasFlag(EntityFlags.TutorialImmobilized); }
        public bool IsStunned { get => _flags.HasFlag(EntityFlags.Stunned) || _flags.HasFlag(EntityFlags.StunnedByHitReact); }
        public bool NPCAmbientLock { get => _flags.HasFlag(EntityFlags.NPCAmbientLock); }
        public bool IsInPowerLock { get => _flags.HasFlag(EntityFlags.PowerLock); }
        public bool NoCollide { get => _flags.HasFlag(EntityFlags.NoCollide); }
        public bool HasNoCollideException { get => _flags.HasFlag(EntityFlags.HasNoCollideException); }
        public bool IsIntangible { get => _flags.HasFlag(EntityFlags.Intangible); }
        public bool HasPowerUserOverride { get => _flags.HasFlag(EntityFlags.PowerUserOverrideId); }
        public bool IsMissilePlayerOwned { get => _flags.HasFlag(EntityFlags.MissileOwnedByPlayer); }
        public bool HasMissionPrototype { get => _flags.HasFlag(EntityFlags.HasMissionPrototype); }
        public bool IsAttachedToEntity { get => _flags.HasFlag(EntityFlags.AttachedToEntityId); }
        public bool IsHotspot { get => _flags.HasFlag(EntityFlags.IsHotspot); }
        public bool IsCollidableHotspot { get => _flags.HasFlag(EntityFlags.IsCollidableHotspot); }
        public bool IsReflectingHotspot { get => _flags.HasFlag(EntityFlags.IsReflectingHotspot); }
        public bool HasPowerImmunity { get => _flags.HasFlag(EntityFlags.ImmuneToPower); }
        public bool HasClusterPrototype { get => _flags.HasFlag(EntityFlags.ClusterPrototype); }
        public bool HasEncounterResourcePrototype { get => _flags.HasFlag(EntityFlags.EncounterResource); }
        public bool IgnoreNavi { get => _flags.HasFlag(EntityFlags.IgnoreNavi); }
        public bool IsInTutorialPowerLock { get => _flags.HasFlag(EntityFlags.TutorialPowerLock); }

        #endregion

        #region Property Properties (lol)

        public int CharacterLevel { get => Properties[PropertyEnum.CharacterLevel]; set => Properties[PropertyEnum.CharacterLevel] = value; }
        public int CombatLevel { get => Properties[PropertyEnum.CombatLevel]; set => Properties[PropertyEnum.CombatLevel] = value; }

        public ulong PowerUserOverrideId { get => HasPowerUserOverride ? Properties[PropertyEnum.PowerUserOverrideID] : 0; }
        public PrototypeId ClusterPrototype { get => HasClusterPrototype ? Properties[PropertyEnum.ClusterPrototype] : PrototypeId.Invalid; }
        public PrototypeId EncounterResourcePrototype { get => HasEncounterResourcePrototype ? Properties[PropertyEnum.EncounterResource] : PrototypeId.Invalid; }
        public PrototypeId MissionPrototype { get => HasMissionPrototype ? Properties[PropertyEnum.MissionPrototype] : PrototypeId.Invalid; }

        public PrototypeId State { get => Properties[PropertyEnum.EntityState]; }

        public int CurrentStackSize { get => Properties[PropertyEnum.InventoryStackCount]; }
        public int MaxStackSize { get => Properties[PropertyEnum.InventoryStackSizeMax]; }
        public bool IsRootOwner { get => OwnerId == 0; }
        public bool IsInGame { get => TestStatus(EntityStatus.InGame); }

        #endregion

        public Entity(Game game)
        {
            Game = game;
        }

        public virtual void PreInitialize(EntitySettings settings) { }

        public virtual bool Initialize(EntitySettings settings)
        {   
            // Old
            var entityProto = GameDatabase.GetPrototype<EntityPrototype>(settings.EntityRef);
            bool overrideSnapToFloor = false;
            if (entityProto is WorldEntityPrototype worldEntityProto)
            {
                bool snapToFloor = settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.HasOverrideSnapToFloor)
                    ? settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.OverrideSnapToFloorValue)
                    : worldEntityProto.SnapToFloorOnSpawn;

                overrideSnapToFloor = snapToFloor != worldEntityProto.SnapToFloorOnSpawn;
            }

            // ---

            if (Game == null) return Logger.WarnReturn(false, "Initialize(): Game == null");

            Id = settings.Id;
            if (Id == InvalidId) return Logger.WarnReturn(false, "Initialize(): Id == Entity.InvalidId");

            DatabaseUniqueId = settings.DbGuid;

            PrototypeDataRef = settings.EntityRef;
            if (PrototypeDataRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "Initialize(): Invalid PrototypeDataRef");

            Prototype = PrototypeDataRef.As<EntityPrototype>();
            if (Prototype == null) return Logger.WarnReturn(false, "Initialize(): Prototype == null");

            // Is this correct? Should the flag NOT be set?
            if (settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.EnterGameWorld) == false)
            {
                BaseData.Position = settings.Position;
                BaseData.Orientation = settings.Orientation;

                if (settings.Position != null && settings.Orientation != null)
                    BaseData.FieldFlags |= EntityCreateMessageFlags.HasPositionAndOrientation;

                if (overrideSnapToFloor) BaseData.FieldFlags |= EntityCreateMessageFlags.OverrideSnapToFloorOnSpawn;
            }

            RegionId = settings.RegionId;

            // New
            Properties = new(Game.CurrentRepId);
            
            if (entityProto.Properties != null) // TODO: Filter properties during serialization
                Properties.FlattenCopyFrom(entityProto.Properties, true); 
            
            if (settings.Properties != null)
                Properties.FlattenCopyFrom(settings.Properties, false);
            
            OnPropertyChange(); // Temporary solution for for _flags

            InventoryCollection.Initialize(this);
            InitInventories(settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.PopulateInventories));

            return true;
        }

        public virtual void OnPostInit(EntitySettings settings)
        {
            // TODO init
        }

        public virtual bool Serialize(Archive archive)
        {
            PropertyCollection defaultCollection = null;    // TODO: Get the default collection from the prototype
            return Properties.SerializeWithDefault(archive, defaultCollection);
        }

        public NetMessageEntityCreate ToNetMessageEntityCreate()
        {
            ByteString archiveData;
            using (Archive archive = new Archive(ArchiveSerializeType.Replication, (ulong)ReplicationPolicy))
            {
                Serialize(archive);
                archiveData = archive.ToByteString();
            }

            return NetMessageEntityCreate.CreateBuilder()
                .SetBaseData(BaseData.ToByteString())
                .SetArchiveData(archiveData)
                .Build();
        }

        protected virtual void BuildString(StringBuilder sb)
        {
            sb.AppendLine($"{nameof(ReplicationPolicy)}: {ReplicationPolicy}");
            sb.AppendLine($"{nameof(Properties)}: {Properties}");
        }

        public override string ToString()
        {
            return $"{nameof(Id)}={Id}, {nameof(Prototype)}={Prototype}";
        }

        public string ToStringVerbose()
        {
            StringBuilder sb = new();
            BuildString(sb);
            return sb.ToString();
        }

        public virtual void EnterGame(EntitySettings settings)
        {
            if (IsInGame == false) SetStatus(EntityStatus.InGame, true);
            // TODO InventoryIterator
        }

        public virtual void ExitGame()
        {
            SetStatus(EntityStatus.InGame, false);
            // TODO InventoryIterator
        }

        // NOTE: TestStatus and SetStatus can be potentially replaced with an indexer property

        public bool TestStatus(EntityStatus status)
        {
            return Status.HasFlag(status);
        }

        public void SetStatus(EntityStatus status, bool value)
        {
            if (value) Status |= status;
            else Status &= ~status;
        }

        public virtual SimulateResult SetSimulated(bool simulated)
        {
            if (IsSimulated != simulated)
            {
                if (simulated == false || (this is WorldEntity worldEntity && worldEntity.IsInWorld))
                    Logger.Debug($"SetSimulated(): An entity must be in the world to be simulated {ToString()}");
                ModifyCollectionMembership(EntityCollection.Simulated, simulated);
                return simulated ? SimulateResult.Set : SimulateResult.Clear;
            }
            return SimulateResult.None;
        }

        public virtual void Destroy()
        {
            //CancelScheduledLifespanExpireEvent();
            //CancelDestroyEvent();
            Game?.EntityManager?.DestroyEntity(this);
        }

        public bool DestroyContained()
        {
            if (Game == null) return Logger.WarnReturn(false, "DestroyContained(): Game == null");

            foreach (Inventory inventory in InventoryCollection)
                inventory.DestroyContained();

            return true;
        }

        public bool IsDestroyed()
        {
            return Status.HasFlag(EntityStatus.Destroyed);
        }

        public bool IsAlive()
        {
            if (IsDead == false) return true;

            // Respawn entity if needed
            if (DateTime.Now.Subtract(DeathTime).TotalMinutes >= 1)
            {
                _flags &= ~EntityFlags.IsDead;
                Properties[PropertyEnum.Health] = Properties[PropertyEnum.HealthMaxOther];
                Properties[PropertyEnum.IsDead] = false;
                return true;
            }

            return false;
        }

        // Test death for respawning
        public void Kill()
        {
            _flags |= EntityFlags.IsDead;
            DeathTime = DateTime.Now;
        }

        public bool IsAPrototype(PrototypeId protoRef)
        {
            return GameDatabase.DataDirectory.PrototypeIsAPrototype(PrototypeDataRef, protoRef);
        }

        #region Event Handlers

        public virtual void OnPropertyChange()
        {
            if (Properties.HasProperty(PropertyEnum.ClusterPrototype)) _flags |= EntityFlags.ClusterPrototype;
            if (Properties.HasProperty(PropertyEnum.EncounterResource)) _flags |= EntityFlags.EncounterResource;
            if (Properties.HasProperty(PropertyEnum.MissionPrototype)) _flags |= EntityFlags.HasMissionPrototype;
        }

        public void OnOtherEntityAddedToMyInventory(Entity entity, InventoryLocation invLoc, bool unpackedArchivedEntity)
        {
        }

        public void OnOtherEntityRemovedFromMyInventory(Entity entity, InventoryLocation invLoc)
        {
        }

        public void OnDetachedFromDestroyedContainer()
        {
        }

        public virtual void OnDeallocate()
        {
        }

        #endregion

        #region Inventory Management

        public RegionLocation GetOwnerLocation()
        {
            Entity owner = GetOwner();
            while (owner != null)
            {
                if (owner is WorldEntity worldEntity)
                {
                    if (worldEntity.IsInWorld)
                        return worldEntity.RegionLocation;
                }
                else
                {
                    if (owner is Player player)
                    {
                        Avatar avatar = player.CurrentAvatar;
                        if (avatar != null && avatar.IsInWorld)
                            return avatar.RegionLocation;
                    }
                }

                owner = owner.GetOwner();
            }

            return null;
        }

        public Entity GetOwner()
        {
            return Game.EntityManager.GetEntity<Entity>(OwnerId);
        }

        public T GetOwnerOfType<T>() where T : Entity
        {
            Entity owner = GetOwner();
            while (owner != null)
            {
                if (owner is T currentCast)
                    return currentCast;
                owner = owner.GetOwner();
            }
            return null;
        }

        public T GetSelfOrOwnerOfType<T>() where T : Entity
        {
            if (this is T typedOwner) return typedOwner;
            return GetOwnerOfType<T>();
        }

        public Entity GetRootOwner()
        {
            Entity owner = this;
            while (owner != null)
            {
                if (owner.IsRootOwner) return owner;
                owner = owner.GetOwner();
            }
            return this;
        }

        public bool CanBePlayerOwned()
        {
            var prototype = Prototype;
            if (prototype is AvatarPrototype) return true;
            if (prototype is AgentTeamUpPrototype) return true;
            if (prototype is MissilePrototype) return IsMissilePlayerOwned;

            ulong ownerId = PowerUserOverrideId;
            if (ownerId != 0)
            {
                Game game = Game;
                if (game == null) return false;
                Agent owner = game.EntityManager.GetEntity<Agent>(ownerId);
                if (owner != null)
                    if (owner.IsControlledEntity || owner is Avatar || owner.IsTeamUpAgent) return true;
            }

            return false;
        }

        public Inventory GetInventory(InventoryConvenienceLabel convenienceLabel)
        {
            foreach (Inventory inventory in InventoryCollection)
            {
                if (inventory.ConvenienceLabel == convenienceLabel)
                    return inventory;
            }

            return null;
        }

        public Inventory GetInventoryByRef(PrototypeId invProtoRef)
        {
            return InventoryCollection.GetInventoryByRef(invProtoRef);
        }

        public Inventory GetOwnerInventory()
        {
            Entity container = Game.EntityManager.GetEntity<Entity>(InventoryLocation.ContainerId);
            if (container == null) return null;
            return container.GetInventoryByRef(InventoryLocation.InventoryRef);
        }

        public InventoryResult CanChangeInventoryLocation(Inventory destInventory)
        {
            PropertyEnum propertyEnum = PropertyEnum.Invalid;
            return CanChangeInventoryLocation(destInventory, ref propertyEnum);
        }

        public InventoryResult CanChangeInventoryLocation(Inventory destInventory, ref PropertyEnum propertyRestriction)
        {
            InventoryResult result = destInventory.PassesContainmentFilter(PrototypeDataRef);
            if (result != InventoryResult.Success) return result;

            return destInventory.PassesEquipmentRestrictions(this, ref propertyRestriction);
        }

        public InventoryResult ChangeInventoryLocation(Inventory destination, uint destSlot = Inventory.InvalidSlot)
        {
            ulong? stackEntityId = null;
            return ChangeInventoryLocation(destination, destSlot, ref stackEntityId, true);
        }

        public InventoryResult ChangeInventoryLocation(Inventory destInventory, uint destSlot, ref ulong? stackEntityId, bool allowStacking)
        {
            allowStacking &= IsInGame;

            // If we have a valid destination, it means we are adding or moving, so we need to verify that this entity matches the destination inventory
            if (destInventory != null)
            {
                InventoryResult destInventoryResult = CanChangeInventoryLocation(destInventory);
                if (destInventoryResult != InventoryResult.Success) return Logger.WarnReturn(destInventoryResult,
                    $"ChangeInventoryLocation(): result=[{destInventoryResult}] allowStacking=[{allowStacking}] destSlot=[{destSlot}] destInventory=[{destInventory}] entity=[{Id}]");
            }

            return Inventory.ChangeEntityInventoryLocation(this, destInventory, destSlot, ref stackEntityId, allowStacking);
        }

        public bool ValidateInventorySlot(Inventory inventory, uint slot)
        {
            // this literally does nothing
            return true;
        }

        public bool CanStack()
        {
            if (MaxStackSize < 2) return false;
            if (CurrentStackSize > MaxStackSize) Logger.WarnReturn(false, "CanStack(): CurrentStackSize > MaxStackSize");
            if (CurrentStackSize == MaxStackSize) return false;
            return true;
        }

        public virtual bool IsAutoStackedWhenAddedToInventory()
        {
            return CanStack();
        }

        public bool CanStackOnto(Entity other, bool isAdding = false)
        {
            if (CanStack() == false || other.CanStack() == false) return false;
            if (PrototypeDataRef != other.PrototypeDataRef) return false;
            if (isAdding && CurrentStackSize + other.CurrentStackSize > other.MaxStackSize) return false;
            return true;
        }

        protected virtual bool InitInventories(bool populateInventories)
        {
            bool success = true;

            EntityPrototype entityPrototype = Prototype;
            if (entityPrototype == null) return Logger.WarnReturn(false, "InitInventories(): entityPrototype == null");

            foreach (EntityInventoryAssignmentPrototype invAssignmentProto in entityPrototype.Inventories)
            {
                if (AddInventory(invAssignmentProto.Inventory, invAssignmentProto.LootTable) == false)
                {
                    Logger.Warn($"InitInventories(): Failed to add inventory, invProtoRef={GameDatabase.GetPrototypeName(invAssignmentProto.Inventory)}");
                    success = false;
                }
            }

            return success;
        }

        protected bool AddInventory(PrototypeId invProtoRef, PrototypeId lootTableRef = PrototypeId.Invalid)
        {
            // lootTableRef seems to be unused
            return InventoryCollection.CreateAndAddInventory(invProtoRef);
        }

        #endregion

        #region Invasive List Implementation

        public bool ModifyCollectionMembership(EntityCollection collection, bool add)
        {
            if (collection == EntityCollection.All) return true;
            var list = GetInvasiveCollection(collection);
            if (list == null) return Logger.WarnReturn(false, "ModifyCollectionMembership(): list == null");

            bool isInCollection = IsInCollection(collection);
            if (add && isInCollection == false)
            {
                if (collection == EntityCollection.Simulated || collection == EntityCollection.Locomotion)
                {
                    if (TestStatus(EntityStatus.Destroyed))
                        return Logger.WarnReturn(false, $"ModifyCollectionMembership(): Trying to add destroyed entity {ToString()} to collection {collection}");
                     
                    if (IsInGame == false)
                        return Logger.WarnReturn(false, $"ModifyCollectionMembership(): Trying to add out of game entity {ToString()} to collection {collection}");
      
                    if (this is WorldEntity worldEntity && worldEntity.IsInWorld == false)
                        return Logger.WarnReturn(false, $"ModifyCollectionMembership(): Trying to add out of world entity {ToString()} to collection {collection}");
                }

                if (collection == EntityCollection.Simulated) _flags |= EntityFlags.IsSimulated;
                list.AddBack(this);
            }
            else if (add == false && isInCollection)
            {
                list.Remove(this);
                if (collection == EntityCollection.Simulated) _flags &= ~EntityFlags.IsSimulated;
            }

            return true;
        }

        private bool IsInCollection(EntityCollection collection)
        {
            var list = GetInvasiveCollection(collection);
            if (list != null) return list.Contains(this);
            return false;
        }

        public InvasiveList<Entity> GetInvasiveCollection(EntityCollection collection)
        {
            EntityManager entityManager = Game?.EntityManager;
            if (entityManager == null) return null;

            return collection switch
            {
                EntityCollection.Simulated => entityManager.SimulatedEntities,
                EntityCollection.Locomotion => entityManager.LocomotionEntities,
                EntityCollection.All => entityManager.AllEntities,
                _ => null,
            };
        }

        public InvasiveListNode<Entity> GetInvasiveListNode(int listId)
        {
            return _entityListNodes.GetInvasiveListNode(listId);
        }

        internal TimeSpan GetRemainingLifespan()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
