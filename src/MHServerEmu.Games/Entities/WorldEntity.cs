using System.Diagnostics;
using System.Text;
using Gazillion;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.Physics;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public enum PowerMovementPreventionFlags
    {
        Forced = 0,
        NonForced = 1,
        Sync = 2,
    }

    [Flags]
    public enum KillFlags
    {
        None,
        NoDeadEvent         = 1 << 0,
        NoExp               = 1 << 1,
        NoLoot              = 1 << 2,
        DestroyImmediate    = 1 << 3,
        Release            = 1 << 4,
    }

    [Flags]
    public enum ChangePositionFlags
    {
        None                = 0,
        ForceUpdate         = 1 << 0,
        DoNotSendToOwner    = 1 << 1,
        DoNotSendToServer   = 1 << 2,
        DoNotSendToClients  = 1 << 3,
        Orientation         = 1 << 4,
        Force               = 1 << 5,
        Teleport            = 1 << 6,
        HighFlying          = 1 << 7,
        PhysicsResolve      = 1 << 8,
        SkipInterestUpdate  = 1 << 9,
        EnterWorld          = 1 << 10,
    }

    public enum ChangePositionResult
    {
        InvalidPosition,
        PositionChanged,
        NotChanged,
        Teleport
    }

    public partial class WorldEntity : Entity, IKeyworded
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly EventPointer<ScheduledExitWorldEvent> _exitWorldEvent = new();
        private readonly EventPointer<ScheduledKillEvent> _scheduledKillEvent = new();
        private readonly EventPointer<NegateHotspotsEvent> _negateHotspotsEvent = new();

        private AlliancePrototype _allianceProto;
        private Transform3 _transform = Transform3.Identity();

        // We keep track of the last interest update position to avoid updating interest too often when moving around.
        private Vector3 _lastInterestUpdatePosition = Vector3.Zero;

        // Same with map location
        private Vector3 _lastMapPosition = Vector3.Zero;
        private float _lastMapOrientation = 0f;

        protected EntityTrackingContextMap _trackingContextMap;
        protected ConditionCollection _conditionCollection;
        protected PowerCollection _powerCollection;
        protected int _unkEvent;

        public Event<EntityCollisionEvent> OverlapBeginEvent = new();
        public Event<EntityCollisionEvent> CollideEvent = new();
        public Event<EntityCollisionEvent> OverlapEndEvent = new();

        public EntityTrackingContextMap TrackingContextMap { get => _trackingContextMap; }
        public ConditionCollection ConditionCollection { get => _conditionCollection; }
        public PowerCollection PowerCollection { get => _powerCollection; }
        public AlliancePrototype Alliance { get => GetAlliance(); }
        public RegionLocation RegionLocation { get; private set; } = new();
        public Cell Cell { get => RegionLocation.Cell; }
        public Area Area { get => RegionLocation.Area; }
        public RegionLocationSafe ExitWorldRegionLocation { get; private set; } = new();
        public EntityRegionSpatialPartitionLocation SpatialPartitionLocation { get; }
        public Aabb RegionBounds { get; set; }
        public Bounds Bounds { get; set; } = new();
        public Region Region { get => RegionLocation.Region; }
        public NaviMesh NaviMesh { get => RegionLocation.NaviMesh; }
        public Orientation Orientation { get => RegionLocation.Orientation; }
        public WorldEntityPrototype WorldEntityPrototype { get => Prototype as WorldEntityPrototype; }
        public bool ShouldSnapToFloorOnSpawn { get; private set; }
        public EntityActionComponent EntityActionComponent { get; protected set; }
        public SpawnSpec SpawnSpec { get; private set; }
        public SpawnGroup SpawnGroup { get => SpawnSpec?.Group; }
        public Locomotor Locomotor { get; protected set; }
        public virtual Bounds EntityCollideBounds { get => Bounds; set { } }
        public virtual bool IsTeamUpAgent { get => false; }
        public bool IsInWorld { get => RegionLocation.IsValid(); }
        public bool IsAliveInWorld { get => IsInWorld && IsDead == false; }
        public bool IsInPvPMatch { get => Region?.ContainsPvPMatch() == true; }
        public bool CanHeal { get => Properties[PropertyEnum.Health] > 0L && Properties[PropertyEnum.HealingBlocked] == false; }
        public bool IsVendor { get => Properties[PropertyEnum.VendorType] != PrototypeId.Invalid; }
        public EntityPhysics Physics { get; private set; }
        public bool HasNavigationInfluence { get; private set; }
        public NavigationInfluence NaviInfluence { get; private set; }
        public virtual bool IsMovementAuthoritative { get => true; }
        public virtual bool CanBeRepulsed { get => Locomotor != null && Locomotor.IsMoving && !IsExecutingPower; }
        public virtual bool CanRepulseOthers { get => true; }
        public PrototypeId ActivePowerRef { get; protected set; }
        public Power ActivePower { get => GetActivePower(); }
        public bool IsExecutingPower { get => ActivePowerRef != PrototypeId.Invalid; }
        public PrototypeId[] Keywords { get => WorldEntityPrototype?.Keywords; }
        public KeywordsMask KeywordsMask { get => WorldEntityPrototype?.KeywordsMask; }
        public Vector3 Forward { get => GetTransform().Col0; }
        public Vector3 GetUp { get => GetTransform().Col2; }
        public float MovementSpeedRate { get => Properties[PropertyEnum.MovementSpeedRate]; } // PropertyTemp[PropertyEnum.MovementSpeedRate]
        public float MovementSpeedOverride { get => Properties[PropertyEnum.MovementSpeedOverride]; } // PropertyTemp[PropertyEnum.MovementSpeedOverride]
        public float BonusMovementSpeed => Locomotor?.GetBonusMovementSpeed(false) ?? 0.0f;
        public NaviPoint NavigationInfluencePoint { get => NaviInfluence.Point; }
        public bool DefaultRuntimeVisibility { get => WorldEntityPrototype != null && WorldEntityPrototype.VisibleByDefault; }
        public virtual int Throwability { get => 0; }
        public virtual int InteractRange { get => GameDatabase.GlobalsPrototype?.InteractRange ?? 0; }
        public int InteractFallbackRange { get => GameDatabase.GlobalsPrototype?.InteractFallbackRange ?? 0; }
        public bool IsWeaponMissing { get => Properties[PropertyEnum.WeaponMissing]; }
        public bool IsGlobalEventVendor { get => GetVendorGlobalEvent() != PrototypeId.Invalid; }
        public bool IsHighFlying { get => Locomotor?.IsHighFlying ?? false; }
        public bool IsDestructible { get => HasKeyword(GameDatabase.KeywordGlobalsPrototype.DestructibleKeywordPrototype); }
        public bool IsDestroyProtectedEntity { get => IsControlledEntity || IsTeamUpAgent || this is Avatar; }  // Persistent entities cannot be easily destroyed
        public bool IsDiscoverable { get => CompatibleReplicationChannels.HasFlag(AOINetworkPolicyValues.AOIChannelDiscovery); }
        public bool IsTrackable { get => WorldEntityPrototype?.TrackingDisabled == false; }
        public bool IsLiveTuningEnabled { get => WorldEntityPrototype?.IsLiveTuningEnabled() == true; }
        public bool IsVacuumable { get => WorldEntityPrototype?.IsVacuumable == true; }
        public bool IsCrafter { get => ((PrototypeId)Properties[PropertyEnum.VendorType]).As<VendorTypePrototype>()?.IsCrafter == true; }
        public bool IsStash { get => Properties[PropertyEnum.OpenPlayerStash]; }
        public Dictionary<ulong, long> TankingContributors { get; private set; }
        public Dictionary<ulong, long> DamageContributors { get; private set; }
        public TagPlayers TagPlayers { get; private set; }
        public Inventory SummonedInventory { get => GetInventory(InventoryConvenienceLabel.Summoned); }

        public WorldEntity(Game game) : base(game)
        {
            SpatialPartitionLocation = new(this);
            Physics = new();
            HasNavigationInfluence = false;
            NaviInfluence = new();
        }

        public override bool Initialize(EntitySettings settings)
        {
            if (base.Initialize(settings) == false) return Logger.WarnReturn(false, "Initialize(): base.Initialize(settings) == false");

            WorldEntityPrototype worldEntityProto = WorldEntityPrototype;

            if (worldEntityProto.IsVacuumable)
                SetFlag(EntityFlags.IsNeverAffectedByPowers, true);

            if (settings.IgnoreNavi)
                SetFlag(EntityFlags.IgnoreNavi, true);

            ShouldSnapToFloorOnSpawn = settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.HasOverrideSnapToFloor)
                ? settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.OverrideSnapToFloorValue)
                : worldEntityProto.SnapToFloorOnSpawn;

            OnAllianceChanged(Properties[PropertyEnum.AllianceOverride]);
            RegionLocation.Initialize(this);
            SpawnSpec = settings.SpawnSpec;
            SetFlag(EntityFlags.IsPopulation, settings.IsPopulation);

            if (worldEntityProto.Bounds != null)
            {
                Bounds.InitializeFromPrototype(worldEntityProto.Bounds);
                if (settings.BoundsScaleOverride != 1f)
                    Bounds.Scale(settings.BoundsScaleOverride);
            }

            Physics.Initialize(this);

            _trackingContextMap = new();
            _conditionCollection = new(this);
            _powerCollection = new(this);
            _unkEvent = 0;

            if (Properties.HasProperty(PropertyEnum.Rank) == false && worldEntityProto.Rank != PrototypeId.Invalid)
                Properties[PropertyEnum.Rank] = worldEntityProto.Rank;

            // LiveTuning MobHealth
            Properties[PropertyEnum.HealthPctBonus] = LiveTuningManager.GetLiveWorldEntityTuningVar(worldEntityProto, WorldEntityTuningVar.eWETV_MobHealth) - 1.0f;
            Properties[PropertyEnum.VariationSeed] = settings.VariationSeed != 0 ? settings.VariationSeed : Game.Random.Next(1, 10000);

            TagPlayers = new(this);

            return true;
        }

        public override void OnPostInit(EntitySettings settings)
        {
            base.OnPostInit(settings);
        }

        public void ClearSpawnSpec()
        {
            SpawnSpec = null;
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);

            if (archive.IsTransient)
                success &= Serializer.Transfer(archive, ref _trackingContextMap);

            success &= Serializer.Transfer(archive, ref _conditionCollection);

            uint numRecords = 0;
            success &= PowerCollection.SerializeRecordCount(archive, _powerCollection, ref numRecords);
            if (numRecords > 0)
            {
                if (archive.IsPacking)
                {
                    success &= PowerCollection.SerializeTo(archive, _powerCollection, numRecords);
                }
                else
                {
                    if (_powerCollection == null) _powerCollection = new(this);
                    success &= PowerCollection.SerializeFrom(archive, _powerCollection, numRecords);
                }
            }

            if (archive.IsReplication)
                success &= Serializer.Transfer(archive, ref _unkEvent);

            return success;
        }

        public override void OnUnpackComplete(Archive archive)
        {
            base.OnUnpackComplete(archive);
            ConditionCollection?.OnUnpackComplete(archive);
        }

        public void AddTankingContributor(Player player, long damage)
        {
            if (player == null) return;
            ulong playerUid = player.DatabaseUniqueId;

            TankingContributors ??= new();
            TankingContributors.TryGetValue(playerUid, out long oldDamage);
            TankingContributors[playerUid] = oldDamage + damage;
        }

        public void AddDamageContributor(Player player, long damage)
        {
            if (player == null) return;
            ulong playerUid = player.DatabaseUniqueId;

            DamageContributors ??= new();
            DamageContributors.TryGetValue(playerUid, out long oldDamage);
            DamageContributors[playerUid] = oldDamage + damage;
        }

        public virtual void OnKilled(WorldEntity killer, KillFlags killFlags, WorldEntity directKiller)
        {
            var worldEntityProto = WorldEntityPrototype;
            CancelScheduledLifespanExpireEvent();
            SummonedInventory?.DestroyContained();
            EntityActionComponent?.CancelAll();

            // Trigger OnPetDeath procs if this is a pet with an owner
            ulong powerUserOverrideId = PowerUserOverrideId;
            if (powerUserOverrideId != InvalidId && IsSummonedPet())
            {
                WorldEntity owner = Game.EntityManager.GetEntity<WorldEntity>(powerUserOverrideId);
                if (owner != null && owner.IsInWorld)
                    owner.TryActivateOnPetDeathProcs(this);
            }

            AdjustSummonCount(-1);

            // Loot and XP
            AwardKillLoot(killer, killFlags, directKiller);

            var region = Region;

            // Trigger EntityDead Event
            if (killFlags.HasFlag(KillFlags.NoDeadEvent) == false && this is not Missile)
            {
                var player = killer?.GetOwnerOfType<Player>();
                region?.EntityDeadEvent.Invoke(new(this, killer, player));
            }

            SpawnSpec?.OnDefeat(killer, false);

            // Remove conditions
            ConditionCollection?.RemoveCancelOnKilledConditions();

            // Send kill message to clients
            var killMessage = NetMessageEntityKill.CreateBuilder()
                .SetIdEntity(Id)
                .SetIdKillerEntity(killer != null ? killer.Id : InvalidId)
                .SetKillFlags((uint)killFlags)
                .Build();

            Game.NetworkManager.SendMessageToInterested(killMessage, this, AOINetworkPolicyValues.AOIChannelProximity);

            if (worldEntityProto.PostKilledState != null)
                ApplyStateFromPrototype(worldEntityProto.PostKilledState);

            region?.UIDataProvider.OnEntityLifecycle(this);

            OnRemoveFromWorld(killFlags);
        }

        public void OnRemoveFromWorld(KillFlags killFlags)
        {
            var worldEntityProto = WorldEntityPrototype;

            // Remove navi influence if needed
            if (worldEntityProto.RemoveNavInfluenceOnKilled)
                Properties[PropertyEnum.NoEntityCollide] = true;

            if (IsControlledEntity || this is Avatar) return;

            // Schedule destruction
            int removeFromWorldTimerMS = worldEntityProto.RemoveFromWorldTimerMS;
            if (killFlags.HasFlag(KillFlags.DestroyImmediate) && IsTeamUpAgent == false)
                removeFromWorldTimerMS = 0;

            if (removeFromWorldTimerMS < 0)     // -1 means entities are not destroyed (e.g. avatars)
                return;

            TimeSpan removeFromWorldTimer = TimeSpan.FromMilliseconds(removeFromWorldTimerMS);

            // Team-ups continue existing in player's inventory even after they are defeated because their unlocks are tied to their entities
            if (IsTeamUpAgent)
            {
                if (removeFromWorldTimer == TimeSpan.Zero)
                    ExitWorld();
                else
                    ScheduleExitWorldEvent(removeFromWorldTimer);

                return;
            }

            // Other entities are destroyed 
            if (removeFromWorldTimer == TimeSpan.Zero)
                Destroy();
            else
                ScheduleDestroyEvent(removeFromWorldTimer);
        }

        public void AttachToEntity(WorldEntity target)
        {
            if (target == null || this == target) return;
            if (target.IsInWorld && target.TestStatus(EntityStatus.ExitingWorld) == false)
                Properties[PropertyEnum.AttachedToEntityId] = target.Id;
        }

        #region Summon

        public SummonEntityContextPrototype GetSummonEntityContext()
        {
            PrototypeId powerRef = Properties[PropertyEnum.CreatorPowerPrototype];
            if (powerRef == PrototypeId.Invalid) return null;

            var summonProto = GameDatabase.GetPrototype<SummonPowerPrototype>(powerRef);
            if (summonProto == null) return null;

            int contextIndex = Properties[PropertyEnum.SummonContextIndex];
            return summonProto.GetSummonEntityContext(contextIndex);
        }

        public override void OnSelfAddedToOtherInventory()
        {
            base.OnSelfAddedToOtherInventory();
            var invLoc = InventoryLocation;
            if (invLoc.IsValid && invLoc.InventoryConvenienceLabel == InventoryConvenienceLabel.Summoned)
                AddSummonerCondition(invLoc.ContainerId);
        }

        public override void OnSelfRemovedFromOtherInventory(InventoryLocation prevInvLoc)
        {
            base.OnSelfRemovedFromOtherInventory(prevInvLoc);
            if (prevInvLoc.IsValid && prevInvLoc.InventoryConvenienceLabel == InventoryConvenienceLabel.Summoned)
                RemoveSummonerCondition(prevInvLoc.ContainerId);
        }

        public void AddSummonerCondition(ulong summonerId)
        {
            if (Properties.HasProperty(PropertyEnum.SummonOwnerConditionId)) return;

            var summonProto = GetSummonEntityContext();
            if (summonProto == null) return;

            var conditionProto = GameDatabase.GetPrototype<ConditionPrototype>(summonProto.TrackInInventoryOwnerCondition);
            if (conditionProto == null) return;

            var summoner = Game.EntityManager.GetEntity<WorldEntity>(summonerId);
            if (summoner == null) return;

            var conditionCollection = summoner.ConditionCollection;
            var conditionId = conditionCollection.NextConditionId;
            Condition condition = ConditionCollection.AllocateCondition();
            var duration = conditionProto.GetDuration(summoner.Properties, this);
            if (condition.InitializeFromConditionPrototype(conditionId, Game, summonerId, summonerId, Id, conditionProto, duration)) 
            {
                conditionCollection.AddCondition(condition);
                Properties[PropertyEnum.SummonOwnerConditionId] = conditionId;
            }
            else
                ConditionCollection.DeleteCondition(condition);
        }

        public void RemoveSummonerCondition(ulong summonerId)
        {
            ulong conditionId = Properties[PropertyEnum.SummonOwnerConditionId];
            Properties.RemoveProperty(PropertyEnum.SummonOwnerConditionId);
            if (conditionId == 0) return;
            var summoner = Game.EntityManager.GetEntity<WorldEntity>(summonerId);
            if (summoner == null) return;

            var conditionCollection = summoner.ConditionCollection;
            conditionCollection.RemoveCondition(conditionId);
        }

        private void ClearSummonedInventory()
        {
            var manager = Game?.EntityManager;
            if (manager == null) return;

            var inventory = SummonedInventory;
            if (inventory == null) return;

            if (manager.IsDestroyingAllEntities)
            {
                inventory.DestroyContained();
            }
            else
            {
                List<WorldEntity> destroyList = ListPool<WorldEntity>.Instance.Get();

                foreach (var summoned in new SummonedEntityIterator(this))
                    if (summoned.IsDead 
                        || summoned.TestStatus(EntityStatus.PendingDestroy) 
                        || summoned.Properties[PropertyEnum.SummonedEntityIsRegionPersisted] == false)
                        destroyList.Add(summoned);

                foreach (var summoned in destroyList)
                {
                    if (summoned.Properties[PropertyEnum.DetachOnContainerDestroyed])
                    {
                        summoned.ChangeInventoryLocation(null);
                        summoned.OnDetachedFromDestroyedContainer();
                    }
                    else
                    {
                        summoned.Destroy();
                    }
                }

                ListPool<WorldEntity>.Instance.Return(destroyList);
            }
        }

        public override void OnDetachedFromDestroyedContainer()
        {
            var summonProto = GetSummonEntityContext();
            if (summonProto == null) return;
            if (summonProto.VisibleWhileAttached == false) SetVisible(true);
        }

        private void AdjustSummonCount(int value)
        {
            bool decrement = value < 0;
            if (decrement && SummonDecremented) return;
            if (Properties[PropertyEnum.SummonedByPower] == false) return;

            PrototypeId powerRef = Properties[PropertyEnum.CreatorPowerPrototype];
            if (powerRef == PrototypeId.Invalid) return;

            var summoner = Game.EntityManager.GetEntity<WorldEntity>(PowerUserOverrideId);
            var powerProto = GameDatabase.GetPrototype<SummonPowerPrototype>(powerRef);
            if (summoner == null || powerProto == null) return;

            summoner.Properties.AdjustProperty(value, new PropertyId(PropertyEnum.PowerSummonedEntityCount, powerRef));
            if (powerProto.SummonMaxCountWithOthers.HasValue())
                foreach (var protoRef in powerProto.SummonMaxCountWithOthers)
                    summoner.Properties.AdjustProperty(value, new PropertyId(PropertyEnum.PowerSummonedEntityCount, protoRef));

            SetFlag(EntityFlags.SummonDecremented, decrement);

            if (summoner is not Avatar avatar) return;
            
            var vanityKeyword = GameDatabase.KeywordGlobalsPrototype.VanityPetKeywordPrototype;
            if (HasKeyword(vanityKeyword))
            {
                var player = avatar.GetOwnerOfType<Player>();
                player?.UpdateScoringEventContext();
            }           
        }

        public virtual void SetAsPersistent(Avatar avatar, bool newOnServer)
        {
            var region = avatar.Region;
            if (region == null) return;
            Properties[PropertyEnum.PowerUserOverrideID] = avatar.Id;

            if (IsInWorld) return;

            Vector3 position = avatar.RegionLocation.Position;
            if (this is Agent) position = GetPositionNearAvatar(avatar);

            Orientation orientation = avatar.RegionLocation.Orientation;

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();

            if (newOnServer)
            {
                settings.OptionFlags |= EntitySettingsOptionFlags.IsNewOnServer;
                if (IsTeamUpAgent)
                    settings.OptionFlags |= EntitySettingsOptionFlags.IsClientEntityHidden;
            }

            EnterWorld(region, position, orientation, settings);
        }

        #endregion

        public void Kill(WorldEntity killer = null, KillFlags killFlags = KillFlags.None, WorldEntity directKiller = null)
        {
            CancelKillEvent();

            if (this is not Missile)
            {
                long health = Properties[PropertyEnum.Health];
                var region = Region;
                if (health > 0 && region != null) 
                {
                    var avatar = killer?.GetMostResponsiblePowerUser<Avatar>();
                    var player = avatar?.GetOwnerOfType<Player>();
                    region.AdjustHealthEvent.Invoke(new(this, killer, player, -health, false));
                }
            }

            Properties[PropertyEnum.Health] = 0;
            OnKilled(killer, killFlags, directKiller);   
        }

        public virtual bool OnKilledOther(PowerResults powerResults)
        {
            if (powerResults == null) return Logger.WarnReturn(false, "OnKilledOther(): powerResults == null");

            if (IsInWorld == false)
                return false;

            // Trigger power events
            PowerPrototype powerProto = powerResults.PowerPrototype;
            if (powerProto != null)
            {
                Power power = GetPower(powerProto.DataRef);
                power?.HandleTriggerPowerEventOnTargetKill(powerResults);
            }

            // Try activate procs
            TryActivateOnKillProcs(ProcTriggerType.OnKillOther, powerResults);

            if (powerResults.TestFlag(PowerResultFlags.Critical))
                TryActivateOnKillProcs(ProcTriggerType.OnKillOtherCritical, powerResults);
            else if (powerResults.TestFlag(PowerResultFlags.SuperCritical))
                TryActivateOnKillProcs(ProcTriggerType.OnKillOtherSuperCrit, powerResults);

            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(powerResults.TargetId);
            if (target != null)
            {
                if (target.IsDestructible)
                    TryActivateOnKillProcs(ProcTriggerType.OnKillDestructible, powerResults);
                else if (IsFriendlyTo(target))
                    TryActivateOnKillProcs(ProcTriggerType.OnKillAlly, powerResults);
            }

            return true;
        }

        public override void Destroy()
        {
            if (Game == null) return;

            SpawnSpec?.Destroy();

            ExitWorld();
            if (IsDestroyed == false)
            {
                CancelExitWorldEvent();
                CancelKillEvent();
                CancelDestroyEvent();
                base.Destroy();
            }
        }

        #region World and Positioning

        public override void ExitGame()
        {
            ExitWorld();

            if (Locomotor?.IsEnabled == true)
                Logger.Warn($"ExitGame(): Entity is exiting game but locomotor is still enabled {this}");

            base.ExitGame();
        }

        public virtual bool EnterWorld(Region region, Vector3 position, Orientation orientation, EntitySettings settings = null)
        {
            SetStatus(EntityStatus.EnteringWorld, true);

            RegionLocation.Region = region;

            Physics.AcquireCollisionId();

            ChangePositionResult result = ChangeRegionPosition(position, orientation,
                ChangePositionFlags.ForceUpdate | ChangePositionFlags.DoNotSendToServer | ChangePositionFlags.SkipInterestUpdate | ChangePositionFlags.EnterWorld);

            if (result == ChangePositionResult.PositionChanged)
            {
                CancelExitWorldEvent();

                ApplyState(Properties[PropertyEnum.EntityState]);

                OnEnteredWorld(settings);
            }
            else
            {
                ClearWorldLocation();
            }

            SetStatus(EntityStatus.EnteringWorld, false);

            return IsInWorld;
        }

        public void ExitWorld()
        {
            if (IsInWorld == false) return;

            bool exitStatus = !TestStatus(EntityStatus.ExitingWorld);
            SetStatus(EntityStatus.ExitingWorld, true);

            Physics.ReleaseCollisionId();
            if (IsAttachedToEntity)
            {
                ulong parentId = Properties[PropertyEnum.AttachedToEntityId];
                if (parentId != InvalidId)
                {
                    var parent = Game.EntityManager.GetEntity<WorldEntity>(parentId);
                    if (parent != null && parent.IsInWorld && parent.TestStatus(EntityStatus.ExitingWorld) == false)
                        parent.Physics.DetachChild(Physics);
                }
            }
            Physics.DetachAllChildren();

            DisableNavigationInfluence();

            if (Locomotor != null)
            {
                Locomotor.Stop();
                Locomotor.SetMethod(LocomotorMethod.Default);
            }

            var entityManager = Game.EntityManager;
            if (entityManager == null) return;
            entityManager.PhysicsManager?.OnExitedWorld(Physics);
            OnExitedWorld();
            var oldLocation = ClearWorldLocation();
            SendLocationChangeEvents(oldLocation, RegionLocation, ChangePositionFlags.None);
            ModifyCollectionMembership(EntityCollection.Simulated, false);
            ModifyCollectionMembership(EntityCollection.Locomotion, false);

            if (exitStatus)
                SetStatus(EntityStatus.ExitingWorld, false);
        }

        public override void UpdateInterestPolicies(bool updateForAllPlayers, EntitySettings settings = null)
        {
            base.UpdateInterestPolicies(updateForAllPlayers, settings);
            _lastInterestUpdatePosition = IsInWorld ? RegionLocation.Position : Vector3.Zero;
        }

        public SimulateResult UpdateSimulationState()
        {
            // Never simulate when not in the world
            if (IsInWorld == false)
                return SetSimulated(false);

            // Simulate if the prototype is flagged as always simulated
            if (WorldEntityPrototype?.AlwaysSimulated == true)
                return SetSimulated(true);

            // Fix for team-up AI getting disabled when they get stuck and you run away too far from them
            if (IsTeamUpAgent)
                return SetSimulated(true);

            // Simulate is there are any player interested in this world entity or its cell
            return SetSimulated(Cell?.HasAnyInterest == true ||
                                InterestReferences.IsAnyPlayerInterested(AOINetworkPolicyValues.AOIChannelProximity) ||
                                InterestReferences.IsAnyPlayerInterested(AOINetworkPolicyValues.AOIChannelClientIndependent));
        }

        public virtual bool CanRotate()
        {
            return true;
        }

        public virtual bool CanMove()
        {
            return Locomotor != null && Locomotor.GetCurrentSpeed() > 0.0f;
        }

        public virtual ChangePositionResult ChangeRegionPosition(Vector3? position, Orientation? orientation, ChangePositionFlags flags = ChangePositionFlags.None)
        {
            bool positionChanged = false;
            bool orientationChanged = false;
            Cell previousCell = Cell;

            RegionLocation preChangeLocation = new(RegionLocation);
            Region region = Game.RegionManager.GetRegion(preChangeLocation.RegionId);
            if (region == null) return ChangePositionResult.NotChanged;

            if (position.HasValue && (flags.HasFlag(ChangePositionFlags.ForceUpdate) || preChangeLocation.Position != position))
            {
                var result = RegionLocation.SetPosition(position.Value);

                if (result != RegionLocation.SetPositionResult.Success)     // onSetPositionFailure()
                {
                    return Logger.WarnReturn(ChangePositionResult.NotChanged, string.Format(
                        "ChangeRegionPosition(): Failed to set entity new position (Moved out of world)\n\tEntity: {0}\n\tResult: {1}\n\tPrev Loc: {2}\n\tNew Pos: {3}",
                        this, result, RegionLocation, position));
                }

                if (Bounds.Geometry != GeometryType.None)
                    Bounds.Center = position.Value;

                if (flags.HasFlag(ChangePositionFlags.PhysicsResolve) == false)
                    RegisterForPendingPhysicsResolve();

                positionChanged = true;
            }

            if (orientation.HasValue && (flags.HasFlag(ChangePositionFlags.ForceUpdate) || preChangeLocation.Orientation != orientation))
            {
                RegionLocation.Orientation = orientation.Value;

                if (Bounds.Geometry != GeometryType.None)
                    Bounds.Orientation = orientation.Value;
                if (Physics.HasAttachedEntities())
                    RegisterForPendingPhysicsResolve();
                orientationChanged = true;
            }

            if (Locomotor != null && flags.HasFlag(ChangePositionFlags.PhysicsResolve) == false)
            {
                if (positionChanged)
                    Locomotor.ClearSyncState();
                else if (orientationChanged)
                    Locomotor.ClearOrientationSyncState();
            }

            if (positionChanged == false && orientationChanged == false)
                return ChangePositionResult.NotChanged;

            UpdateRegionBounds(); // Add to Quadtree
            SendLocationChangeEvents(preChangeLocation, RegionLocation, flags);
            SetStatus(EntityStatus.ToTransform, true);
            if (RegionLocation.IsValid())
                ExitWorldRegionLocation.Set(RegionLocation);

            if (positionChanged && flags.HasFlag(ChangePositionFlags.SkipInterestUpdate) == false)
            {
                // Update interest when this world entity moves to another cell or it has moved far enough from the last interest update position
                if (Cell != null &&
                   (Cell != previousCell || Vector3.DistanceSquared2D(_lastInterestUpdatePosition, RegionLocation.Position) >= AreaOfInterest.UpdateDistanceSquared))
                {
                    UpdateInterestPolicies(true);
                }
            }

            // Send position to clients if needed
            if (flags.HasFlag(ChangePositionFlags.DoNotSendToClients) == false)
            {
                bool excludeOwner = flags.HasFlag(ChangePositionFlags.DoNotSendToOwner);

                PlayerConnectionManager networkManager = Game.NetworkManager;
                List<PlayerConnection> interestedClientList = ListPool<PlayerConnection>.Instance.Get();
                if (networkManager.GetInterestedClients(interestedClientList, this, AOINetworkPolicyValues.AOIChannelProximity, excludeOwner))
                {
                    var entityPositionMessageBuilder = NetMessageEntityPosition.CreateBuilder()
                        .SetIdEntity(Id)
                        .SetFlags((uint)flags);

                    if (position.HasValue) entityPositionMessageBuilder.SetPosition(position.Value.ToNetStructPoint3());
                    if (orientation.HasValue) entityPositionMessageBuilder.SetOrientation(orientation.Value.ToNetStructPoint3());

                    networkManager.SendMessageToMultiple(interestedClientList, entityPositionMessageBuilder.Build());
                }

                ListPool<PlayerConnection>.Instance.Return(interestedClientList);
            }

            // Update map location if needed
            if (((CompatibleReplicationChannels & AOINetworkPolicyValues.MapChannels) != 0) &&
                (flags.HasFlag(ChangePositionFlags.ForceUpdate) || ((InterestedPoliciesUnion & AOINetworkPolicyValues.MapChannels) != 0)))
            {
                UpdateMapLocation();
            }

            return ChangePositionResult.PositionChanged;
        }

        public RegionLocation ClearWorldLocation()
        {
            if (RegionLocation.IsValid()) ExitWorldRegionLocation.Set(RegionLocation);
            if (Region != null && SpatialPartitionLocation.IsValid()) Region.RemoveEntityFromSpatialPartition(this);
            RegionLocation oldLocation = new(RegionLocation);
            RegionLocation.Set(RegionLocation.Invalid);
            return oldLocation;
        }

        public Vector3 FloorToCenter(Vector3 position)
        {
            Vector3 resultPosition = position;
            if (Bounds.Geometry != GeometryType.None)
                resultPosition.Z += Bounds.HalfHeight;
            // TODO Locomotor.GetCurrentFlyingHeight
            return resultPosition;
        }

        public bool ShouldUseSpatialPartitioning() => Bounds.Geometry != GeometryType.None;

        public EntityRegionSPContext GetEntityRegionSPContext()
        {
            EntityRegionSPContextFlags flags = EntityRegionSPContextFlags.ActivePartition;
            ulong playerRestrictedGuid = 0;

            WorldEntityPrototype entityProto = WorldEntityPrototype;
            if (entityProto == null) return new(flags);

            if (entityProto.CanCollideWithPowerUserItems)
            {
                Avatar avatar = GetMostResponsiblePowerUser<Avatar>();
                if (avatar != null)
                    playerRestrictedGuid = avatar.OwnerPlayerDbId;
            }

            if (!(IsNeverAffectedByPowers || (IsHotspot && !IsCollidableHotspot && !IsReflectingHotspot)))
                flags |= EntityRegionSPContextFlags.StaticPartition;

            return new(flags, playerRestrictedGuid);
        }

        public void UpdateRegionBounds()
        {
            RegionBounds = Bounds.ToAabb();
            if (ShouldUseSpatialPartitioning())
                Region.UpdateEntityInSpatialPartition(this);
        }

        public float GetDistanceTo(WorldEntity other, bool calcRadius)
        {
            if (other == null) return 0f;
            float distance = Vector3.Distance2D(RegionLocation.Position, other.RegionLocation.Position);
            if (calcRadius)
                distance -= Bounds.Radius + other.Bounds.Radius;
            return Math.Max(0.0f, distance);
        }

        public Vector3 GetPositionNearAvatar(Avatar avatar)
        {
            Region region = avatar.Region;
            region.ChooseRandomPositionNearPoint(avatar.Bounds, Region.GetPathFlagsForEntity(WorldEntityPrototype), PositionCheckFlags.PreferNoEntity,
                    BlockingCheckFlags.CheckSpawns, 50, 200, out Vector3 position);
            return position;
        }

        public bool OrientToward(Vector3 point, bool ignorePitch = false, ChangePositionFlags changeFlags = ChangePositionFlags.None)
        {
            return OrientToward(point, RegionLocation.Position, ignorePitch, changeFlags);
        }

        private bool OrientToward(Vector3 point, Vector3 origin, bool ignorePitch = false, ChangePositionFlags changeFlags = ChangePositionFlags.None)
        {
            if (IsInWorld == false) Logger.Debug($"Trying to orient entity that is not in the world {this}.  point={point}, ignorePitch={ignorePitch}, cpFlags={changeFlags}");
            Vector3 delta = point - origin;
            if (ignorePitch) delta.Z = 0.0f;
            if (Vector3.LengthSqr(delta) >= MathHelper.PositionSqTolerance)
                return ChangeRegionPosition(null, Orientation.FromDeltaVector(delta), changeFlags) == ChangePositionResult.PositionChanged;
            return false;
        }

        public Vector3 GetVectorFrom(WorldEntity other)
        {
            if (other == null) return Vector3.Zero;
            return RegionLocation.GetVectorFrom(other.RegionLocation);
        }

        private Transform3 GetTransform()
        {
            if (TestStatus(EntityStatus.ToTransform))
            {
                _transform = Transform3.BuildTransform(RegionLocation.Position, RegionLocation.Orientation);
                SetStatus(EntityStatus.ToTransform, false);
            }
            return _transform;
        }

        private void SendLocationChangeEvents(RegionLocation oldLocation, RegionLocation newLocation, ChangePositionFlags flags)
        {
            if (flags.HasFlag(ChangePositionFlags.EnterWorld))
                OnRegionChanged(null, newLocation.Region);
            else if (oldLocation.Region != newLocation.Region)
                OnRegionChanged(oldLocation.Region, newLocation.Region);

            if (oldLocation.Area != newLocation.Area)
                OnAreaChanged(oldLocation, newLocation);

            if (oldLocation.Cell != newLocation.Cell)
                OnCellChanged(oldLocation, newLocation, flags);
        }

        private void UpdateMapLocation()
        {
            const float MapPositionThreshold = 64f * 64f;
            const float MapOrientationThreshold = 0.1f;

            // Remove from the map if no longer in the world
            if (RegionLocation.IsValid() == false)
            {
                Properties.RemoveProperty(PropertyEnum.MapPosition);
                Properties.RemoveProperty(PropertyEnum.MapOrientation);
                return;
            }

            if (Properties.HasProperty(PropertyEnum.MapPosition) == false ||
                Vector3.DistanceSquared2D(RegionLocation.Position, _lastMapPosition) > MapPositionThreshold)
            {
                _lastMapPosition = RegionLocation.Position;
                Properties[PropertyEnum.MapPosition] = _lastMapPosition;
            }

            if (Properties.HasProperty(PropertyEnum.MapOrientation) == false ||
                Segment.EpsilonTest(RegionLocation.Orientation.Yaw, _lastMapOrientation, MapOrientationThreshold) == false)
            {
                // NOTE: The MapOrientation property has an interval of [0;65535], so it can't store negative values.
                // To work around this, we use WrapAngleRadians() instead of GetYawNormalized() to get a [0:2PI] value instead of [-PI:PI].
                _lastMapOrientation = RegionLocation.Orientation.Yaw;
                Properties[PropertyEnum.MapOrientation] = Orientation.WrapAngleRadians(_lastMapOrientation);
            }
        }

        #endregion

        #region Physics

        public bool CanBeBlockedBy(WorldEntity other)
        {
            if (other == null || CanCollideWith(other) == false || Bounds.CanBeBlockedBy(other.Bounds) == false) return false;

            if (NoCollide || other.NoCollide)
            {
                bool noEntityCollideException = (HasNoCollideException && Properties[PropertyEnum.NoEntityCollideException] == other.Id) ||
                   (other.HasNoCollideException && other.Properties[PropertyEnum.NoEntityCollideException] == Id);
                return noEntityCollideException;
            }

            var worldEntityProto = WorldEntityPrototype;
            var boundsProto = worldEntityProto?.Bounds;
            var otherWorldEntityProto = other.WorldEntityPrototype;
            var otherBoundsProto = otherWorldEntityProto?.Bounds;

            if ((boundsProto != null && boundsProto.BlockOnlyMyself)
                || (otherBoundsProto != null && otherBoundsProto.BlockOnlyMyself))
                return PrototypeDataRef == other.PrototypeDataRef;

            if ((otherBoundsProto != null && otherBoundsProto.IgnoreBlockingWithAvatars && this is Avatar) ||
                (boundsProto != null && boundsProto.IgnoreBlockingWithAvatars && other is Avatar)) return false;

            bool locomotionNoCollide = Locomotor != null && (Locomotor.HasLocomotionNoEntityCollide || IsInKnockback);
            bool otherLocomotionNoCollide = other.Locomotor != null && (other.Locomotor.HasLocomotionNoEntityCollide || other.IsInKnockback);

            if (locomotionNoCollide || otherLocomotionNoCollide || IsIntangible || other.IsIntangible)
            {
                bool locomotorMovementPower = false;
                if (otherBoundsProto != null)
                {
                    switch (otherBoundsProto.BlocksMovementPowers)
                    {
                        case BoundsMovementPowerBlockType.All:
                            locomotorMovementPower = (Locomotor != null
                                && (Locomotor.IsMovementPower || Locomotor.IsHighFlying))
                                || IsInKnockback || IsIntangible;
                            break;
                        case BoundsMovementPowerBlockType.Ground:
                            locomotorMovementPower = (Locomotor != null
                                && (Locomotor.IsMovementPower && Locomotor.CurrentMoveHeight == 0)
                                && !Locomotor.IgnoresWorldCollision && !IsIntangible)
                                || IsInKnockback;
                            break;
                        case BoundsMovementPowerBlockType.None:
                        default:
                            break;
                    }
                }
                if (locomotorMovementPower == false) return false;
            }

            if (CanBePlayerOwned() && other.CanBePlayerOwned())
            {
                if (GetAlliance() == other.GetAlliance())
                {
                    if (HasPowerUserOverride == false || other.HasPowerUserOverride == false) return false;
                    uint powerId = Properties[PropertyEnum.PowerUserOverrideID];
                    uint otherPowerId = other.Properties[PropertyEnum.PowerUserOverrideID];
                    if (powerId != otherPowerId) return false;
                }
                if (other.IsInKnockdown || other.IsInKnockup) return false;
            }
            return true;
        }

        public virtual bool CanCollideWith(WorldEntity other)
        {
            if (other == null) return false;

            if (TestStatus(EntityStatus.Destroyed) || !IsInWorld
                || other.TestStatus(EntityStatus.Destroyed) || !other.IsInWorld) return false;

            if (Bounds.CollisionType == BoundsCollisionType.None
                || other.Bounds.CollisionType == BoundsCollisionType.None) return false;

            if ((other.Bounds.Geometry == GeometryType.Triangle || other.Bounds.Geometry == GeometryType.Wedge)
                && (Bounds.Geometry == GeometryType.Triangle || Bounds.Geometry == GeometryType.Wedge)) return false;

            var entityProto = WorldEntityPrototype;
            if (entityProto == null) return false;

            if (IsCloneParent()) return false;

            var boundsProto = entityProto.Bounds;
            if (boundsProto != null && boundsProto.IgnoreCollisionWithAllies && IsFriendlyTo(other)) return false;

            if (IsDormant || other.IsDormant) return false;

            return true;
        }

        public void RegisterForPendingPhysicsResolve()
        {
            PhysicsManager physMan = Game?.EntityManager?.PhysicsManager;
            physMan?.RegisterEntityForPendingPhysicsResolve(this);
        }

        #endregion

        #region Navi and Senses

        public void EnableNavigationInfluence()
        {
            if (IsInWorld == false || TestStatus(EntityStatus.ExitingWorld)) return;

            if (HasNavigationInfluence == false)
            {
                var region = Region;
                if (region == null) return;
                if (region.NaviMesh.AddInfluence(RegionLocation.Position, Bounds.Radius, NaviInfluence) == false)
                    Logger.Warn($"Failed to add navi influence for ENTITY={this} MISSION={GameDatabase.GetFormattedPrototypeName(MissionPrototype)}");
                HasNavigationInfluence = true;
            }
        }

        public void DisableNavigationInfluence()
        {
            if (HasNavigationInfluence)
            {
                Region region = Region;
                if (region == null) return;
                if (region.NaviMesh.RemoveInfluence(NaviInfluence) == false)
                    Logger.Warn($"Failed to remove navi influence for ENTITY={this} MISSION={GameDatabase.GetFormattedPrototypeName(MissionPrototype)}");
                HasNavigationInfluence = false;
            }
        }

        public bool CanInfluenceNavigationMesh()
        {
            if (IsInWorld == false || TestStatus(EntityStatus.ExitingWorld) || NoCollide || IsIntangible || IsCloneParent())
                return false;

            var prototype = WorldEntityPrototype;
            if (prototype != null && prototype.Bounds != null)
                return prototype.Bounds.CollisionType == BoundsCollisionType.Blocking && prototype.AffectNavigation;

            return false;
        }

        public void UpdateNavigationInfluence()
        {
            if (HasNavigationInfluence == false) return;

            Region region = Region;
            if (region == null) return;
            var regionPosition = RegionLocation.Position;
            if (NaviInfluence.Point != null)
            {
                if (NaviInfluence.Point.Pos.X != regionPosition.X ||
                    NaviInfluence.Point.Pos.Y != regionPosition.Y)
                    if (region.NaviMesh.UpdateInfluence(NaviInfluence, regionPosition, Bounds.Radius) == false)
                        Logger.Warn($"Failed to update navi influence for ENTITY={ToString()} MISSION={GameDatabase.GetFormattedPrototypeName(MissionPrototype)}");
            }
            else
                if (region.NaviMesh.AddInfluence(regionPosition, Bounds.Radius, NaviInfluence) == false)
                Logger.Warn($"Failed to add navi influence for ENTITY={ToString()} MISSION={GameDatabase.GetFormattedPrototypeName(MissionPrototype)}");
        }

        public PathFlags GetPathFlags()
        {
            if (Locomotor != null) return Locomotor.PathFlags;
            if (WorldEntityPrototype == null) return PathFlags.None;
            return Locomotor.GetPathFlags(WorldEntityPrototype.NaviMethod);
        }

        public NaviPathResult CheckCanPathTo(Vector3 toPosition)
        {
            return CheckCanPathTo(toPosition, GetPathFlags());
        }

        public NaviPathResult CheckCanPathTo(Vector3 toPosition, PathFlags pathFlags)
        {
            var region = Region;
            if (IsInWorld == false || region == null)
            {
                Logger.Warn($"Entity not InWorld when trying to check for a path! Entity: {ToString()}");
                return NaviPathResult.Failed;
            }

            bool hasNaviInfluence = false;
            if (HasNavigationInfluence)
            {
                DisableNavigationInfluence();
                hasNaviInfluence = HasNavigationInfluence;
            }

            var result = NaviPath.CheckCanPathTo(region.NaviMesh, RegionLocation.Position, toPosition, Bounds.Radius, pathFlags);
            if (hasNaviInfluence) EnableNavigationInfluence();

            return result;
        }

        public virtual bool CheckLandingSpot(Power power)
        {
            // TODO: Overrides in Agent and Avatar
            return true;
        }

        public bool LineOfSightTo(WorldEntity other, float radius = 0.0f, float padding = 0.0f, float height = 0.0f)
        {
            if (other == null) return false;
            if (this == other) return true;
            if (other.IsInWorld == false) return false;
            Region region = Region;
            if (region == null) return false;

            Vector3 startPosition = GetEyesPosition();
            return region.LineOfSightTo(startPosition, this, other.RegionLocation.Position, other.Id, radius, padding, height);
        }

        public bool LineOfSightTo(Vector3 targetPosition, float radius = 0.0f, float padding = 0.0f, float height = 0.0f, PathFlags pathFlags = PathFlags.Sight)
        {
            Region region = Region;
            if (region == null) return false;
            Vector3 startPosition = GetEyesPosition();
            return region.LineOfSightTo(startPosition, this, targetPosition, InvalidId, radius, padding, height, pathFlags);
        }

        private Vector3 GetEyesPosition()
        {
            Vector3 retPos = RegionLocation.Position;
            Bounds bounds = Bounds;
            retPos.Z += bounds.EyeHeight;
            return retPos;
        }

        #endregion

        #region Powers

        public Power GetPower(PrototypeId powerProtoRef) => _powerCollection?.GetPower(powerProtoRef);
        public Power GetThrowablePower() => _powerCollection?.ThrowablePower;
        public Power GetThrowableCancelPower() => _powerCollection?.ThrowableCancelPower;
        public virtual bool IsMelee() => false;

        public bool HasPowerInPowerCollection(PrototypeId powerProtoRef)
        {
            if (_powerCollection == null) return Logger.WarnReturn(false, "HasPowerInPowerCollection(): PowerCollection == null");
            return _powerCollection.ContainsPower(powerProtoRef);
        }

        public Power AssignPower(PrototypeId powerProtoRef, in PowerIndexProperties indexProps, bool sendPowerAssignmentToClients = true, PrototypeId triggeringPowerRef = PrototypeId.Invalid)
        {
            if (_powerCollection == null) return Logger.WarnReturn<Power>(null, "AssignPower(): _powerCollection == null");
            Power assignedPower = _powerCollection.AssignPower(powerProtoRef, indexProps, triggeringPowerRef, sendPowerAssignmentToClients);
            if (assignedPower == null) return Logger.WarnReturn(assignedPower, "AssignPower(): assignedPower == null");
            return assignedPower;
        }

        public bool UnassignPower(PrototypeId powerProtoRef, bool sendPowerUnassignToClients = true)
        {
            if (HasPowerInPowerCollection(powerProtoRef) == false) return false;    // This includes the null check for PowerCollection

            if (_powerCollection.UnassignPower(powerProtoRef, sendPowerUnassignToClients) == false)
                return Logger.WarnReturn(false, "UnassignPower(): Failed to unassign power");

            return true;
        }

        public virtual PowerUseResult ActivatePower(PrototypeId powerRef, ref PowerActivationSettings settings)
        {
            Power power = GetPower(powerRef);
            if (power == null)
            {
                Logger.Warn($"ActivatePower(): Requested activation of power {GameDatabase.GetPrototypeName(powerRef)} but that power not found on {this}");
                return PowerUseResult.AbilityMissing;
            }
            return ActivatePower(power, ref settings);
        }

        public void EndAllPowers(bool notSimulated)
        {
            // No powers to end if no collection
            if (PowerCollection == null)
                return;

            // Ending powers can remove them, so we store all proto refs in a temporary collection.
            Span<PrototypeId> powerProtoRefs = stackalloc PrototypeId[PowerCollection.PowerCount];
            int i = 0;

            foreach (var kvp in PowerCollection)
                powerProtoRefs[i++] = kvp.Key;

            foreach (PrototypeId powerProtoRef in powerProtoRefs)
            {
                Power power = PowerCollection.GetPower(powerProtoRef);

                if (power == null)
                    continue;

                if (notSimulated && power.Properties[PropertyEnum.RemovePowerWhenNotSimulated] == false)
                    continue;

                EndPowerFlags flags = EndPowerFlags.ExplicitCancel | EndPowerFlags.Interrupting;
                if (notSimulated)
                    flags |= EndPowerFlags.ExitWorld;

                power.EndPower(flags);
            }
        }

        public T GetMostResponsiblePowerUser<T>(bool skipPet = false) where T : WorldEntity
        {
            if (Game == null)
                return Logger.WarnReturn<T>(null, "GetMostResponsiblePowerUser(): Entity has no associated game. \nEntity: " + ToString());

            WorldEntity currentWorldEntity = this;
            T result = null;
            EntityManager entityManager = Game.EntityManager;

            while (currentWorldEntity != null)
            {
                if (skipPet && currentWorldEntity.IsSummonedPet())
                    return null;

                if (currentWorldEntity is T possibleResult)
                    result = possibleResult;

                if (currentWorldEntity.HasPowerUserOverride == false)
                    break;

                ulong powerUserOverrideId = currentWorldEntity.Properties[PropertyEnum.PowerUserOverrideID];
                currentWorldEntity = entityManager.GetEntity<WorldEntity>(powerUserOverrideId);

                if (currentWorldEntity == this)
                    return Logger.WarnReturn<T>(null, "GetMostResponsiblePowerUser(): Circular reference in PowerUserOverrideID chain!");
            }

            return result;
        }

        public bool ActivePowerPreventsMovement(PowerMovementPreventionFlags movementPreventionFlag)
        {
            if (IsExecutingPower == false) return false;

            var activePower = ActivePower;
            if (activePower == null) return false;

            if (activePower.IsPartOfAMovementPower())
                return movementPreventionFlag == PowerMovementPreventionFlags.NonForced;

            if (movementPreventionFlag == PowerMovementPreventionFlags.NonForced && activePower.PreventsNewMovementWhileActive())
            {
                if (activePower.IsChannelingPower() == false) return true;
                else if (activePower.IsNonCancellableChannelPower()) return true;
            }

            if (movementPreventionFlag == PowerMovementPreventionFlags.Sync)
                if (activePower.IsChannelingPower() == false || activePower.IsCancelledOnMove())
                    return true;

            if (activePower.TriggersComboPowerOnEvent(PowerEventType.OnPowerEnd))
                return true;

            return false;
        }

        public bool ActivePowerDisablesOrientation()
        {
            if (IsExecutingPower == false) return false;
            var activePower = ActivePower;
            if (activePower == null)
            {
                Logger.Warn($"WorldEntity has ActivePowerRef set, but is missing the power in its power collection! Power: [{GameDatabase.GetPrototypeName(ActivePowerRef)}] WorldEntity: [{ToString()}]");
                return false;
            }
            return activePower.DisableOrientationWhileActive();
        }

        public bool ActivePowerOrientsToTarget()
        {
            if (IsExecutingPower == false) return false;

            var activePower = ActivePower;
            if (activePower == null) return false;

            return activePower.ShouldOrientToTarget();
        }

        public void OrientForPower(Power power, Vector3 targetPosition, Vector3 userPosition)
        {
            if (power.ShouldOrientToTarget() == false)
                return;

            if (power.GetTargetingShape() == TargetingShapeType.Self)
                return;

            if (Properties[PropertyEnum.LookAtMousePosition])
                return;

            OrientToward(targetPosition, userPosition, true, ChangePositionFlags.DoNotSendToServer | ChangePositionFlags.DoNotSendToClients);
        }

        public virtual PowerUseResult CanTriggerPower(PowerPrototype powerProto, Power power, PowerActivationSettingsFlags flags)
        {
            if (power == null && powerProto.Properties == null) return PowerUseResult.GenericError;
            if (power != null && power.Prototype != powerProto) return PowerUseResult.GenericError;

            var powerProperties = power != null ? power.Properties : powerProto.Properties;

            var region = Region;
            if (region == null) return PowerUseResult.GenericError;
            if (Power.CanBeUsedInRegion(powerProto, powerProperties, region) == false)
                return PowerUseResult.RegionRestricted;

            if (Power.IsMovementPower(powerProto)
                && (IsSystemImmobilized || (IsImmobilized && powerProperties[PropertyEnum.NegStatusUsable] == false)))
                return PowerUseResult.RestrictiveCondition;

            if (powerProperties[PropertyEnum.PowerUsesReturningWeapon] && IsWeaponMissing)
                return PowerUseResult.WeaponMissing;

            var targetingShape = Power.GetTargetingShape(powerProto);
            if (targetingShape == TargetingShapeType.Self)
            {
                if (Power.IsValidTarget(powerProto, this, Alliance, this) == false)
                    return PowerUseResult.BadTarget;
            }
            else if (targetingShape == TargetingShapeType.TeamUp)
            {
                if (this is not Avatar avatar)
                    return PowerUseResult.GenericError;
                var teamUpAgent = avatar.CurrentTeamUpAgent;
                if (teamUpAgent == null)
                    return PowerUseResult.TargetIsMissing;
                if (Power.IsValidTarget(powerProto, this, Alliance, teamUpAgent) == false)
                    return PowerUseResult.BadTarget;
            }

            if (powerProto.IsHighFlyingPower)
            {
                var naviMesh = region.NaviMesh;
                var pathFlags = GetPathFlags();
                pathFlags |= PathFlags.Fly;
                pathFlags &= ~PathFlags.Walk;
                if (naviMesh.Contains(RegionLocation.Position, Bounds.Radius, new DefaultContainsPathFlagsCheck(pathFlags)) == false)
                    return PowerUseResult.RegionRestricted;
            }

            return PowerUseResult.Success;
        }

        public virtual bool CanPowerTeleportToPosition(Vector3 position)
        {
            if (Region == null) return false;

            return Region.NaviMesh.Contains(position, Bounds.GetRadius(), new DefaultContainsPathFlagsCheck(GetPathFlags()));
        }

        public int GetPowerChargesAvailable(PrototypeId powerProtoRef)
        {
            return Properties[PropertyEnum.PowerChargesAvailable, powerProtoRef];
        }

        public int GetPowerChargesMax(PrototypeId powerProtoRef)
        {
            return Properties[PropertyEnum.PowerChargesMax, powerProtoRef];
        }

        public TimeSpan GetAbilityCooldownStartTime(PowerPrototype powerProto)
        {
            return Properties[PropertyEnum.PowerCooldownStartTime, powerProto.DataRef];
        }

        public virtual TimeSpan GetAbilityCooldownTimeElapsed(PowerPrototype powerProto)
        {
            // Overriden in Avatar
            return Game.CurrentTime - GetAbilityCooldownStartTime(powerProto);
        }

        public virtual TimeSpan GetAbilityCooldownTimeRemaining(PowerPrototype powerProto)
        {
            // Overriden in Agent
            TimeSpan cooldownDurationForLastActivation = GetAbilityCooldownDurationUsedForLastActivation(powerProto);
            TimeSpan cooldownTimeElapsed = Clock.Max(GetAbilityCooldownTimeElapsed(powerProto), TimeSpan.Zero);
            return Clock.Max(cooldownDurationForLastActivation - cooldownTimeElapsed, TimeSpan.Zero);
        }

        public TimeSpan GetAbilityCooldownDuration(PowerPrototype powerProto)
        {
            Power power = GetPower(powerProto.DataRef);
            
            if (power != null)
                return power.GetCooldownDuration();

            if (powerProto.Properties == null) return Logger.WarnReturn(TimeSpan.Zero, "GetAbilityCooldownDuration(): powerProto.Properties == null");
            return Power.GetCooldownDuration(powerProto, this, powerProto.Properties);
        }

        public TimeSpan GetAbilityCooldownDurationUsedForLastActivation(PowerPrototype powerProto)
        {
            TimeSpan powerCooldownDuration = TimeSpan.Zero;

            if (Power.IsCooldownOnPlayer(powerProto))
            {
                Player powerOwnerPlayer = GetOwnerOfType<Player>();
                if (powerOwnerPlayer != null)
                    powerCooldownDuration = powerOwnerPlayer.Properties[PropertyEnum.PowerCooldownDuration, powerProto.DataRef];
                else
                    Logger.Warn("GetAbilityCooldownDurationUsedForLastActivation(): powerOwnerPlayer == null");
            }
            else
            {
                powerCooldownDuration = Properties[PropertyEnum.PowerCooldownDuration, powerProto.DataRef];
            }

            return powerCooldownDuration;
        }

        public bool IsPowerOnCooldown(PowerPrototype powerProto)
        {
            return GetAbilityCooldownTimeRemaining(powerProto) > TimeSpan.Zero;
        }

        public virtual TimeSpan GetPowerInterruptCooldown(PowerPrototype powerProto)
        {
            // Overriden in Agent and Avatar
            return TimeSpan.Zero;
        }

        public bool IsTargetable(WorldEntity entity)
        {
            if (IsTargetableInternal() == false) return false;
            if (entity == null) return false;

            var player = GetOwnerOfType<Player>();
            if (player != null && player.IsTargetable(entity.Alliance) == false) return false;

            return true;
        }

        public bool IsAffectedByPowers()
        {
            if (IsAffectedByPowersInternal() == false)
                return false;

            if (Alliance == null)
                return false;

            return true;
        }

        public virtual void ActivatePostPowerAction(Power power, EndPowerFlags flags)
        {
            // NOTE: Overriden in avatar
        }

        public virtual void UpdateRecurringPowerApplication(PowerApplication powerApplication, PrototypeId powerProtoRef)
        {
            // NOTE: Overriden in avatar
        }

        public virtual bool ShouldContinueRecurringPower(Power power, ref EndPowerFlags flags)
        {
            // NOTE: Overriden in avatar
            return true;
        }

        public bool UpdateProcEffectPowers(PropertyCollection properties, bool assignPowers)
        {
            // Cannot assign proc powers is not in world
            if (IsInWorld == false)
                return true;

            bool success = true;

            EntityManager entityManager = Game.EntityManager;

            using PropertyCollection procProperties = GetProcProperties(properties);
            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesAll))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId procPowerProtoRef);
                if (procPowerProtoRef == PrototypeId.Invalid)
                {
                    Logger.Warn("UpdateProcEffectPowers(): procPowerProtoRef == PrototypeId.Invalid");
                    continue;
                }

                WorldEntity caster = this;

                // Check if we have a caster override for this
                ulong procCasterOverrideId = properties[PropertyEnum.ProcCasterOverride, procPowerProtoRef];
                if (procCasterOverrideId != InvalidId)
                {
                    caster = entityManager.GetEntity<WorldEntity>(procCasterOverrideId);
                    if (caster == null || caster.IsInWorld == false)
                        continue;
                }

                if (assignPowers)
                {
                    PowerIndexProperties indexProps = new(0, caster.CharacterLevel, caster.CombatLevel);
                    PrototypeId triggeringPowerRef = properties[PropertyEnum.TriggeringPowerRef, procPowerProtoRef];

                    if (caster.AssignPower(procPowerProtoRef, indexProps, true, triggeringPowerRef) == null)
                    {
                        Logger.Warn($"UpdateProcEffectPowers(): Failed to assign {procPowerProtoRef.GetName()} to [{this}]");
                        success = false;
                    }
                }
                else
                {
                    UnassignPower(procPowerProtoRef);
                }

                // Try to active certain proc trigger types right away
                Property.FromParam(kvp.Key, 0, out AssetId procTriggerTypeAssetRef);
                ProcTriggerType procTriggerType = (ProcTriggerType)AssetDirectory.Instance.GetEnumValue(procTriggerTypeAssetRef);
                switch (procTriggerType)
                {
                    case ProcTriggerType.OnHealthAbove:
                    case ProcTriggerType.OnHealthAboveToggle:
                    case ProcTriggerType.OnHealthBelow:
                    case ProcTriggerType.OnHealthBelowToggle:
                        // Activate health procs at the end of the frame (for cases when we don't have our health yet)
                        EventPointer<ScheduledHealthProcUpdateEvent> healthProcUpdate = new();
                        ScheduleEntityEvent(healthProcUpdate, TimeSpan.Zero, procPowerProtoRef);
                        break;

                    case ProcTriggerType.OnOverlapBegin:
                        // Check overlaps that began before this proc was assigned
                        TryActivateOnOverlapBeginProcs(kvp.Key);
                        break;
                }
            }

            return success;
        }

        protected virtual void InitializeProcEffectPowers()
        {
            if (UpdateProcEffectPowers(Properties, true) == false)
                Logger.Warn($"InitializeProcEffectPowers(): UpdateProcEffectPowers failed when initializing entity=[{this}]");
        }

        protected override void OnAttachedPropertiesPreAdd(PropertyCollection properties)
        {
            base.OnAttachedPropertiesPreAdd(properties);

            if (UpdateProcEffectPowers(properties, true) == false)
                Logger.Warn($"OnAttachedPropertiesPreAdd(): UpdateProcEffectPowers failed when attaching properties to entity=[{this}]");
        }

        protected override void OnAttachedPropertiesPostRemove(PropertyCollection properties)
        {
            base.OnAttachedPropertiesPostRemove(properties);

            UpdateProcEffectPowers(properties, false);
        }

        public float GetNegStatusResistPercent(int ccResistScore, PropertyCollection otherProperties)
        {
            EvalPrototype evalNegStatusResistPctFormula = GameDatabase.CombatGlobalsPrototype?.EvalNegStatusResistPctFormulaPrototype;
            if (evalNegStatusResistPctFormula == null) return Logger.WarnReturn(0f, "GetNegStatusResistPercent(): evalNegStatusResistPctFormula == null");

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, otherProperties);
            evalContext.SetVar_Int(EvalContext.Var1, ccResistScore);

            float resistPercent = Eval.RunFloat(evalNegStatusResistPctFormula, evalContext);
            return Math.Clamp(resistPercent, 0f, 1f);
        }

        public bool ApplyPowerResults(PowerResults powerResults)
        {
            // Send power results to clients
            if (powerResults.ShouldSendToClient())
            {
                NetMessagePowerResult powerResultMessage = ArchiveMessageBuilder.BuildPowerResultMessage(powerResults);
                Game.NetworkManager.SendMessageToInterested(powerResultMessage, this, AOINetworkPolicyValues.AOIChannelProximity);
            }

            bool success = false;

            if (IsInWorld)
            {
                WorldEntity powerOwner = Game.EntityManager.GetEntity<WorldEntity>(powerResults.PowerOwnerId);
                powerOwner ??= Game.EntityManager.GetEntity<WorldEntity>(powerResults.UltimateOwnerId);

                ApplyMovementPowerResults(powerResults);

                if (powerResults.IsAtMaxRecursionDepth() == false)
                {
                    if (powerResults.IsAvoided == false && powerResults.TestFlag(PowerResultFlags.Hostile) && powerOwner?.IsInWorld == true)
                        TriggerOnHitEffects(powerResults, powerOwner);

                    if (powerResults.IsBlocked)
                        TryActivateOnBlockProcs(powerResults);

                    if (powerResults.IsDodged)
                        TryActivateOnDodgeProcs(powerResults);
                }

                // Check if this entity was destroyed by procs
                if (IsInWorld == false || TestStatus(EntityStatus.Destroyed))
                    success = true;
                else // Apply the actual results if not
                    success = ApplyPowerResultsInternal(powerResults);
            }

            // Clear only conditions here because these results may still be used for procs
            powerResults.ClearConditionInstances();
            return success;
        }

        public bool ApplyDamageTransferPowerResults(PowerResults powerResults)
        {
            // Applies power results without extra stuff (e.g. checking procs)

            // Send power results to clients
            if (powerResults.ShouldSendToClient())
            {
                NetMessagePowerResult powerResultMessage = ArchiveMessageBuilder.BuildPowerResultMessage(powerResults);
                Game.NetworkManager.SendMessageToInterested(powerResultMessage, this, AOINetworkPolicyValues.AOIChannelProximity);
            }

            return ApplyPowerResultsInternal(powerResults);
        }

        private bool TriggerOnHitEffects(PowerResults powerResults, WorldEntity powerOwner)
        {
            // powerOwner has been null checked above in ApplyPowerResults()

            PowerPrototype powerProto = powerResults.PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "TriggerOnHitEffects(): powerProto == null");

            Avatar avatar = powerOwner.GetMostResponsiblePowerUser<Avatar>(true);

            // TODO: Set LastInflictedDamageTime for avatars

            // Enter combat if this is not an over time effect
            if (powerResults.TestFlag(PowerResultFlags.OverTime) == false)
            {
                powerOwner.EnterCombat();
                if (avatar != null && avatar != powerOwner && avatar.IsInWorld)
                    avatar.EnterCombat();
            }

            Power power = powerOwner.GetPower(powerProto.DataRef);

            // Trigger OnProjectileHit power events (projectiles are non-missile powers that have projectile speed)
            if (power != null && powerProto is not MissilePowerPrototype &&
                (powerProto.ProjectileTimeToImpactOverride > 0f || powerProto.GetProjectileSpeed(power.Properties, powerOwner.Properties) > 0f))
            {
                power.HandleTriggerPowerEventOnProjectileHit(powerResults);
            }

            // Trigger OnAnyHit procs
            powerOwner.TryActivateOnHitProcs(ProcTriggerType.OnAnyHit, powerResults);
            powerOwner.TryActivateOnHitProcs(ProcTriggerType.OnAnyHitForPctHealth, powerResults);
            powerOwner.TryActivateOnHitProcs(ProcTriggerType.OnAnyHitTargetHealthBelowPct, powerResults);

            // Trigger OnHitKeyword power events
            power?.HandleTriggerPowerEventOnHitKeyword(powerResults);

            // Trigger OnPetHit procs for the summoner if needed
            ulong powerUserOverrideId = powerOwner.Properties[PropertyEnum.PowerUserOverrideID];
            if (powerUserOverrideId != InvalidId)
            {
                WorldEntity summoner = Game.EntityManager.GetEntity<WorldEntity>(powerUserOverrideId);
                if (summoner != null && summoner.IsInWorld)
                    summoner.TryActivateOnPetHitProcs(powerResults, powerOwner);
            }

            // OnPowerHitPhysical / OnPowerHitEnergy / OnPowerHitMental
            foreach (var kvp in powerResults.Properties.IteratePropertyRange(PropertyEnum.Damage))
            {
                Property.FromParam(kvp.Key, 0, out int damageType);

                ProcTriggerType triggerType = (DamageType)damageType switch
                {
                    DamageType.Physical => ProcTriggerType.OnPowerHitPhysical,
                    DamageType.Energy   => ProcTriggerType.OnPowerHitEnergy,
                    DamageType.Mental   => ProcTriggerType.OnPowerHitMental,
                    _                   => ProcTriggerType.None
                };

                if (triggerType == ProcTriggerType.None)
                {
                    Logger.Warn("TriggerOnHitEffects(): triggerType == ProcTriggerType.None");
                    continue;
                }

                powerOwner.TryActivateOnHitProcs(triggerType, powerResults);
            }

            // OnPowerHit / OnPowerHitNormal
            powerOwner.TryActivateOnHitProcs(ProcTriggerType.OnPowerHit, powerResults);

            if (powerProto.PowerCategory == PowerCategoryType.NormalPower)
                powerOwner.TryActivateOnHitProcs(ProcTriggerType.OnPowerHitNormal, powerResults);

            // OnCrit / OnSuperCrit procs and OnCriticalHit power events
            if (powerResults.TestFlag(PowerResultFlags.Critical))
            {
                powerOwner.TryActivateOnHitProcs(ProcTriggerType.OnCrit, powerResults);
                power?.HandleTriggerPowerEventOnCriticalHit(powerResults);
            }
            else if (powerResults.TestFlag(PowerResultFlags.SuperCritical))
            {
                powerOwner.TryActivateOnHitProcs(ProcTriggerType.OnSuperCrit, powerResults);
            }

            // OnPowerHitNotOverTime
            if (powerResults.TestFlag(PowerResultFlags.OverTime) == false)
                powerOwner.TryActivateOnHitProcs(ProcTriggerType.OnPowerHitNotOverTime, powerResults);

            return true;
        }

        private bool TriggerOnDamagedEffects(PowerResults powerResults)
        {
            if (powerResults == null)
                return false;

            if (IsInWorld == false)
                return false;

            WorldEntity powerOwner = Game.EntityManager.GetEntity<WorldEntity>(powerResults.UltimateOwnerId);

            float healthDelta = powerResults.Properties[PropertyEnum.Healing];

            foreach (var kvp in powerResults.Properties.IteratePropertyRange(PropertyEnum.Damage))
            {
                float damage = kvp.Value;
                healthDelta -= damage;

                Property.FromParam(kvp.Key, 0, out int damageType);

                ProcTriggerType triggerType = (DamageType)damageType switch
                {
                    DamageType.Physical => ProcTriggerType.OnGotDamagedPhysical,
                    DamageType.Energy   => ProcTriggerType.OnGotDamagedEnergy,
                    DamageType.Mental   => ProcTriggerType.OnGotDamagedMental,
                    _                   => ProcTriggerType.None
                };

                if (triggerType == ProcTriggerType.None)
                {
                    Logger.Warn("TriggerOnDamagedEffects(): triggerType == ProcTriggerType.None");
                    continue;
                }

                TryActivateOnGotDamagedProcs(triggerType, powerResults, -damage);
            }

            if (healthDelta < 0f)
            {
                // Agent-only: interrupt on cancel on damaged powers
                OnDamaged(powerResults);

                TryActivateOnGotDamagedProcs(ProcTriggerType.OnGotDamaged, powerResults, healthDelta);
                TryActivateOnGotDamagedProcs(ProcTriggerType.OnGotDamagedForPctHealth, powerResults, healthDelta);
                TryActivateOnGotDamagedProcs(ProcTriggerType.OnGotDamagedHealthBelowPct, powerResults, healthDelta);

                if (powerResults.TestFlag(PowerResultFlags.Critical))
                    TryActivateOnGotDamagedProcs(ProcTriggerType.OnGotDamagedByCrit, powerResults, healthDelta);
                else if (powerResults.TestFlag(PowerResultFlags.SuperCritical))
                    TryActivateOnGotDamagedProcs(ProcTriggerType.OnGotDamagedBySuperCrit, powerResults, healthDelta);

                ConditionCollection?.RemoveCancelOnHitConditions();
            }

            return true;
        }

        private bool ApplyPowerResultsInternal(PowerResults powerResults)
        {
            // TODO: More stuff
            WorldEntity ultimateOwner = Game.EntityManager.GetEntity<WorldEntity>(powerResults.UltimateOwnerId);

            if (powerResults.Flags.HasFlag(PowerResultFlags.Resurrect) && IsDead)
                ResurrectFromOther(ultimateOwner);

            if (powerResults.IsAvoided == false)
            {
                // Add / remove conditions
                ApplyConditionPowerResults(powerResults);

                // Reset lifespan if needed for non-avatar entities
                TimeSpan lifespan = TimeSpan.FromMilliseconds((int)powerResults.Properties[PropertyEnum.SetTargetLifespanMS]);
                if (this is not Avatar && lifespan > TimeSpan.Zero)
                    ResetLifespan(lifespan);
            }

            // Adjust health
            ApplyHealthPowerResults(powerResults, ultimateOwner);

            if (powerResults.IsAvoided == false)
            {
                ApplyResourcePowerResults(powerResults);
                ApplyDamageAccumulationPowerResults(powerResults);
            }

            return true;
        }

        protected virtual void ResurrectFromOther(WorldEntity ultimateOwner) { }

        private bool ApplyMovementPowerResults(PowerResults powerResults)
        {
            if (Locomotor == null)
                return false;

            if (Properties[PropertyEnum.NoForcedMovement] && powerResults.PowerOwnerId != Id)
                return false;

            // Teleport
            if (powerResults.TestFlag(PowerResultFlags.Teleport))
            {
                Vector3 teleportPosition = FloorToCenter(RegionLocation.ProjectToFloor(Region, Cell, powerResults.TeleportPosition));
                ChangeRegionPosition(teleportPosition, null, ChangePositionFlags.Force);
                return true;
            }

            // Knockback
            float knockbackTime = powerResults.Properties[PropertyEnum.KnockbackTimeResult];
            if (Segment.IsNearZero(knockbackTime))
                return false;

            Vector3 knockbackSource = powerResults.KnockbackSourcePosition;
            float knockbackSpeed = powerResults.Properties[PropertyEnum.KnockbackSpeedResult];
            float knockbackAcceleration = powerResults.Properties[PropertyEnum.KnockbackAccelerationResult];
            Physics.ApplyKnockbackForce(knockbackSource, knockbackTime, knockbackSpeed, knockbackAcceleration);

            // Orient this entity for forced movement
            if (powerResults.PowerOwnerId != Id)
            {
                Orientation orientation;

                bool isMovingAway = knockbackSpeed > 0f || (knockbackSpeed == 0f && knockbackAcceleration > 0f);
                bool reverseOrientation = powerResults.Properties[PropertyEnum.KnockbackReverseTargetOri];

                if ((isMovingAway && reverseOrientation == false) || (isMovingAway == false && reverseOrientation))
                    orientation = Orientation.FromDeltaVector(powerResults.KnockbackSourcePosition - RegionLocation.Position);  // Face away from source
                else
                    orientation = Orientation.FromDeltaVector(RegionLocation.Position - powerResults.KnockbackSourcePosition);  // Face towards source

                ChangeRegionPosition(null, orientation, ChangePositionFlags.Orientation);
            }

            return true;
        }

        private bool ApplyConditionPowerResults(PowerResults powerResults)
        {
            if (powerResults == null) return Logger.WarnReturn(false, "ApplyConditionPowerResults(): powerResults == null");

            ConditionCollection conditionCollection = ConditionCollection;
            if (conditionCollection == null) return true;

            // NOTE: There may not be an owner
            WorldEntity powerOwner = Game.EntityManager.GetEntity<WorldEntity>(powerResults.PowerOwnerId);

            // NOTE: We use for instead of foreach for iteration to avoid boxing the enumerator for IReadOnlyList

            // Add new conditions
            for (int i = 0; i < powerResults.ConditionAddList.Count; i++)
            {
                Condition condition = powerResults.ConditionAddList[i];
                if (condition == null)
                {
                    Logger.Warn("ApplyConditionPowerResults(): condition == null");
                    continue;
                }

                // Skip conditions that need to be manually removed if the owner no longer exists
                if (condition.Duration == TimeSpan.Zero && powerOwner == null)
                    continue;

                conditionCollection.AddCondition(condition);
            }

            // Remove existing conditions
            if (powerResults.ConditionRemoveList.Count == 0)
                return true;

            int numRemoved = 0;
            bool removedNegativeStatusEffect = false;

            for (int i = 0; i < powerResults.ConditionRemoveList.Count; i++)
            {
                ulong conditionId = powerResults.ConditionRemoveList[i];

                Condition condition = conditionCollection.GetCondition(conditionId);
                if (condition == null) continue;    // This may have already been removed

                numRemoved++;

                // IsANegativeStatusEffect iterates the property collection of this condition, so don't do it again if we already found one
                if (removedNegativeStatusEffect == false && condition.IsANegativeStatusEffect())
                    removedNegativeStatusEffect = true;

                conditionCollection.RemoveCondition(conditionId);
            }

            // Trigger relevant power events
            if (numRemoved == 0 || powerOwner == null || powerOwner.IsInWorld == false)
                return true;

            Power power = powerOwner.GetPower(powerResults.PowerPrototype.DataRef);
            if (power != null)
            {
                power.HandleTriggerPowerEventOnRemoveCondition(powerResults, numRemoved);

                if (removedNegativeStatusEffect)
                    power.HandleTriggerPowerEventOnRemoveNegStatusEffect(powerResults);
            }

            return true;
        }

        private bool ApplyHealthPowerResults(PowerResults powerResults, WorldEntity ultimateOwner)
        {
            Region region = Region;
            if (region == null) return Logger.WarnReturn(false, "ApplyHealthPowerResults(): region == null");

            // Calculate health difference based on all damage types and healing
            // NOTE: Health can be > 2147483647, so we have to use 64-bit integers here to avoid overflows
            long health = Properties[PropertyEnum.Health];
            long startHealth = health;
            long healthDelta = 0;

            if (powerResults.Flags.HasFlag(PowerResultFlags.InstantKill))
            {
                // INSTANT KILL
                healthDelta -= health;
            }
            else
            {
                // Calculate damage delta normally
                foreach (var kvp in powerResults.Properties.IteratePropertyRange(PropertyEnum.Damage))
                    healthDelta -= MathHelper.RoundToInt64(kvp.Value);

                healthDelta += MathHelper.RoundToInt64(powerResults.Properties[PropertyEnum.Healing]);
            }

            // Check for invulnerability
            if (powerResults.TestFlag(PowerResultFlags.Hostile) && Properties[PropertyEnum.Invulnerable])
                healthDelta = 0;

            // Check procs (even if invulnerable)
            if (powerResults.TestFlag(PowerResultFlags.Hostile) && powerResults.IsAtMaxRecursionDepth() == false)
            {
                TryActivateOnGotAttackedProcs(powerResults);
                EnterCombat();
                OnGotHit(ultimateOwner);
            }

            // Abort if not valid
            // Case 1: No health change
            if (healthDelta == 0)
                return false;

            // Case 2: you are already dead (NANI)
            if (healthDelta <= 0 && health <= 0)
                return false;

            // Case 3: Ignores damage from alliance
            if (ultimateOwner != null)
            {
                PrototypeId allianceProtoRef = Properties[PropertyEnum.DamageIgnoreFromAlliance];
                if (allianceProtoRef != PrototypeId.Invalid && allianceProtoRef == ultimateOwner.Alliance.DataRef)
                    return false;
            }

            // Calculate the new health value
            health += healthDelta;
            health = Math.Clamp(health, Properties[PropertyEnum.HealthMin], Properties[PropertyEnum.HealthMax]);

            // Trigger health events
            WorldEntity powerUser = Game.EntityManager.GetEntity<WorldEntity>(powerResults.PowerOwnerId);

            long adjustHealth = health - startHealth;

            var avatar = ultimateOwner?.GetMostResponsiblePowerUser<Avatar>();

            if (region != null)
            {
                var player = avatar?.GetOwnerOfType<Player>();
                bool isDodged = powerResults.TestFlag(PowerResultFlags.Dodged);
                region.AdjustHealthEvent.Invoke(new(this, ultimateOwner, player, adjustHealth, isDodged));
            }

            if (powerResults.IsAvoided)
                return false;

            // Apply health change
            bool killed = false;

            if (health <= 0 && Properties[PropertyEnum.AIDefeated] == false)
            {
                Properties[PropertyEnum.Health] = 0;

                if (this is Avatar killedAvatar)
                {
                    var killedPlayer = GetOwnerOfType<Player>();
                    region?.OnRecordPlayerDeath(killedPlayer, killedAvatar, ultimateOwner);

                    killedPlayer.OnScoringEvent(new(ScoringEventType.AvatarDeath));
                    var killer = avatar?.GetOwnerOfType<Player>();
                   
                    foreach (var tagPlayer in TagPlayers.GetPlayers())
                    {
                        if (tagPlayer == killer)
                            tagPlayer.OnScoringEvent(new(ScoringEventType.AvatarKill));
                        else
                            tagPlayer.OnScoringEvent(new(ScoringEventType.AvatarKillAssist));
                    }
                }

                if (powerResults.PowerOwnerId != powerResults.TargetId)
                {
                    if (powerUser != null && powerUser != ultimateOwner)
                        powerUser.OnKilledOther(powerResults);
                    else
                        ultimateOwner?.OnKilledOther(powerResults);

                    ultimateOwner?.TriggerEntityActionEvent(EntitySelectorActionEventType.OnKilledOther);

                    if (IsControlledEntity == false)
                        TryActivateOnDeathProcs(powerResults);
                }

                // Check health again in case a cheat death proc activated
                if (Properties[PropertyEnum.Health] <= 0L)
                {
                    var killFlags = KillFlags.None;
                    if (powerResults != null)
                    {
                        if (powerResults.Properties[PropertyEnum.NoLootDrop])
                            killFlags |= KillFlags.NoLoot;
                        if (powerResults.Properties[PropertyEnum.NoExpOnDeath])
                            killFlags |= KillFlags.NoExp;
                        if (powerResults.Properties[PropertyEnum.OnKillDestroyImmediate])
                            killFlags |= KillFlags.DestroyImmediate;
                    }

                    Kill(ultimateOwner, killFlags, powerUser);
                    killed = true;
                    TriggerEntityActionEvent(EntitySelectorActionEventType.OnGotKilled);
                }
            }
            else
            {
                Properties[PropertyEnum.Health] = health;

                // Procs
                if (adjustHealth < 0 && powerResults.IsAtMaxRecursionDepth() == false)
                    TriggerOnDamagedEffects(powerResults);

                TriggerEntityActionEvent(EntitySelectorActionEventType.OnGotDamaged);
            }

            if (this is Agent agent && adjustHealth < 0 && CanBePlayerOwned() == false)
                agent.AIController?.OnAIGotDamaged(ultimateOwner, adjustHealth);

            if (killed)
            {
                ulong playerUid = 0;
                Player player = null;
                bool isCombatActive = false;

                var powerTime = Game.CurrentTime - TimeSpan.FromSeconds(10);
                var manager = Game.EntityManager;

                foreach (var tag in TagPlayers.Tags)
                {
                    if (playerUid != tag.PlayerUID)
                    {
                        player = manager.GetEntityByDbGuid<Player>(tag.PlayerUID);
                        isCombatActive = player != null && player.CurrentAvatar.IsCombatActive();

                        if (isCombatActive)
                            player.OnScoringEvent(new(ScoringEventType.EntityDeath, Prototype, GetRankPrototype()));

                        playerUid = tag.PlayerUID;
                    }

                    if (isCombatActive && tag.PowerPrototype != null && tag.Time >= powerTime)
                        player.OnScoringEvent(new(ScoringEventType.EntityDeathViaPower, Prototype, tag.PowerPrototype, GetRankPrototype()), Id);
                }
            }

            return true;
        }

        private void ApplyResourcePowerResults(PowerResults powerResults)
        {
            // Primary resource (endurance / spirit)
            foreach (var kvp in powerResults.Properties.IteratePropertyRange(PropertyEnum.EnduranceChange))
            {
                Property.FromParam(kvp.Key, 0, out int manaType);
                float enduranceChange = kvp.Value;

                // Check if the resource change is within margin of error
                if (Segment.IsNearZero(enduranceChange))
                    continue;

                // Make sure we can gain endurance if we are gaining
                if (enduranceChange > 0f && Properties[PropertyEnum.DisableEnduranceGain, manaType])
                    continue;

                // Adjust
                float endurance = Properties[PropertyEnum.Endurance, manaType];
                endurance += enduranceChange;
                endurance = Math.Clamp(endurance, 0f, Properties[PropertyEnum.EnduranceMax, manaType]);
                Properties[PropertyEnum.Endurance, manaType] = endurance;
            }

            // Secondary resource
            float secondaryResourceChange = powerResults.Properties[PropertyEnum.SecondaryResourceChange];

            // Check if the resource change is within margin of error
            if (Segment.IsNearZero(secondaryResourceChange))
                return;

            // Make sure we can gain secondary resource if we are gaining
            if (secondaryResourceChange > 0f && Properties[PropertyEnum.DisableSecondaryResourceGain])
                return;

            // Adjust
            float secondaryResource = Properties[PropertyEnum.SecondaryResource];
            secondaryResource += secondaryResourceChange;
            secondaryResource = Math.Clamp(secondaryResource, 0f, Properties[PropertyEnum.SecondaryResourceMax]);
            Properties[PropertyEnum.SecondaryResource] = secondaryResource;
        }

        private void ApplyDamageAccumulationPowerResults(PowerResults powerResults)
        {
            ConditionCollection conditionCollection = ConditionCollection;
            if (conditionCollection == null)
                return;

            Dictionary<DamageType, float> adjustDict = DictionaryPool<DamageType, float>.Instance.Get();

            foreach (Condition condition in conditionCollection)
            {
                PropertyCollection conditionProps = condition.Properties;

                foreach (var kvp in conditionProps.IteratePropertyRange(PropertyEnum.DamageAccumulationLimit))
                {
                    Property.FromParam(kvp.Key, 0, out int damageTypeValue);
                    DamageType damageType = (DamageType)damageTypeValue;

                    float damageAccumulationChange = powerResults.Properties[PropertyEnum.DamageAccumulationChange, damageType];
                    if (damageAccumulationChange == 0f)
                        continue;

                    float accumulationLimit = GetDamageAccumulationLimit(conditionProps, damageType);
                    accumulationLimit -= conditionProps[PropertyEnum.DamageAccumulation, damageType];

                    adjustDict.Add(damageType, Math.Min(damageAccumulationChange, accumulationLimit));
                }

                foreach (var kvp in adjustDict)
                    conditionProps.AdjustProperty(kvp.Value, new(PropertyEnum.DamageAccumulation, kvp.Key));

                adjustDict.Clear();
            }

            DictionaryPool<DamageType, float>.Instance.Return(adjustDict);
        }

        public void ApplyPropertyTicker(PropertyTicker.TickData tickData)
        {
            //Logger.Debug($"ApplyPropertyTicker(): [{tickData}] => [{this}]");

            if (IsInWorld == false || tickData.TickDurationSeconds <= 0f)
                return;

            using PropertyCollection overTimeProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            foreach (var kvp in tickData.PropertyList)
                overTimeProperties[kvp.Key] = kvp.Value;

            // Try to find the payload that created this tick
            PowerPayload payload = null;
            bool hasConditionPayload = false;

            if (tickData.ConditionId != ConditionCollection.InvalidConditionId)
            {
                Condition condition = ConditionCollection?.GetCondition(tickData.ConditionId);
                payload = condition?.PropertyTickerPayload;
            }

            if (payload == null)
            {
                // If we couldn't find a power payload, use a dummy one
                PowerPayload.ReusableTickerPayload ??= new();
                payload = PowerPayload.ReusableTickerPayload;
                payload.Init(Game);
            }
            else
            {
                // Clean up the payload that we are reusing
                payload.ClearResult();
                hasConditionPayload = true;
            }

            // Initialize and calculate results
            WorldEntity ultimateCreator = Game.EntityManager.GetEntity<WorldEntity>(tickData.UltimateCreatorId);
            
            Vector3 powerOwnerPosition = ultimateCreator != null && ultimateCreator.IsInWorld
                ? ultimateCreator.RegionLocation.Position
                : Vector3.Zero;

            bool isHostile = ultimateCreator?.IsHostileTo(this) == true;

            PowerResults results = new();
            results.Init(tickData.CreatorId, tickData.UltimateCreatorId, Id, powerOwnerPosition, tickData.PowerProto, payload.PowerAssetRefOverride, isHostile);
            results.SetFlag(PowerResultFlags.OverTime, true);

            if (hasConditionPayload)
                results.SetKeywordsMask(payload.KeywordsMask);
            else if (tickData.PowerProto != null)
                results.SetKeywordsMask(tickData.PowerProto.KeywordsMask);

            // Only condition-based tickers can deal damage over time
            payload.CalculateOverTimeProperties(this, overTimeProperties, tickData.TickDurationSeconds, hasConditionPayload);
            payload.CalculatePowerResultsOverTime(results, this, hasConditionPayload);

            // Scale bounds if needed
            float boundsScaleChange = Properties[PropertyEnum.BoundsScaleRadiusCOTUnitsPerSec] * tickData.TickDurationSeconds;
            ApplyBoundsScaleChange(boundsScaleChange);

            // Apply health cost over time (different from damage, e.g. Blade's Thirst)
            float healthCostOverTime = overTimeProperties[PropertyEnum.PowerHealthCostOverTime];
            if (ultimateCreator != null && Segment.IsNearZero(healthCostOverTime) == false)
            {
                long health = ultimateCreator.Properties[PropertyEnum.Health];
                if (health > 0)
                {
                    // Cap health at 1 so the cost doesn't kill the creator
                    health = Math.Max(health - MathHelper.RoundToInt64(healthCostOverTime), 1L);
                    health = Math.Max(health, ultimateCreator.Properties[PropertyEnum.HealthMin]);
                    ultimateCreator.Properties[PropertyEnum.Health] = health;
                }
            }

            if (results.HasMeaningfulResults() == false)
                return;

            ApplyPowerResults(results);

            // Break stealth if needed
            WorldEntity creator = Game.EntityManager.GetEntity<WorldEntity>(tickData.CreatorId);
            Power.TryBreakStealth(creator, ultimateCreator, tickData.PowerProto, isHostile, true);
        }

        private bool ApplyBoundsScaleChange(float boundsScaleChange)
        {
            if (Segment.IsNearZero(boundsScaleChange))
                return true;

            Bounds bounds = new(Bounds);
            float oldRadius = bounds.Radius;
            if (oldRadius == 0f) return Logger.WarnReturn(false, "ApplyBoundsScaleChange(): oldRadius == 0f");  // guard against div by 0
            float newRadius = oldRadius + boundsScaleChange;
            bounds.Scale(newRadius / oldRadius);
            Bounds = bounds;

            bounds = new(EntityCollideBounds);
            oldRadius = bounds.Radius;
            if (oldRadius == 0f) return Logger.WarnReturn(false, "ApplyBoundsScaleChange(): oldRadius == 0f");  // guard against div by 0
            newRadius = oldRadius + boundsScaleChange;
            bounds.Scale(newRadius / oldRadius);
            EntityCollideBounds = bounds;

            RegisterForPendingPhysicsResolve();
            return true;
        }

        public float ApplyDamageConversion(float damageBase, DamageType damageType, PowerResults powerResults, WorldEntity user, PropertyCollection powerProperties, float difficultyMult)
        {
            PowerPrototype powerProto = powerResults.PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(damageBase, "ApplyDamageConversion(): powerProto == null");

            DamageConversionContext context = new(damageBase, damageType, powerProto);

            // Convert incoming (target -> target)
            context.SetIncoming(Properties, this);
            ApplyDamageConversionInternal(ref context);

            if (user != null && user.IsDead == false)
            {
                // Convert outgoing (user -> user)
                context.SetOutgoing(user.Properties, user, difficultyMult);
                ApplyDamageConversionInternal(ref context);

                // Convert for power (power -> user)
                context.SetForPower(powerProperties, user);
                ApplyDamageConversionInternal(ref context);
            }

            return context.DamageConverted;
        }

        private void ApplyDamageConversionInternal(ref DamageConversionContext context)
        {
            // Defer property changes because we are likely converting properties on the same collection (target -> target or user -> user)
            List<(PropertyEnum, float)> conversionResults = ListPool<(PropertyEnum, float)>.Instance.Get();

            PropertyInfoTable propertyInfoTable = GameDatabase.PropertyInfoTable;

            foreach (var kvp in context.SourceProperties.IteratePropertyRange(context.ConversionProperty))
            {
                // Check if this conversion property matches our context's damage type
                Property.FromParam(kvp.Key, 0, out int damageTypeValue);
                DamageType damageType = (DamageType)damageTypeValue;

                if (damageType != DamageType.Any && damageType != context.DamageType)
                    continue;

                Property.FromParam(kvp.Key, 1, out PrototypeId convertedPropertyProtoRef);
                PropertyEnum convertedProperty = propertyInfoTable.GetPropertyEnumFromPrototype(convertedPropertyProtoRef);

                // Validate data type - this system supports only float properties and health
                PropertyInfo propertyInfo = propertyInfoTable.LookupPropertyInfo(convertedProperty);
                if (propertyInfo.DataType != PropertyDataType.Real && convertedProperty != PropertyEnum.Health)
                {
                    Logger.Warn($"ApplyDamageConversionInternal(): Trying to convert to invalid property type for power {context.PowerPrototype}");
                    continue;
                }

                // Convert value
                float convertedValue = context.DamageBase * kvp.Value;

                // Apply conversion ration
                float conversionRatio = context.SourceProperties[context.ConversionRatioProperty, convertedPropertyProtoRef];
                if (Segment.IsNearZero(conversionRatio) == false)
                    convertedValue /= conversionRatio;

                // Clamp to max value
                float conversionMax = context.SourceProperties[context.ConversionMaxProperty, convertedPropertyProtoRef];
                if (Segment.IsNearZero(conversionMax) == false)
                    convertedValue = Math.Max(convertedValue, conversionMax);

                // Calculate conversion cost (i.e. damage lost a result of this conversion)
                float convertedPct = 1f;    // Default to full conversion cost
                Property.FromParam(kvp.Key, 2, out int conversionCostParam);
                
                // Clamp conversion cost if needed
                if (conversionCostParam > 1)
                {
                    Properties.GetPropertyMinMaxFloat(convertedProperty, out float min, out float max);
                    float current = Properties[convertedProperty];
                    float remainingConvertedProperty = (convertedValue > 0f) ? (max - current) : (current - min);
                    convertedPct = Math.Abs(remainingConvertedProperty / convertedValue);
                    convertedPct = Math.Clamp(convertedPct, 0f, 1f);
                }

                // Remove difficulty multiplier from the converted value
                if (context.ConversionProperty == PropertyEnum.DamageConversionOutgoing)
                {
                    float difficultyMultiplier = context.DifficultyMultiplier;
                    if (difficultyMultiplier > 0f && difficultyMultiplier < 1f && Segment.IsNearZero(difficultyMultiplier) == false)
                        convertedValue /= difficultyMultiplier;
                }

                // Add conversion results to be applied below
                conversionResults.Add((convertedProperty, convertedValue));

                // Apply conversion cost to the damage
                if (conversionCostParam != 0)
                {
                    float costMultiplier = Math.Clamp(kvp.Value * convertedPct, 0f, 1f);
                    float damageConverted = context.DamageConverted - (context.DamageBase * costMultiplier);
                    damageConverted = Math.Max(damageConverted, 0f);
                    context.DamageConverted = damageConverted;
                }
            }

            // Apply resulting property adjustments to the target
            WorldEntity target = context.Target;

            foreach (var result in conversionResults)
                target.SetDamageConvertedProperty(result.Item1, result.Item2);

            ListPool<(PropertyEnum, float)>.Instance.Return(conversionResults);
        }

        private void SetDamageConvertedProperty(PropertyEnum propertyEnum, float delta)
        {
            switch (propertyEnum)
            {
                // By default simply adjust the value
                default:
                    Properties.AdjustProperty(delta, propertyEnum);
                    break;

                // Special handling for health / mana / secondary resource
                case PropertyEnum.Health:
                    if (delta > 0f && Properties[PropertyEnum.DisableHealthGain])
                        return;

                    long health = Properties[PropertyEnum.Health];
                    health += MathHelper.RoundToInt64(delta);
                    health = Math.Clamp(health, 0, Properties[PropertyEnum.HealthMax]);

                    Properties[PropertyEnum.Health] = health;

                    break;

                case PropertyEnum.Endurance:
                    if (this is not Avatar avatar)
                        return;

                    foreach (PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto in avatar.GetPrimaryResourceManaBehaviors())
                    {
                        ManaType manaType = primaryManaBehaviorProto.ManaType;

                        if (delta > 0f && Properties[PropertyEnum.DisableEnduranceGain, manaType])
                            continue;

                        float endurance = Properties[PropertyEnum.Endurance, manaType];
                        endurance += delta;
                        endurance = Math.Clamp(endurance, 0f, Properties[PropertyEnum.EnduranceMax, manaType]);

                        Properties[PropertyEnum.Endurance, manaType] = endurance;
                    }

                    break;

                case PropertyEnum.SecondaryResource:
                    if (this is not Avatar)
                        return;

                    if (delta > 0f && Properties[PropertyEnum.DisableSecondaryResourceGain])
                        return;

                    float secondaryResource = Properties[PropertyEnum.SecondaryResource];
                    secondaryResource += delta;
                    secondaryResource = Math.Clamp(secondaryResource, 0f, Properties[PropertyEnum.SecondaryResourceMax]);

                    Properties[PropertyEnum.SecondaryResource] = secondaryResource;

                    break;
            }
        }

        public float GetDamageAccumulationLimit(PropertyCollection conditionProperties, DamageType damageType)
        {
            float damageAccumulationLimit = conditionProperties[PropertyEnum.DamageAccumulationLimit, damageType];

            if (damageAccumulationLimit > 0f)
                damageAccumulationLimit += Properties[PropertyEnum.DamageAccumulationLimitBonus];

            PropertyInfoTable propertyInfoTable = GameDatabase.PropertyInfoTable;
            foreach (var kvp in conditionProperties.IteratePropertyRange(PropertyEnum.DamageShieldScaleByStat))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId propertyInfoProtoRef);
                if (propertyInfoProtoRef == PrototypeId.Invalid)
                    continue;

                PropertyEnum scaleByProperty = propertyInfoTable.GetPropertyEnumFromPrototype(propertyInfoProtoRef);
                float scaleByValue = Properties[scaleByProperty];
                damageAccumulationLimit += scaleByValue * kvp.Value;
            }

            float damageAccumScaleByPlayers = conditionProperties[PropertyEnum.DamageAccumScaleByPlayers];
            if (damageAccumScaleByPlayers > 0f)
                damageAccumulationLimit *= damageAccumScaleByPlayers;

            return damageAccumulationLimit;
        }

        public void TriggerEntityActionEventAlly(EntitySelectorActionEventType eventType)
        {
            if (SpawnGroup == null) return;
            foreach (var spec in SpawnGroup.Specs)
                spec.ActiveEntity?.TriggerEntityActionEvent(eventType);
        }

        public virtual void OnGotHit(WorldEntity attacker)
        {
            TriggerEntityActionEvent(EntitySelectorActionEventType.OnGotAttacked);
            TriggerEntityActionEventAlly(EntitySelectorActionEventType.OnAllyGotAttacked);
            if (attacker != null && attacker.GetMostResponsiblePowerUser<Avatar>() != null)
            {
                TriggerEntityActionEvent(EntitySelectorActionEventType.OnGotAttackedByPlayer);
                TriggerEntityActionEventAlly(EntitySelectorActionEventType.OnAllyGotAttackedByPlayer);
            }
        }

        public string PowerCollectionToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Powers:");
            foreach (var kvp in _powerCollection)
                sb.AppendLine($" {GameDatabase.GetFormattedPrototypeName(kvp.Value.PowerPrototypeRef)}");
            return sb.ToString();
        }

        public string ConditionCollectionToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Conditions:");
            foreach (var condition in _conditionCollection)
                sb.AppendLine($" {GameDatabase.GetFormattedPrototypeName(condition.CreatorPowerPrototypeRef)}");
            return sb.ToString();
        }

        protected virtual PowerUseResult ActivatePower(Power power, ref PowerActivationSettings settings)
        {
            return power.Activate(ref settings);
        }

        protected PowerUseResult ActivateProcPower(Power procPower, ref PowerActivationSettings settings, WorldEntity activator, bool interruptActivePower = false)
        {
            if (IsSimulated == false)
                return PowerUseResult.OwnerNotSimulated;

            PrototypeId procPowerProtoRef = procPower.PrototypeDataRef;
            //Logger.Debug($"ActivateProcPower(): {procPowerProtoRef.GetName()} on [{this}]");

            // Apply target override if there is one
            ulong procTargetOverrideId = activator.Properties[PropertyEnum.ProcTargetOverride, procPowerProtoRef];
            if (procTargetOverrideId != InvalidId)
            {
                WorldEntity procTargetOverride = Game.EntityManager.GetEntity<WorldEntity>(procTargetOverrideId);
                if (procTargetOverride != null && procTargetOverride.IsInWorld)
                {
                    settings.TargetEntityId = procTargetOverride.Id;
                    settings.TargetPosition = procTargetOverride.RegionLocation.Position;
                }
            }

            // Find the target
            WorldEntity target = null;
            switch (procPower.GetTargetingShape())
            {
                case TargetingShapeType.Self:
                    target = procPower.Owner;
                    break;

                case TargetingShapeType.SingleTargetRandom:
                    target = procPower.GetRandomTarget();
                    if (target != null)
                    {
                        settings.TargetEntityId = target.Id;
                        settings.TargetPosition = target.RegionLocation.Position;
                    }
                    break;

                default:
                    target = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);
                    break;
            }

            // Pre-validate activation
            PowerUseResult result = procPower.CanActivate(target, settings.TargetPosition, settings.Flags);
            if (result != PowerUseResult.Success)
                return result;

            // Interrupt the current power if requested
            if (interruptActivePower && procPower.IsExclusiveActivation())
            {
                Power activePower = GetActivePower();
                activePower?.EndPower(EndPowerFlags.ExplicitCancel | EndPowerFlags.Interrupting);
            }

            // Set index properties on the proc power
            procPower.Properties[PropertyEnum.PowerRank] = Properties.HasProperty(PropertyEnum.PowerRank)
                ? Properties[PropertyEnum.PowerRank]
                : activator.Properties[PropertyEnum.ProcPowerRank, procPowerProtoRef];

            procPower.Properties[PropertyEnum.CharacterLevel] = CharacterLevel;
            procPower.Properties[PropertyEnum.CombatLevel] = CombatLevel;
            procPower.Properties[PropertyEnum.ItemLevel] = Properties[PropertyEnum.ProcPowerItemLevel, procPowerProtoRef];
            procPower.Properties[PropertyEnum.ItemVariation] = Properties[PropertyEnum.ProcPowerItemVariation, procPowerProtoRef];
            procPower.Properties[PropertyEnum.InventoryStackCount] = Properties[PropertyEnum.ProcPowerInvStackCount, procPowerProtoRef];
            procPower.Properties[PropertyEnum.SpawnGroupId] = Properties[PropertyEnum.SpawnGroupId];

            // Set additional settings.
            // NOTE: TriggeringPowerRef is applied to the owner by conditions that also grant the procs.
            settings.TriggeringPowerRef = Properties[PropertyEnum.TriggeringPowerRef, procPowerProtoRef];
            settings.FXRandomSeed = Game.Random.Next(1, 10000);

            // Activate the proc power
            result = procPower.Activate(ref settings);
            if (result == PowerUseResult.Success && settings.Flags.HasFlag(PowerActivationSettingsFlags.NoOnPowerUseProcs) == false)
                TryActivateOnPowerUseProcs(ProcTriggerType.OnPowerUseProcEffect, procPower, ref settings);

            return result;
        }

        private Power GetActivePower()
        {
            if (ActivePowerRef != PrototypeId.Invalid)
                return PowerCollection?.GetPower(ActivePowerRef);
            return null;
        }

        private bool IsTargetableInternal()
        {
            if (IsAffectedByPowersInternal() == false) return false;
            if (IsUntargetable) return false;
            if (Alliance == null) return false;
            return true;
        }

        private bool IsAffectedByPowersInternal()
        {
            if (IsNeverAffectedByPowers
                || IsInWorld == false || IsSimulated == false
                || IsDormant || IsUnaffectable || IsHotspot) return false;
            return true;
        }

        #endregion

        #region Combat State

        public virtual bool EnterCombat()
        {
            // Overriden in Agent
            return true;
        }

        #endregion

        #region Stats

        public int GetDynamicCombatLevel(int levelToScaleTo)
        {
            Region region = Region;
            if (region == null)
                return levelToScaleTo;

            Area area = Area;
            if (area == null)
                return levelToScaleTo;

            // Do not allow scaling below the area level
            int areaLevel = region.GetAreaLevel(area);
            return Math.Max(areaLevel, levelToScaleTo);
        }

        public RankPrototype GetRankPrototype()
        {
            var rankRef = Properties[PropertyEnum.Rank];
            if (rankRef != PrototypeId.Invalid)
            {
                var rankProto = GameDatabase.GetPrototype<RankPrototype>(rankRef);
                if (rankProto == null) return null;
                return rankProto;
            }
            else
            {
                var worldEntityProto = WorldEntityPrototype;
                if (worldEntityProto == null) return null;
                return GameDatabase.GetPrototype<RankPrototype>(worldEntityProto.Rank);
            }
        }

        public float GetDefenseRating(DamageType damageType)
        {
            float defense = Properties[PropertyEnum.Defense, damageType];
            float defenseMult = 1f + Properties[PropertyEnum.DefenseChangePercent, damageType];

            if (damageType != DamageType.Any)
            {
                defense += Properties[PropertyEnum.Defense, DamageType.Any];
                defenseMult += Properties[PropertyEnum.DefenseChangePercent, DamageType.Any];
            }

            return Math.Max(0f, defense * defenseMult);
        }

        public float GetDamageReductionPct(float defenseRating, PropertyCollection attackerProperties, PowerPrototype powerProto)
        {
            EvalPrototype evalProto = GameDatabase.CombatGlobalsPrototype.EvalDamageReduction;
            if (evalProto == null) return Logger.WarnReturn(0f, "GetDamageReductionPct(): evalProto == null");

            return GetDamageReductionPct(Properties, evalProto, defenseRating, attackerProperties, powerProto);
        }

        private static float GetDamageReductionPct(PropertyCollection targetProperties, EvalPrototype evalProto, float defenseRating,
            PropertyCollection attackerProperties, PowerPrototype powerProto)
        {
            // Block / dodge chances also provide damage reduction
            float blockChance = Power.GetBlockChance(powerProto, attackerProperties, targetProperties, InvalidId);
            float dodgeChance = Power.GetDodgeChance(powerProto, attackerProperties, targetProperties, InvalidId);

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, targetProperties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, attackerProperties);
            evalContext.SetVar_Float(EvalContext.Var1, defenseRating);

            // The eval truncates values, so we need to multiply by 100 to save the decimal part for block / dodge chances
            evalContext.SetVar_Int(EvalContext.Var2, MathHelper.RoundToInt(blockChance * 100f));
            evalContext.SetVar_Int(EvalContext.Var3, MathHelper.RoundToInt(dodgeChance * 100f));

            return Eval.RunFloat(evalProto, evalContext);
        }

        public float GetDamageRating(DamageType damageType = DamageType.Any)
        {
            CombatGlobalsPrototype combatGlobals = GameDatabase.CombatGlobalsPrototype;
            if (combatGlobals == null) return Logger.WarnReturn(0f, "GetDamageRating(): combatGlobal == null");

            float damageRating = Properties[PropertyEnum.DamageRating];
            damageRating += Properties[PropertyEnum.DamageRatingBonusHardcore] * combatGlobals.GetHardcoreAttenuationFactor(Properties);
            damageRating += Properties[PropertyEnum.DamageRatingBonusMvmtSpeed] * MathF.Max(0f, BonusMovementSpeed);

            if (damageType != DamageType.Any)
                damageRating += Properties[PropertyEnum.DamageRatingBonusByType, (int)damageType];

            return damageRating;
        }

        public float GetCastSpeedPct(PowerPrototype powerProto)
        {
            float castSpeedPct = (float)Properties[PropertyEnum.CastSpeedIncrPct] - (float)Properties[PropertyEnum.CastSpeedDecrPct];
            float castSpeedMult = Properties[PropertyEnum.CastSpeedMult];

            if (powerProto != null)
            {
                // Apply power-specific multiplier
                castSpeedMult += Properties[PropertyEnum.CastSpeedMultPower, powerProto.DataRef];

                // Apply keyword bonuses
                foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.CastSpeedIncrPctKwd))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId keywordRef);
                    if (powerProto.HasKeyword(keywordRef.As<KeywordPrototype>()))
                        castSpeedPct += kvp.Value;
                }

                foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.CastSpeedMultKwd))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId keywordRef);
                    if (powerProto.HasKeyword(keywordRef.As<KeywordPrototype>()))
                        castSpeedMult += kvp.Value;
                }

                // Apply tab bonuses for avatars
                if (Prototype is AvatarPrototype avatarProto)
                {
                    PrototypeId powerProgTableRef = avatarProto.GetPowerProgressionTableTabRefForPower(powerProto.DataRef);
                    if (powerProgTableRef != PrototypeId.Invalid)
                        castSpeedPct += Properties[PropertyEnum.CastSpeedIncrPctTab, powerProgTableRef, avatarProto.DataRef];
                }
            }

            // Cast speed is capped at 50000%
            castSpeedPct = MathF.Min(castSpeedPct, 500f);

            // Diminishing returns?
            if (castSpeedPct > 0f)
            {
                float pow = MathF.Pow(2.718f, -3f * castSpeedPct);
                castSpeedPct = MathF.Min(castSpeedPct, 0.4f - (0.4f * pow));
            }

            // Apply multiplier
            castSpeedPct = (1f + castSpeedPct) * (1f + castSpeedMult);

            // Cast speed can't go below 50%
            castSpeedPct = MathF.Max(castSpeedPct, 0.5f);

            return castSpeedPct;
        }

        public float GetEnduranceCostMultiplier(ManaType manaType, PowerPrototype powerProto, bool canSkipCost)
        {
            // NOTE: CombatGlobalsPrototype.EnduranceCostChangePctMin is 0f, which is what prevents the multiplier from going negative.
            CombatGlobalsPrototype combatGlobals = GameDatabase.CombatGlobalsPrototype;
            if (combatGlobals == null) return Logger.WarnReturn(1f, "GetEnduranceCostMultiplier(): combatGlobals == null");

            // Check for endurance cost skips
            if (canSkipCost && Properties[PropertyEnum.NoEnduranceCosts, manaType])
                return 0f;

            // Check for overrides
            // NOTE: The default value for EnduranceCostChangePctOverride is -1f, which indicates no override
            // Mana type specific override takes priority over the global one
            float enduranceCostChangePctOverride = Properties[PropertyEnum.EnduranceCostChangePctOverride, manaType];

            if (enduranceCostChangePctOverride < 0f)
                enduranceCostChangePctOverride = Properties[PropertyEnum.EnduranceCostChangePctOverride, ManaType.TypeAll];

            if (enduranceCostChangePctOverride >= 0f)
                return MathF.Max(1f + enduranceCostChangePctOverride, combatGlobals.EnduranceCostChangePctMin);

            // Accumulate modifiers
            float multiplier = 1f;

            if (manaType != ManaType.TypeAll)
                multiplier += Properties[PropertyEnum.EnduranceCostChangePct, manaType];

            multiplier += Properties[PropertyEnum.EnduranceCostChangePct, ManaType.TypeAll];

            if (powerProto != null)
            {
                // Keyword modifiers
                if (manaType != ManaType.TypeAll)
                    Power.AccumulateKeywordProperties(ref multiplier, powerProto, Properties, Properties, PropertyEnum.EnduranceCostChangePctKeywrd, (int)manaType);

                Power.AccumulateKeywordProperties(ref multiplier, powerProto, Properties, Properties, PropertyEnum.EnduranceCostChangePctKeywrd, (int)ManaType.TypeAll);

                // Apply tab modifiers for avatars
                if (Prototype is AvatarPrototype avatarProto)
                {
                    PrototypeId powerProgTableRef = avatarProto.GetPowerProgressionTableTabRefForPower(powerProto.DataRef);
                    if (powerProgTableRef != PrototypeId.Invalid)
                        multiplier += Properties[PropertyEnum.EnduranceCostChangePctTab, powerProgTableRef, avatarProto.DataRef];
                }
            }

            return MathF.Max(multiplier, combatGlobals.EnduranceCostChangePctMin);
        }

        #endregion

        #region Mods

        protected bool ModChangeModEffects(PrototypeId modRef, int rank)
        {
            if (IsInWorld == false) return Logger.WarnReturn(false, "ModChangeModEffects(): IsInWorld == false");
            if (modRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "ModChangeModEffects(): modRef == PrototypeId.Invalid");

            ModPrototype modProto = GameDatabase.GetPrototype<ModPrototype>(modRef);
            if (modProto == null) return Logger.WarnReturn(false, "ModChangeModEffects(): modProto == null");

            if (modProto.Type == PrototypeId.Invalid) return Logger.WarnReturn(false, "modProto.Type == PrototypeId.Invalid");

            // Attached properties
            if ((modProto.Properties != null && modProto.Properties.IsEmpty == false) || modProto.EvalOnCreate.HasValue() || modProto.PropertiesForTooltips.HasValue())
            {
                if (rank > 0)
                {
                    using PropertyCollection indexProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
                    indexProperties[PropertyEnum.CharacterLevel] = CharacterLevel;
                    indexProperties[PropertyEnum.CombatLevel] = CombatLevel;
                    indexProperties.CopyProperty(Properties, PropertyEnum.ItemLevel);

                    if (modProto is InfinityGemBonusPrototype)
                        indexProperties.CopyPropertyRange(Properties, PropertyEnum.InfinityGemBonusRank);
                    else if (modProto is OmegaBonusPrototype)
                        indexProperties.CopyPropertyRange(Properties, PropertyEnum.OmegaRank);

                    AttachProperties(modProto.Type, modRef, 0, modProto.Properties, indexProperties, rank, true);
                }
                else
                {
                    DetachProperties(modProto.Type, modRef, 0);
                }
            }

            // Passive powers
            if (modProto.PassivePowers.HasValue())
            {
                if (rank > 0)
                {
                    PowerIndexProperties indexProps = new(0, CharacterLevel, CombatLevel);

                    foreach (PrototypeId powerProtoRef in modProto.PassivePowers)
                    {
                        // Unassign power if it's already there
                        UnassignPower(powerProtoRef);

                        if (AssignPower(powerProtoRef, indexProps) == null)
                            Logger.Warn($"ModChangeModEffects(): Failed to assign passive power {powerProtoRef.GetName()} for mod {modProto}");
                    }
                }
                else
                {
                    // Infinity is a piece of crap codewise, and ranks in different nodes can assign/unassign the same synergy passive,
                    // so we can't verify removal here, because full respec can unassign the same synergy passive multiple times.
                    // Also removing all points from one of the nodes can remove the passive that is also granted by other nodes.
                    foreach (PrototypeId powerProtoRef in modProto.PassivePowers)
                        UnassignPower(powerProtoRef);
                }
            }

            return true;
        }

        public void TwinEnemyBoost(Cell cell)
        {
            var popGlobals = GameDatabase.PopulationGlobalsPrototype;
            // TODO share damage with twin enemy
            // PropertyEnum.DamageTransferID
            // popGlobals.TwinEnemyCondition
            Properties[PropertyEnum.EnemyBoost, popGlobals.TwinEnemyBoost] = true;
        }

        #endregion

        #region Alliances

        public bool IsFriendlyTo(WorldEntity other, AlliancePrototype allianceProto = null)
        {
            if (other == null) return false;
            return IsFriendlyTo(other.Alliance, allianceProto);
        }

        public bool IsFriendlyTo(AlliancePrototype otherAllianceProto, AlliancePrototype allianceOverrideProto = null)
        {
            if (otherAllianceProto == null) return false;
            AlliancePrototype thisAllianceProto = allianceOverrideProto ?? Alliance;
            if (thisAllianceProto == null) return false;
            return thisAllianceProto.IsFriendlyTo(otherAllianceProto) && !thisAllianceProto.IsHostileTo(otherAllianceProto);
        }

        public bool IsHostileToPlayers()
        {
            var globalProto = GameDatabase.GlobalsPrototype;
            if (globalProto == null) return false;
            return IsHostileTo(globalProto.PlayerAlliancePrototype);
        }

        public bool IsHostileTo(AlliancePrototype otherAllianceProto, AlliancePrototype allianceOverrideProto = null)
        {
            if (otherAllianceProto == null) return false;
            AlliancePrototype thisAllianceProto = allianceOverrideProto ?? Alliance;
            if (thisAllianceProto == null) return false;
            return thisAllianceProto.IsHostileTo(otherAllianceProto);
        }

        public bool IsHostileTo(WorldEntity other, AlliancePrototype allianceOverride = null)
        {
            if (other == null) return false;

            if (this is not Avatar && IsMissionCrossEncounterHostilityOk == false)
            {
                bool isPlayer = false;
                if (HasPowerUserOverride)
                {
                    var userId = Properties[PropertyEnum.PowerUserOverrideID];
                    if (userId != InvalidId)
                    {
                        var user = Game.EntityManager.GetEntity<Entity>(userId);
                        if (user?.GetOwnerOfType<Player>() != null)
                            isPlayer = true;
                    }
                }

                if (isPlayer == false
                    && IgnoreMissionOwnerForTargeting == false
                    && HasMissionPrototype && other.HasMissionPrototype
                    && (PrototypeId)Properties[PropertyEnum.MissionPrototype] != (PrototypeId)other.Properties[PropertyEnum.MissionPrototype])
                    return false;
            }

            return IsHostileTo(other.Alliance, allianceOverride);
        }

        private void OnAllianceChanged(PrototypeId allianceRef)
        {
            if (allianceRef != PrototypeId.Invalid)
            {
                var allianceProto = GameDatabase.GetPrototype<AlliancePrototype>(allianceRef);
                if (allianceProto != null)
                    _allianceProto = allianceProto;
            }
            else
            {
                var worldEntityProto = WorldEntityPrototype;
                if (worldEntityProto != null)
                    _allianceProto = GameDatabase.GetPrototype<AlliancePrototype>(worldEntityProto.Alliance);
            }
        }

        private AlliancePrototype GetAlliance()
        {
            if (_allianceProto == null) return null;

            PrototypeId allianceRef = _allianceProto.DataRef;
            if (IsControlledEntity && _allianceProto.WhileControlled != PrototypeId.Invalid)
                allianceRef = _allianceProto.WhileControlled;
            if (IsConfused && _allianceProto.WhileConfused != PrototypeId.Invalid)
                allianceRef = _allianceProto.WhileConfused;

            return GameDatabase.GetPrototype<AlliancePrototype>(allianceRef);
        }

        #endregion

        #region Interaction

        public virtual bool InInteractRange(WorldEntity interactee, InteractionMethod interaction, bool interactFallbackRange = false)
        {
            if (IsSingleInteraction(interaction) == false && interaction.HasFlag(InteractionMethod.Throw)) return false;

            if (IsInWorld == false || interactee.IsInWorld == false) return false;

            float checkRange;
            float interactRange = InteractRange;

            if (interaction == InteractionMethod.Throw)
                if (Prototype is AgentPrototype agentProto) interactRange = agentProto.InteractRangeThrow;

            var worldEntityProto = WorldEntityPrototype;
            if (worldEntityProto == null) return false;

            var interacteeWorldEntityProto = interactee.WorldEntityPrototype;
            if (interacteeWorldEntityProto == null) return false;

            if (interacteeWorldEntityProto.InteractIgnoreBoundsForDistance == false)
                checkRange = Bounds.Radius + interactee.Bounds.Radius + interactRange + worldEntityProto.InteractRangeBonus + interacteeWorldEntityProto.InteractRangeBonus;
            else
                checkRange = interactRange;

            if (checkRange <= 0f) return false;

            if (interactFallbackRange)
                checkRange += InteractFallbackRange;

            float checkRangeSq = checkRange * checkRange;
            float rangeSq = Vector3.DistanceSquared2D(interactee.RegionLocation.Position, RegionLocation.Position);

            return rangeSq <= checkRangeSq;
        }

        public static bool IsSingleInteraction(InteractionMethod interaction)
        {
            return interaction != InteractionMethod.None; // IO::BitfieldHasSingleBitSet
        }

        public virtual InteractionResult AttemptInteractionBy(EntityDesc interactorDesc, InteractionFlags flags, InteractionMethod method)
        {
            var interactor = interactorDesc.GetEntity<Agent>(Game);
            if (interactor == null || interactor.IsInWorld == false) return InteractionResult.Failure;
            InteractData data = null;
            InteractionMethod iteractionStatus = InteractionManager.CallGetInteractionStatus(new EntityDesc(this), interactor, InteractionOptimizationFlags.None, flags, ref data);
            iteractionStatus &= method;

            switch (iteractionStatus)
            {
                case InteractionMethod.None:
                    return InteractionResult.Failure;

                // case InteractionMethod.Attack: // client only
                //    if (interactor.StartDefaultAttack(flags.HaveFlag(InteractionFlags.StopMove) == false))
                //        return InteractionResult.Success;
                //    else
                //        return InteractionResult.AttackFail; 

                case InteractionMethod.Throw: // server

                    if (interactor.InInteractRange(this, InteractionMethod.Throw) == false)
                        return InteractionResult.OutOfRange;
                    if (interactor.IsExecutingPower)
                        return InteractionResult.ExecutingPower;
                    if (interactor.StartThrowing(Id))
                        return InteractionResult.Success;

                    break;

                    // case InteractionMethod.Converse: // client only
                    // case InteractionMethod.Use:
                    //    return PostAttemptInteractionBy(interactor, iteractionStatus) 
            }

            return InteractionResult.Failure;
        }

        public bool IsThrowableBy(WorldEntity thrower)
        {
            if (IsDead || Properties[PropertyEnum.ThrowablePower] == PrototypeId.Invalid) return false;
            if (thrower != null)
                if (thrower.Throwability < Properties[PropertyEnum.Throwability]) return false;
            return true;
        }

        #endregion

        #region Event Handlers

        public override void OnChangePlayerAOI(Player player, InterestTrackOperation operation, AOINetworkPolicyValues newInterestPolicies, AOINetworkPolicyValues previousInterestPolicies, AOINetworkPolicyValues archiveInterestPolicies = AOINetworkPolicyValues.AOIChannelNone)
        {
            base.OnChangePlayerAOI(player, operation, newInterestPolicies, previousInterestPolicies, archiveInterestPolicies);

            AOINetworkPolicyValues lostPolicies = previousInterestPolicies & ~newInterestPolicies;
            AOINetworkPolicyValues gainedPolicies = newInterestPolicies & ~previousInterestPolicies;

            // We need to update our simulation state when we lose proximity because when a player's AOI is cleared,
            // cells are removed before entities, and at that point entities still have the proximity policy.
            //
            // UPDATE 2024-12-22: Actually we can't do this because region location is cleared after OnExitedWorld() is called,
            // so this triggers auto-activated (passive) powers. I hate this codebase so much.
            //
            //if (lostPolicies.HasFlag(AOINetworkPolicyValues.AOIChannelProximity))
                //UpdateSimulationState();

            // Update map location if we gained a policy that allows us to exist outside of proximity (Party / Discovery)
            if ((gainedPolicies & AOINetworkPolicyValues.MapChannels) != 0)
                UpdateMapLocation();
        }

        public virtual void OnEnteredWorld(EntitySettings settings)
        {
            if (CanInfluenceNavigationMesh())
                EnableNavigationInfluence();

            PowerCollection?.OnOwnerEnteredWorld();

            if (IsAttachedToEntity)
            {
                ulong parentId = Properties[PropertyEnum.AttachedToEntityId];
                if (parentId != InvalidId)
                {
                    var parent = Game.EntityManager.GetEntity<WorldEntity>(parentId);
                    if (parent != null && parent.IsInWorld && parent.TestStatus(EntityStatus.ExitingWorld) == false)
                        parent.Physics.AttachChild(Physics);
                }
            }

            var region = Region;

            region.EntityEnteredWorldEvent.Invoke(new(this));

            if (WorldEntityPrototype.DiscoverInRegion)
                region.DiscoverEntity(this, false);

            if (Bounds.CollisionType != BoundsCollisionType.None)
                RegisterForPendingPhysicsResolve();

            AdjustSummonCount(1);

            InitializeProcEffectPowers();

            ConditionCollection?.OnOwnerEnteredWorld();

            UpdateInterestPolicies(true, settings);
            region.EntityTracker.ConsiderForTracking(this);
            UpdateSimulationState();
        }

        public virtual void OnExitedWorld()
        {
            SetSimulated(false);

            Region region = Region;
            if (region != null)
            {
                if (region.EntityTracker != null)
                {
                    region.EntityExitedWorldEvent.Invoke(new(this));
                    region.EntityTracker.RemoveFromTracking(this);
                }

                // Undiscover from region
                if (WorldEntityPrototype.DiscoverInRegion)
                    region.UndiscoverEntity(this, true);
            }

            // Undiscover from players
            if (InterestReferences.IsAnyPlayerInterested(AOINetworkPolicyValues.AOIChannelDiscovery))
            {
                EntityManager entityManager = Game.EntityManager;

                foreach (ulong playerId in InterestReferences)
                {
                    Player player = entityManager.GetEntity<Player>(playerId);

                    if (player == null)
                    {
                        Logger.Warn("OnExitedWorld(): player == null");
                        continue;
                    }

                    player.UndiscoverEntity(this, false);   // Skip interest update for undiscover because we are doing an update below anyway
                }
            }

            ConditionCollection?.OnOwnerExitedWorld();

            StopAllPropertyTickers();

            AdjustSummonCount(-1);
            ClearSummonedInventory();

            PowerCollection?.OnOwnerExitedWorld();

            UpdateInterestPolicies(false);
        }

        public override void OnDeallocate()
        {
            base.OnDeallocate();
            PowerCollection?.OnOwnerDeallocate();
            ConditionCollection?.OnOwnerDeallocate();
        }

        public override void OnLifespanExpired()
        {
            TryActivateOnLifespanExpiredProcs();
            Kill();
        }

        public virtual void OnDramaticEntranceEnd() { }

        public override void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            base.OnPropertyChange(id, newValue, oldValue, flags);
            if (flags.HasFlag(SetPropertyFlags.Refresh)) return;

            switch (id.Enum)
            {
                case PropertyEnum.AllianceOverride:
                    OnAllianceChanged(newValue);
                    break;

                case PropertyEnum.CastSpeedDecrPct:
                case PropertyEnum.CastSpeedIncrPct:
                case PropertyEnum.CastSpeedMult:
                    PowerCollection?.OnOwnerCastSpeedChange(PrototypeId.Invalid);
                    break;

                case PropertyEnum.CastSpeedIncrPctKwd:
                case PropertyEnum.CastSpeedMultKwd:
                    if (PowerCollection != null)
                    {
                        Property.FromParam(id, 0, out PrototypeId powerKeywordRef);

                        if (powerKeywordRef == PrototypeId.Invalid)
                        {
                            Logger.Warn("OnPropertyChange(): powerKeywordRef == PrototypeId.Invalid");
                            break;
                        }

                        PowerCollection.OnOwnerCastSpeedChange(powerKeywordRef);
                    }

                    break;

                case PropertyEnum.CastSpeedIncrPctTab:
                    if (PowerCollection != null)
                    {
                        Property.FromParam(id, 0, out PrototypeId powerTabRef);

                        if (powerTabRef == PrototypeId.Invalid)
                        {
                            Logger.Warn("OnPropertyChange(): powerTabRef == PrototypeId.Invalid");
                            break;
                        }

                        PowerCollection.OnOwnerCastSpeedChange(powerTabRef);
                    }

                    break;

                case PropertyEnum.CastSpeedMultPower:
                    Property.FromParam(id, 0, out PrototypeId powerProtoRef);

                    if (powerProtoRef == PrototypeId.Invalid)
                    {
                        Logger.Warn("OnPropertyChange(): powerProtoRef == PrototypeId.Invalid");
                        break;
                    }

                    GetPower(powerProtoRef)?.OnOwnerCastSpeedChange();

                    break;

                case PropertyEnum.DisablePowerEffects:
                    Property.FromParam(id, 0, out PrototypeId disablePowerRef);
                    if (disablePowerRef == PrototypeId.Invalid)
                    {
                        Logger.Warn("OnPropertyChange(): disablePowerRef == PrototypeId.Invalid");
                        break;
                    }

                    bool enable = newValue == false;    // !doDisable

                    if (ConditionCollection?.EnablePowerConditions(disablePowerRef, enable) == false)
                        Logger.Warn($"OnPropertyChange(): EnablePowerConditions failed to [{(enable ? "enable" : "disable")}] conditions of creatorPower=[{disablePowerRef.GetName()}] on owner=[{this}]");

                    break;

                case PropertyEnum.Endurance:
                    if (IsInWorld)
                    {
                        Property.FromParam(id, 0, out int manaTypeValue);
                        TryActivateOnEnduranceProcs((ManaType)manaTypeValue);
                    }
                    break;

                case PropertyEnum.EnemyBoost:
                    if (IsSimulated)
                    {
                        // If this entity is not currently being simulated, this will be done in SetSimulated
                        Property.FromParam(id, 0, out PrototypeId modProtoRef);
                        if (modProtoRef == PrototypeId.Invalid)
                        {
                            Logger.Warn("OnPropertyChange(): modProtoRef == PrototypeId.Invalid");
                            return;
                        }

                        ModChangeModEffects(modProtoRef, newValue);
                    }

                    break;

                case PropertyEnum.Health:
                    if (IsInWorld && TestStatus(EntityStatus.EnteringWorld) == false)
                        TryActivateOnHealthProcs();

                    break;

                case PropertyEnum.HealthMax:
                    Properties[PropertyEnum.HealthMaxOther] = newValue;

                    // Scale current health
                    long health = Properties[PropertyEnum.Health];
                    if (health > 0 && flags.HasFlag(SetPropertyFlags.Deserialized) == false)
                    {
                        long oldHealthMax = oldValue;
                        float ratio = Math.Min(MathHelper.Ratio(health, oldHealthMax), 1f);
                        long newHealth = Math.Max(MathHelper.RoundToInt64((long)newValue * ratio), 1);  // Do not allow health to go to 0 here

                        Properties[PropertyEnum.Health] = newHealth;
                    }

                    break;

                case PropertyEnum.MissileBlockingHotspot:
                    if (IsHotspot)
                        SetFlag(EntityFlags.IsCollidableHotspot, newValue);
                    break;

                case PropertyEnum.MissionPrototype:
                    if (IsInWorld && Region != null && Region.EntityTracker != null)
                    {
                        PrototypeId missionRef = newValue;
                        bool isTracked = IsTrackedByContext(missionRef);
                        if ((missionRef == PrototypeId.Invalid && isTracked) || (missionRef != PrototypeId.Invalid && !isTracked))
                            Region.EntityTracker.ConsiderForTracking(this);
                    }
                    break;

                case PropertyEnum.NegateHotspots:

                    ScheduleNegateHotspots(newValue);
                    break;

                case PropertyEnum.AttachedToEntityId:

                    ulong oldParentId = oldValue;
                    ulong newParentId = newValue;
                    SetFlag(EntityFlags.AttachedToEntityId, newParentId != InvalidId);

                    var manager = Game.EntityManager;
                    if (oldParentId != InvalidId)
                    {
                        var oldParent = manager.GetEntity<WorldEntity>(oldParentId);
                        if (oldParent != null && oldParent.IsInWorld && oldParent.TestStatus(EntityStatus.ExitingWorld) == false)
                            oldParent.Physics.DetachChild(Physics);
                    }

                    if (IsInWorld && TestStatus(EntityStatus.ExitingWorld) == false)
                    {
                        if (newParentId != InvalidId)
                        {
                            var newParent = manager.GetEntity<WorldEntity>(newParentId);
                            if (newParent != null && newParent.IsInWorld && newParent.TestStatus(EntityStatus.ExitingWorld) == false)
                                newParent.Physics.AttachChild(Physics);
                        }
                    }

                    break;

                case PropertyEnum.NoEntityCollide:
                    SetFlag(EntityFlags.NoCollide, newValue);
                    bool canInfluence = CanInfluenceNavigationMesh();
                    if (canInfluence ^ HasNavigationInfluence)
                    {
                        if (canInfluence)
                            EnableNavigationInfluence();
                        else
                            DisableNavigationInfluence();
                    }
                    break;

                case PropertyEnum.NoEntityCollideException:
                    SetFlag(EntityFlags.HasNoCollideException, newValue != InvalidId);
                    break;

                case PropertyEnum.Intangible:
                    SetFlag(EntityFlags.Intangible, newValue);
                    canInfluence = CanInfluenceNavigationMesh();
                    if (canInfluence ^ HasNavigationInfluence)
                    {
                        if (canInfluence)
                            EnableNavigationInfluence();
                        else
                            DisableNavigationInfluence();
                    }
                    break;

                case PropertyEnum.Rank:
                    if (IsSimulated)
                    {
                        // If this entity is not currently being simulated, this will be done in SetSimulated
                        PrototypeId modTypeRef = GameDatabase.GlobalsPrototype.ModGlobals.RankModType;
                        ClearAttachedPropertiesOfType(modTypeRef);

                        PrototypeId newRankProtoRef = newValue;
                        PrototypeId oldRankProtoRef = oldValue;

                        if (newRankProtoRef != PrototypeId.Invalid)
                            ModChangeModEffects(newRankProtoRef, 1);
                        else
                            ModChangeModEffects(oldRankProtoRef, 0);
                    }

                    break;

                case PropertyEnum.SkillshotReflectChancePct:
                    if (IsHotspot)
                        SetFlag(EntityFlags.IsReflectingHotspot, newValue);
                    break;
            }
        }

        public virtual void OnCellChanged(RegionLocation oldLocation, RegionLocation newLocation, ChangePositionFlags flags)
        {
            Cell oldCell = oldLocation.Cell;
            Cell newCell = newLocation.Cell;

            if (newCell != null)
                Properties[PropertyEnum.MapCellId] = newCell.Id;

            // Simulation updates for entering world happen in OnEnteredWorld()
            if (flags.HasFlag(ChangePositionFlags.EnterWorld) == false)
                UpdateSimulationState();

            // TODO other events
        }

        public virtual void OnAreaChanged(RegionLocation oldLocation, RegionLocation newLocation)
        {
            Area oldArea = oldLocation.Area;
            Area newArea = newLocation.Area;
            if (oldArea == newArea) return;

            oldArea?.Region.EntityLeftAreaEvent.Invoke(new(this, oldArea));

            if (newArea != null)
            {
                newArea.Region.EntityEnteredAreaEvent.Invoke(new(this, newArea));

                if (this is Avatar avatar)
                {
                    var player = avatar.GetOwnerOfType<Player>();
                    player?.OnScoringEvent(new(ScoringEventType.AreaEnter, newArea.Prototype));
                }

                Properties[PropertyEnum.MapAreaId] = newArea.Id;
                Properties[PropertyEnum.ContextAreaRef] = newArea.PrototypeDataRef;
            }
        }

        public virtual void OnRegionChanged(Region oldRegion, Region newRegion)
        {
            if (oldRegion == newRegion)
                return;

            if (newRegion != null)
                ApplyLootTableSourceOverrides(newRegion);

            Properties[PropertyEnum.MapRegionId] = newRegion != null ? newRegion.Id : 0;
        }

        public virtual void OnLocomotionStateChanged(LocomotionState oldLocomotionState, LocomotionState newLocomotionState)
        {
            if (IsInWorld == false) return;

            // Check if locomotion state requires updating
            LocomotionState.CompareLocomotionStatesForSync(oldLocomotionState, newLocomotionState,
                out bool syncRequired, out bool pathNodeSyncRequired, newLocomotionState.FollowEntityId != InvalidId);

            if (syncRequired == false && pathNodeSyncRequired == false) return;

            // Send locomotion update to interested clients
            // NOTE: Avatars are locomoted on their local client independently, so they are excluded from locomotion updates.
            PlayerConnectionManager networkManager = Game.NetworkManager;
            List<PlayerConnection> interestedClientList = ListPool<PlayerConnection>.Instance.Get();
            if (networkManager.GetInterestedClients(interestedClientList, this, AOINetworkPolicyValues.AOIChannelProximity, IsMovementAuthoritative == false))
            {
                NetMessageLocomotionStateUpdate locomotionStateUpdateMessage = ArchiveMessageBuilder.BuildLocomotionStateUpdateMessage(
                    this, oldLocomotionState, newLocomotionState, pathNodeSyncRequired);

                networkManager.SendMessageToMultiple(interestedClientList, locomotionStateUpdateMessage);
            }

            ListPool<PlayerConnection>.Instance.Return(interestedClientList);
        }

        public virtual void OnPreGeneratePath(Vector3 start, Vector3 end, List<WorldEntity> entities) { }

        public override void OnPostAOIAddOrRemove(Player player, InterestTrackOperation operation,
            AOINetworkPolicyValues newInterestPolicies, AOINetworkPolicyValues previousInterestPolicies)
        {
            base.OnPostAOIAddOrRemove(player, operation, newInterestPolicies, previousInterestPolicies);

            if (IsInWorld == false) return;

            AOINetworkPolicyValues gainedPolicies = newInterestPolicies & ~previousInterestPolicies;

            if (gainedPolicies.HasFlag(AOINetworkPolicyValues.AOIChannelProximity))
            {
                // Send our entire power collection when we gain proximity and enter game world on the client
                // (the client needs to already be aware of us through ownership or some other channel)
                if (previousInterestPolicies != AOINetworkPolicyValues.AOIChannelNone)
                    PowerCollection?.SendEntireCollection(player);

                // Mark as discovered by the player if needed
                if (IsDiscoverable && operation == InterestTrackOperation.Add && WorldEntityPrototype.ObjectiveInfo?.TrackAfterDiscovery == true)
                    player.DiscoverEntity(this, true);
            }
        }

        public virtual bool OnPowerAssigned(Power power) { return true; }
        public virtual bool OnPowerUnassigned(Power power) { return true; }
        public virtual void OnPowerEnded(Power power, EndPowerFlags flags) { }
        public virtual void OnConditionRemoved(Condition condition) { }

        public virtual bool OnNegativeStatusEffectApplied(ulong conditionId)
        {
            TryActivateOnNegStatusAppliedProcs();
            return true;
        }

        public virtual void OnOverlapBegin(WorldEntity whom, Vector3 whoPos, Vector3 whomPos)
        {
            if (whom is not Hotspot hotspot) return;
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.NegateHotspots))
            {
                Property.FromParam(kvp.Key, 0, out int type);
                var allianceType = (HotspotNegateByAllianceType)type;
                if (allianceType == HotspotNegateByAllianceType.None) continue;

                Property.FromParam(kvp.Key, 1, out PrototypeId keywordRef);
                if (keywordRef == PrototypeId.Invalid) continue;

                Property.FromParam(kvp.Key, 2, out int users);

                hotspot.OnHotspotNegated(this, allianceType, keywordRef, users);
            }
        }

        private void OnNegateHotspots()
        {
            var manager = Game?.EntityManager;
            if (manager == null) return;

            List<ulong> overlappingEntities = ListPool<ulong>.Instance.Get();
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.NegateHotspots))
            {
                Property.FromParam(kvp.Key, 0, out int type);
                var allianceType = (HotspotNegateByAllianceType)type;
                if (allianceType == HotspotNegateByAllianceType.None) continue;

                Property.FromParam(kvp.Key, 1, out PrototypeId keywordRef);
                if (keywordRef == PrototypeId.Invalid) continue;

                Property.FromParam(kvp.Key, 2, out int users);

                overlappingEntities.Clear();
                if (Physics.GetOverlappingEntities(overlappingEntities))
                    foreach (ulong entityId in overlappingEntities)
                    {
                        var hotspot = manager.GetEntity<Hotspot>(entityId);
                        if (hotspot == null) continue;
                        hotspot.OnHotspotNegated(this, allianceType, keywordRef, users);
                    }
            }

            ListPool<ulong>.Instance.Return(overlappingEntities);
        }

        private void ScheduleNegateHotspots(bool schedule)
        {
            if (schedule == false)
                Game.GameEventScheduler.CancelEvent(_negateHotspotsEvent);
            else if (_negateHotspotsEvent.IsValid == false)
                ScheduleEntityEvent(_negateHotspotsEvent, TimeSpan.Zero);
        }

        public virtual void OnOverlapEnd(WorldEntity whom) { }
        public virtual void OnCollide(WorldEntity whom, Vector3 whoPos) { }

        public virtual void OnSkillshotReflected(Missile missile)
        {
            TryActivateOnSkillshotReflectProcs();
        }

        protected virtual void OnDamaged(PowerResults powerResults) { }

        #endregion

        #region Rewards

        public bool GetXPAwarded(out long xp, out long minXP, bool applyGlobalTuning)
        {
            return WorldEntityPrototype.GetXPAwarded(CharacterLevel, out xp, out minXP, applyGlobalTuning);
        }

        public bool AwardKillLoot(WorldEntity killer, KillFlags killFlags, WorldEntity directKiller)
        {
            if (this is Missile || this is Hotspot)
                return false;

            if (IsInWorld == false)
                return false;

            List<Player> playerList = ListPool<Player>.Instance.Get();
            // NOTE: Compute nearby players on demand for performance reasons

            // Loot Tables
            if (killFlags.HasFlag(KillFlags.NoLoot) == false && Properties[PropertyEnum.NoLootDrop] == false)
            {
                Power.ComputeNearbyPlayers(Region, RegionLocation.Position, 0, true, playerList);
                // TODO: Manually add faraway mission participants if needed

                // OnKilled loot table is different based on the rank of this entity
                RankPrototype rankProto = GetRankPrototype();
                LootDropEventType lootDropEventType = rankProto.LootTableParam != LootDropEventType.None
                    ? rankProto.LootTableParam
                    : LootDropEventType.OnKilled;

                AwardLootForDropEvent(lootDropEventType, playerList);

                // Bonus Item Find (aka Shield Supply Boost) points
                AwardBonusLoot(playerList);
            }

            // XP
            if (killer is Avatar && killFlags.HasFlag(KillFlags.NoExp) == false && Properties[PropertyEnum.NoExpOnDeath] == false)
            {
                // Compute player count if we haven't done so already for loot tables
                if (playerList.Count == 0)
                    Power.ComputeNearbyPlayers(Region, RegionLocation.Position, 0, true, playerList);

                AwardKillXP(playerList);
            }

            ListPool<Player>.Instance.Return(playerList);
            return true;
        }

        private bool AwardInteractionLoot(ulong interactorEntityId)
        {
            // TODO: Per-player clones for chests, use interactorEntity for this
            WorldEntity interactorEntity = Game.EntityManager.GetEntity<WorldEntity>(interactorEntityId);
            if (interactorEntity == null) return Logger.WarnReturn(false, "AwardInteractionLoot(): interactorEntity == null");

            // NOTE: Bowling ball dispenser is not per-player cloned, so interacting
            // with it will give a ball to all players nearby. This doesn't seem right.
            List<Player> playerList = ListPool<Player>.Instance.Get();
            Power.ComputeNearbyPlayers(Region, RegionLocation.Position, 0, false, playerList);

            AwardLootForDropEvent(LootDropEventType.OnInteractedWith, playerList);

            ListPool<Player>.Instance.Return(playerList);
            return true;
        }

        private bool AwardLootForDropEvent(LootDropEventType eventType, List<Player> playerList)
        {
            const int MaxTables = 8;    // The maximum we've seen in 1.52 prototypes is 4, double this just in case

            // Check if we have any players to award loot to
            if (playerList.Count == 0)
                return true;

            Span<(PrototypeId, LootActionType)> tables = stackalloc (PrototypeId, LootActionType)[MaxTables];
            int numTables = 0;

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.LootTablePrototype, (int)eventType))
            {
                if (numTables >= MaxTables)
                {
                    Logger.Warn($"AwardLootForDropEvent(): Exceeded the maximum number of loot tables in {this}");
                    break;
                }

                Property.FromParam(kvp.Key, 2, out int actionTypeInt);
                LootActionType actionType = (LootActionType)actionTypeInt;

                PrototypeId lootTableProtoRef = kvp.Value;
                if (lootTableProtoRef == PrototypeId.Invalid)
                {
                    Logger.Warn($"AwardLootForDropEvent(): Invalid loot table proto ref for property {kvp.Key} in {this}");
                    continue;
                }

                tables[numTables++] = (lootTableProtoRef, actionType);
            }

            tables = tables[..numTables];

            // Roll and distribute the rewards
            int recipientId = 1;
            foreach (Player player in playerList)
            {
                using LootInputSettings inputSettings = ObjectPoolManager.Instance.Get<LootInputSettings>();
                inputSettings.Initialize(LootContext.Drop, player, this);
                inputSettings.EventType = eventType;
                Game.LootManager.AwardLootFromTables(tables, inputSettings, recipientId++);
            }

            return true;
        }

        private bool AwardKillXP(List<Player> playerList)
        {
            Region region = Region;
            if (region == null) return Logger.WarnReturn(false, "AwardKillXP(): region == null");

            TuningTable tuningTable = region.TuningTable;
            if (tuningTable == null) return Logger.WarnReturn(false, "AwardKillXP(): tuningTable == null");

            foreach (Player player in playerList)
            {
                Avatar avatar = player.CurrentAvatar;
                if (avatar == null)
                {
                    Logger.Warn("AwardKillXP(): avatar == null");
                    continue;
                }

                if (WorldEntityPrototype.GetXPAwarded(avatar.CharacterLevel, out long xp, out long minXP, player.CanUseLiveTuneBonuses()))
                {
                    xp = avatar.ApplyXPModifiers(xp, true, tuningTable);
                    avatar.AwardXP(xp, minXP, Properties[PropertyEnum.ShowXPRewardText]);
                }
            }

            return true;
        }

        private bool HasLootDropEventType(LootDropEventType eventType)
        {
            PropertyList.Iterator iterator = Properties.IteratePropertyRange(PropertyEnum.LootTablePrototype, (int)eventType);
            return iterator.GetEnumerator().MoveNext();
        }

        private bool AwardBonusLoot(List<Player> playerList)
        {
            Region region = Region;
            if (region == null) return Logger.WarnReturn(false, "AwardBonusLoot(): region == null");

            int bonusItemFindMultiplier = region.GetBonusItemFindMultiplier();
            if (bonusItemFindMultiplier <= 0)
                return true;

            RankPrototype rankProto = GetRankPrototype();
            if (rankProto == null || rankProto.BonusItemFindPoints <= 0)
                return true;

            int bonusItemFindPoints = rankProto.BonusItemFindPoints * bonusItemFindMultiplier;

            foreach (Player player in playerList)
            {
                using LootInputSettings settings = ObjectPoolManager.Instance.Get<LootInputSettings>();
                settings.Initialize(LootContext.Drop, player, this);
                player.AwardBonusItemFindPoints(bonusItemFindPoints, settings);
            }

            return true;
        }

        private bool ApplyLootTableSourceOverrides(Region region)
        {
            // See if we have an override source (e.g. this is used primarily for chests with variable rewards in Holo-Sim / X-Defense / Danger Room)
            AssetId lootTableSource = Properties[PropertyEnum.LootTableSource];
            if (lootTableSource == AssetId.Invalid)
                return true;

            WorldEntityPrototype worldEntityProto = WorldEntityPrototype;
            RegionPrototype regionProto = region.Prototype;

            Dictionary<PropertyId, PropertyValue> overrides = DictionaryPool<PropertyId, PropertyValue>.Instance.Get();

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.LootTablePrototype))
            {
                Property.FromParam(kvp.Key, 0, out int lootEventValue);
                LootDropEventType lootEvent = (LootDropEventType)lootEventValue;
                PrototypeId lootTableOverrideRef = PrototypeId.Invalid;

                // Region property overrides (e.g. affixes) take priority over the region prototype

                // Check event-specific events
                AssetId lootEventAsset = Property.PropertyEnumToAsset(PropertyEnum.LootTablePrototype, 0, (int)lootEvent);
                lootTableOverrideRef = region.Properties[PropertyEnum.LootSourceTableOverride, lootTableSource, lootEventAsset];
                if (lootTableOverrideRef != PrototypeId.Invalid)
                {
                    overrides[kvp.Key] = lootTableOverrideRef;
                    continue;
                }

                // Region property overrides for unspecified events (OnKilled / OnInteractedWith)
                if (lootEvent == LootDropEventType.OnKilled || lootEvent == LootDropEventType.OnInteractedWith)
                {
                    lootEventAsset = Property.PropertyEnumToAsset(PropertyEnum.LootTablePrototype, 0, (int)LootDropEventType.None);
                    lootTableOverrideRef = region.Properties[PropertyEnum.LootSourceTableOverride, lootTableSource, lootEventAsset];
                    if (lootTableOverrideRef != PrototypeId.Invalid)
                    {
                        overrides[kvp.Key] = lootTableOverrideRef;
                        continue;
                    }
                }

                // Region prototype overrides
                lootTableOverrideRef = regionProto.GetLootTableOverride(this, lootTableSource, lootEvent);
                if (lootTableOverrideRef != PrototypeId.Invalid)
                {
                    overrides[kvp.Key] = lootTableOverrideRef;
                    continue;
                }

                Logger.Warn($"ApplyLootTableSourceOverrides(): Failed to find override for loot table source {lootTableSource.GetName()} for entity [{this}] in region [{region}]");
            }

            foreach (var kvp in overrides)
                Properties[kvp.Key] = kvp.Value;

            DictionaryPool<PropertyId, PropertyValue>.Instance.Return(overrides);
            return true;
        }

        #endregion

        public virtual AssetId GetEntityWorldAsset()
        {
            // NOTE: Overriden in Agent, Avatar, and Missile
            return GetOriginalWorldAsset();
        }

        public AssetId GetOriginalWorldAsset()
        {
            return GetOriginalWorldAsset(WorldEntityPrototype);
        }

        public static AssetId GetOriginalWorldAsset(WorldEntityPrototype prototype)
        {
            if (prototype == null) return Logger.WarnReturn(AssetId.Invalid, $"GetOriginalWorldAsset(): prototype == null");
            return prototype.UnrealClass;
        }

        public bool IsInTown()
        {
            var region = Region;
            return region != null && region.Behavior == RegionBehavior.Town;
        }

        public virtual bool IsSummonedPet()
        {
            return false;
        }

        public bool IsCloneParent()
        {
            return WorldEntityPrototype.ClonePerPlayer && Properties[PropertyEnum.RestrictedToPlayerGuid] == 0;
        }

        public override bool ApplyState(PrototypeId stateRef)
        {
            if (base.ApplyState(stateRef) == false) return false;

            stateRef = Properties[PropertyEnum.EntityState];
            var entityStateProto = GameDatabase.GetPrototype<EntityStatePrototype>(stateRef);
            if (entityStateProto == null) return false;

            /*  MoloidInvasionCoverTransition NotInGame
            if (entityStateProto is DoorEntityStatePrototype doorStateProto)
            {
                

                var region = Region;
                if (doorStateProto.IsOpen == false) 
                {
                    if (region != null && _naviDoorHandle == null)
                        _naviDoorHandle = region.NaviMesh.CreateDoorEdge(RegionLocation.Position, Vector3.Perp2D(Forward), NaviContentTags.Blocking, 1024.0f);
                }
                else if (_naviDoorHandle != null)
                {
                    if (region != null && _naviDoorHandle != null)
                    {
                        _naviDoorHandle = region.NaviMesh.RemoveDoorEdge(_naviDoorHandle);
                        _naviDoorHandle = null;
                    }
                }
            }*/

            if (entityStateProto.OnActivatePowers.HasValue())
            {
                // Not Used
            }
            return true;
        }

        public override bool ClearState()
        {
            if (base.ClearState() == false) return false;

            PrototypeId stateRef = Properties[PropertyEnum.EntityState];
            var entityStateProto = GameDatabase.GetPrototype<EntityStatePrototype>(stateRef);
            if (entityStateProto == null) return false;

            /*  MoloidInvasionCoverTransition NotInGame
            if (entityStateProto is DoorEntityStatePrototype doorStateProto)
            {
                if (doorStateProto.IsOpen == false)
                { 
                    var region = Region;
                    if (region != null && _naviDoorHandle != null)
                    {
                        _naviDoorHandle = region.NaviMesh.RemoveDoorEdge(_naviDoorHandle);
                        _naviDoorHandle = null;
                    }
                }
            }*/
            return true;
        }

        public void SetTaggedBy(Player player, PowerPrototype powerProto)
        {
            TagPlayers.Add(player, powerProto);
            var group = SpawnGroup;
            if (group != null && group.SpawnerId != InvalidId)
            {
                var spawner = Game.EntityManager.GetEntity<Spawner>(group.SpawnerId);
                spawner?.SetTaggedBy(player, null);
            }
        }

        public override SimulateResult SetSimulated(bool simulated)
        {
            SimulateResult result = base.SetSimulated(simulated);

            if (result != SimulateResult.None && Locomotor != null)
                ModifyCollectionMembership(EntityCollection.Locomotion, IsSimulated);

            if (result == SimulateResult.Set)
            {
                // Apply mods from boosts and rank

                foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.EnemyBoost).ToArray())
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId modProtoRef);
                    if (modProtoRef == PrototypeId.Invalid)
                    {
                        Logger.Warn("SetSimulated(): modProtoRef == PrototypeId.Invalid");
                        continue;
                    }

                    ModChangeModEffects(modProtoRef, kvp.Value);
                }

                if (Properties.HasProperty(PropertyEnum.Rank))
                {
                    PrototypeId modTypeRef = GameDatabase.GlobalsPrototype.ModGlobals.RankModType;
                    ClearAttachedPropertiesOfType(modTypeRef);

                    // NOTE: The client iterates over a range of Rank properties, which is pointless because Rank does not have params.
                    // Also the rank proto ref is never going to be invalid here because we do a HasProperty check above.
                    ModChangeModEffects(Properties[PropertyEnum.Rank], 1);
                }
            }
            else if (result == SimulateResult.Clear)
            {
                EndAllPowers(true);
            }

            if (result != SimulateResult.None)
            {
                if (Region != null)
                {
                    if (simulated)
                        Region.EntitySetSimulatedEvent.Invoke(new(this));
                    else
                        Region.EntitySetUnSimulatedEvent.Invoke(new(this));
                }
                SpawnSpec?.OnUpdateSimulation();

                ConditionCollection?.OnOwnerSimulationStateChanged(simulated);
            }

            return result;
        }

        public void EmergencyRegionCleanup(Region region)
        {
            if (region == Region)
            {
                DisableNavigationInfluence();
                RegionLocation = null;
                SpatialPartitionLocation.Clear();
            }
        }

        public bool IsTrackedByContext(PrototypeId context)
        {
            return _trackingContextMap.ContainsKey(context);
        }

        #region Actions

        public void RegisterActions(List<EntitySelectorActionPrototype> actions)
        {
            if (actions == null) return;
            EntityActionComponent ??= new(this);
            EntityActionComponent.Register(actions);
        }

        public void DestroyEntityActionComponent()
        {
            EntityActionComponent?.Destroy();
            EntityActionComponent = null;
        }

        public ScriptRoleKeyEnum GetScriptRoleKey()
        {
            if (SpawnSpec != null)
                return SpawnSpec.RoleKey;
            else
                return (ScriptRoleKeyEnum)(uint)Properties[PropertyEnum.ScriptRoleKey];
        }

        public bool CanEntityActionTrigger(EntitySelectorActionEventType eventType)
        {
            if (EntityActionComponent != null)
                return EntityActionComponent.CanTrigger(eventType);
            return false;
        }

        public void TriggerEntityActionEvent(EntitySelectorActionEventType actionType)
        {
            if (EntityActionComponent != null)
            {
                // Logger.Trace($"TriggerEntityActionEvent {PrototypeName} {actionType}");
                EntityActionComponent.Trigger(actionType);
            }
        }

        public virtual bool ProcessEntityAction(EntitySelectorActionPrototype action)
        {
            if (IsControlledEntity || EntityActionComponent == null || IsInWorld == false) return false;

            if (action.SpawnerTrigger != PrototypeId.Invalid)
                TriggerLocalSpawner(action.SpawnerTrigger);

            var aiOverride = action.PickAIOverride(Game.Random);
            if (aiOverride != null)
            {
                var powerRef = aiOverride.Power;
                if (powerRef != PrototypeId.Invalid)
                {
                    if (aiOverride.PowerRemove)
                    {
                        UnassignPower(powerRef);
                        EntityActionComponent.PerformPowers.Remove(powerRef);
                    }
                    else
                    {
                        PowerIndexProperties indexProps = new(0, CharacterLevel, CombatLevel);
                        AssignPower(powerRef, indexProps);

                        PowerActivationSettings powerSettings = new(Id, Vector3.Zero, RegionLocation.Position);
                        powerSettings.Flags |= PowerActivationSettingsFlags.NotifyOwner;
                        var result = ActivatePower(powerRef, ref powerSettings);
                        if (result == PowerUseResult.Success)
                            EntityActionComponent.PerformPowers.Add(powerRef);
                        else
                            return Logger.WarnReturn(false, $"ProcessEntityAction ActivatePower [{powerRef}] = {result}");
                    }
                }
            }

            return true;
        }

        public void TriggerLocalSpawner(PrototypeId spawnerTrigger)
        {
            var spawnerTriggerProto = GameDatabase.GetPrototype<EntityActionSpawnerTriggerPrototype>(spawnerTrigger);
            if (spawnerTriggerProto == null) return;

            if (spawnerTriggerProto.EnableClusterLocalSpawner && SpawnGroup != null) 
                foreach (var spec in SpawnGroup.Specs)
                    if (spec.ActiveEntity is Spawner spawner)
                        spawner.ScheduleEnableTrigger();
        }

        public void ShowOverheadText(LocaleStringId idText, float duration)
        {
            var message = NetMessageShowOverheadText.CreateBuilder()
                .SetIdAgent(Id)
                .SetIdText((ulong)idText)
                .SetDuration(duration)
                .Build();

            Game.NetworkManager.SendMessageToInterested(message, this, AOINetworkPolicyValues.AOIChannelProximity);
        }

        #endregion

        public bool HasKeyword(PrototypeId keyword)
        {
            return HasKeyword(GameDatabase.GetPrototype<KeywordPrototype>(keyword));
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            return keywordProto != null && WorldEntityPrototype.HasKeyword(keywordProto);
        }

        public bool HasConditionWithKeyword(PrototypeId keywordRef)
        {
            return HasConditionWithKeyword(keywordRef.As<KeywordPrototype>());
        }

        public bool HasConditionWithKeyword(KeywordPrototype keywordProto)
        {
            if (keywordProto == null) return Logger.WarnReturn(false, "HasConditionWithKeyword(): keywordProto == null");

            if (keywordProto is not PowerKeywordPrototype)
                return false;

            return HasConditionWithKeyword(GameDatabase.DataDirectory.GetPrototypeEnumValue(keywordProto.DataRef, GameDatabase.DataDirectory.KeywordBlueprint));
        }

        private bool HasConditionWithKeyword(int keyword)
        {
            var conditionCollection = ConditionCollection;
            if (conditionCollection != null)
            {
                KeywordsMask keywordsMask = conditionCollection.ConditionKeywordsMask;
                return keywordsMask[keyword];
            }
            return false;
        }

        public bool HasConditionWithAnyKeyword(PrototypeId[] keywordProtoRefs)
        {
            foreach (PrototypeId keywordProtoRef in keywordProtoRefs)
            {
                if (HasConditionWithKeyword(keywordProtoRef))
                    return true;
            }

            return false;
        }

        public void AccumulateKeywordProperties(PropertyEnum propertyEnum, PropertyCollection properties, ref float value)
        {
            foreach (var kvp in properties.IteratePropertyRange(propertyEnum))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId keywordProtoRef);
                var keywordPrototype = keywordProtoRef.As<KeywordPrototype>();

                if (HasKeyword(keywordPrototype) || HasConditionWithKeyword(keywordProtoRef))
                    value += kvp.Value;
            }
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            foreach (var kvp in _trackingContextMap)
                sb.AppendLine($"{nameof(_trackingContextMap)}[{GameDatabase.GetPrototypeName(kvp.Key)}]: {kvp.Value}");

            foreach (Condition condition in _conditionCollection)
                sb.AppendLine($"{nameof(_conditionCollection)}[{condition.Id}]: {condition}");

            if (_powerCollection.PowerCount > 0)
            {
                sb.AppendLine($"{nameof(_powerCollection)}:");
                foreach (var kvp in _powerCollection)
                    sb.AppendLine(kvp.Value.ToString());
                sb.AppendLine();
            }

            sb.AppendLine($"{nameof(_unkEvent)}: 0x{_unkEvent:X}");
        }

        public bool ModifyTrackingContext(PrototypeId contextRef, EntityTrackingFlag flags)
        {
            bool modified = false;

            if (flags != EntityTrackingFlag.None)
            {
                if (_trackingContextMap.TryGetValue(contextRef, out var modifyFlags) == false || modifyFlags != flags)
                {
                    _trackingContextMap[contextRef] = flags;
                    modified = true;
                }
            }
            else
            {
                if (_trackingContextMap.ContainsKey(contextRef))
                {
                    _trackingContextMap.Remove(contextRef);
                    modified = true;
                }
            }

            if (modified == false) return false;

            var entityTracked = NetMessageEntityTracked.CreateBuilder()
                .SetIdEntity(Id)
                .SetTrackingProtoId((ulong)contextRef)
                .SetFlags((uint)flags)
                .Build();

            var policy = AOINetworkPolicyValues.AOIChannelProximity
                | AOINetworkPolicyValues.AOIChannelDiscovery
                | AOINetworkPolicyValues.AOIChannelParty
                | AOINetworkPolicyValues.AOIChannelOwner;

            Game.NetworkManager.SendMessageToInterested(entityTracked, this, policy);

            Region?.UIDataProvider?.OnEntityTracked(this, contextRef);

            return true;
        }

        public static bool CheckWithinAngle(in Vector3 targetPosition, in Vector3 targetForward, in Vector3 position, float angle)
        {
            if (angle > 0)
            {
                Vector3 distance = Vector3.SafeNormalize(position - targetPosition);
                float targetForwardDot = Vector3.Dot(targetForward, distance);
                float checkAngle = MathHelper.ToDegrees(MathF.Acos(targetForwardDot)) * 2;
                if (checkAngle < angle)
                    return true;
            }
            return false;
        }

        public bool DiscoveredForPlayer(Player player)
        {
            if (IsDiscoverable == false) return false;
            var playerRegion = player.GetRegion();
            if (playerRegion != null && playerRegion == Region && playerRegion.IsEntityDiscovered(this)) return true;
            if (player.IsEntityDiscovered(this) && WorldEntityPrototype?.ObjectiveInfo?.TrackAfterDiscovery == true) return true;
            return false;
        }

        public void OnInteractedWith(WorldEntity interactorEntity)
        {
            int usesLeft = Properties[PropertyEnum.InteractableUsesLeft];
            bool used = usesLeft == -1 || usesLeft > 0;

            if (usesLeft != -1)
            {
                usesLeft--;
                Properties[PropertyEnum.InteractableUsesLeft] = usesLeft;
            }

            bool lastUse = used && usesLeft == 0;
            if (lastUse)
                TryActivateOnInteractedWithProcs(ProcTriggerType.OnInteractedWithOutOfUses, interactorEntity);
            else
                TryActivateOnInteractedWithProcs(ProcTriggerType.OnInteractedWith, interactorEntity);

            if (HasLootDropEventType(LootDropEventType.OnInteractedWith))
            {
                long interactableSpawnLootDelayMS = Properties[PropertyEnum.InteractableSpawnLootDelayMS];

                if (interactableSpawnLootDelayMS > 0)
                {
                    // Award interaction loot after a delay to let the opening animation play
                    TimeSpan interactableSpawnLootDelay = TimeSpan.FromMilliseconds(interactableSpawnLootDelayMS);
                    EventPointer<AwardInteractionLootEvent> awardInteractionLootEvent = new();
                    Game.GameEventScheduler.ScheduleEvent(awardInteractionLootEvent, interactableSpawnLootDelay);
                    awardInteractionLootEvent.Get().Initialize(this, interactorEntity.Id);
                }
                else
                {
                    // Award loot immediately if no delay is specified for this world entity
                    AwardInteractionLoot(interactorEntity.Id);
                }
            }

            if (lastUse)
            {
                long destroyDelayMS = Properties[PropertyEnum.InteractableDestroyDelayMS];
                if (destroyDelayMS > 0)
                    ScheduleKillEvent(TimeSpan.FromMilliseconds(destroyDelayMS));
            }

            if (WorldEntityPrototype.PostInteractState != null)
                ApplyStateFromPrototype(WorldEntityPrototype.PostInteractState);
        }

        public PrototypeId GetVendorGlobalEvent()
        {
            if (IsVendor == false)
                return PrototypeId.Invalid;

            PrototypeId vendorTypeProtoRef = Properties[PropertyEnum.VendorType];
            VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
            if (vendorTypeProto == null) return Logger.WarnReturn(PrototypeId.Invalid, "GetVendorGlobalEvent(): vendorTypeProto == null");

            return vendorTypeProto.GlobalEvent;
        }

        #region Scheduled Events

        public bool ScheduleKillEvent(TimeSpan delay)
        {
            if (TestStatus(EntityStatus.PendingDestroy))
                return Logger.WarnReturn(false, $"ScheduleKillEvent(): WorldEntity {this} is already pending destroy");

            if (TestStatus(EntityStatus.Destroyed))
                return Logger.WarnReturn(false, $"ScheduleKillEvent(): WorldEntity {this} is already destroyed");

            if (IsDead)
                return Logger.WarnReturn(false, $"ScheduleKillEvent(): WorldEntity {this} is dead");

            if (_scheduledKillEvent.IsValid)
            {
                if (_scheduledKillEvent.Get().FireTime > (Game.CurrentTime + delay))
                    Game?.GameEventScheduler?.RescheduleEvent(_scheduledKillEvent, delay);
            }
            else ScheduleEntityEvent(_scheduledKillEvent, delay);

            return true;
        }

        public override bool ScheduleDestroyEvent(TimeSpan delay)
        {
            if (IsDestroyProtectedEntity)
                return Logger.WarnReturn(false, $"ScheduleDestroyEvent(): Trying to schedule destruction of a destroy-protected entity {this}");

            return base.ScheduleDestroyEvent(delay);
        }

        public void ScheduleExitWorldEvent(TimeSpan time)
        {
            if (_exitWorldEvent.IsValid)
            {
                if (_exitWorldEvent.Get().FireTime > Game.CurrentTime + time)
                    Game.GameEventScheduler.RescheduleEvent(_exitWorldEvent, time);
            }
            else
                ScheduleEntityEvent(_exitWorldEvent, time);
        }

        public void ScheduleUnassignPowerEvent(PrototypeId powerProtoRef)
        {
            EventPointer<ScheduledUnassignPowerEvent> scheduledUnassignPower = new();
            ScheduleEntityEvent(scheduledUnassignPower, TimeSpan.FromMilliseconds(1), powerProtoRef);
        }

        public bool ScheduleApplyPowerResultsEvent(PowerResults powerResults)
        {
            if (IsSimulated == false)
                return false;

            if (powerResults.PowerPrototype.ApplyResultsImmediately)
            {
                ApplyPowerResults(powerResults);
            }
            else
            {
                EventPointer<ScheduledPowerResultsEvent> scheduledPowerResults = new();
                ScheduleEntityEvent(scheduledPowerResults, TimeSpan.Zero, powerResults);
            }

            return true;
        }

        public void ScheduleTickEvent(PropertyTicker.TickData tickData)
        {
            EventPointer<ScheduledTickEvent> scheduledTick = new();
            ScheduleEntityEvent(scheduledTick, TimeSpan.Zero, tickData);
        }

        public void ScheduleWeaponReturnEvent(TimeSpan delay)
        {
            EventPointer<ScheduledWeaponReturnEvent> scheduledWeaponReturn = new();
            ScheduleEntityEvent(scheduledWeaponReturn, delay);
        }

        public void CancelExitWorldEvent()
        {
            if (_exitWorldEvent.IsValid)
                Game?.GameEventScheduler?.CancelEvent(_exitWorldEvent);
        }

        public void CancelKillEvent()
        {
            if (_scheduledKillEvent.IsValid)
                Game?.GameEventScheduler?.CancelEvent(_scheduledKillEvent);
        }

        protected class ScheduledExitWorldEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => (t as WorldEntity)?.ExitWorld();
        }

        protected class ScheduledKillEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => (t as WorldEntity)?.Kill();
        }

        private class ScheduledUnassignPowerEvent : CallMethodEventParam1<Entity, PrototypeId>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => ((WorldEntity)t).UnassignPower(p1);
        }

        private class ScheduledHealthProcUpdateEvent : CallMethodEventParam1<Entity, PrototypeId>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => ((WorldEntity)t).TryActivateOnHealthProcs(p1);
        }

        private class ScheduledPowerResultsEvent : CallMethodEventParam1<Entity, PowerResults>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => ((WorldEntity)t).ApplyPowerResults(p1);

            public override bool OnCancelled()
            {
                _param1.Clear();    // Clear to prevent conditions leaking from their pool
                return true;
            }
        }

        private class NegateHotspotsEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((WorldEntity)t).OnNegateHotspots();
        }

        private class ScheduledTickEvent : CallMethodEventParam1<Entity, PropertyTicker.TickData>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => ((WorldEntity)t).ApplyPropertyTicker(p1);
        }

        private class ScheduledWeaponReturnEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => t.Properties[PropertyEnum.WeaponMissing] = false;
            public override bool OnCancelled() => _eventTarget.Properties[PropertyEnum.WeaponMissing] = false;
        }

        private class AwardInteractionLootEvent : CallMethodEventParam1<Entity, ulong>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => ((WorldEntity)t).AwardInteractionLoot(p1);
        }

        #endregion
    }
}
