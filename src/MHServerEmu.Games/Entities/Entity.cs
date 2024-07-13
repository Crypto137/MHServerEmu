using System.Text;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Social;

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
    {                                       // Reference method
        PendingDestroy          = 1 << 0,
        Destroyed               = 1 << 1,
        ToTransform             = 1 << 2,
        InGame                  = 1 << 3,
        DisableDBOps            = 1 << 4,   // EntityManager::CreateEntity()
        Status5                 = 1 << 5,
        SkipItemBindingCheck    = 1 << 6,   // CItem::CanChangeInventoryLocation()
        HasArchiveData          = 1 << 7,
        ClientOnly              = 1 << 8,   // CEntity::ExitGame()
        EnteringWorld           = 1 << 9,   // WorldEntity::EnterWorld()
        ExitingWorld            = 1 << 10,  // WorldEntity::EnterWorld()
        DeferAdapterChanges     = 1 << 11   // CWorldEntity::OnEnteredWorld()
    }

    public enum SimulateResult
    {
        None,
        Set,
        Clear
    }

    #endregion

    public class Entity : ISerialize, IPropertyChangeWatcher
    {
        public const ulong InvalidId = 0;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly InvasiveListNodeCollection<Entity> _entityListNodes = new(3);

        private readonly EventGroup _pendingEvents = new();

        private readonly EventPointer<ScheduledLifespanEvent> _scheduledLifespanEvent = new();
        private readonly EventPointer<ScheduledDestroyEvent> _scheduledDestroyEvent = new();

        private EntityFlags _flags;

        public ulong Id { get; private set; }
        public ulong DatabaseUniqueId { get; private set; }

        public Game Game { get; set; }
        public EntityStatus Status { get; set; }
        public bool IsInGame { get => TestStatus(EntityStatus.InGame); }
        public bool IsDestroyed { get => TestStatus(EntityStatus.Destroyed); }

        public ReplicatedPropertyCollection Properties { get; set; }

        public Party Party { get; internal set; }
        public virtual ulong PartyId { get { var ownerPlayer = GetOwnerOfType<Player>(); return ownerPlayer != null ? ownerPlayer.PartyId : 0; } }

        public EntityPrototype Prototype { get; private set; }
        public PrototypeId PrototypeDataRef { get; private set; }
        public string PrototypeName { get => GameDatabase.GetFormattedPrototypeName(PrototypeDataRef); }

        public virtual AOINetworkPolicyValues CompatibleReplicationChannels { get => Prototype.RepNetwork; }
        public InterestReferences InterestReferences { get; } = new();

        public InventoryCollection InventoryCollection { get; } = new();
        public InventoryLocation InventoryLocation { get; private set; } = new();
        public ulong OwnerId { get => InventoryLocation.ContainerId; }
        public bool IsRootOwner { get => OwnerId == 0; }
        public virtual bool IsWakingUp { get => false; }
        public TimeSpan TotalLifespan { get; private set; }

        public ulong RegionId { get; set; } = 0;    // REMOVEME: non-world entities should not have a region

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

        #endregion

        public Entity(Game game)
        {
            Game = game;
        }

        protected void SetFlag(EntityFlags flag, bool value)
        {
            if (value)
                _flags |= flag;
            else
                _flags &= ~flag;
        }

        public virtual bool PreInitialize(EntitySettings settings)
        {
            return true;
        }

        public virtual bool Initialize(EntitySettings settings)
        {
            if (Game == null) return Logger.WarnReturn(false, "Initialize(): Game == null");

            Id = settings.Id;
            if (Id == InvalidId) return Logger.WarnReturn(false, "Initialize(): Id == Entity.InvalidId");

            DatabaseUniqueId = settings.DbGuid;

            // Set prototype reference
            PrototypeDataRef = settings.EntityRef;
            if (PrototypeDataRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "Initialize(): Invalid PrototypeDataRef");

            Prototype = PrototypeDataRef.As<EntityPrototype>();
            if (Prototype == null) return Logger.WarnReturn(false, "Initialize(): Prototype == null");

            // Set region - REMOVEME: non-world entities should not have a region
            RegionId = settings.RegionId;

            // Initialize property collection and copy baseline properties from prototype / settings
            // TODO: Bind to ArchiveMessageDispatcher
            Properties = new(this, Game.CurrentRepId);

            // We use IPropertyChangeWatcher implementation as a replacement for multiple inheritance
            Attach(Properties);

            if (Prototype.Properties != null)
                Properties.FlattenCopyFrom(Prototype.Properties, true);

            if (settings.Properties != null)
                Properties.FlattenCopyFrom(settings.Properties, false);

            // Inventories
            InventoryCollection.Initialize(this);
            InitInventories(settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.PopulateInventories));

            // Lifespan
            TotalLifespan = settings.Lifespan;
            InitLifespan(settings.Lifespan);

            return true;
        }

        public virtual void OnPostInit(EntitySettings settings)
        {
            if (Prototype.EvalOnCreate?.Length > 0)
            {
                EvalContextData contextData = new(Game);
                contextData.SetVar_PropertyCollectionPtr(EvalContext.Default, Properties);

                foreach (EvalPrototype evalProto in Prototype.EvalOnCreate)
                {
                    if (Eval.RunBool(evalProto, contextData) == false)
                        Logger.Warn($"OnPostInit(): Failed to run eval {evalProto.ExpressionString()}");
                }
            }
        }

        public virtual bool Serialize(Archive archive)
        {
            return Properties.SerializeWithDefault(archive, Prototype?.Properties);
        }

        public virtual bool ApplyInitialReplicationState(ref EntitySettings settings) => true;

        public void TEMP_ReplacePrototype(PrototypeId prototypeRef)
        {
            // Temp method for hacks that replace entity prototype after creation - use with caution and remove this later
            Prototype = prototypeRef.As<EntityPrototype>();
            PrototypeDataRef = prototypeRef;
        }

        protected virtual void BuildString(StringBuilder sb)
        {
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

        public virtual void EnterGame(EntitySettings settings = null)
        {
            if (IsInGame) return;

            SetStatus(EntityStatus.InGame, true);
            UpdateInterestPolicies(true);

            // Put all inventory entities into the game as well
            foreach (Inventory inventory in new InventoryIterator(this))
            {
                foreach (var entry in inventory)
                {
                    Entity containedEntity = Game.EntityManager.GetEntity<Entity>(entry.Id);
                    if (containedEntity != null) containedEntity.EnterGame();
                }
            }
        }

        public virtual void ExitGame()
        {
            SetStatus(EntityStatus.InGame, false);
            UpdateInterestPolicies(false);

            // Remove contained entities
            foreach (Inventory inventory in new InventoryIterator(this))
            {
                foreach (var entry in inventory)
                {
                    Entity containedEntity = Game.EntityManager.GetEntity<Entity>(entry.Id);
                    if (containedEntity != null) containedEntity.ExitGame();
                }
            }
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
                if (simulated == true && (this is not WorldEntity worldEntity || worldEntity.IsInWorld == false))
                    Logger.Debug($"SetSimulated(): An entity must be in the world to be simulated {ToString()}");
                ModifyCollectionMembership(EntityCollection.Simulated, simulated);
                return simulated ? SimulateResult.Set : SimulateResult.Clear;
            }
            return SimulateResult.None;
        }

        public virtual void Destroy()
        {
            CancelScheduledLifespanExpireEvent();
            CancelDestroyEvent();
            Game?.EntityManager?.DestroyEntity(this);
        }

        public bool DestroyContained()
        {
            if (Game == null) return Logger.WarnReturn(false, "DestroyContained(): Game == null");

            foreach (Inventory inventory in InventoryCollection)
                inventory.DestroyContained();

            return true;
        }

        public bool IsAPrototype(PrototypeId protoRef)
        {
            return GameDatabase.DataDirectory.PrototypeIsAPrototype(PrototypeDataRef, protoRef);
        }

        #region AOI

        public void UpdateInterestPolicies(bool updateForAllPlayers, EntitySettings settings = null)
        {
            if (updateForAllPlayers)
            {
                // Update interest policies for all players in the game (slow).
                foreach (Player player in new PlayerIterator(Game))
                    player.PlayerConnection.AOI.ConsiderEntity(this, settings);
            }
            else
            {
                // Update only players who are already interested in this entity.
                // This is what should be used to remove entities if possible.
                foreach (ulong playerId in InterestReferences.PlayerIds)
                {
                    Player player = Game.EntityManager.GetEntity<Player>(playerId);
                    player?.PlayerConnection.AOI.ConsiderEntity(this, settings);
                }
            }
        }

        #endregion

        #region Event Handlers

        public virtual void OnSelfAddedToOtherInventory()
        {
        }

        public virtual void OnSelfRemovedFromOtherInventory(InventoryLocation prevInvLoc)
        {
        }

        public virtual void OnOtherEntityAddedToMyInventory(Entity entity, InventoryLocation invLoc, bool unpackedArchivedEntity)
        {
        }

        public virtual void OnOtherEntityRemovedFromMyInventory(Entity entity, InventoryLocation invLoc)
        {
        }

        public virtual void OnDetachedFromDestroyedContainer()
        {
        }

        public virtual void OnDeallocate()
        {
            Game.GameEventScheduler.CancelAllEvents(_pendingEvents);
        }

        public virtual void OnChangePlayerAOI(Player player, InterestTrackOperation operation,
            AOINetworkPolicyValues newInterestPolicies, AOINetworkPolicyValues previousInterestPolicies,
            AOINetworkPolicyValues archiveInterestPolicies = AOINetworkPolicyValues.AOIChannelNone)
        {
            Properties.OnEntityChangePlayerAOI(player, operation, newInterestPolicies, previousInterestPolicies, archiveInterestPolicies);

            AOINetworkPolicyValues gainedPolicies = newInterestPolicies & ~previousInterestPolicies;
            AOINetworkPolicyValues lostPolicies = previousInterestPolicies & ~newInterestPolicies;
            InterestReferences.Track(this, player.Id, operation, gainedPolicies, lostPolicies);
        }

        public virtual void OnPostAOIAddOrRemove(Player player, InterestTrackOperation operation,
            AOINetworkPolicyValues newInterestPolicies, AOINetworkPolicyValues previousInterestPolicies)
        {

        }

        public virtual void OnLifespanExpired()
        {
            Destroy();
        }

        #endregion

        #region IPropertyChangeWatcher

        public void Attach(PropertyCollection propertyCollection)
        {
            if (propertyCollection != Properties)
            {
                Logger.Warn("Attach(): Entities can attach only to their own property collection");
                return;
            }

            Properties.AttachWatcher(this);
        }

        public void Detach(bool removeFromAttachedCollection)
        {
            if (removeFromAttachedCollection)
                Properties.DetachWatcher(this);
        }

        public virtual void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            if (flags.HasFlag(SetPropertyFlags.Refresh)) return;

            switch (id.Enum)
            {
                case PropertyEnum.AIMasterAvatar:
                    SetFlag(EntityFlags.AIMasterAvatar, newValue);
                    break;

                case PropertyEnum.ClusterPrototype:
                    SetFlag(EntityFlags.ClusterPrototype, newValue);
                    break;

                case PropertyEnum.EncounterResource:
                    SetFlag(EntityFlags.EncounterResource, newValue);
                    break;

                case PropertyEnum.IsDead:
                    SetFlag(EntityFlags.IsDead, newValue);
                    break;

                case PropertyEnum.MissionPrototype:
                    SetFlag(EntityFlags.HasMissionPrototype, newValue);
                    break;

                case PropertyEnum.PowerUserOverrideID:
                    SetFlag(EntityFlags.PowerUserOverrideId, newValue);
                    break;
            }
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

        /// <summary>
        /// Returns <see langword="true"/> if the specified entity id matches this <see cref="Entity"/> or one of its owners.
        /// </summary>
        public bool IsOwnedBy(ulong entityId)
        {
            // NOTE: If the provided entityId matches this entity, this check will
            // return true, even though GetOwner() would return null. In other words,
            // an entity without an owner is owned by itself. This is expected behavior,
            // because Player entities own themselves.

            Entity potentialOwner = this;

            while (potentialOwner != null)
            {
                if (potentialOwner.Id == entityId)
                    return true;

                potentialOwner = potentialOwner.GetOwner();
            }

            return false;
        }

        public bool Owns(ulong entityId)
        {
            Entity entity = Game.EntityManager.GetEntity<Entity>(entityId);
            return Owns(entity);
        }

        public bool Owns(Entity entity)
        {
            if (entity == null) return Logger.WarnReturn(false, "Owns(): entity == null");
            return entity.IsOwnedBy(Id);
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

                if (collection == EntityCollection.Simulated) SetFlag(EntityFlags.IsSimulated, true);
                list.AddBack(this);
            }
            else if (add == false && isInCollection)
            {
                list.Remove(this);
                if (collection == EntityCollection.Simulated) SetFlag(EntityFlags.IsSimulated, false);
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

        #endregion

        #region Lifespan

        public void InitLifespan(TimeSpan overrideLifespan)
        {
            if (Prototype == null) return;
            if (overrideLifespan > TimeSpan.Zero)
                ResetLifespan(overrideLifespan);
            else if (Prototype.LifespanMS > 0)
                ResetLifespan(TimeSpan.FromMilliseconds(Prototype.LifespanMS));
        }

        public void ResetLifespan(TimeSpan lifespan)
        {
            if (_scheduledLifespanEvent.IsValid)
            {
                var scheduler = Game?.GameEventScheduler;
                if (scheduler == null) return;
                scheduler.RescheduleEvent(_scheduledLifespanEvent, lifespan);
            }
            else
                ScheduleEntityEvent(_scheduledLifespanEvent, lifespan);

            TotalLifespan = lifespan;
        }

        public void CancelScheduledLifespanExpireEvent()
        {
            Game.GameEventScheduler.CancelEvent(_scheduledLifespanEvent);
        }

        public void ScaleRemainingLifespan(float scaleFactor)
        {
            if (scaleFactor < 0.0f) return;
            if (_scheduledLifespanEvent.IsValid)
            {
                Game game = Game;
                if (game == null) return;
                TimeSpan remainingLifespan = _scheduledLifespanEvent.Get().FireTime - game.CurrentTime;
                ResetLifespan(remainingLifespan * scaleFactor);
            }
        }

        public TimeSpan GetRemainingLifespan()
        {
            if (_scheduledLifespanEvent.IsValid == false) return TimeSpan.Zero;
            if (Game == null) return TimeSpan.Zero;
            return _scheduledLifespanEvent.Get().FireTime - Game.CurrentTime;
        }

        #endregion

        #region Generic Event Scheduling

        public void ScheduleEntityEvent<TEvent>(EventPointer<TEvent> eventPointer, TimeSpan timeOffset)
            where TEvent : CallMethodEvent<Entity>, new()
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.ScheduleEvent(eventPointer, timeOffset, _pendingEvents);
            eventPointer.Get().Initialize(this);
        }

        public void ScheduleEntityEvent<TEvent, TParam1>(EventPointer<TEvent> eventPointer, TimeSpan timeOffset, TParam1 param1)
            where TEvent : CallMethodEventParam1<Entity, TParam1>, new()
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.ScheduleEvent(eventPointer, timeOffset, _pendingEvents);
            eventPointer.Get().Initialize(this, param1);
        }

        public void ScheduleEntityEvent<TEvent, TParam1, TParam2>(EventPointer<TEvent> eventPointer, TimeSpan timeOffset, TParam1 param1, TParam2 param2)
            where TEvent : CallMethodEventParam2<Entity, TParam1, TParam2>, new()
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.ScheduleEvent(eventPointer, timeOffset, _pendingEvents);
            eventPointer.Get().Initialize(this, param1, param2);
        }

        public void ScheduleEntityEvent<TEvent, TParam1, TParam2, TParam3>(EventPointer<TEvent> eventPointer, TimeSpan lifespan, TParam1 param1, TParam2 param2, TParam3 param3)
            where TEvent : CallMethodEventParam3<Entity, TParam1, TParam2, TParam3>, new()
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.ScheduleEvent(eventPointer, lifespan, _pendingEvents);
            eventPointer.Get().Initialize(this, param1, param2, param3);
        }

        public void ScheduleEntityEventCustom<TEvent>(EventPointer<TEvent> eventPointer, TimeSpan timeOffset)
            where TEvent : TargetedScheduledEvent<Entity>, new()
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.ScheduleEvent(eventPointer, timeOffset, _pendingEvents);
        }

        #endregion

        #region Scheduled Events

        public virtual bool ScheduleDestroyEvent(TimeSpan delay)
        {
            if (TestStatus(EntityStatus.PendingDestroy))
                return Logger.WarnReturn(false, $"ScheduleDestroyEvent(): Entity {this} is already pending destroy");

            if (TestStatus(EntityStatus.Destroyed))
                return Logger.WarnReturn(false, $"ScheduleDestroyEvent(): Entity {this} is already destroyed");

            if (_scheduledDestroyEvent.IsValid)
            {
                if (_scheduledDestroyEvent.Get().FireTime > (Game.CurrentTime + delay))
                    Game?.GameEventScheduler?.RescheduleEvent(_scheduledDestroyEvent, delay);
            }
            else
            {
                ScheduleEntityEvent(_scheduledDestroyEvent, delay);
            }

            return true;
        }

        public void CancelDestroyEvent()
        {
            if (_scheduledDestroyEvent.IsValid)
                Game?.GameEventScheduler?.CancelEvent(_scheduledDestroyEvent);
        }

        public bool ScheduledDestroyCallback()
        {
            if (TestStatus(EntityStatus.PendingDestroy))
                return Logger.WarnReturn(false, $"ScheduledDestroyCallback(): Entity {this} is already pending destroy");

            if (TestStatus(EntityStatus.Destroyed))
                return Logger.WarnReturn(false, $"ScheduledDestroyCallback(): Entity {this} is already destroyed");

            Destroy();
            return true;
        }

        private class ScheduledLifespanEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => t.OnLifespanExpired();
        }

        private class ScheduledDestroyEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => t.ScheduledDestroyCallback();
        }

        #endregion
    }
}
