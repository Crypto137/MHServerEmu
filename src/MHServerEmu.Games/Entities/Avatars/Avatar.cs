﻿using System.Text;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Guilds;
using MHServerEmu.Games.Social.Parties;

namespace MHServerEmu.Games.Entities.Avatars
{
    public partial class Avatar : Agent
    {
        private const int MaxNumTransientAbilityKeyMappings = 1;
        private const uint TalentGroupIndexInvalid = 0;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly TimeSpan StandardContinuousPowerRecheckDelay = TimeSpan.FromMilliseconds(150);

        private readonly EventPointer<ActivateSwapInPowerEvent> _activateSwapInPowerEvent = new();
        private readonly EventPointer<RecheckContinuousPowerEvent> _recheckContinuousPowerEvent = new();
        private readonly EventPointer<DelayedPowerActivationEvent> _delayedPowerActivationEvent = new();
        private readonly EventPointer<AvatarEnteredRegionEvent> _avatarEnteredRegionEvent = new();
        private readonly EventPointer<RefreshStatsPowersEvent> _refreshStatsPowerEvent = new();
        private readonly EventPointer<DismissTeamUpAgentEvent> _dismissTeamUpAgentEvent = new();
        private readonly EventPointer<DespawnControlledEvent> _despawnControlledEvent = new();
        private readonly EventPointer<TransformModeChangeEvent> _transformModeChangeEvent = new();
        private readonly EventPointer<TransformModeExitPowerEvent> _transformModeExitPowerEvent = new();
        private readonly EventPointer<UnassignMappedPowersForRespecEvent> _unassignMappedPowersForRespec = new();
        private readonly EventPointer<BodyslideTeleportToTownEvent> _bodyslideTeleportToTownEvent = new();
        private readonly EventPointer<BodyslideTeleportFromTownEvent> _bodyslideTeleportFromTownEvent = new();
        private readonly EventPointer<PowerTeleportEvent> _powerTeleportEvent = new();
        private readonly EventPointer<DeathDialogEvent> _deathDialogEvent = new();

        private readonly EventPointer<EnableEnduranceRegenEvent>[] _enableEnduranceRegenEvents = new EventPointer<EnableEnduranceRegenEvent>[(int)ManaType.NumTypes];
        private readonly EventPointer<UpdateEnduranceEvent>[] _updateEnduranceEvents = new EventPointer<UpdateEnduranceEvent>[(int)ManaType.NumTypes];

        private RepString _playerName = new();
        private ulong _ownerPlayerDbId;

        private List<AbilityKeyMapping> _abilityKeyMappings = new();    // Persistent ability key mappings for each spec
        private List<AbilityKeyMapping> _transientAbilityKeyMappings;   // Non-persistent ability key mappings used for transform modes (init on demand)
        private AbilityKeyMapping _currentAbilityKeyMapping;            // Reference to the currently active ability key mapping

        private ulong _guildId = GuildMember.InvalidGuildId;
        private string _guildName = string.Empty;
        private GuildMembership _guildMembership = GuildMembership.eGMNone;
        private readonly PendingPowerData _continuousPowerData = new();
        private readonly PendingAction _pendingAction = new();

        private PrototypeId _travelPowerOverrideProtoRef = PrototypeId.Invalid;

        private ulong _avatarSynergyConditionId = ConditionCollection.InvalidConditionId;

        public uint AvatarWorldInstanceId { get; } = 1;
        public string PlayerName { get => _playerName.Get(); }
        public ulong OwnerPlayerDbId { get => _ownerPlayerDbId; }
        public Agent CurrentTeamUpAgent { get => GetTeamUpAgent(Properties[PropertyEnum.AvatarTeamUpAgent]); }
        public Agent CurrentVanityPet { get => GetCurrentVanityPet(); }

        public AvatarPrototype AvatarPrototype { get => Prototype as AvatarPrototype; }
        public int PrestigeLevel { get => Properties[PropertyEnum.AvatarPrestigeLevel]; }
        public override bool IsAtLevelCap { get => CharacterLevel >= GetAvatarLevelCap(); }
        public override int Throwability { get => GetThrowability(); }

        public PrototypeId EquippedCostumeRef { get => Properties[PropertyEnum.CostumeCurrent]; }
        public CostumePrototype EquippedCostume { get => EquippedCostumeRef.As<CostumePrototype>(); }

        public bool IsUsingGamepadInput { get; set; } = false;
        public PrototypeId CurrentTransformMode { get; private set; } = PrototypeId.Invalid;

        public override bool IsMovementAuthoritative => false;
        public override bool CanBeRepulsed => false;
        public override bool CanRepulseOthers => false;

        public bool IsContinuouslyAttacking { get => _continuousPowerData.PowerProtoRef != PrototypeId.Invalid; }
        public PrototypeId ContinuousPowerDataRef { get => _continuousPowerData.PowerProtoRef; }
        public ulong ContinuousAttackTarget { get => _continuousPowerData.TargetId; }

        public Power PendingPower { get => GetPower(_pendingAction.PowerProtoRef); }
        public PrototypeId PendingPowerDataRef { get => _pendingAction.PowerProtoRef; }
        public PendingActionState PendingActionState { get => _pendingAction.PendingActionState; }

        public PrototypeId TeamUpPowerRef { get => GameDatabase.GlobalsPrototype.TeamUpSummonPower; }
        public PrototypeId UltimatePowerRef { get => AvatarPrototype.UltimatePowerRef; }

        public AvatarModePrototype AvatarModePrototype { get => GameDatabase.GetPrototype<AvatarModePrototype>(Properties[PropertyEnum.AvatarMode]); }
        public AvatarMode AvatarMode { get => AvatarModePrototype?.AvatarModeEnum ?? AvatarMode.Invalid; }
        public PrototypeGuid PrototypeGuid { get => GameDatabase.GetPrototypeGuid(PrototypeDataRef); }
        public Inventory ControlledInventory { get => GetInventory(InventoryConvenienceLabel.Controlled); }
        public Agent ControlledAgent { get => GetControlledAgent(); }

        public Avatar(Game game) : base(game) { }

        public override string ToString()
        {
            return $"{base.ToString()}, Player={_playerName?.Get()} (0x{_ownerPlayerDbId:X})";
        }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);

            // NOTE: We need to set owner player dbid asap for it to be restored in persistent conditions
            if (settings.InventoryLocation != null)
            {
                Player player = Game.EntityManager.GetEntity<Player>(settings.InventoryLocation.ContainerId);
                if (player != null)
                    _ownerPlayerDbId = player.DatabaseUniqueId;
            }

            return true;
        }

        public override bool ApplyInitialReplicationState(ref EntitySettings settings)
        {
            if (base.ApplyInitialReplicationState(ref settings) == false)
                return false;

            Player player = null;
            if (settings.InventoryLocation != null)
                player = Game.EntityManager.GetEntity<Player>(settings.InventoryLocation.ContainerId);

            if (player == null)
                Logger.Warn("ApplyInitialReplicationState(): player == null");

            if (settings.ArchiveData != null)
            {
                if (player != null)
                    TryLevelUp(player, true);

                ResetResources(false);
            }

            // Resurrect if dead
            if (IsDead)
                Resurrect();

            // Restore level state by running the level up code
            int level = CharacterLevel;
            OnLevelUp(level, level, false);

            return true;
        }

        protected override void BindReplicatedFields()
        {
            base.BindReplicatedFields();

            _playerName.Bind(this, AOINetworkPolicyValues.AOIChannelProximity | AOINetworkPolicyValues.AOIChannelParty | AOINetworkPolicyValues.AOIChannelOwner);
        }

        protected override void UnbindReplicatedFields()
        {
            base.UnbindReplicatedFields();

            _playerName.Unbind();
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);

            if (archive.IsTransient)
            {
                success &= Serializer.Transfer(archive, ref _playerName);
                success &= Serializer.Transfer(archive, ref _ownerPlayerDbId);

                // There is an unused string here that is always empty
                string emptyString = string.Empty;
                success &= Serializer.Transfer(archive, ref emptyString);
                if (emptyString != string.Empty)
                    Logger.Warn($"Serialize(): emptyString is not empty!");

                if (archive.IsReplication)
                    success &= GuildMember.SerializeReplicationRuntimeInfo(archive, ref _guildId, ref _guildName, ref _guildMembership);
            }

            success &= Serializer.Transfer(archive, ref _abilityKeyMappings);

            return success;
        }

        public override void OnUnpackComplete(Archive archive)
        {
            base.OnUnpackComplete(archive);

            // Restore persistent cooldowns
            if (archive.IsPersistent)
            {
                Dictionary<PropertyId, PropertyValue> setDict = DictionaryPool<PropertyId, PropertyValue>.Instance.Get();

                foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.PowerCooldownDurationPersistent))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId powerProtoRef);
                    PowerPrototype powerProto = powerProtoRef.As<PowerPrototype>();
                    if (powerProto == null)
                    {
                        Logger.Warn("OnUnpackComplete(): powerProto == null");
                        continue;
                    }

                    // Discard if no longer flagged as persistent
                    if (Power.IsCooldownPersistent(powerProto) == false)
                        continue;

                    setDict[new(PropertyEnum.PowerCooldownDuration, powerProtoRef)] = kvp.Value;
                }

                foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.PowerCooldownStartTimePersistent))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId powerProtoRef);
                    PowerPrototype powerProto = powerProtoRef.As<PowerPrototype>();
                    if (powerProto == null)
                    {
                        Logger.Warn("OnUnpackComplete(): powerProto == null");
                        continue;
                    }

                    // Discard if no longer flagged as persistent
                    if (Power.IsCooldownPersistent(powerProto) == false)
                        continue;

                    setDict[new(PropertyEnum.PowerCooldownStartTime, powerProtoRef)] = kvp.Value;
                }

                foreach (var kvp in setDict)
                    Properties[kvp.Key] = kvp.Value;

                DictionaryPool<PropertyId, PropertyValue>.Instance.Return(setDict);
            }
        }

        public void SetPlayer(Player player)
        {
            _playerName.Set(player.GetName());
            _ownerPlayerDbId = player.DatabaseUniqueId;
        }

        public void SetTutorialProps(HUDTutorialPrototype hudTutorialProto)
        {
            if (hudTutorialProto.AllowMovement == false)
                Properties[PropertyEnum.TutorialImmobilized] = true;
            if (hudTutorialProto.AllowPowerUsage == false)
                Properties[PropertyEnum.TutorialPowerLock] = true;
            if (hudTutorialProto.AllowTakingDamage == false)
                Properties[PropertyEnum.TutorialInvulnerable] = true;
        }

        public void ResetTutorialProps()
        {
            Properties.RemoveProperty(PropertyEnum.TutorialImmobilized);
            Properties.RemoveProperty(PropertyEnum.TutorialPowerLock);
            Properties.RemoveProperty(PropertyEnum.TutorialInvulnerable);
        }

        public bool SelectVanityTitle(PrototypeId vanityTitleProtoRef)
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "SelectVanityTitle(): player == null");

            if (player.IsVanityTitleUnlocked(vanityTitleProtoRef) == false)
                return false;

            Properties[PropertyEnum.AvatarVanityTitle] = vanityTitleProtoRef;
            return true;
        }

        #region World and Positioning

        public override SimulateResult SetSimulated(bool simulated)
        {
            SimulateResult result = base.SetSimulated(simulated);

            if (result == SimulateResult.Set)
            {
                // TODO: Add a helper function for applying mods? (pvp / infinity / omega)

                // Apply PvP upgrade bonuses
                List<(PrototypeId, int)> pvpUpgradeList = ListPool<(PrototypeId, int)>.Instance.Get();

                foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.OmegaRank))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId omegaBonusProtoRef);
                    int rank = kvp.Value;
                    pvpUpgradeList.Add((omegaBonusProtoRef, rank));
                }

                foreach (var pvpUpgrade in pvpUpgradeList)
                    ModChangeModEffects(pvpUpgrade.Item1, pvpUpgrade.Item2);

                ListPool<(PrototypeId, int)>.Instance.Return(pvpUpgradeList);

                // Apply alternate advancement (infinity / omega) bonuses
                if (Game.InfinitySystemEnabled)
                    ApplyInfinityBonuses();
                else
                    ApplyOmegaBonuses();
            }

            return result;
        }

        public override bool CanMove()
        {
            if (base.CanMove() == false)
                return IsInPendingActionState(PendingActionState.FindingLandingSpot);

            return PendingActionState != PendingActionState.VariableActivation && PendingActionState != PendingActionState.AvatarSwitchInProgress;
        }

        public override ChangePositionResult ChangeRegionPosition(Vector3? position, Orientation? orientation, ChangePositionFlags flags = ChangePositionFlags.None)
        {
            if (RegionLocation.IsValid() == false)
                return Logger.WarnReturn(ChangePositionResult.NotChanged, "ChangeRegionPosition(): Cannot change region position without entering the world first");

            // We only need to do AOI processing if the avatar is changing its position
            if (position == null)
            {
                if (orientation != null)
                    return base.ChangeRegionPosition(position, orientation, flags);
                else
                    return Logger.WarnReturn(ChangePositionResult.NotChanged, "ChangeRegionPosition(): No position or orientation provided");
            }

            // Get player for AOI update
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(ChangePositionResult.NotChanged, "ChangeRegionPosition(): player == null");

            ChangePositionResult result;

            if (player.AOI.ContainsPosition(position.Value))
            {
                if (flags.HasFlag(ChangePositionFlags.Teleport))
                    DespawnPersistentAgents();

                // Do a normal position change and update AOI if the position is loaded
                result = base.ChangeRegionPosition(position, orientation, flags);
                if (result == ChangePositionResult.PositionChanged)
                    player.AOI.Update(RegionLocation.Position);

                if (flags.HasFlag(ChangePositionFlags.Teleport))
                    RespawnPersistentAgents();
            }
            else
            {
                // If we are moving outside of our AOI, start a teleport and exit world.
                // The avatar will be put back into the world when all cells at the destination are loaded.
                if (RegionLocation.Region.GetCellAtPosition(position.Value) == null)
                    return Logger.WarnReturn(ChangePositionResult.InvalidPosition, $"ChangeRegionPosition(): Invalid position {position.Value}");

                player.BeginTeleport(RegionLocation.RegionId, position.Value, orientation != null ? orientation.Value : Orientation.Zero);
                ConditionCollection.RemoveCancelOnIntraRegionTeleportConditions();
                ExitWorld();
                player.AOI.Update(position.Value);
                result = ChangePositionResult.Teleport;
            }

            if (result == ChangePositionResult.PositionChanged)
            {
                player.RevealDiscoveryMap(position.Value);
                player.UpdateSpawnMap(position.Value);
            }

            return result;
        }

        public override void OnKilled(WorldEntity killer, KillFlags killFlags, WorldEntity directKiller)
        {
            CancelPendingAction();

            // Revert transform
            if (CurrentTransformMode != PrototypeId.Invalid)
            {
                EventScheduler scheduler = Game.GameEventScheduler;
                scheduler.CancelEvent(_transformModeExitPowerEvent);
                scheduler.CancelEvent(_transformModeChangeEvent);

                DoTransformModeChangeCallback(PrototypeId.Invalid, CurrentTransformMode);
            }

            base.OnKilled(killer, killFlags, directKiller);

            // Deplete resources if needed
            foreach (PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto in GetPrimaryResourceManaBehaviors())
            {
                if (primaryManaBehaviorProto.DepleteOnDeath)
                    Properties.RemoveProperty(new(PropertyEnum.Endurance, primaryManaBehaviorProto.ManaType));
            }

            SecondaryResourceManaBehaviorPrototype secondaryManaBehaviorProto = GetSecondaryResourceManaBehavior();
            if (secondaryManaBehaviorProto != null && secondaryManaBehaviorProto.DepleteOnDeath)
                Properties.RemoveProperty(PropertyEnum.SecondaryResource);

            Properties.RemoveProperty(PropertyEnum.NumMissionAllies);

            // Set up death release timeout
            Game.GameEventScheduler.CancelEvent(_deathDialogEvent);

            AvatarOnKilledInfoPrototype onKilledInfoProto = Region?.GetAvatarOnKilledInfo();
            if (onKilledInfoProto != null)
                ScheduleEntityEvent(_deathDialogEvent, TimeSpan.FromMilliseconds(onKilledInfoProto.DeathReleaseTimeoutMS));
            else
                Logger.Warn("OnKilled(): onKilledInfoProto == null");
        }

        public override bool Resurrect()
        {
            Properties[PropertyEnum.HasResurrectPending] = false;

            bool success = base.Resurrect();

            foreach (PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto in GetPrimaryResourceManaBehaviors())
            {
                ManaType manaType = primaryManaBehaviorProto.ManaType;
                float endurance = primaryManaBehaviorProto.StartsEmpty ? 0f : Properties[PropertyEnum.EnduranceMax, manaType];
                Properties[PropertyEnum.Endurance, manaType] = endurance;
            }

            Game.GameEventScheduler.CancelEvent(_deathDialogEvent);

            return success;
        }

        public void ResurrectOtherAvatar(Avatar targetAvatar)
        {
            if (targetAvatar == null || targetAvatar.IsDead == false)
                return;

            if (IsInWorld == false)
                return;

            if (targetAvatar.Id == Properties[PropertyEnum.PendingResurrectEntityId])
                return;

            PrototypeId resurrectOtherEntityPower = AvatarPrototype.ResurrectOtherEntityPower;
            if (resurrectOtherEntityPower == PrototypeId.Invalid)
            {
                Logger.Warn("ResurrectOtherAvatar(): resurrectOtherEntityPower == PrototypeId.Invalid");
                return;
            }

            PowerActivationSettings settings = new(targetAvatar.Id, targetAvatar.RegionLocation.Position, RegionLocation.Position);
            settings.Flags |= PowerActivationSettingsFlags.NotifyOwner;

            if (ActivatePower(resurrectOtherEntityPower, ref settings) == PowerUseResult.Success)
                Properties[PropertyEnum.PendingResurrectEntityId] = targetAvatar.Id;
;        }

        public bool DoDeathRelease(DeathReleaseRequestType requestType)
        {
            // Resurrect
            if (Resurrect() == false)
                return Logger.WarnReturn(false, $"DoDeathRelease(): Failed to resurrect avatar {this}");

            // Move to waypoint or some other place depending on the request and the region prototype
            Region region = Region;
            if (region == null) return Logger.WarnReturn(false, "DoDeathRelease(): region == null");

            Player owner = GetOwnerOfType<Player>();
            if (owner == null) return Logger.WarnReturn(false, "DoDeathRelease(): owner == null");

            switch (requestType)
            {
                case DeathReleaseRequestType.Checkpoint:
                    AvatarOnKilledInfoPrototype onKilledInfoProto = region.GetAvatarOnKilledInfo();
                    if (onKilledInfoProto == null) return Logger.WarnReturn(false, "DoDeathRelease(): onKilledInfoProto == null");

                    if (onKilledInfoProto.DeathReleaseBehavior == DeathReleaseBehavior.ReturnToWaypoint)
                    {
                        // Find the target for our respawn teleport
                        PrototypeId deathReleaseTarget = FindDeathReleaseTarget(out PrototypeId regionProtoRefOverride);
                        if (deathReleaseTarget == PrototypeId.Invalid)
                            return Logger.WarnReturn(false, "DoDeathRelease(): Failed to find a target to move to");

                        RegionConnectionTargetPrototype targetProto = deathReleaseTarget.As<RegionConnectionTargetPrototype>();
                        if (targetProto == null) return Logger.WarnReturn(false, "DoDeathRelease(): targetProto == null");

                        PrototypeId regionProtoRef = regionProtoRefOverride != PrototypeId.Invalid ? regionProtoRefOverride : targetProto.Region;
                        PrototypeId areaProtoRef = targetProto.Area;
                        PrototypeId cellProtoRef = GameDatabase.GetDataRefByAsset(targetProto.Cell);
                        PrototypeId entityProtoRef = targetProto.Entity;

                        Player player = GetOwnerOfType<Player>();

                        using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
                        teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Resurrect);
                        return teleporter.TeleportToTarget(regionProtoRef, areaProtoRef, cellProtoRef, entityProtoRef);
                    }
                    else 
                    {
                        return Logger.WarnReturn(false, $"DoDeathRelease(): Unimplemented behavior {onKilledInfoProto.DeathReleaseBehavior}");
                    }

                case DeathReleaseRequestType.Corpse:
                    // No need to move.
                    return true;

                default:
                    return Logger.WarnReturn(false, $"DoDeathRelease(): Unimplemented request type {requestType}");
            }
        }

        public bool ResurrectRequest(ulong resurrectorId)
        {
            if (Properties[PropertyEnum.HasResurrectPending])
                return true;

            if (resurrectorId == InvalidId) return Logger.WarnReturn(false, "ResurrectRequest(): resurrectorId == InvalidId");

            AvatarOnKilledInfoPrototype onKilledInfoProto = Region?.GetAvatarOnKilledInfo();
            if (onKilledInfoProto == null) return Logger.WarnReturn(false, "ResurrectRequest(): onKilledInfoProto == null");

            Game.GameEventScheduler.CancelEvent(_deathDialogEvent);
            ScheduleEntityEvent(_deathDialogEvent, TimeSpan.FromMilliseconds(onKilledInfoProto.ResurrectionTimeoutMS));

            Properties[PropertyEnum.HasResurrectPending] = true;

            var resurrectRequestMessage = NetMessageOnResurrectRequest.CreateBuilder()
                .SetTargetId(Id)
                .SetResurrectorId(resurrectorId)
                .Build();

            Game.NetworkManager.SendMessageToInterested(resurrectRequestMessage, this, AOINetworkPolicyValues.AOIChannelProximity);

            return true;
        }

        public void ResurrectDecline()
        {
            Properties[PropertyEnum.HasResurrectPending] = false;

            var resurrectDeclineMessage = NetMessageOnResurrectDecline.CreateBuilder()
                .SetTargetId(Id)
                .Build();

            Game.NetworkManager.SendMessageToInterested(resurrectDeclineMessage, this, AOINetworkPolicyValues.AOIChannelProximity);
        }

        protected override void ResurrectFromOther(WorldEntity ultimateOwner)
        {
            if (ultimateOwner == null)
            {
                Logger.Warn("ResurrectFromOther(): ultimateOwner == null");
                return;
            }

            if (ultimateOwner is Avatar && ultimateOwner.Properties[PropertyEnum.PendingResurrectEntityId] == Id)
            {
                // Ask this player for confirmation if this is a resurrect from another player.
                ultimateOwner.Properties.RemoveProperty(PropertyEnum.PendingResurrectEntityId);
                ResurrectRequest(ultimateOwner.Id);
            }
            else
            {
                // Apply resurrection from other sources immediately.
                Resurrect();
            }
        }

        private PrototypeId FindDeathReleaseTarget(out PrototypeId regionProtoRefOverride)
        {
            regionProtoRefOverride = PrototypeId.Invalid;

            Region region = Region;
            if (region == null) return Logger.WarnReturn(PrototypeId.Invalid, "FindDeathReleaseTarget(): region == null");

            Area area = Area;
            if (area == null) return Logger.WarnReturn(PrototypeId.Invalid, "FindDeathReleaseTarget(): area == null");

            Cell cell = Cell;
            if (cell == null) return Logger.WarnReturn(PrototypeId.Invalid, "FindDeathReleaseTarget(): cell == null");

            Player player = GetOwnerOfType<Player>();

            // Apply region overrides to terminal targets
            if (region.Prototype.DailyCheckpointStartTarget)
                regionProtoRefOverride = region.PrototypeDataRef;

            // Check if there is a hotspot override
            if (player != null)
            {
                PrototypeId respawnTarget = GetRespawHotspotOverrideTarget(player);
                if (respawnTarget != PrototypeId.Invalid)
                    return respawnTarget;
            }

            // Check if there is RegionStartTargetOverride property
            PrototypeId startTargetRef = region.Properties[PropertyEnum.RegionStartTargetOverride];
            if (startTargetRef != PrototypeId.Invalid)
                return startTargetRef;

            // Check if there is an area / cell override
            PrototypeId areaRespawnOverride = area.GetRespawnOverride(cell);
            if (areaRespawnOverride != PrototypeId.Invalid)
                return areaRespawnOverride;

            // Check if there is DividedStartTarget
            if (region.GetDividedStartTarget(player, ref startTargetRef))
                return startTargetRef;

            // Check if there is a region-wide override
            if (region.Prototype.RespawnOverride != PrototypeId.Invalid)
                return region.Prototype.RespawnOverride;

            // Fall back to the region's start target as the last resort
            return region.Prototype.StartTarget;
        }

        private void DeathDialogCallback()
        {
            DoDeathRelease(DeathReleaseRequestType.Checkpoint);
        }

        public PrototypeId GetRespawHotspotOverrideTarget(Player player)
        {
            PrototypeId respawnTarget = PrototypeId.Invalid;

            var manager = Game.EntityManager;
            var position = RegionLocation.Position;
            float minDistance = float.MaxValue;

            foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.RespawnHotspotOverrideInst))
            {
                if ((ulong)kvp.Value == InvalidId) continue;
                var hotspot = manager.GetEntity<Hotspot>(kvp.Value);
                if (hotspot == null || hotspot.IsInWorld == false) continue;

                var center = hotspot.RegionLocation.Position;
                float distance = Vector3.Distance2D(position, center);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    Property.FromParam(kvp.Key, 0, out respawnTarget);
                }
            }

            return respawnTarget;
        }

        public void SendSwitchToAvatarFailedMessage(SwitchToAvatarFailedReason reason)
        {
            var message = NetMessageSwitchToPendingNewAvatarFailed.CreateBuilder()
                .SetTargetId(Id)
                .SetReason(reason)
                .Build();

            Game.NetworkManager.SendMessageToInterested(message, this, AOINetworkPolicyValues.AOIChannelProximity | AOINetworkPolicyValues.AOIChannelOwner);
        }

        /// <summary>
        /// Checks if the provided position is valid to use as a start location. Chooses a random position nearby if it's not.
        /// Returns <see langword="true"/> if the position is valid or was successfully adjusted.
        /// </summary>c
        public static bool AdjustStartPositionIfNeeded(Region region, ref Vector3 position, bool checkOtherAvatars = false, float boundsRadius = 64f)
        {
            Bounds bounds = new();
            bounds.InitializeCapsule(boundsRadius, boundsRadius * 2f, BoundsCollisionType.Blocking, BoundsFlags.None);
            bounds.Center = position;

            PositionCheckFlags posFlags = PositionCheckFlags.CanBeBlockedEntity;
            if (checkOtherAvatars)
                posFlags |= PositionCheckFlags.CanBeBlockedAvatar;

            BlockingCheckFlags blockFlags = BlockingCheckFlags.CheckGroundMovementPowers | BlockingCheckFlags.CheckLanding | BlockingCheckFlags.CheckSpawns;

            // Do not modify the position if it's valid as is.
            if (region.IsLocationClear(bounds, Navi.PathFlags.Walk, posFlags, blockFlags))
                return true;

            // Try to pick a replacement position.
            if (region.ChooseRandomPositionNearPoint(bounds, Navi.PathFlags.Walk, posFlags, blockFlags & ~BlockingCheckFlags.CheckSpawns, 0f, 64f, out Vector3 newPosition, null, null, 50) == false)
                return false;

            position = newPosition;
            return true;
        }

        #endregion

        #region Teleports

        public void SetLastTownRegion(PrototypeId regionProtoRef)
        {
            Properties[PropertyEnum.LastTownRegion] = regionProtoRef;

            Player player = GetOwnerOfType<Player>();
            if (player != null)
                player.Properties[PropertyEnum.LastTownRegionForAccount] = regionProtoRef;
        }

        public bool ScheduleBodyslideTeleport()
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "ScheduleBodyslideTeleport(): player == null");

            Region region = Region;
            if (region == null) return Logger.WarnReturn(false, "ScheduleBodyslideTeleport(): region == null");

            Area area = Area;
            if (area == null) return Logger.WarnReturn(false, "ScheduleBodyslideTeleport(): area == null");

            RegionPrototype regionProto = region.Prototype;
            if (regionProto == null) return Logger.WarnReturn(false, "ScheduleBodyslideTeleport(): regionProto == null");

            if (regionProto.Behavior != RegionBehavior.Town)    // -> To Town
            {
                if (regionProto.BodySliderOneWay == false)
                {
                    // Set bodyslider properties to be able to return to where we left
                    player.Properties[PropertyEnum.BodySliderRegionId] = region.Id;
                    player.Properties[PropertyEnum.BodySliderRegionRef] = region.PrototypeDataRef;
                    player.Properties[PropertyEnum.BodySliderDifficultyRef] = region.DifficultyTierRef;
                    player.Properties[PropertyEnum.BodySliderRegionSeed] = region.RandomSeed;
                    player.Properties[PropertyEnum.BodySliderAreaRef] = area.PrototypeDataRef;
                    player.Properties[PropertyEnum.BodySliderRegionPos] = RegionLocation.Position;
                }
                else
                {
                    // No return here
                    player.RemoveBodysliderProperties();
                }

                ScheduleEntityEvent(_bodyslideTeleportToTownEvent, TimeSpan.Zero);
            }
            else if (player.HasBodysliderProperties())          // <- From Town
            {
                // From town
                ScheduleEntityEvent(_bodyslideTeleportFromTownEvent, TimeSpan.Zero);
            }

            return true;
        }

        public void SchedulePowerTeleport(PrototypeId targetProtoRef, TimeSpan delay)
        {
            if (_powerTeleportEvent.IsValid)
                Game.GameEventScheduler.CancelEvent(_powerTeleportEvent);

            if (IsDead)
                Resurrect();

            ScheduleEntityEvent(_powerTeleportEvent, delay, targetProtoRef);
        }

        private bool DoBodyslideTeleportToTown()
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "DoBodyslideTeleportToTown(): player == null");

            PrototypeId bodyslideTargetRef = Bodyslider.GetBodyslideTargetRef(player);
            if (bodyslideTargetRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "DoBodyslideTeleportToTown(): bodyslideTargetRef == PrototypeId.Invalid");

            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Bodyslide);
            return teleporter.TeleportToTarget(bodyslideTargetRef);
        }

        private bool DoBodyslideTeleportFromTown()
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "DoBodyslideTeleportFromTown(): player == null");

            ulong regionId = player.Properties[PropertyEnum.BodySliderRegionId];
            Vector3 position = player.Properties[PropertyEnum.BodySliderRegionPos];
            player.RemoveBodysliderProperties();

            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Bodyslide);
            return teleporter.TeleportToRegionLocation(regionId, position);
        }

        private bool DoPowerTeleport(PrototypeId targetProtoRef)
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "DoRegionTeleport(): player == null");

            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Power);
            return teleporter.TeleportToTarget(targetProtoRef);
        }

        #endregion

        #region Powers

        public bool PerformPreInteractPower(WorldEntity target, bool hasDialog)
        {
            var player = GetOwnerOfType<Player>();
            if (player == null) return false;

            var targetProto = target.WorldEntityPrototype;
            if (targetProto == null || IsExecutingPower) return false;

            var powerRef = targetProto.PreInteractPower;
            var powerProto = GameDatabase.GetPrototype<PowerPrototype>(powerRef);
            if (powerProto == null) return false;

            if (HasPowerInPowerCollection(powerRef) == false)
                AssignPower(powerRef, new(0, CharacterLevel, CombatLevel));

            if (powerProto.Activation != PowerActivationType.Passive)
            {
                PowerActivationSettings settings = new(Id, RegionLocation.Position, RegionLocation.Position);
                settings.Flags |= PowerActivationSettingsFlags.NotifyOwner;
                var result = ActivatePower(powerRef, ref settings);
                if (result != PowerUseResult.Success)
                    return Logger.WarnReturn(false, $"PerformPreInteractPower ActivatePower [{powerRef}] = {result}");
            }

            player.Properties[PropertyEnum.InteractTargetId] = target.Id;
            player.Properties[PropertyEnum.InteractHasDialog] = hasDialog;

            return true;
        }

        public bool PreInteractPowerEnd()
        {
            var player = GetOwnerOfType<Player>();
            if (player == null) return false;

            ulong targetId = player.Properties[PropertyEnum.InteractTargetId];
            player.Properties.RemoveProperty(PropertyEnum.InteractTargetId);
            player.Properties.RemoveProperty(PropertyEnum.InteractHasDialog);

            var targetEntity = Game.EntityManager.GetEntity<WorldEntity>(targetId);
            if (targetEntity == null) return false;

            player.Properties[PropertyEnum.InteractReadyForTargetId] = targetId;

            if (player.InterestedInEntity(this, AOINetworkPolicyValues.AOIChannelOwner))
                player.SendMessage(NetMessageOnPreInteractPowerEnd.CreateBuilder()
                    .SetIdTargetEntity(targetId)
                    .SetAvatarIndex(0).Build());

            return true;
        }

        public override bool OnPowerAssigned(Power power)
        {
            if (base.OnPowerAssigned(power) == false)
                return false;

            // Set charges to max if the assigned power uses charges
            if (Properties.HasProperty(new PropertyId(PropertyEnum.PowerChargesMax, power.PrototypeDataRef)) == false)
            {
                GlobalsPrototype globalsPrototype = GameDatabase.GlobalsPrototype;
                if (globalsPrototype == null) return Logger.WarnReturn(false, "OnPowerAssigned(): globalsPrototype == null");

                int powerChargesMax = power.Properties[PropertyEnum.PowerChargesMax, globalsPrototype.PowerPrototype];
                if (powerChargesMax > 0)
                {
                    PowerPrototype powerProto = power.Prototype;
                    if (powerProto?.CooldownOnPlayer == true)
                        Logger.Warn($"OnPowerAssigned(): CooldownOnPlayer not supported on power with charges.\n{power}");

                    Properties[PropertyEnum.PowerChargesAvailable, power.PrototypeDataRef] = powerChargesMax;
                    Properties[PropertyEnum.PowerChargesMax, power.PrototypeDataRef] = powerChargesMax;
                }
            }

            return true;
        }

        public override void OnPowerEnded(Power power, EndPowerFlags flags)
        {
            base.OnPowerEnded(power, flags);

            PowerPrototype powerProto = power.Prototype;
            if (powerProto.DisableEnduranceRegenTypes.HasValue())
            {
                if (powerProto.DisableEnduranceRegenOnActivate && powerProto.DisableEnduranceRegenOnEnd == false)
                {
                    foreach (ManaType manaType in powerProto.DisableEnduranceRegenTypes)
                        EnableEnduranceRegen(manaType);
                }
                else if (powerProto.DisableEnduranceRegenOnEnd)
                {
                    foreach (ManaType manaType in powerProto.DisableEnduranceRegenTypes)
                    {
                        // Disable endurance regen
                        Properties[PropertyEnum.DisableEnduranceRegen, manaType] = true;

                        // Schedule regen re-enablement
                        int index = (int)manaType;

                        if (_enableEnduranceRegenEvents[index] == null)
                            _enableEnduranceRegenEvents[index] = new();
                        else
                            Game.GameEventScheduler?.CancelEvent(_enableEnduranceRegenEvents[index]);

                        // Schedule the next update tick
                        TimeSpan delay = TimeSpan.FromMilliseconds(GameDatabase.GlobalsPrototype.DisableEndurRegenOnPowerEndMS);
                        ScheduleEntityEvent(_enableEnduranceRegenEvents[index], delay, manaType);
                    }
                }
            }
        }

        public override PowerUseResult ActivatePower(PrototypeId powerRef, ref PowerActivationSettings settings)
        {
            // Check if we have the power before the main validation in case there is lag
            Power power = GetPower(powerRef);
            if (power == null)
                return PowerUseResult.AbilityMissing;

            return base.ActivatePower(powerRef, ref settings);
        }

        protected override PowerUseResult ActivatePower(Power power, ref PowerActivationSettings settings)
        {
            PrototypeId powerRef = power.PrototypeDataRef;

            // Handle edge cases related to continuous powers and conflicting inputs.
            // In many ways this mirrors the behavior of CAvatar::TryActivatePower().

            PowerUseResult result = CanActivatePower(power, settings.TargetEntityId, settings.TargetPosition, settings.Flags, settings.ItemSourceId);

            Power activePower = ActivePower;

            // This is a continuous power that will be activated later
            if (result == PowerUseResult.MinimumReactivateTime && activePower != null && powerRef == ContinuousPowerDataRef)
                return result;

            if ((result == PowerUseResult.PowerInProgress || result == PowerUseResult.MinimumReactivateTime) && activePower != null)
            {
                // Another continuous power case that will be activated on its own later
                if (powerRef == activePower.PrototypeDataRef && powerRef == ContinuousPowerDataRef)
                    return result;

                // Try to end the current power if it's different
                if (powerRef != activePower.PrototypeDataRef)
                {
                    EndPowerFlags endPowerFlags = EndPowerFlags.ExplicitCancel | EndPowerFlags.ClientRequest;

                    // Interrupt movement client-side (the client is movement authoritative for avatars)
                    if (IsMovementAuthoritative == false && power.IsPartOfAMovementPower())
                        endPowerFlags |= EndPowerFlags.Interrupting;

                    activePower.EndPower(endPowerFlags);
                }

                // Now do this again
                PowerUseResult secondTryResult = CanActivatePower(power, settings.TargetEntityId, settings.TargetPosition, settings.Flags, settings.ItemSourceId);
                if (secondTryResult != PowerUseResult.Success)
                {
                    // If we failed to cancel the current power, try to delay the activation of the new power

                    if (power.GetPowerCategory() == PowerCategoryType.NormalPower)
                    {
                        // This messy thing is coming straight from the client.
                        // The end result is that we set pending action that will be activated in ActivatePostPowerAction().
                        if (powerRef != ActivePowerRef || _pendingAction.PendingActionState != PendingActionState.WaitingForPrevPower)
                        {
                            // Activate the power that's non-recurring and different from the current one after the current one ends (see )
                            if (powerRef != ActivePowerRef || power.IsRecurring() == false)
                            {
                                if (_pendingAction.PendingActionState != PendingActionState.WaitingForPrevPower || power.IsChannelingPower() == false || power.IsContinuous() == false)
                                {
                                    if (_pendingAction.PendingActionState != PendingActionState.WaitingForPrevPower || GetPowerSlot(powerRef) != AbilitySlot.PrimaryAction)
                                    {
                                        Vector3 targetPosition = settings.TargetPosition;
                                        if (power.IsMovementPower())
                                            targetPosition -= RegionLocation.Position;

                                        _pendingAction.SetData(PendingActionState.WaitingForPrevPower, powerRef, settings.TargetEntityId, targetPosition, settings.ItemSourceId);
                                        return result;
                                    }
                                }
                            }
                        }
                        else if (secondTryResult == PowerUseResult.MinimumReactivateTime)
                        {
                            // Delay reactivation of this power until it's over
                            TimeSpan timeSinceActivation = Game.CurrentTime - power.LastActivateGameTime;
                            TimeSpan delay = power.GetActivationTime() - timeSinceActivation;
                            DelayActivatePower(powerRef, delay, settings.TargetEntityId, settings.TargetPosition, settings.ItemSourceId);
                            return result;
                        }
                    }
                }
                else
                {
                    result = secondTryResult;
                }
            }

            // Edge case for toggled power activations during fullscreen movies
            bool toggleAutoActiveDuringFullscreenMovie = result == PowerUseResult.FullscreenMovie &&
                settings.Flags.HasFlag(PowerActivationSettingsFlags.AutoActivate)
                && power.IsToggled();

            // Failed validation despite everything above, clean up and bail out
            if (result != PowerUseResult.Success && result != PowerUseResult.TargetIsMissing && toggleAutoActiveDuringFullscreenMovie == false)
            {
                // Notify the client
                SendActivatePowerFailedMessage(powerRef, result);

                // Clean up throwable powers
                if (power.IsThrowablePower() || power.GetPowerCategory() == PowerCategoryType.ThrowableCancelPower)
                    UnassignPower(powerRef);

                return result;
            }

            // Now do the actual activation
            result = base.ActivatePower(power, ref settings);

            if (result == PowerUseResult.Success)
            {
                PowerPrototype powerProto = power.Prototype;

                // Stop endurance regen if needed
                if (powerProto.DisableEnduranceRegenTypes.HasValue() && powerProto.DisableEnduranceRegenOnActivate)
                {
                    foreach (ManaType manaType in powerProto.DisableEnduranceRegenTypes)
                    {
                        Properties[PropertyEnum.DisableEnduranceRegen, manaType] = true;

                        // Cancel scheduled re-enablement (this will be rescheduled after the power is over)
                        int index = (int)manaType;

                        if (_enableEnduranceRegenEvents[index] != null)
                            Game.GameEventScheduler?.CancelEvent(_enableEnduranceRegenEvents[index]);
                    }
                }

                // Invoke the AvatarUsedPowerEvent
                Player player = GetOwnerOfType<Player>();
                Region region = Region;
                if (player != null && region != null)
                {
                    region.AvatarUsedPowerEvent.Invoke(new(player, this, powerRef, settings.TargetEntityId));

                    if (powerProto.PowerCategory == PowerCategoryType.EmotePower)
                        region.EmotePerformedEvent.Invoke(new(player, powerRef));
                }
            }
            else
            {
                // Activation failed despite the validation, something went wrong
                Logger.Warn($"ActivatePower(): Activation failed for power [{power}] on [{this}] despite passing preliminary validation!");
                SendActivatePowerFailedMessage(powerRef, result);
            }

            return result;
        }

        public override PowerUseResult CanTriggerPower(PowerPrototype powerProto, Power power, PowerActivationSettingsFlags flags)
        {
            if (PendingActionState == PendingActionState.FindingLandingSpot)
                return PowerUseResult.NoFlyingUse;

            if (powerProto.Activation != PowerActivationType.Passive)
            {
                // Do not allow any non-passive powers other than throw cancel when we are throwing
                Power throwablePower = GetThrowablePower();
                if (throwablePower != null && throwablePower.Prototype != powerProto)
                {
                    Power throwCancelPower = GetThrowableCancelPower();
                    if (throwCancelPower == null) return Logger.WarnReturn(PowerUseResult.GenericError, "CanTriggerPower(): throwCancelPower == null");
                    
                    if (throwCancelPower.Prototype != powerProto)
                        return PowerUseResult.PowerInProgress;
                }
            }

            if (IsPowerAllowedInCurrentTransformMode(powerProto.DataRef) == false)
                return PowerUseResult.NotAllowedByTransformMode;

            return base.CanTriggerPower(powerProto, power, flags);
        }

        public override void ActivatePostPowerAction(Power power, EndPowerFlags flags)
        {
            // Clean up the property used for resurrecting other avatars if needed.
            if (power.PrototypeDataRef == AvatarPrototype.ResurrectOtherEntityPower)
                Properties.RemoveProperty(PropertyEnum.PendingResurrectEntityId);

            // Try to activate pending action (see CAvatar::ActivatePostPowerAction() for reference)
            if (ActivePowerRef == PrototypeId.Invalid && power.IsProcEffect() == false && power.TriggersComboPowerOnEvent(PowerEventType.OnPowerEnd) == false)
            {
                PrototypeId pendingPowerProtoRef = _pendingAction.PowerProtoRef;
                if (pendingPowerProtoRef != PrototypeId.Invalid && IsInPendingActionState(PendingActionState.WaitingForPrevPower))
                {
                    Power nextPower = GetPower(pendingPowerProtoRef);
                    if (nextPower == null)
                    {
                        Logger.Warn("ActivatePostPowerAction(): nextPower == null");
                        return;
                    }

                    PowerActivationSettings settings = new();
                    FixupPendingActivateSettings(nextPower, ref settings);

                    CancelPendingAction();

                    PowerUseResult result = CanActivatePower(nextPower, settings.TargetEntityId, settings.TargetPosition);
                    if (result == PowerUseResult.Success)
                        ActivatePower(nextPower, ref settings);
                    else
                        SendActivatePowerFailedMessage(nextPower.PrototypeDataRef, result);
                }
            }

            // Base implementation from common code below
            base.ActivatePostPowerAction(power, flags);

            // Try to reactivate the current continuous power
            if (_continuousPowerData.PowerProtoRef == PrototypeId.Invalid)
                return;

            if (power.IsProcEffect() || power.IsItemPower())
                return;

            if (_continuousPowerData.PowerProtoRef == power.PrototypeDataRef && power.TriggersComboPowerOnEvent(PowerEventType.OnPowerEnd))
                return;

            if (flags.HasFlag(EndPowerFlags.ExplicitCancel) && power.IsRecurring() == false)
                return;

            if (flags.HasFlag(EndPowerFlags.ExitWorld) || flags.HasFlag(EndPowerFlags.Unassign))
                return;

            CheckContinuousPower();
        }

        public override void UpdateRecurringPowerApplication(PowerApplication powerApplication, PrototypeId powerProtoRef)
        {
            base.UpdateRecurringPowerApplication(powerApplication, powerProtoRef);

            // Update target from continuous power
            if (powerProtoRef == _continuousPowerData.PowerProtoRef)
            {
                powerApplication.TargetEntityId = _continuousPowerData.TargetId;
                powerApplication.TargetPosition = _continuousPowerData.TargetPosition;
            }
        }

        public override bool ShouldContinueRecurringPower(Power power, ref EndPowerFlags flags)
        {
            if (base.ShouldContinueRecurringPower(power, ref flags) == false)
                return false;

            if (power == null) return Logger.WarnReturn(false, "ShouldContinueRecurringPower(): power == null");

            // Check endurance (mana) costs
            foreach (PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto in GetPrimaryResourceManaBehaviors())
            {
                float endurance = Properties[PropertyEnum.Endurance, primaryManaBehaviorProto.ManaType];
                float enduranceCost = power.GetEnduranceCost(primaryManaBehaviorProto.ManaType, true);

                if (endurance < enduranceCost)
                {
                    flags |= EndPowerFlags.ExplicitCancel | EndPowerFlags.NotEnoughEndurance;
                    return false;
                }
            }

            // Check if continuous power changed
            if (ContinuousPowerDataRef != power.PrototypeDataRef)
            {
                TimeSpan timeSinceLastActivation = Game.CurrentTime - power.LastActivateGameTime;

                if (power.GetChannelMinTime() > timeSinceLastActivation)
                    return true;

                flags |= EndPowerFlags.ExplicitCancel;
                return false;
            }

            // Check the power's CanTriggerEval
            return power.CheckCanTriggerEval();
        }

        public void SetContinuousPower(PrototypeId powerProtoRef, ulong targetId, Vector3 targetPosition, int randomSeed, bool notifyOwner)
        {
            // Validate client input
            Power power = GetPower(powerProtoRef);

            if (powerProtoRef != PrototypeId.Invalid && power == null)
                return;

            if (power != null && ((power.IsContinuous() || power.IsRecurring()) == false))
                return;

            // Check if anything changed
            bool noChanges = true;
            noChanges &= powerProtoRef == _continuousPowerData.PowerProtoRef;
            noChanges &= targetId == _continuousPowerData.TargetId;
            noChanges &= targetId == InvalidId && Vector3.DistanceSquared2D(_continuousPowerData.TargetPosition, targetPosition) <= 16f;
            if (noChanges)
                return;

            // Update data
            _continuousPowerData.SetData(powerProtoRef, targetId, targetPosition, InvalidId);
            _continuousPowerData.RandomSeed = randomSeed;

            if (_continuousPowerData.PowerProtoRef != PrototypeId.Invalid)
                ScheduleRecheckContinuousPower(StandardContinuousPowerRecheckDelay);

            // Notify clients
            PlayerConnectionManager networkManager = Game.NetworkManager;
            List<PlayerConnection> interestedClientList = ListPool<PlayerConnection>.Instance.Get();
            if (networkManager.GetInterestedClients(interestedClientList, this, AOINetworkPolicyValues.AOIChannelProximity, notifyOwner == false))
            {
                var continuousPowerUpdateMessage = NetMessageContinuousPowerUpdateToClient.CreateBuilder()
                    .SetIdAvatar(Id)
                    .SetPowerPrototypeId((ulong)powerProtoRef)
                    .SetIdTargetEntity(targetId)
                    .SetTargetPosition(targetPosition.ToNetStructPoint3())
                    .SetRandomSeed((uint)randomSeed)
                    .Build();

                networkManager.SendMessageToMultiple(interestedClientList, continuousPowerUpdateMessage);
            }

            ListPool<PlayerConnection>.Instance.Return(interestedClientList);
        }

        public void ClearContinuousPower()
        {
            _continuousPowerData.SetData(PrototypeId.Invalid, InvalidId, Vector3.Zero, InvalidId);
            _continuousPowerData.RandomSeed = 0;

            if (_recheckContinuousPowerEvent.IsValid)
                Game.GameEventScheduler.CancelEvent(_recheckContinuousPowerEvent);
        }

        public void CheckContinuousPower()
        {
            // We could make this a bit cleaner with just a little bit of goto... After all... why not? Why shouldn't I?
            if (IsInWorld && _continuousPowerData.PowerProtoRef != PrototypeId.Invalid)
            {
                ulong targetId = _continuousPowerData.TargetId;
                Vector3 targetPosition = _continuousPowerData.TargetPosition;

                Power continuousPower = GetPower(_continuousPowerData.PowerProtoRef);
                if (continuousPower == null)
                {
                    Logger.Warn(string.Format(
                        "CheckContinuousPower(): Could not find continuous power to activate after previous power end.\nAvatar: {0}\nPower proto:{1}",
                        this,
                        GameDatabase.GetPrototypeName(_continuousPowerData.PowerProtoRef)));
                    return;
                }

                // We should either have no active power or the continuous powers needs to be recurring
                if (IsExecutingPower == false || (ActivePowerRef == _continuousPowerData.PowerProtoRef && continuousPower.IsRecurring()))
                {
                    WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(targetId);

                    bool targetIsValid = true;

                    bool targetIsAvailable = target != null && target.IsInWorld && target.IsTargetable(this);
                    if (continuousPower.NeedsTarget())
                    {
                        // The power needs a target and the specified target is not available
                        if (targetIsAvailable == false)
                            targetIsValid = false;
                    }
                    else if (targetId != InvalidId && targetIsAvailable == false)
                    {
                        // The power does not need a target, but it has one anyway, but it is not available
                        targetIsValid = false;
                    }

                    if (targetIsValid)
                    {
                        if (target?.RegionLocation.IsValid() == true)
                        {
                            // Update target position
                            switch (continuousPower.GetTargetingShape())
                            {
                                case TargetingShapeType.Self:
                                case TargetingShapeType.SingleTarget:
                                    targetPosition = target.RegionLocation.Position;
                                    break;

                                case TargetingShapeType.SkillShot:
                                case TargetingShapeType.SkillShotAlongGround:
                                    if (continuousPower.AlwaysTargetsMousePosition() == false)
                                        targetPosition = target.RegionLocation.Position;
                                    break;

                                default:
                                    if (continuousPower.AlwaysTargetsMousePosition() == false)
                                        targetPosition = target.RegionLocation.ProjectToFloor();
                                    break;
                            }
                        }

                        if (continuousPower.IsActive && continuousPower.IsRecurring())
                        {
                            // Update target position for recurring powers
                            _continuousPowerData.SetData(_continuousPowerData.PowerProtoRef, _continuousPowerData.TargetId,
                                targetPosition, _continuousPowerData.SourceItemId);
                        }
                        else
                        {
                            // Activate the power again
                            PowerActivationSettings settings = new(targetId, targetPosition, RegionLocation.Position);
                            settings.PowerRandomSeed = _continuousPowerData.RandomSeed;
                            settings.Flags |= PowerActivationSettingsFlags.Continuous;

                            // Update random seed
                            GRandom random = new(_continuousPowerData.RandomSeed);
                            _continuousPowerData.RandomSeed = random.Next(0, 10000);

                            // We omit ActivateContinuousPower(), continuousPower.UpdateContinuousPowerActivationSettings()
                            // and onContinuousPowerResumed becaused they are not really needed on the server.

                            PowerUseResult result = CanActivatePower(continuousPower, targetId, targetPosition);
                            if (result == PowerUseResult.Success)
                                ActivatePower(continuousPower, ref settings);
                            //else
                            //    Logger.Debug($"CheckContinuousPower(): result={result}");
                        }
                    }
                }

                // onContinuousPowerFailedActivate()
            }

            if (_continuousPowerData.PowerProtoRef != PrototypeId.Invalid)
                ScheduleRecheckContinuousPower(StandardContinuousPowerRecheckDelay);
        }

        public bool IsInPendingActionState(PendingActionState pendingActionState)
        {
            return _pendingAction.PendingActionState == pendingActionState;
        }

        public void CancelPendingAction()
        {
            _pendingAction.Clear();
        }

        public bool IsCombatActive()
        {
            // TODO: Check PropertyEnum.LastInflictedDamageTime
            return true;
        }

        public override TimeSpan GetPowerInterruptCooldown(PowerPrototype powerProto)
        {
            // Not interrupt cooldowns for avatars
            return TimeSpan.Zero;
        }

        public override bool HasPowerWithKeyword(PowerPrototype powerProto, PrototypeId keywordProtoRef)
        {
            KeywordPrototype keywordPrototype = GameDatabase.GetPrototype<KeywordPrototype>(keywordProtoRef);
            if (keywordPrototype == null) return Logger.WarnReturn(false, "HasPowerWithKeyword(): keywordPrototype == null");

            // Check if the assigned power has the specified keyword
            Power power = GetPower(powerProto.DataRef);
            if (power != null)
                return power.HasKeyword(keywordPrototype);

            // Check if there are any keyword override in our properties
            int powerKeywordChange = Properties[PropertyEnum.PowerKeywordChange, powerProto.DataRef, keywordProtoRef];

            return powerKeywordChange == (int)TriBool.True || (powerProto.HasKeyword(keywordPrototype) && powerKeywordChange != (int)TriBool.False);
        }

        public bool IsValidTargetForCurrentPower(WorldEntity target)
        {
            if (_pendingAction.PowerProtoRef != PrototypeId.Invalid && IsInPendingActionState(PendingActionState.Targeting))
            {
                var power = GetPower(_pendingAction.PowerProtoRef);
                if (power == null) return false;
                return power.IsValidTarget(target);
            }
            else
                return IsHostileTo(target);
        }

        public bool InitPowerFromCreationItem(Item item)
        {
            if (item.GetOwnerOfType<Player>() != GetOwnerOfType<Player>()) return Logger.WarnReturn(false, "InitPowerFromCreationItem(): item.GetOwnerOfType<Player>() != GetOwnerOfType<Player>()");

            if (item.GetPowerGranted(out PrototypeId powerProtoRef) == false) return Logger.WarnReturn(false, "InitPowerFromCreationItem(): item.GetPowerGranted(out PrototypeId powerProtoRef) == false");
            if (powerProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "InitPowerFromCreationItem(): powerProtoRef == PrototypeId.Invalid");

            Power power = GetPower(powerProtoRef);
            if (power == null)
                return false;

            power.Properties[PropertyEnum.ItemLevel] = item.Properties[PropertyEnum.ItemLevel];
            return true;
        }

        public ulong FindAbilityItem(ItemPrototype itemProto, ulong skipItemId = InvalidId)
        {
            List<Inventory> inventoryList = ListPool<Inventory>.Instance.Get();

            try
            {
                // Add equipment inventories
                foreach (Inventory inventory in new InventoryIterator(this, InventoryIterationFlags.Equipment))
                    inventoryList.Add(inventory);

                // Add general inventories if needed
                if (itemProto.AbilitySettings == null || itemProto.AbilitySettings.OnlySlottableWhileEquipped == false)
                {
                    Player playerOwner = GetOwnerOfType<Player>();
                    if (playerOwner == null) return Logger.WarnReturn(InvalidId, "FindAbilityItem(): playerOwner == null");

                    foreach (Inventory inventory in new InventoryIterator(playerOwner, InventoryIterationFlags.PlayerGeneral | InventoryIterationFlags.PlayerGeneralExtra))
                        inventoryList.Add(inventory);
                }

                // Do the search
                EntityManager entityManager = Game.EntityManager;

                foreach (Inventory inventory in inventoryList)
                {
                    foreach (var entry in inventory)
                    {
                        ulong itemId = entry.Id;

                        Item item = entityManager.GetEntity<Item>(itemId);
                        if (item == null)
                        {
                            Logger.Warn("FindAbilityItem(): item == null");
                            continue;
                        }

                        if (item.PrototypeDataRef != itemProto.DataRef)
                            continue;

                        if (skipItemId != InvalidId && itemId == skipItemId)
                            continue;

                        return itemId;
                    }
                }

                return InvalidId;
            }
            finally
            {
                // Make sure our inventory list is returned to the pool for reuse when we are done
                ListPool<Inventory>.Instance.Return(inventoryList);
            }
        }

        public ulong FindOwnedItemThatGrantsPower(PrototypeId powerProtoRef)
        {
            ulong itemId = InvalidId;

            // Search avatar equipment
            foreach (Inventory inventory in new InventoryIterator(this, InventoryIterationFlags.Equipment))
            {
                itemId = FindOwnedItemThatGrantsPowerHelper(powerProtoRef, inventory);
                if (itemId != InvalidId)
                    return itemId;
            }

            // Search the player's general inventories
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(InvalidId, "FindOwnedItemThatGrantsPower(): player == null");

            foreach (Inventory inventory in new InventoryIterator(player, InventoryIterationFlags.PlayerGeneral | InventoryIterationFlags.PlayerGeneralExtra))
            {
                itemId = FindOwnedItemThatGrantsPowerHelper(powerProtoRef, inventory);
                if (itemId != InvalidId)
                    return itemId;
            }

            return itemId;
        }

        private ulong FindOwnedItemThatGrantsPowerHelper(PrototypeId powerProtoRef, Inventory inventory)
        {
            EntityManager entityManager = Game.EntityManager;

            foreach (var entry in inventory)
            {
                Item item = entityManager.GetEntity<Item>(entry.Id);
                if (item == null)
                {
                    Logger.Warn("FindOwnedItemThatGrantsPowerHelper(): item == null");
                    continue;
                }

                if (item.GetPowerGranted(out PrototypeId powerGrantedProtoRef) && powerGrantedProtoRef == powerProtoRef)
                    return item.Id;
            }

            return InvalidId;
        }

        private bool InitializePowers()
        {
            PowerIndexProperties defaultIndexProps = new(0, CharacterLevel, CombatLevel);

            AssignGameFunctionPowers(defaultIndexProps);

            // Initialize resources
            InitializePrimaryManaBehaviors();
            InitializeSecondaryManaBehaviors();

            AssignItemPowers();

            AssignEmotePowers(defaultIndexProps);

            // Assign hidden passive powers (this needs to happen before updating power progression powers)
            AssignHiddenPassivePowers(defaultIndexProps);

            UpdatePowerProgressionPowers(false);

            UpdateTravelPower();

            return true;
        }

        private bool AssignGameFunctionPowers(in PowerIndexProperties indexProps)
        {
            AvatarPrototype avatarPrototype = AvatarPrototype;

            // Add game function powers (the order is the same as captured packets)
            AssignPower(GameDatabase.GlobalsPrototype.AvatarSwapChannelPower, indexProps);
            AssignPower(GameDatabase.GlobalsPrototype.AvatarSwapInPower, indexProps);
            AssignPower(GameDatabase.GlobalsPrototype.ReturnToHubPower, indexProps);
            AssignPower(GameDatabase.GlobalsPrototype.ReturnToFieldPower, indexProps);
            AssignPower(GameDatabase.GlobalsPrototype.TeleportToPartyMemberPower, indexProps);
            AssignPower(GameDatabase.GlobalsPrototype.TeamUpSummonPower, indexProps);
            AssignPower(GameDatabase.GlobalsPrototype.PetTechVacuumPower, indexProps);
            AssignPower(avatarPrototype.ResurrectOtherEntityPower, indexProps);
            AssignPower(avatarPrototype.StatsPower, indexProps);
            ScheduleStatsPowerRefresh();
            AssignPower(GameDatabase.GlobalsPrototype.AvatarHealPower, indexProps);

            return true;
        }

        private bool AssignItemPowers()
        {
            // This has similar structure to FindAbilityItem()
            Player playerOwner = GetOwnerOfType<Player>();
            if (playerOwner == null) return Logger.WarnReturn(false, "AssignItemPowers(): playerOwner == null");

            List<Inventory> inventoryList = ListPool<Inventory>.Instance.Get();

            try
            {
                // Add equipment inventories
                foreach (Inventory inventory in new InventoryIterator(this, InventoryIterationFlags.Equipment))
                    inventoryList.Add(inventory);

                // Add general inventories
                foreach (Inventory inventory in new InventoryIterator(playerOwner, InventoryIterationFlags.PlayerGeneral | InventoryIterationFlags.PlayerGeneralExtra))
                    inventoryList.Add(inventory);

                EntityManager entityManager = Game.EntityManager;
                int characterLevel = CharacterLevel;
                int combatLevel = CombatLevel;

                foreach (Inventory inventory in inventoryList)
                {
                    foreach (var entry in inventory)
                    {
                        ulong itemId = entry.Id;

                        Item item = entityManager.GetEntity<Item>(itemId);
                        if (item == null)
                        {
                            Logger.Warn("AssignItemPowers(): item == null");
                            continue;
                        }

                        ItemPrototype itemProto = item.ItemPrototype;
                        if (itemProto == null)
                        {
                            Logger.Warn("AssignItemPowers(): itemProto == null");
                            continue;
                        }

                        PrototypeId itemPowerProtoRef = PrototypeId.Invalid;

                        PrototypeId onUsePowerProtoRef = item.OnUsePower;
                        PrototypeId onEquipPowerProtoRef = item.OnEquipPower;

                        if (onUsePowerProtoRef != PrototypeId.Invalid)
                        {
                            if (itemProto.AbilitySettings == null ||
                                itemProto.AbilitySettings.OnlySlottableWhileEquipped == false ||
                                inventory.IsEquipment)
                            {
                                itemPowerProtoRef = onUsePowerProtoRef;
                            }
                        }
                        else if (onEquipPowerProtoRef != PrototypeId.Invalid)
                        {
                            if (inventory.IsEquipment)
                                itemPowerProtoRef = onEquipPowerProtoRef;
                        }

                        if (itemPowerProtoRef != PrototypeId.Invalid && GetPower(itemPowerProtoRef) == null)
                        {
                            int itemLevel = item.Properties[PropertyEnum.ItemLevel];
                            float itemVariation = item.Properties[PropertyEnum.ItemVariation];
                            PowerIndexProperties indexProps = new(0, characterLevel, combatLevel, itemLevel, itemVariation);

                            if (AssignPower(itemPowerProtoRef, indexProps) == null)
                                Logger.Warn($"AssignItemPowers(): Failed to assign item power {itemPowerProtoRef.GetName()} to avatar {this}");
                        }
                    }
                }

                return true;
            }
            finally
            {
                // Make sure our inventory list is returned to the pool for reuse when we are done
                ListPool<Inventory>.Instance.Return(inventoryList);
            }
        }

        private bool AssignEmotePowers(in PowerIndexProperties indexProps)
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "AssignEmotePowers(): player == null");

            PlayerPrototype playerPrototype = player.Prototype as PlayerPrototype;

            // Starting emotes
            foreach (AbilityAssignmentPrototype emoteAssignment in playerPrototype.StartingEmotes)
            {
                PrototypeId emoteProtoRef = emoteAssignment.Ability;
                if (GetPower(emoteProtoRef) != null)
                    continue;

                if (AssignPower(emoteProtoRef, indexProps) == null)
                    Logger.Warn($"AssignEmotePowers(): Failed to assign starting emote {GameDatabase.GetPrototypeName(emoteProtoRef)} to {this}");
            }

            // Unlockable emotes
            foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.AvatarEmoteUnlocked, PrototypeDataRef))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId emoteProtoRef);
                if (GetPower(emoteProtoRef) != null)
                    continue;

                if (AssignPower(emoteProtoRef, indexProps) == null)
                    Logger.Warn($"AssignEmotePowers(): Failed to assign unlockable emote {GameDatabase.GetPrototypeName(emoteProtoRef)} to {this}");
            }

            return true;
        }

        private bool AssignHiddenPassivePowers(in PowerIndexProperties indexProps)
        {
            AvatarPrototype avatarPrototype = AvatarPrototype;

            if (avatarPrototype.HiddenPassivePowers.HasValue())
            {
                foreach (AbilityAssignmentPrototype abilityAssignmentProto in avatarPrototype.HiddenPassivePowers)
                {
                    if (GetPower(abilityAssignmentProto.Ability) == null)
                        AssignPower(abilityAssignmentProto.Ability, indexProps);
                }
            }

            return true;
        }

        private void AssignRegionPowers()
        {
            Region region = Region;
            if (region == null)
                return;

            // Assign and activate region powers
            PowerIndexProperties indexProps = new(0, CharacterLevel, CombatLevel);
            
            foreach (var kvp in region.Properties.IteratePropertyRange(PropertyEnum.RegionAvatarPower))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId powerProtoRef);
                if (powerProtoRef == PrototypeId.Invalid)
                    continue;

                if (AssignPower(powerProtoRef, indexProps) == null)
                    continue;

                // Force activate this power
                PowerActivationSettings settings = new(Id, Vector3.Zero, RegionLocation.Position);
                settings.Flags |= PowerActivationSettingsFlags.NotifyOwner;

                if (ActivatePower(powerProtoRef, ref settings) != PowerUseResult.Success)
                    Logger.Warn($"AssignRegionPowers(): Failed to activate power {powerProtoRef.GetName()} in region [{region}]");
            }

            // Assign metagame bodyslide overrides (e.g. for PvP)
            EntityManager entityManager = Game.EntityManager;
            foreach (ulong metaGameId in Region.MetaGames)
            {
                MetaGame metaGame = entityManager.GetEntity<MetaGame>(metaGameId);
                if (metaGame == null)
                    continue;

                MetaGamePrototype metaGameProto = metaGame.MetaGamePrototype;
                if (metaGameProto == null || metaGameProto.BodysliderOverride == PrototypeId.Invalid)
                    continue;

                AssignPower(metaGameProto.BodysliderOverride, new());
            }
        }

        private bool RestoreSelfAppliedPowerConditions()
        {
            // Powers are unassigned when avatar exits world, but the conditions remain.
            // We need to reconnect existing conditions to the newly reassigned powers.

            ConditionCollection conditionCollection = ConditionCollection;
            if (conditionCollection == null) return Logger.WarnReturn(false, "RestoreSelfAppliedPowerConditions(): conditionCollection == null");

            List<ulong> conditionCleanupList = ListPool<ulong>.Instance.Get();

            // Try to restore condition connections for self-applied powers
            foreach (Condition condition in ConditionCollection.IterateConditions(false))
            {
                PowerPrototype powerProto = condition.CreatorPowerPrototype;
                if (powerProto == null)
                    continue;

                if (Power.GetTargetingShape(powerProto) != TargetingShapeType.Self)
                    continue;

                if (conditionCollection.TryRestorePowerCondition(condition, this) == false)
                    conditionCleanupList.Add(condition.Id);
            }

            // Clean up conditions that are no longer valid
            foreach (ulong conditionId in conditionCleanupList)
                conditionCollection.RemoveCondition(conditionId);

            ListPool<ulong>.Instance.Return(conditionCleanupList);
            return true;
        }

        protected override bool CanThrow(WorldEntity throwableEntity)
        {
            if (throwableEntity == null) return Logger.WarnReturn(false, "CanThrow(): throwableEntity == null");

            PrototypeId throwablePowerProtoRef = throwableEntity.Properties[PropertyEnum.ThrowablePower];
            PowerPrototype throwablePowerProto = throwablePowerProtoRef.As<PowerPrototype>();
            if (throwablePowerProto == null) return Logger.WarnReturn(false, "CanThrow(): throwableEntity == null");

            bool success = true;

            // Validate
            success &= IsAliveInWorld;
            success &= IsExecutingPower == false;
            success &= throwableEntity.IsThrowableBy(this);
            success &= InInteractRange(throwableEntity, InteractionMethod.Throw);
            success &= CanTriggerPower(throwablePowerProto, null, PowerActivationSettingsFlags.None) == PowerUseResult.Success;

            // Cancel the throw power on the client to prevent it from getting stuck
            if (success == false)
                SendActivatePowerFailedMessage(throwablePowerProtoRef, PowerUseResult.GenericError);
            
            return success;
        }

        private int GetThrowability()
        {
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, Properties);
            evalContext.SetReadOnlyVar_ProtoRefVectorPtr(EvalContext.Var1, Keywords);

            return Eval.RunInt(GameDatabase.AdvancementGlobalsPrototype.AvatarThrowabilityEvalPrototype, evalContext);
        }

        private bool FixupPendingActivateSettings(Power power, ref PowerActivationSettings settings)
        {
            settings.TargetEntityId = _pendingAction.TargetId;
            settings.TargetPosition = _pendingAction.TargetPosition;
            settings.UserPosition = RegionLocation.Position;
            settings.PowerRandomSeed = Game.Random.Next(1, 10000);

            if (power.Prototype is MovementPowerPrototype movementPowerProto)
            {
                WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);
                if (target == null)
                    settings.TargetPosition += RegionLocation.Position;
                else
                    settings.TargetPosition = target.RegionLocation.Position;
            }

            if (power.IsInRange(settings.TargetPosition, RangeCheckType.Activation) == false)
            {
                PowerPrototype powerProto = power.Prototype;
                if (powerProto == null) return Logger.WarnReturn(false, "FixupPendingActivateSettings(): powerProto == null");

                if (powerProto.WhenOutOfRange == WhenOutOfRangeType.ActivateInDirection ||
                    (powerProto.WhenOutOfRange == WhenOutOfRangeType.MoveIfTargetingMOB && settings.TargetEntityId == InvalidId))
                {
                    Vector3 userPosition = RegionLocation.Position;
                    Vector3 direction = Vector3.SafeNormalize(settings.TargetPosition - userPosition);
                    settings.TargetPosition = userPosition + (direction * power.GetRange());
                }
            }

            return true;
        }

        private bool DelayActivatePower(PrototypeId powerProtoRef, TimeSpan delay, ulong targetId, Vector3 targetPosition, ulong sourceItemId)
        {
            if (_pendingAction.SetData(PendingActionState.DelayedPowerActivate, powerProtoRef, targetId, targetPosition, sourceItemId) == false)
                return false;

            EventScheduler scheduler = Game.GameEventScheduler;

            if (_delayedPowerActivationEvent.IsValid)
                scheduler.CancelEvent(_delayedPowerActivationEvent);

            ScheduleEntityEvent(_delayedPowerActivationEvent, delay);

            return true;
        }

        private bool DelayedPowerActivation()
        {
            if (IsInPendingActionState(PendingActionState.DelayedPowerActivate) == false)
                return false;

            ulong targetId = _pendingAction.TargetId;
            Vector3 targetPosition = _pendingAction.TargetPosition;
            ulong sourceItemId = _pendingAction.SourceItemId;
            Power power = GetPower(_pendingAction.PowerProtoRef);

            CancelPendingAction();

            if (power == null)
                return false;

            if (CanActivatePower(power, targetId, targetPosition, PowerActivationSettingsFlags.None, sourceItemId) != PowerUseResult.Success)
                return false;

            PowerActivationSettings settings = new(targetId, targetPosition, RegionLocation.Position);
            settings.ItemSourceId = sourceItemId;

            ActivatePower(power.PrototypeDataRef, ref settings);

            return true;
        }

        private bool SendActivatePowerFailedMessage(PrototypeId powerProtoRef, PowerUseResult result)
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "SendActivatePowerFailedMessage(): player == null");

            if (player.InterestedInEntity(this, AOINetworkPolicyValues.AOIChannelOwner))
            {
                NetMessageActivatePowerFailed activatePowerFailedMessage = NetMessageActivatePowerFailed.CreateBuilder()
                    .SetAvatarIndex(0)  // TODO: Console couch co-op
                    .SetPowerPrototypeId((ulong)powerProtoRef)
                    .SetReason((uint)result)
                    .Build();

                player.SendMessage(activatePowerFailedMessage);
            }

            return true;
        }

        private void ScheduleStatsPowerRefresh()
        {
            EventScheduler scheduler = Game.GameEventScheduler;
            scheduler.CancelEvent(_refreshStatsPowerEvent);
            ScheduleEntityEvent(_refreshStatsPowerEvent, TimeSpan.Zero);
        }

        private bool RefreshStatsPower()
        {
            if (IsInWorld == false)
                return false;

            Power statsPower = GetPower(AvatarPrototype.StatsPower);
            if (statsPower == null) return Logger.WarnReturn(false, "RefreshStatsPower(): statsPower == null");

            // Reactivate the stats power to force it to recalculate the condition it applies
            statsPower.EndPower(EndPowerFlags.ExplicitCancel | EndPowerFlags.Force);

            Vector3 position = RegionLocation.Position;
            PowerActivationSettings settings = new(Id, position, position);
            ActivatePower(statsPower, ref settings);

            return true;
        }

        #endregion

        #region Power Ranks

        public InteractionValidateResult CanUpgradeUltimate()
        {
            PrototypeId ultimateRef = UltimatePowerRef;
            if (ultimateRef == PrototypeId.Invalid) return Logger.WarnReturn(InteractionValidateResult.UnknownFailure, "CanUpgradeUltimate(): ultimateRef == ultimateRef == PrototypeId.Invalid");

            GetPowerProgressionInfo(ultimateRef, out PowerProgressionInfo powerInfo);
            int powerSpec = GetPowerSpecIndexActive();

            int rankMax = GetMaxPossibleRankForPowerAtCurrentLevel(ref powerInfo, powerSpec);
            if (rankMax < 0)
                return InteractionValidateResult.AvatarUltimateNotUnlocked;

            int rankBase = ComputePowerRankBase(ref powerInfo, powerSpec);
            if (rankBase >= rankMax)
                return InteractionValidateResult.AvatarUltimateAlreadyMaxedOut;

            return InteractionValidateResult.Success;
        }

        protected override int ComputePowerRankBase(ref PowerProgressionInfo powerInfo, int powerSpecIndexActive)
        {
            // Check avatar-specific overrides
            if (powerInfo.IsInPowerProgression)
            {
                // Talents
                if (powerInfo.IsTalent)
                {
                    if (powerInfo.GetRequiredLevel() > CharacterLevel)
                        return PowerProgressionInfo.RankLocked;

                    return IsTalentPowerEnabledForSpec(powerInfo.PowerRef, powerSpecIndexActive) ? 1 : 0;
                }
            }
            else
            {
                // Mapped powers
                PrototypeId originalPowerProtoRef = GetOriginalPowerFromMappedPower(powerInfo.PowerRef);
                if (originalPowerProtoRef != PrototypeId.Invalid)
                    return GetPowerRank(originalPowerProtoRef);

                // Transform powers
                AvatarPrototype avatarProto = AvatarPrototype;
                if (avatarProto == null) return Logger.WarnReturn(0, "ComputePowerRankBase(): avatarProto == null");

                TransformModePrototype transformModeProto = avatarProto.FindTransformModeThatAssignsPower(powerInfo.PowerRef);
                if (transformModeProto != null && transformModeProto.UseRankOfPower != PrototypeId.Invalid)
                    return GetPowerRank(transformModeProto.UseRankOfPower);
            }

            // Fall back to base implementation if we didn't find any avatar-specific overrides 
            return base.ComputePowerRankBase(ref powerInfo, powerSpecIndexActive);
        }

        protected override bool UpdatePowerRank(ref PowerProgressionInfo powerInfo, bool forceUnassign)
        {
            PowerPrototype powerProto = powerInfo.PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "UpdatePowerRank(): powerProto == null");

            if (powerProto.Activation == PowerActivationType.Passive && IsPowerAllowedInCurrentTransformMode(powerInfo.PowerRef) == false)
                return false;

            if (base.UpdatePowerRank(ref powerInfo, forceUnassign) == false)
                return false;

            // Update mapped power if needed
            PrototypeId mappedPowerRef = powerInfo.MappedPowerRef;
            if (mappedPowerRef != PrototypeId.Invalid)
            {
                // Check for recursion
                if (mappedPowerRef == powerInfo.PowerRef)
                    return Logger.WarnReturn(false, $"UpdatePowerRank(): Recursion detected for mapped power {mappedPowerRef.GetName()}");

                PowerProgressionInfo mappedPowerInfo = new();
                mappedPowerInfo.InitNonProgressionPower(mappedPowerRef);
                UpdatePowerRank(ref mappedPowerInfo, forceUnassign);
            }

            // Fire scoring events
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "UpdatePowerRank(): player == null");

            int powerRank = GetPowerRank(powerProto.DataRef);

            player.OnScoringEvent(new(ScoringEventType.PowerRank, powerProto, powerRank));

            if (powerProto.IsUltimate)
                player.OnScoringEvent(new(ScoringEventType.PowerRankUltimate, powerRank));

            return true;
        }


        #endregion

        #region Power Progression

        public override int GetLatestPowerProgressionVersion()
        {
            if (AvatarPrototype == null) return 0;
            return AvatarPrototype.PowerProgressionVersion;
        }

        public override bool HasPowerInPowerProgression(PrototypeId powerRef)
        {
            if (GameDataTables.Instance.PowerOwnerTable.GetPowerProgressionEntry(PrototypeDataRef, powerRef) != null)
                return true;

            if (GameDataTables.Instance.PowerOwnerTable.GetTalentEntry(PrototypeDataRef, powerRef) != null)
                return true;

            return false;
        }

        public override bool GetPowerProgressionInfo(PrototypeId powerProtoRef, out PowerProgressionInfo powerInfo)
        {
            powerInfo = new();

            if (powerProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, "GetPowerProgressionInfo(): powerProtoRef == PrototypeId.Invalid");

            AvatarPrototype avatarProto = AvatarPrototype;
            if (avatarProto == null)
                return Logger.WarnReturn(false, "GetPowerProgressionInfo(): avatarProto == null");

            PrototypeId progressionInfoPower = powerProtoRef;
            PrototypeId mappedPowerRef;

            // Check if this is a mapped power
            PrototypeId originalPowerRef = GetOriginalPowerFromMappedPower(powerProtoRef);
            if (originalPowerRef != PrototypeId.Invalid)
            {
                mappedPowerRef = powerProtoRef;
                progressionInfoPower = originalPowerRef;
            }
            else
            {
                mappedPowerRef = GetMappedPowerFromOriginalPower(powerProtoRef);
            }

            PowerOwnerTable powerOwnerTable = GameDataTables.Instance.PowerOwnerTable;

            // Initialize info
            // Case 1 - Progression Power
            PowerProgressionEntryPrototype powerProgressionEntry = powerOwnerTable.GetPowerProgressionEntry(avatarProto.DataRef, progressionInfoPower);
            if (powerProgressionEntry != null)
            {
                PrototypeId powerTabRef = powerOwnerTable.GetPowerProgressionTab(avatarProto.DataRef, progressionInfoPower);
                if (powerTabRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "GetPowerProgressionInfo(): powerTabRef == PrototypeId.Invalid");

                powerInfo.InitForAvatar(powerProgressionEntry, mappedPowerRef, powerTabRef);
                return powerInfo.IsValid;
            }

            // Case 2 - Talent
            var talentEntryPair = powerOwnerTable.GetTalentEntryPair(avatarProto.DataRef, progressionInfoPower);
            var talentGroupPair = powerOwnerTable.GetTalentGroupPair(avatarProto.DataRef, progressionInfoPower);
            if (talentEntryPair.Item1 != null && talentGroupPair.Item1 != null)
            {
                powerInfo.InitForAvatar(talentEntryPair.Item1, talentGroupPair.Item1, talentEntryPair.Item2, talentGroupPair.Item2);
                return powerInfo.IsValid;
            }

            // Case 3 - Non-Progression Power
            powerInfo.InitNonProgressionPower(powerProtoRef);
            return powerInfo.IsValid;
        }

        public override bool GetPowerProgressionInfos(List<PowerProgressionInfo> powerInfoList)
        {
            AvatarPrototype avatarProto = AvatarPrototype;
            if (avatarProto == null) return Logger.WarnReturn(false, "GetPowerProgressionInfos(): avatarProto == null");

            if (avatarProto.PowerProgressionTables.HasValue())
            {
                foreach (PowerProgressionTablePrototype powerProgTableProto in avatarProto.PowerProgressionTables)
                {
                    if (powerProgTableProto.PowerProgressionEntries.IsNullOrEmpty())
                        continue;

                    foreach (PowerProgressionEntryPrototype powerProgEntry in powerProgTableProto.PowerProgressionEntries)
                    {
                        AbilityAssignmentPrototype abilityAssignmentProto = powerProgEntry.PowerAssignment;
                        if (abilityAssignmentProto == null)
                        {
                            Logger.Warn("GetPowerProgressionInfos(): abilityAssignmentProto == null");
                            continue;
                        }

                        PrototypeId mappedPowerRef = GetMappedPowerFromOriginalPower(abilityAssignmentProto.Ability);

                        PowerProgressionInfo powerInfo = new();
                        powerInfo.InitForAvatar(powerProgEntry, mappedPowerRef, powerProgTableProto.PowerProgTableTabRef);
                        powerInfoList.Add(powerInfo);
                    }
                }
            }

            return true;
        }

        #endregion

        #region Multi-Spec

        public override int GetPowerSpecIndexUnlocked()
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null)
                return 0;

            return player.PowerSpecIndexUnlocked;
        }

        public bool SetActivePowerSpec(int newSpecIndex)
        {
            if (newSpecIndex < 0) return Logger.WarnReturn(false, "SetActivePowerSpec(): specIndex < 0");

            int currentSpecIndex = GetPowerSpecIndexActive();
            if (newSpecIndex == currentSpecIndex)
                return true;

            if (newSpecIndex > GetPowerSpecIndexUnlocked())
                return false;

            if (Properties.HasProperty(PropertyEnum.IsInCombat))
                return false;

            // Unassign talents
            List<PrototypeId> talentPowerList = ListPool<PrototypeId>.Instance.Get();
            GetTalentPowersForSpec(currentSpecIndex, talentPowerList);

            foreach (PrototypeId talentPowerRef in talentPowerList)
                UnassignTalentPower(talentPowerRef, currentSpecIndex, true);

            ListPool<PrototypeId>.Instance.Return(talentPowerList);

            // Clear mapped powers
            if (CanStealPowers() == false)
                UnassignAllMappedPowers();

            // "Unequip" powers for the spec we are disabling
            if (IsInWorld)
                UnequipPowersForCurrentSpec();

            // Change spec
            Properties[PropertyEnum.PowerSpecIndexActive] = newSpecIndex;

            // Refresh powers
            if (IsInWorld)
            {
                UpdateTalentPowers();
                UpdatePowerProgressionPowers(true);
            }

            RefreshAbilityKeyMapping(false); // false because the client will do it on its own when it handles the change in PowerSpecIndexActive
            
            // "Equip" powers for the spec we enabled
            if (IsInWorld)
                EquipPowersForCurrentSpec();

            return false;
        }

        public override bool RespecPowerSpec(int specIndex, PowersRespecReason reason, bool skipValidation = false, PrototypeId powerProtoRef = PrototypeId.Invalid)
        {
            // Schedule deferred removal of mapped powers after respec finishes doing its thing
            Game.GameEventScheduler.CancelEvent(_unassignMappedPowersForRespec);
            ScheduleEntityEvent(_unassignMappedPowersForRespec, TimeSpan.FromMilliseconds(500));

            // Unassign talents
            List<PrototypeId> talentPowerList = ListPool<PrototypeId>.Instance.Get();
            GetTalentPowersForSpec(specIndex, talentPowerList);

            foreach (PrototypeId talentPowerRef in talentPowerList)
                UnassignTalentPower(talentPowerRef, specIndex);

            if (talentPowerList.Count > 0)
            {
                // Set the new respec
                if (powerProtoRef == PrototypeId.Invalid)
                    powerProtoRef = GameDatabase.GlobalsPrototype.PowerPrototype;

                Properties[PropertyEnum.PowersRespecResult, specIndex, (int)reason, powerProtoRef] = true;

                // Early return (V48_TODO: this probably shouldn't happen for pre-BUE?)
                ListPool<PrototypeId>.Instance.Return(talentPowerList);
                return true;
            }

            // Fall back to base implementation if no talents were unassigned
            ListPool<PrototypeId>.Instance.Return(talentPowerList);
            return base.RespecPowerSpec(specIndex, reason, skipValidation, powerProtoRef);
        }

        #endregion

        #region Talents (Specialization Powers)

        public void GetTalentPowersForSpec(int specIndex, List<PrototypeId> talentPowerList)
        {
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.AvatarSpecializationPower, specIndex))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId talentPowerRef);
                talentPowerList.Add(talentPowerRef);
            }
        }

        public bool IsTalentPowerEnabledForSpec(PrototypeId talentPowerRef, int specIndex)
        {
            return Properties[PropertyEnum.AvatarSpecializationPower, specIndex, talentPowerRef];
        }

        public bool EnableTalentPower(PrototypeId talentPowerRef, int specIndex, bool enable)
        {
            if (talentPowerRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "EnableTalentPower(): talentPowerRef == PrototypeId.Invalid");

            SpecializationPowerPrototype talentProto = talentPowerRef.As<SpecializationPowerPrototype>();
            if (talentProto == null) return Logger.WarnReturn(false, "EnableTalentPower(): talentProto == null");

            if (enable)
            {
                if (Game.CustomGameOptions.AllowSameGroupTalents == false)
                {
                    // Turn off mutually exclusive talents (belonging to the same group)
                    PowerOwnerTable powerOwnerTable = GameDataTables.Instance.PowerOwnerTable;

                    uint talentGroupIndex = powerOwnerTable.GetTalentGroupIndex(PrototypeDataRef, talentPowerRef);
                    if (talentGroupIndex == TalentGroupIndexInvalid) return Logger.WarnReturn(false, "EnableTalentPower(): talentGroupIndex == TalentGroupIndexInvalid");

                    List<PrototypeId> talentPowerList = ListPool<PrototypeId>.Instance.Get();
                    GetTalentPowersForSpec(specIndex, talentPowerList);

                    foreach (PrototypeId talentPowerRefToCheck in talentPowerList)
                    {
                        uint talentGroupIndexToCheck = powerOwnerTable.GetTalentGroupIndex(PrototypeDataRef, talentPowerRefToCheck);
                        if (talentGroupIndexToCheck == talentGroupIndex)
                            UnassignTalentPower(talentPowerRefToCheck, specIndex);
                    }

                    ListPool<PrototypeId>.Instance.Return(talentPowerList);
                }

                // Enable
                AssignTalentPower(talentPowerRef, specIndex);
            }
            else
            {
                // Disable
                UnassignTalentPower(talentPowerRef, specIndex);
            }

            return true;
        }

        public CanToggleTalentResult CanToggleTalentPower(PrototypeId talentPowerRef, int specIndex, bool enteringWorld, bool enable)
        {
            SpecializationPowerPrototype talentPowerProto = talentPowerRef.As<SpecializationPowerPrototype>();
            if (talentPowerProto == null)
                return CanToggleTalentResult.GenericError;

            // Skip combat check if this avatar is entering the world
            if (enteringWorld == false && Properties.HasProperty(PropertyEnum.IsInCombat))
                return CanToggleTalentResult.InCombat;

            int specIndexUnlocked = GetPowerSpecIndexUnlocked();
            if (specIndex > specIndexUnlocked) return Logger.WarnReturn(CanToggleTalentResult.GenericError, "CanToggleTalentPower(): specIndex < specIndexUnlocked");

            GetPowerProgressionInfo(talentPowerRef, out PowerProgressionInfo talentPowerInfo);

            if (CharacterLevel < talentPowerInfo.GetRequiredLevel())
                return CanToggleTalentResult.LevelRequirement;

            uint talentGroupIndex = GameDataTables.Instance.PowerOwnerTable.GetTalentGroupIndex(PrototypeDataRef, talentPowerRef);
            if (talentGroupIndex == TalentGroupIndexInvalid)
                return Logger.WarnReturn(CanToggleTalentResult.GenericError, $"CanToggleTalentPower(): Talent missing its talent group index for some reason!\nTalent: {talentPowerProto}\nOwner: [{this}]\nenteringWorld: {enteringWorld}");

            // Skip evla check if this avatar is entering the world
            if (enable && enteringWorld == false && talentPowerProto.EvalCanEnable.HasValue())
            {
                foreach (EvalPrototype evalProto in talentPowerProto.EvalCanEnable)
                {
                    using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, null);
                    evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Entity, this);
                    evalContext.SetReadOnlyVar_ConditionCollectionPtr(EvalContext.Var1, ConditionCollection);

                    if (Eval.RunBool(evalProto, evalContext) == false)
                        return CanToggleTalentResult.RestrictiveCondition;
                }
            }

            return CanToggleTalentResult.Success;
        }

        private bool AssignTalentPower(PrototypeId talentPowerRef, int specIndex)
        {
            if (talentPowerRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "AssignTalentPower(): talentPowerRef == PrototypeId.Invalid");

            SpecializationPowerPrototype talentPowerProto = talentPowerRef.As<SpecializationPowerPrototype>();
            if (talentPowerProto == null) return Logger.WarnReturn(false, "AssignTalentPower(): talentPowerProto == null");

            if (IsInWorld && specIndex == GetPowerSpecIndexActive())
            {
                // Assign the talent power if the spec is currently active
                // Talent powers always have a rank of 1
                PowerIndexProperties indexProps = new(1, CharacterLevel, CombatLevel);
                Power talentPower = AssignPower(talentPowerRef, indexProps);
                if (talentPower == null) return Logger.WarnReturn(false, "AssignTalentPower(): talentPower == null");

                talentPower.HandleTriggerPowerEventOnSpecializationPowerAssigned();
                RefreshDependentPassivePowers(talentPowerProto, 1);
            }

            Properties[PropertyEnum.AvatarSpecializationPower, specIndex, talentPowerRef] = true;
            return true;
        }

        private bool UnassignTalentPower(PrototypeId talentPowerRef, int specIndex, bool isSwitchingSpec = false)
        {
            if (talentPowerRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "UnassignTalentPower(): talentPowerRef == PrototypeId.Invalid");

            Power talentPower = GetPower(talentPowerRef);
            if (talentPower != null && IsInWorld && specIndex == GetPowerSpecIndexActive())
            {
                // Unassign the talent power if the spec is currently active
                PowerPrototype talentPowerProto = talentPower.Prototype;
                if (talentPowerProto == null) return Logger.WarnReturn(false, "UnassignTalentPower(): talentPowerProto == null");

                if (UnassignPower(talentPowerRef) == false)
                    return Logger.WarnReturn(false, $"UnassignTalentPower(): Failed to unassign talent power {talentPowerProto} for owner [{this}]");

                talentPower.HandleTriggerPowerEventOnSpecializationPowerUnassigned();
                RefreshDependentPassivePowers(talentPowerProto, 0);                
            }

            // Do not remove the property if we are simply switching specs
            if (isSwitchingSpec == false)
                Properties.RemoveProperty(new(PropertyEnum.AvatarSpecializationPower, specIndex, talentPowerRef));

            return true;
        }

        private void UpdateTalentPowers()
        {
            int specIndex = GetPowerSpecIndexActive();

            List<PrototypeId> talentPowerList = ListPool<PrototypeId>.Instance.Get();
            GetTalentPowersForSpec(specIndex, talentPowerList);

            foreach (PrototypeId talentPowerRef in talentPowerList)
            {
                bool enabled = Properties[PropertyEnum.AvatarSpecializationPower, specIndex, talentPowerRef];
                if (CanToggleTalentPower(talentPowerRef, specIndex, true, enabled) == CanToggleTalentResult.Success)
                {
                    if (GetPower(talentPowerRef) == null)
                        AssignTalentPower(talentPowerRef, specIndex);
                }
                else
                {
                    UnassignTalentPower(talentPowerRef, specIndex);
                }
            }

            ListPool<PrototypeId>.Instance.Return(talentPowerList);
        }

        #endregion

        #region Mapped Powers (and Stolen Powers)

        public bool HasMappedPower(PrototypeId mappedPowerRef)
        {
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.AvatarMappedPower))
            {
                if (kvp.Value == mappedPowerRef)
                    return true;
            }

            return false;
        }

        public PrototypeId GetOriginalPowerFromMappedPower(PrototypeId mappedPowerRef)
        {
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.AvatarMappedPower))
            {
                if ((PrototypeId)kvp.Value != mappedPowerRef) continue;
                Property.FromParam(kvp.Key, 0, out PrototypeId originalPower);
                return originalPower;
            }

            return PrototypeId.Invalid;
        }

        public PrototypeId GetMappedPowerFromOriginalPower(PrototypeId originalPowerRef)
        {
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.AvatarMappedPower, originalPowerRef))
            {
                PrototypeId mappedPowerRef = kvp.Value;

                if (mappedPowerRef == PrototypeId.Invalid)
                    Logger.Warn("GetMappedPowerFromOriginalPower(): mappedPowerRefTemp == PrototypeId.Invalid");

                return mappedPowerRef;
            }

            return PrototypeId.Invalid;
        }

        public bool MapPower(PrototypeId originalPowerRef, PrototypeId mappedPowerRef)
        {
            if (originalPowerRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "MapPower(): originalPowerRef == PrototypeId.Invalid");
            if (mappedPowerRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "MapPower(): mappedPowerRef == PrototypeId.Invalid");

            PowerPrototype mappedPowerProto = mappedPowerRef.As<PowerPrototype>();
            if (mappedPowerProto == null) return Logger.WarnReturn(false, "MapPower(): mappedPowerProto == null");

            // Map
            Properties[PropertyEnum.AvatarMappedPower, originalPowerRef] = mappedPowerRef;

            // Refresh powers
            GetPowerProgressionInfo(originalPowerRef, out PowerProgressionInfo originalPowerInfo);
            if (UpdatePowerRank(ref originalPowerInfo, false) == false)
            {
                // If the original power's rank didn't change as a result of the update,
                // we need to update the mapped power's rank manually
                PowerProgressionInfo mappedPowerInfo = new();
                mappedPowerInfo.InitNonProgressionPower(mappedPowerRef);
                UpdatePowerRank(ref mappedPowerInfo, false);
            }

            // Replace the slotted original power if it was usable
            if (GetPowerRank(originalPowerRef) > 0)
            {
                List<AbilitySlot> slotList = ListPool<AbilitySlot>.Instance.Get();
                int specIndex = GetPowerSpecIndexActive();

                foreach (AbilityKeyMapping keyMapping in _abilityKeyMappings)
                {
                    if (keyMapping.PowerSpecIndex != specIndex)
                        continue;

                    keyMapping.GetActiveAbilitySlotsContainingProtoRef(originalPowerRef, slotList);
                    if (slotList.Count == 0)
                        continue;

                    foreach (AbilitySlot slot in slotList)
                    {
                        if (SlotAbility(mappedPowerRef, slot, true, true) == false)
                            Logger.Warn($"MapPower(): Failed to slot mapped power {mappedPowerProto} in slot {slot} for avatar [{this}]");
                    }

                    slotList.Clear();
                }

                ListPool<AbilitySlot>.Instance.Return(slotList);
            }

            return true;
        }

        public bool UnassignMappedPower(PrototypeId mappedPowerRef)
        {
            if (mappedPowerRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "UnassignMappedPower(): mappedPowerRef == PrototypeId.Invalid");

            // Early return if this power is not mapped (valid result)
            PrototypeId originalPowerRef = GetOriginalPowerFromMappedPower(mappedPowerRef);
            if (originalPowerRef == PrototypeId.Invalid)
                return true;

            PowerPrototype originalPowerProto = originalPowerRef.As<PowerPrototype>();
            if (originalPowerProto == null) return Logger.WarnReturn(false, "UnassignMappedPower(): originalPowerProto == null");

            // Restore the original power in key mappings
            List<AbilitySlot> slotList = ListPool<AbilitySlot>.Instance.Get();
            int specIndex = GetPowerSpecIndexActive();

            foreach (AbilityKeyMapping keyMapping in _abilityKeyMappings)
            {
                if (keyMapping.PowerSpecIndex != specIndex)
                    continue;

                keyMapping.GetActiveAbilitySlotsContainingProtoRef(mappedPowerRef, slotList);
                if (slotList.Count == 0)
                    continue;

                foreach (AbilitySlot slot in slotList)
                {
                    if (SlotAbility(originalPowerRef, slot, true, true) == false)
                        Logger.Warn($"UnassignMappedPower(): Failed to slot original power {originalPowerProto} in slot {slot} for avatar [{this}]");
                }

                slotList.Clear();
            }

            ListPool<AbilitySlot>.Instance.Return(slotList);

            // Unassign
            UnassignPower(mappedPowerRef);
            Properties.RemoveProperty(new(PropertyEnum.AvatarMappedPower, originalPowerRef));

            // Refresh the original power
            GetPowerProgressionInfo(originalPowerRef, out PowerProgressionInfo originalPowerInfo);
            UpdatePowerRank(ref originalPowerInfo, false);

            return true;
        }

        public void UnassignAllMappedPowers()
        {
            while (Properties.HasProperty(PropertyEnum.AvatarMappedPower))
            {
                foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.AvatarMappedPower))
                {
                    UnassignMappedPower(kvp.Value);
                    break;
                }
            }
        }

        public bool IsStolenPowerAvailable(PrototypeId stolenPowerRef)
        {
            if (stolenPowerRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "IsStolenPowerAvailable(): stolenPowerRef == PrototypeId.Invalid");
            return Properties[PropertyEnum.StolenPowerAvailable, stolenPowerRef];
        }

        public bool CanAssignStolenPower(PrototypeId stolenPowerRefToAssign, PrototypeId currentStolenPowerRef)
        {
            if (stolenPowerRefToAssign == PrototypeId.Invalid) return Logger.WarnReturn(false, "CanAssignStolenPower(): stolenPowerRefToAssign == PrototypeId.Invalid");

            PowerPrototype stolenPowerProto = stolenPowerRefToAssign.As<PowerPrototype>();
            if (stolenPowerProto == null) return Logger.WarnReturn(false, "CanAssignStolenPower(): stolenPowerProto == null");

            GlobalsPrototype globals = GameDatabase.GlobalsPrototype;
            if (globals.StolenPowerRestrictions.IsNullOrEmpty())
                return true;

            foreach (PrototypeId restrictionProtoRef in globals.StolenPowerRestrictions)
            {
                StolenPowerRestrictionPrototype restrictionProto = restrictionProtoRef.As<StolenPowerRestrictionPrototype>();
                if (restrictionProto == null)
                {
                    Logger.Warn("CanAssignStolenPower(): restrictionProto == null");
                    continue;
                }

                KeywordPrototype keywordProto = restrictionProto.RestrictionKeywordPrototype;

                if (keywordProto == null || restrictionProto.RestrictionKeywordCount <= 0)
                    continue;

                if (stolenPowerProto.HasKeyword(keywordProto) == false)
                    continue;

                int count = 0;
                bool hasMaxCount = false;
                foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.AvatarMappedPower))
                {
                    PowerPrototype mappedPowerProto = GameDatabase.GetPrototype<PowerPrototype>(kvp.Value);
                    if (mappedPowerProto == null)
                    {
                        Logger.Warn("CanAssignStolenPower(): mappedPowerProto == null");
                        continue;
                    }

                    if (mappedPowerProto.DataRef == currentStolenPowerRef)
                        continue;

                    if (mappedPowerProto.HasKeyword(keywordProto) == false)
                        continue;

                    count++;
                    if (count >= restrictionProto.RestrictionKeywordCount)
                    {
                        hasMaxCount = true;
                        break;
                    }
                }

                if (hasMaxCount == false)
                    continue;

                // Max count for this restriction reached, stolen power cannot be assigned

                // Notify the player about this
                Player player = GetOwnerOfType<Player>();
                if (player == null) return Logger.WarnReturn(false, "CanAssignStolenPower(): player == null");

                BannerMessagePrototype bannerMessageProto = restrictionProto.RestrictionBannerMessage.As<BannerMessagePrototype>();
                if (bannerMessageProto == null) return Logger.WarnReturn(false, "CanAssignStolenPower(): bannerMessageProto == null");

                player.SendBannerMessage(bannerMessageProto);

                return false;
            }

            return true;
        }

        public bool CanStealPowers()
        {
            AvatarPrototype avatarProto = AvatarPrototype;
            if (avatarProto == null) return Logger.WarnReturn(false, "CanStealPowers(): avatarProto == null");

            return avatarProto.StealablePowersAllowed.HasValue();
        }

        private void UnassignMappedPowersForRespec()
        {
            // Rogue's mapped powers don't get reset on respec
            if (CanStealPowers() == false)
                return;

            // Key mappings should have already been cleaned up by respec, so just remove the powers
            List<PrototypeId> mappedPowerList = ListPool<PrototypeId>.Instance.Get();
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.AvatarMappedPower))
                mappedPowerList.Add(kvp.Value);

            foreach (PrototypeId mappedPowerRef in mappedPowerList)
                UnassignPower(mappedPowerRef);

            Properties.RemovePropertyRange(PropertyEnum.AvatarMappedPower);
            ListPool<PrototypeId>.Instance.Return(mappedPowerList);
        }

        #endregion

        #region Travel Powers

        public PrototypeId GetTravelPowerRef()
        {
            AvatarPrototype avatarProto = AvatarPrototype;
            if (avatarProto == null) return Logger.WarnReturn(PrototypeId.Invalid, "GetTravelPowerRef(): avatarProto == null");

            if (_travelPowerOverrideProtoRef != PrototypeId.Invalid)
                return _travelPowerOverrideProtoRef;

            return avatarProto.TravelPower;
        }

        public void SetTravelPowerOverride(PrototypeId travelPowerOverrideProtoRef)
        {
            // Called by mapped powers
            _travelPowerOverrideProtoRef = travelPowerOverrideProtoRef;
        }

        /// <summary>
        /// Assigns or unassign the travel power for this <see cref="Avatar"/> based on character level.
        /// </summary>
        private bool UpdateTravelPower()
        {
            PrototypeId travelPowerRef = GetTravelPowerRef();
            if (travelPowerRef == PrototypeId.Invalid)
                return true;

            int characterLevel = CharacterLevel;
            if (characterLevel >= GameDatabase.AdvancementGlobalsPrototype.TravelPowerUnlockLevel)
            {
                if (GetPower(travelPowerRef) == null)
                {
                    PowerIndexProperties indexProps = new(1, characterLevel, CombatLevel);
                    AssignPower(travelPowerRef, indexProps);
                }
            }
            else
            {
                UnassignPower(travelPowerRef);
            }

            return true;
        }

        #endregion

        #region Transform Modes

        public bool ScheduleTransformModeChange(PrototypeId newTransformModeRef, PrototypeId oldTransformModeRef, TimeSpan delay = default)
        {
            EventScheduler scheduler = Game.GameEventScheduler;

            TransformModePrototype oldTransformModeProto = oldTransformModeRef.As<TransformModePrototype>();
            if (delay > TimeSpan.Zero && oldTransformModeProto != null)
            {
                // Schedule transform mode exit
                PowerPrototype exitPowerProto = oldTransformModeProto.ExitTransformModePower.As<PowerPrototype>();
                if (exitPowerProto == null) return Logger.WarnReturn(false, "ScheduleTransformModeChange(): exitPowerProto == null");

                if (_transformModeExitPowerEvent.IsValid)
                {
                    scheduler.RescheduleEvent(_transformModeExitPowerEvent, delay);
                    _transformModeExitPowerEvent.Get().Initialize(this, oldTransformModeRef);
                }
                else
                {
                    ScheduleEntityEvent(_transformModeExitPowerEvent, delay, oldTransformModeRef);
                }
            }
            else
            {
                // Cancel exit
                scheduler.CancelEvent(_transformModeExitPowerEvent);

                // Schedule change
                if (_transformModeChangeEvent.IsValid)
                {
                    scheduler.RescheduleEvent(_transformModeChangeEvent, delay);
                    _transformModeChangeEvent.Get().Initialize(this, newTransformModeRef, oldTransformModeRef);
                }
                else
                {
                    ScheduleEntityEvent(_transformModeChangeEvent, delay, newTransformModeRef, oldTransformModeRef);
                }
            }

            return true;
        }

        public bool IsPowerAllowedInCurrentTransformMode(PrototypeId powerProtoRef)
        {
            AvatarPrototype avatarProto = AvatarPrototype;
            if (avatarProto == null) return Logger.WarnReturn(false, "IsPowerAllowedInCurrentTransformMode(): avatarProto == null");

            return IsPowerAllowedInTransformMode(avatarProto, CurrentTransformMode, powerProtoRef);
        }

        public static bool IsPowerAllowedInTransformMode(AvatarPrototype avatarProto, PrototypeId transformModeRef, PrototypeId powerProtoRef)
        {
            PowerPrototype powerProto = powerProtoRef.As<PowerPrototype>();
            if (powerProto == null) return Logger.WarnReturn(false, "IsPowerAllowedInTransformMode(): powerProto == null");

            if (Power.IsComboEffect(powerProto))
                return true;

            if (powerProto.UsableByAll)
                return true;

            if (powerProto.PowerCategory == PowerCategoryType.HiddenPassivePower)
                return true;

            if (powerProto.Activation == PowerActivationType.Passive && powerProto.HasKeyword(GameDatabase.KeywordGlobalsPrototype.TeamUpAwayPowerKeywordPrototype))
                return true;

            PrototypeId[] allowedPowers = avatarProto.GetAllowedPowersForTransformMode(transformModeRef);
            if (allowedPowers == null)
                return true;

            foreach (PrototypeId allowedPowerProtoRef in allowedPowers)
            {
                if (allowedPowerProtoRef == powerProtoRef)
                    return true;
            }

            return false;
        }

        private bool OnTransformModeChange(PrototypeId newTransformModeRef, PrototypeId oldTransformModeRef, bool enterWorld, TimeSpan remainingDuration = default)
        {
            if (oldTransformModeRef == PrototypeId.Invalid && newTransformModeRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, "OnTransformModeChange(): No transform mode specified!");

            if (oldTransformModeRef != PrototypeId.Invalid && newTransformModeRef != PrototypeId.Invalid)
                return Logger.WarnReturn(false, $"OnTransformModeChange(): Cannot go directly from one transform mode to another! oldTransformMode=[{oldTransformModeRef.GetName()}] newTransformMode=[{newTransformModeRef.GetName()}]");

            if (newTransformModeRef == PrototypeId.Invalid)
            {
                CurrentTransformMode = PrototypeId.Invalid;
                Properties.RemoveProperty(PropertyEnum.TransformMode);
                Properties.RemoveProperty(new(PropertyEnum.TransformModeStartTime, oldTransformModeRef));
            }
            else
            {
                TransformModePrototype newTransformModeProto = newTransformModeRef.As<TransformModePrototype>();
                if (newTransformModeProto == null) return Logger.WarnReturn(false, "OnTransformModeChange(): newTransformModeProto == null");

                TimeSpan duration = remainingDuration > TimeSpan.Zero
                    ? remainingDuration
                    : newTransformModeProto.GetDuration(this);

                // Schedule exit if this is a finite duration transform mode
                if (duration > TimeSpan.Zero)
                    ScheduleTransformModeChange(oldTransformModeRef, newTransformModeRef, duration);

                Properties[PropertyEnum.TransformMode] = newTransformModeRef;
                if (enterWorld == false)
                    Properties[PropertyEnum.TransformModeStartTime, newTransformModeRef] = Game.CurrentTime;
            }

            // Assign or unassign transform mode powers
            if (newTransformModeRef != PrototypeId.Invalid)
                UpdateTransformModeDefaultEquippedAbilities(newTransformModeRef, true);
            else
                UpdateTransformModeDefaultEquippedAbilities(oldTransformModeRef, false);

            UpdateTransformModeAbilityKeyMapping(newTransformModeRef, oldTransformModeRef);

            UpdateTransformModeAllowedPowers(newTransformModeRef, oldTransformModeRef);

            if (_continuousPowerData.PowerProtoRef != PrototypeId.Invalid && GetPower(_continuousPowerData.PowerProtoRef) != null)
                ClearContinuousPower();

            return true;
        }

        private bool OnEnteredWorldSetTransformMode()
        {
            // Restore the previous transform mode (if any)
            CurrentTransformMode = Properties[PropertyEnum.TransformMode];

            if (CurrentTransformMode == PrototypeId.Invalid)
                return true;

            TransformModePrototype currentTransformModeProto = CurrentTransformMode.As<TransformModePrototype>();
            if (currentTransformModeProto == null) return Logger.WarnReturn(false, "OnEnteredWorldSetTransformMode(): currentTransformModeProto == null");

            TimeSpan transformModeDuration = currentTransformModeProto.GetDuration(this);

            if (transformModeDuration == TimeSpan.Zero)
            {
                // TimeSpan.Zero indicates infinite duration
                OnTransformModeChange(CurrentTransformMode, PrototypeId.Invalid, true);
            }
            else
            {
                // Calculate remaining time
                TimeSpan transformModeStartTime = Properties[PropertyEnum.TransformModeStartTime, CurrentTransformMode];
                TimeSpan avatarLastActiveTime = Properties[PropertyEnum.AvatarLastActiveTime];
                TimeSpan elapsedDuration = Clock.Max(avatarLastActiveTime - transformModeStartTime, TimeSpan.Zero);

                // Turn it back on if there is still time, or turn it off
                if (elapsedDuration < transformModeDuration)
                    OnTransformModeChange(CurrentTransformMode, PrototypeId.Invalid, true, transformModeDuration - elapsedDuration);
                else
                    OnTransformModeChange(PrototypeId.Invalid, CurrentTransformMode, true);
            }

            return true;
        }

        private void DoTransformModeChangeCallback(PrototypeId newTransformModeRef, PrototypeId oldTransformModeRef)
        {
            CurrentTransformMode = newTransformModeRef;
            OnTransformModeChange(newTransformModeRef, oldTransformModeRef, false);
        }

        private bool DoTransformModeExitPowerCallback(PrototypeId fromTransformModeProtoRef)
        {
            if (fromTransformModeProtoRef != CurrentTransformMode) return Logger.WarnReturn(false, "DoTransformModeExitPowerCallback(): fromTransformModeProtoRef != CurrentTransformMode");
            if (IsInWorld == false) return Logger.WarnReturn(false, "DoTransformModeExitPowerCallback(): IsInWorld == false");

            TransformModePrototype currentTransformModeProto = CurrentTransformMode.As<TransformModePrototype>();
            if (currentTransformModeProto == null) return Logger.WarnReturn(false, "DoTransformModeExitPowerCallback(): currentTransformModeProto == null");
            if (currentTransformModeProto.ExitTransformModePower == PrototypeId.Invalid) return Logger.WarnReturn(false, "DoTransformModeExitPowerCallback(): currentTransformModeProto.ExitTransformModePower == PrototypeId.Invalid");

            // Abort active powers
            ClearContinuousPower();
            CancelPendingAction();
            ActivePower?.EndPower(EndPowerFlags.ExplicitCancel | EndPowerFlags.Force);

            // Prepare exit power
            Vector3 position = RegionLocation.Position;
            PowerActivationSettings settings = new(Id, position, position);
            settings.TriggeringPowerRef = currentTransformModeProto.EnterTransformModePower;
            settings.FXRandomSeed = Game.Random.Next(1, 10000);

            Power exitTransformModePower = GetPower(currentTransformModeProto.ExitTransformModePower);
            if (exitTransformModePower != null)
            {
                TimeSpan delay = exitTransformModePower.GetFullExecutionTime() + TimeSpan.FromMilliseconds(1);
                if (_transformModeChangeEvent.IsValid)
                {
                    Game.GameEventScheduler.RescheduleEvent(_transformModeChangeEvent, delay);
                    _transformModeChangeEvent.Get().Initialize(this, PrototypeId.Invalid, fromTransformModeProtoRef);
                }
                else
                {
                    ScheduleEntityEvent(_transformModeChangeEvent, delay, PrototypeId.Invalid, fromTransformModeProtoRef);
                }
            }
            else
            {
                Logger.Warn("DoTransformModeExitPowerCallback(): exitTransformModePower == null");
            }

            // Activate exit power
            PowerUseResult result = ActivatePower(currentTransformModeProto.ExitTransformModePower, ref settings);
            if (result != PowerUseResult.Success)
                return Logger.WarnReturn(false, $"DoTransformModeExitPowerCallback(): Failed to activate transform mode exit power for Avatar: [{this}]\nTransform mode: {currentTransformModeProto}");

            return true;
        }

        private bool UpdateTransformModeAbilityKeyMapping(PrototypeId newTransformModeRef, PrototypeId oldTransformModeRef)
        {
            TransformModePrototype newTransformModeProto = newTransformModeRef.As<TransformModePrototype>();
            TransformModePrototype oldTransformModeProto = oldTransformModeRef.As<TransformModePrototype>();

            if (oldTransformModeProto == null && newTransformModeProto == null) return Logger.WarnReturn(false, "UpdateTransformModeAbilityKeyMapping(): oldTransformModeProto == null && newTransformModeProto == null");

            if (oldTransformModeProto?.PowersAreSlottable == false || newTransformModeProto?.PowersAreSlottable == false)
                RefreshAbilityKeyMapping(false);    // Swap to and from non-slottable transform mapping
            else if (newTransformModeProto?.PowersAreSlottable == true)
                RefreshAbilityKeyMapping(false);    // Swap to slottable transform mapping
            else if (newTransformModeProto == null && oldTransformModeProto?.PowersAreSlottable == true)
                RefreshAbilityKeyMapping(false);    // Swap back from slottable transform mapping

            return true;
        }

        private bool UpdateTransformModeDefaultEquippedAbilities(PrototypeId transformModeRef, bool isEntering)
        {
            TransformModePrototype transformModeProto = transformModeRef.As<TransformModePrototype>();
            if (transformModeProto == null) return Logger.WarnReturn(false, "UpdateTransformModeDefaultEquippedAbilities(): transformModeProto == null");

            // Nothing to update
            if (transformModeProto.DefaultEquippedAbilities.IsNullOrEmpty())
                return true;

            PowerIndexProperties indexProps = new(0, CharacterLevel, CombatLevel);
            foreach (AbilityAssignmentPrototype abilityAssignment in transformModeProto.DefaultEquippedAbilities)
            {
                PrototypeId abilityProtoRef = abilityAssignment.Ability;

                if (abilityProtoRef == PrototypeId.Invalid)
                    continue;

                // Abilities can also refer to items
                PowerPrototype powerProto = abilityProtoRef.As<PowerPrototype>();
                if (powerProto == null)
                    continue;

                // Power progression powers are handled separately
                if (HasPowerInPowerProgression(abilityProtoRef))
                    continue;

                if (isEntering)
                {
                    if (HasPowerInPowerCollection(abilityProtoRef))
                        continue;

                    AssignPower(abilityProtoRef, indexProps);

                    PowerProgressionInfo powerInfo = new();
                    powerInfo.InitNonProgressionPower(abilityProtoRef);
                    UpdatePowerRank(ref powerInfo, false);
                }
                else
                {
                    UnassignPower(abilityProtoRef);
                }
            }

            return true;
        }

        private bool UpdateTransformModeAllowedPowers(PrototypeId newTransformModeRef, PrototypeId oldTransformModeRef)
        {
            AvatarPrototype avatarProto = AvatarPrototype;
            if (avatarProto == null) return Logger.WarnReturn(false, "UpdateTransformModeAllowedPowers(): avatarProto == null");

            // Look for powers that are not allowed in the new transform mode
            List<PrototypeId> powerRemoveList = ListPool<PrototypeId>.Instance.Get();
            
            // Power collection
            foreach (var kvp in PowerCollection)
            {
                Power power = kvp.Value.Power;
                if (power == null)
                {
                    Logger.Warn("UpdateTransformModeAllowedPowers(): power == null");
                    continue;
                }

                PrototypeId powerProtoRef = power.PrototypeDataRef;

                bool isPassive = power.GetActivationType() == PowerActivationType.Passive;
                bool isToggledOn = power.IsToggledOn();

                if (isPassive || isToggledOn)
                {
                    if (IsPowerAllowedInCurrentTransformMode(powerProtoRef) == false)
                    {
                        if (isPassive)
                            powerRemoveList.Add(powerProtoRef);
                        else if (isToggledOn)
                            power.EndPower(EndPowerFlags.ExplicitCancel | EndPowerFlags.Unassign);
                    }
                }
                else if (power.IsActive && isPassive == false)
                {
                    power.EndPower(EndPowerFlags.ExplicitCancel | EndPowerFlags.Force);
                }
            }

            // Transform mode specific hidden passives (if we are exiting)
            TransformModePrototype oldTransformModeProto = oldTransformModeRef.As<TransformModePrototype>();
            if (oldTransformModeProto != null && oldTransformModeProto.HiddenPassivePowers.HasValue())
            {
                foreach (PrototypeId hiddenPassivePowerRef in oldTransformModeProto.HiddenPassivePowers)
                    powerRemoveList.Add(hiddenPassivePowerRef);
            }

            // Remove the powers we found
            while (powerRemoveList.Count > 0)
            {
                int index = powerRemoveList.Count - 1;
                PrototypeId powerProtoRef = powerRemoveList[index];
                powerRemoveList.RemoveAt(index);

                // Remove all copies of this power
                while (PowerCollection.GetPower(powerProtoRef) != null)
                    PowerCollection.UnassignPower(powerProtoRef);
            }
            ListPool<PrototypeId>.Instance.Return(powerRemoveList);

            // Assign newly allowed powers
            PrototypeId[] allowedPowers = avatarProto.GetAllowedPowersForTransformMode(newTransformModeRef);
            if (allowedPowers.IsNullOrEmpty()) return Logger.WarnReturn(false, "UpdateTransformModeAllowedPowers(): allowedPowers.IsNullOrEmpty()");

            int characterLevel = CharacterLevel;
            int combatLevel = CombatLevel;

            foreach (PrototypeId allowedPowerRef in allowedPowers)
            {
                PowerPrototype powerProto = allowedPowerRef.As<PowerPrototype>();
                if (powerProto == null)
                {
                    Logger.Warn("UpdateTransformModeAllowedPowers(): powerProto == null");
                    continue;
                }

                if (powerProto.PowerCategory != PowerCategoryType.NormalPower)
                    continue;

                if (powerProto.Activation != PowerActivationType.Passive && powerProto.UsableByAll == false)
                    continue;

                // Do not assign if it doesn't have a rank or it is already assigned
                int powerRank = GetPowerRank(allowedPowerRef);
                if (powerRank <= 0 || GetPower(allowedPowerRef) != null)
                    continue;

                PowerIndexProperties indexProps = new(powerRank, characterLevel, combatLevel);
                AssignPower(allowedPowerRef, indexProps);
            }

            // Assign transform mode specific hidden passives
            TransformModePrototype newTransformModeProto = newTransformModeRef.As<TransformModePrototype>();
            if (newTransformModeProto != null && newTransformModeProto.HiddenPassivePowers.HasValue())
            {
                PowerIndexProperties indexProps = new(0, characterLevel, combatLevel);

                foreach (PrototypeId hiddenPassivePowerRef in newTransformModeProto.HiddenPassivePowers)
                {
                    if (PowerCollection.GetPower(hiddenPassivePowerRef) != null)
                        continue;

                    PowerCollection.AssignPower(hiddenPassivePowerRef, indexProps);

                    PowerProgressionInfo powerInfo = new();
                    powerInfo.InitNonProgressionPower(hiddenPassivePowerRef);
                    UpdatePowerRank(ref powerInfo, false);
                }
            }

            return true;
        }

        #endregion

        #region Ability Slot Management

        public AbilitySlot GetPowerSlot(PrototypeId powerProtoRef)
        {
            AbilityKeyMapping keyMapping = _currentAbilityKeyMapping;
            if (keyMapping == null)
                return Logger.WarnReturn(AbilitySlot.Invalid, $"GetPowerSlot(): No current keyMapping when calling GetPowerSlot [{powerProtoRef.GetName()}]");

            List<AbilitySlot> abilitySlotList = ListPool<AbilitySlot>.Instance.Get();
            keyMapping.GetActiveAbilitySlotsContainingProtoRef(powerProtoRef, abilitySlotList);
            AbilitySlot result = abilitySlotList.Count > 0 ? abilitySlotList[0] : AbilitySlot.Invalid;

            ListPool<AbilitySlot>.Instance.Return(abilitySlotList);
            return result;
        }

        public Power GetPowerInSlot(AbilitySlot slot)
        {
            // Merged with getPowerInSlot(), which is only needed for the client
            if (slot < 0 || _currentAbilityKeyMapping == null)
                return null;

            AbilityKeyMapping keyMapping = _currentAbilityKeyMapping;

            PrototypeId abilityProtoRef = keyMapping.GetAbilityInAbilitySlot(slot);
            if (abilityProtoRef == PrototypeId.Invalid)
                return null;

            Prototype abilityProto = abilityProtoRef.As<Prototype>();
            return abilityProto switch
            {
                PowerPrototype          => GetPower(abilityProtoRef),
                ItemPrototype itemProto => GetPower(itemProto.GetOnUsePower()),
                _ => null,
            };
        }

        public bool HasPowerEquipped(PrototypeId powerProtoRef)
        {
            AbilityKeyMapping keyMapping = _currentAbilityKeyMapping;
            if (keyMapping == null)
                return Logger.WarnReturn(false, "HasPowerEquipped():");

            return keyMapping.ContainsAbilityInActiveSlot(powerProtoRef);
        }

        public bool HasControlPowerEquipped()
        {
            AbilityKeyMapping keyMapping = _currentAbilityKeyMapping;
            if (keyMapping == null) return false;

            for (AbilitySlot slot = AbilitySlot.PrimaryAction; slot < AbilitySlot.NumActions; slot++)
            {
                Power power = GetPowerInSlot(slot);
                if (power != null && power.IsControlPower)
                    return true;
            }

            return false;
        }

        public bool SlotAbility(PrototypeId abilityProtoRef, AbilitySlot slot, bool skipEquipValidation, bool sendToClient)
        {
            if (IsAbilityEquippableInSlot(abilityProtoRef, slot, skipEquipValidation) != AbilitySlotOpValidateResult.Valid)
                return false;

            AbilityKeyMapping keyMapping = GetAbilityKeyMappingIgnoreTransient(GetPowerSpecIndexActive());
            if (keyMapping == null) return Logger.WarnReturn(false, "SlotAbility(): keyMapping == null");

            bool wasEquipped = HasPowerEquipped(abilityProtoRef);

            // Unslot the currently slotted ability if it's something else to trigger unequip
            PrototypeId slottedAbilityProtoRef = keyMapping.GetAbilityInAbilitySlot(slot);
            if (slottedAbilityProtoRef != PrototypeId.Invalid && slottedAbilityProtoRef != abilityProtoRef)
            {
                if (UnslotAbility(slot, false) == false)
                    Logger.Warn($"SlotAbility(): Failed to unslot ability {abilityProtoRef.GetName()} in slot {slot}");
            }

            // Set
            keyMapping.SetAbilityInAbilitySlot(abilityProtoRef, slot);

            // Trigger equip
            if (wasEquipped == false)
            {
                Power power = GetPower(abilityProtoRef);
                power?.OnEquipped();
            }

            // Notify the client if needed
            if (sendToClient)
            {
                Player player = GetOwnerOfType<Player>();
                if (player == null) return Logger.WarnReturn(false, "SlotAbility(): player == null");

                if (player.InterestedInEntity(this, AOINetworkPolicyValues.AOIChannelOwner))
                {
                    player.SendMessage(NetMessageAbilitySlotToAbilityBarFromServer.CreateBuilder()
                        .SetAvatarId(Id)
                        .SetPrototypeRefId((ulong)abilityProtoRef)
                        .SetSlotNumber((uint)slot)
                        .Build());
                }
            }

            return true;
        }

        public bool UnslotAbility(AbilitySlot slot, bool sendToClient)
        {
            if (IsActiveAbilitySlot(slot) == false) return Logger.WarnReturn(false, "UnslotAbility(): AbilityKeyMapping.IsActiveAbilitySlot(slot) == false");

            AbilityKeyMapping keyMapping = GetAbilityKeyMappingIgnoreTransient(GetPowerSpecIndexActive());
            if (keyMapping == null) return Logger.WarnReturn(false, "UnslotAbility(): keyMapping == null");

            PrototypeId abilityProtoRef = keyMapping.GetAbilityInAbilitySlot(slot);
            if (abilityProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "UnslotAbility(): abilityProtoRef == PrototypeId.Invalid");

            // Remove by assigning invalid id
            keyMapping.SetAbilityInAbilitySlot(PrototypeId.Invalid, slot);

            // Trigger unequip
            if (HasPowerEquipped(abilityProtoRef) == false)
            {
                Power power = GetPower(abilityProtoRef);
                power?.OnUnequipped();
            }

            // Notify the client if needed
            if (sendToClient)
            {
                Player player = GetOwnerOfType<Player>();
                if (player == null) return Logger.WarnReturn(false, "UnslotAbility(): player == null");

                if (player.InterestedInEntity(this, AOINetworkPolicyValues.AOIChannelOwner))
                {
                    player.SendMessage(NetMessageAbilityUnslotFromAbilityBarFromServer.CreateBuilder()
                        .SetAvatarId(Id)
                        .SetSlotNumber((uint)slot)
                        .Build());
                }
            }

            return true;
        }

        public bool SwapAbilities(AbilitySlot slotA, AbilitySlot slotB, bool sendToClient)
        {
            // Check A to B
            if (ValidateAbilitySwap(slotA, slotB) != AbilitySlotOpValidateResult.Valid)
                return false;

            AbilityKeyMapping keyMapping = _currentAbilityKeyMapping;
            if (keyMapping == null) return Logger.WarnReturn(false, "SwapAbilities(): keyMapping == null");

            // Check B to A - this is allowed to be invalid, in which case we just discard B
            if (ValidateAbilitySwap(slotB, slotA) != AbilitySlotOpValidateResult.Valid)
                UnslotAbility(slotB, false);

            // Do the swap            
            PrototypeId abilityA = keyMapping.GetAbilityInAbilitySlot(slotA);
            PrototypeId abilityB = keyMapping.GetAbilityInAbilitySlot(slotB);
            keyMapping.SetAbilityInAbilitySlot(abilityB, slotA);
            keyMapping.SetAbilityInAbilitySlot(abilityA, slotB);

            // Notify the client if needed
            if (sendToClient)
            {
                Player player = GetOwnerOfType<Player>();
                if (player == null) return Logger.WarnReturn(false, "SwapAbilities(): player == null");

                if (player.InterestedInEntity(this, AOINetworkPolicyValues.AOIChannelOwner))
                {
                    player.SendMessage(NetMessageAbilitySwapInAbilityBarFromServer.CreateBuilder()
                        .SetAvatarId(Id)
                        .SetSlotNumberA((uint)slotA)
                        .SetSlotNumberB((uint)slotB)
                        .Build());
                }
            }

            return true;
        }

        public bool RefreshAbilityKeyMapping(bool sendToClient)
        {
            // NOTE: The server has nothing to send to client here, but we are keeping the bool arg for now to keep the API the same as the client

            _currentAbilityKeyMapping = GetOrCreateAbilityKeyMapping(GetPowerSpecIndexActive(), CurrentTransformMode);
            if (_currentAbilityKeyMapping == null) return Logger.WarnReturn(false, "RefreshAbilityKeyMapping(): _currentAbilityKeyMapping == null");

            _currentAbilityKeyMapping.InitDedicatedAbilitySlots(this);

            return true;
        }

        private void InitAbilityKeyMappings()
        {
            RefreshAbilityKeyMapping(false);
            CleanUpAbilityKeyMappingsAfterRespec();
        }

        /// <summary>
        /// Automatically slots powers for level up.
        /// </summary>
        private void AutoSlotPowers()
        {
            AbilityKeyMapping keyMapping = _currentAbilityKeyMapping;
            if (keyMapping == null)
                return;

            // Slot ALL default abilities, including those that have already been unlocked.
            // This is dumb, but we have to do this to avoid desync with the client. Also
            // because this is probably happening in combat and the 1.52 client is stupid,
            // we can't do the full SlotAbility() call here that does validation and events.
            // See CAvatar::autoSlotPowers() for reference.
            List<HotkeyData> hotkeyDataList = ListPool<HotkeyData>.Instance.Get();
            if (keyMapping.GetDefaultAbilities(hotkeyDataList, this))
            {
                foreach (HotkeyData hotkeyData in hotkeyDataList)
                    keyMapping.SetAbilityInAbilitySlot(hotkeyData.AbilityProtoRef, hotkeyData.AbilitySlot);
            }

            ListPool<HotkeyData>.Instance.Return(hotkeyDataList);
        }

        private bool CleanUpAbilityKeyMappingsAfterRespec()
        {
            if (IsInWorld == false) return Logger.WarnReturn(false, "CleanUpAbilityKeyMappingsAfterRespec(): IsInWorld == false");

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.PowersRespecResult))
            {
                Property.FromParam(kvp.Key, 0, out int specIndex);
                Property.FromParam(kvp.Key, 1, out int reasonValue);

                // Do not reset key mappings for player requested respecs
                if ((PowersRespecReason)reasonValue == PowersRespecReason.PlayerRequest)
                    continue;

                foreach (AbilityKeyMapping keyMapping in _abilityKeyMappings)
                {
                    if (keyMapping.PowerSpecIndex != specIndex)
                        continue;

                    keyMapping.CleanUpAfterRespec(this);
                }
            }

            return true;
        }

        private void UnequipPowersForCurrentSpec()
        {
            AbilityKeyMapping keyMapping = _currentAbilityKeyMapping;
            if (keyMapping == null) return;

            for (AbilitySlot slot = AbilitySlot.PrimaryAction; slot < AbilitySlot.NumActions; slot++)
            {
                Power power = GetPowerInSlot(slot);
                power?.OnUnequipped();
            }
        }

        private void EquipPowersForCurrentSpec()
        {
            AbilityKeyMapping keyMapping = _currentAbilityKeyMapping;
            if (keyMapping == null) return;

            for (AbilitySlot slot = AbilitySlot.PrimaryAction; slot < AbilitySlot.NumActions; slot++)
            {
                Power power = GetPowerInSlot(slot);
                power?.OnEquipped();
            }
        }

        private AbilityKeyMapping GetOrCreateAbilityKeyMapping(int powerSpecIndex, PrototypeId transformModeProtoRef)
        {
            AbilityKeyMapping keyMapping = null;

            if (powerSpecIndex < 0 || powerSpecIndex > GetPowerSpecIndexUnlocked()) return Logger.WarnReturn(keyMapping, "GetOrCreateAbilityKeyMapping(): powerSpecIndex < 0 || powerSpecIndex > GetPowerSpecIndexUnlocked()");

            TransformModePrototype transformModeProto = transformModeProtoRef.As<TransformModePrototype>();
            if (transformModeProto != null && transformModeProto.PowersAreSlottable == false)
            {
                // Fixed key mappings for transform modes

                // Transient ability key mappings are stored in a fixed array with a length of 1 in the client.
                // Not sure if there were more of them in older versions, so for now I'm keeping it the same.
                // Initializing this collection on demand to avoid allocations for avatars with no transform modes
                // (the vast majority of them).
                _transientAbilityKeyMappings ??= new(MaxNumTransientAbilityKeyMappings);

                foreach (AbilityKeyMapping transientKeyMapping in _transientAbilityKeyMappings)
                {
                    if (transientKeyMapping?.AssociatedTransformMode == transformModeProtoRef)
                    {
                        keyMapping = transientKeyMapping;
                        break;
                    }
                }

                if (keyMapping == null)
                {
                    if (_transientAbilityKeyMappings.Count >= MaxNumTransientAbilityKeyMappings)
                        return Logger.WarnReturn(keyMapping, "GetOrCreateAbilityKeyMapping(): _transientAbilityKeyMappings.Count >= MaxNumTransientAbilityKeyMappings");

                    keyMapping = new();
                    _transientAbilityKeyMappings.Add(keyMapping);

                    keyMapping.AssociatedTransformMode = transformModeProtoRef;
                    keyMapping.SlotDefaultAbilitiesForTransformMode(transformModeProto);

                    keyMapping.PowerSpecIndex = powerSpecIndex;
                    keyMapping.ShouldPersist = false;       // Will be flagged to persist if anything gets changed
                }
            }
            else
            {
                // Normal key mapping and transform modes with swappable slots
                foreach (AbilityKeyMapping keyMappingToCheck in _abilityKeyMappings)
                {
                    // Pre-BUE this is where mapping index would also be checked
                    if (keyMappingToCheck.PowerSpecIndex == powerSpecIndex)
                    {
                        keyMapping = keyMappingToCheck;
                        break;
                    }
                }

                if (keyMapping == null)
                {
                    keyMapping = new();
                    _abilityKeyMappings.Add(keyMapping);

                    // AssociatedTransformMode doesn't seem to be getting used here, is this correct?
                    if (transformModeProto != null && transformModeProto.PowersAreSlottable)
                        keyMapping.SlotDefaultAbilitiesForTransformMode(transformModeProto);
                    else
                        keyMapping.SlotDefaultAbilities(this);

                    keyMapping.PowerSpecIndex = powerSpecIndex;
                    keyMapping.ShouldPersist = false;       // Will be flagged to persist if anything gets changed
                }
            }

            return keyMapping;
        }

        private AbilityKeyMapping GetAbilityKeyMappingIgnoreTransient(int powerSpecIndex)
        {
            return GetOrCreateAbilityKeyMapping(powerSpecIndex, PrototypeId.Invalid);
        }

        private AbilitySlotOpValidateResult IsAbilityEquippableInSlot(PrototypeId abilityProtoRef, AbilitySlot slot, bool skipEquipValidation)
        {
            if (IsActiveAbilitySlot(slot) == false) return Logger.WarnReturn(AbilitySlotOpValidateResult.GenericError, "IsAbilityEquippableInSlot(): IsActiveAbilitySlot(slot) == false");

            if (Properties.HasProperty(PropertyEnum.IsInCombat))
            {
                // Only mapped powers are allowed to be equipp;ed in combat
                if (HasMappedPower(abilityProtoRef) || GetMappedPowerFromOriginalPower(abilityProtoRef) != PrototypeId.Invalid)
                    return AbilitySlotOpValidateResult.Valid;

                return AbilitySlotOpValidateResult.AvatarIsInCombat;
            }

            Prototype abilityProto = abilityProtoRef.As<Prototype>();
            PowerPrototype powerProto = abilityProto as PowerPrototype;
            ItemPrototype itemProto = abilityProto as ItemPrototype;

            if (powerProto == null && itemProto?.AbilitySettings == null)
                return Logger.WarnReturn(AbilitySlotOpValidateResult.GenericError, "IsAbilityEquippableInSlot(): powerProto == null && itemProto?.AbilitySettings == null");

            if (powerProto != null)
            {
                // Mapped powers have their own validation
                if (HasMappedPower(abilityProtoRef))
                    return AbilitySlotOpValidateResult.Valid;

                if (skipEquipValidation == false)
                {
                    AbilitySlotOpValidateResult isPowerEquippableResult = IsPowerEquippable(abilityProtoRef);
                    if (isPowerEquippableResult != AbilitySlotOpValidateResult.Valid)
                        return isPowerEquippableResult;
                }
            }

            if (itemProto != null)
            {
                if (itemProto.AbilitySettings.OnlySlottableWhileEquipped && FindAbilityItem(itemProto) == InvalidId)
                    return AbilitySlotOpValidateResult.ItemNotEquipped;
            }

            return CheckAbilitySlotRestrictions(abilityProtoRef, slot);
        }

        private AbilitySlotOpValidateResult IsPowerEquippable(PrototypeId powerProtoRef)
        {
            AbilitySlotOpValidateResult staticResult = IsPowerEquippable(PrototypeDataRef, powerProtoRef);
            if (staticResult != AbilitySlotOpValidateResult.Valid)
                return staticResult;

            AvatarPrototype avatarProto = AvatarPrototype;
            if (avatarProto == null) return Logger.WarnReturn(AbilitySlotOpValidateResult.GenericError, "IsPowerEquippable(): avatarProto == null");

            PowerPrototype powerProto = powerProtoRef.As<PowerPrototype>();
            if (powerProto == null) return Logger.WarnReturn(AbilitySlotOpValidateResult.GenericError, "IsPowerEquippable(): powerProto == null");

            if (powerProto.PowerCategory == PowerCategoryType.NormalPower)
            {
                int powerRank = GetPowerRank(powerProtoRef);
                if (powerRank <= 0)
                    return AbilitySlotOpValidateResult.PowerNotUnlocked;
            }

            return AbilitySlotOpValidateResult.Valid;
        }

        private static AbilitySlotOpValidateResult IsPowerEquippable(PrototypeId avatarProtoRef, PrototypeId powerProtoRef)
        {
            PowerPrototype powerProto = powerProtoRef.As<PowerPrototype>();
            if (powerProto == null) return Logger.WarnReturn(AbilitySlotOpValidateResult.GenericError, "IsPowerEquippable(): powerProto == null");

            if (powerProto.UsableByAll)
                return AbilitySlotOpValidateResult.Valid;

            // Check avatar-specific restrictions
            AvatarPrototype avatarProto = avatarProtoRef.As<AvatarPrototype>();
            if (avatarProto == null) return Logger.WarnReturn(AbilitySlotOpValidateResult.GenericError, "IsPowerEquippable(): avatarProto == null");

            if (avatarProto.HasPowerProgressionTables == false || avatarProto.HasPowerInPowerProgression(powerProtoRef) == false)
                return AbilitySlotOpValidateResult.PowerNotUsableByAvatar;

            return AbilitySlotOpValidateResult.Valid;
        }

        private AbilitySlotOpValidateResult ValidateAbilitySwap(AbilitySlot slotA, AbilitySlot slotB)
        {
            // This is a one way check, in this case slotA is the source and slotB is the destination
            if (slotA == slotB)
                return AbilitySlotOpValidateResult.SwapSameSlot;

            if (IsActiveAbilitySlot(slotA) == false || IsActiveAbilitySlot(slotB) == false)
                return Logger.WarnReturn(AbilitySlotOpValidateResult.PowerSlotMismatch, "ValidateAbilitySwap(): IsActiveAbilitySlot(slotA) == false || IsActiveAbilitySlot(slotB) == false");

            AbilityKeyMapping keyMapping = _currentAbilityKeyMapping;
            if (keyMapping == null) return Logger.WarnReturn(AbilitySlotOpValidateResult.GenericError, "ValidateAbilitySwap(): keyMapping == null");

            PrototypeId abilityA = keyMapping.GetAbilityInAbilitySlot(slotA);
            if (abilityA != PrototypeId.Invalid && IsAbilityEquippableInSlot(abilityA, slotB, true) != AbilitySlotOpValidateResult.Valid)
                return AbilitySlotOpValidateResult.PowerSlotMismatch;

            return AbilitySlotOpValidateResult.Valid;
        }

        /// <summary>
        /// Checks if an <see cref="AbilitySlot"/> is valid.
        /// </summary>
        public static bool IsActiveAbilitySlot(AbilitySlot slot)
        {
            return slot > AbilitySlot.Invalid && slot < AbilitySlot.NumSlotsTotal;
        }

        /// <summary>
        /// Checks if an <see cref="AbilitySlot"/> is an action key slot (non-mouse bindable slot).
        /// </summary>
        public static bool IsActionKeyAbilitySlot(AbilitySlot slot)
        {
            return slot >= AbilitySlot.ActionKey0 && slot <= AbilitySlot.ActionKey5;
        }

        /// <summary>
        /// Checks if an <see cref="AbilitySlot"/> is a dedicated ability slot (ultimate, travel, etc.).
        /// </summary>
        public static bool IsDedicatedAbilitySlot(AbilitySlot slot)
        {
            return slot > AbilitySlot.NumActions && slot < AbilitySlot.NumSlotsTotal;
        }

        public static AbilitySlotOpValidateResult CheckAbilitySlotRestrictions(PrototypeId abilityProtoRef, AbilitySlot slot)
        {
            if (IsActiveAbilitySlot(slot) == false) return Logger.WarnReturn(AbilitySlotOpValidateResult.GenericError, "CheckAbilitySlotRestrictions(): IsActiveAbilitySlot(slot) == false");

            Prototype abilityProto = abilityProtoRef.As<Prototype>();
            PowerPrototype powerProto = abilityProto as PowerPrototype;
            ItemPrototype itemProto = abilityProto as ItemPrototype;

            if (powerProto == null && itemProto?.AbilitySettings == null)
                return Logger.WarnReturn(AbilitySlotOpValidateResult.GenericError, "CheckAbilitySlotRestrictions(): powerProto == null && itemProto?.AbilitySettings == null");

            if (powerProto != null)
            {
                if (powerProto.Activation != PowerActivationType.Instant &&
                    powerProto.Activation != PowerActivationType.InstantTargeted &&
                    powerProto.Activation != PowerActivationType.TwoStageTargeted)
                {
                    return AbilitySlotOpValidateResult.PowerNotActive;
                }
            }

            if (CanActionSlotContainPowerOrItem(abilityProtoRef, slot) == false)
                return AbilitySlotOpValidateResult.PowerSlotMismatch;

            return AbilitySlotOpValidateResult.Valid;
        }

        public static bool CanActionSlotContainPowerOrItem(PrototypeId abilityProtoRef, AbilitySlot slot)
        {
            if (abilityProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "CanActionSlotContainPowerOrItem(): abilityProtoRef == PrototypeId.Invalid");
            if (IsActiveAbilitySlot(slot) == false) return Logger.WarnReturn(false, "CanActionSlotContainPowerOrItem(): IsActiveAbilitySlot(slot) == false");

            // Dedicated slots (medkit, ultimate, etc.) cannot hold arbitrary abilities
            if (IsDedicatedAbilitySlot(slot))
                return false;

            if (ValidAbilitySlotItemOrPower(abilityProtoRef) == false)
                return false;

            AbilitySlotRestrictionPrototype restrictionProto = GetAbilitySlotRestrictionPrototype(abilityProtoRef);

            if (restrictionProto == null)
                return true;

            if (restrictionProto.LeftMouseSlotOK && slot == AbilitySlot.PrimaryAction)
                return true;

            if (restrictionProto.RightMouseSlotOK && slot == AbilitySlot.SecondaryAction)
                return true;

            if (restrictionProto.ActionKeySlotOK && IsActionKeyAbilitySlot(slot))
                return true;

            return false;
        }

        public static bool ValidAbilitySlotItemOrPower(PrototypeId abilityProtoRef)
        {
            Prototype abilityProto = abilityProtoRef.As<Prototype>();
            PowerPrototype powerProto = abilityProto as PowerPrototype;
            ItemPrototype itemProto = abilityProto as ItemPrototype;

            return powerProto != null || itemProto?.AbilitySettings != null;
        }

        public static AbilitySlotRestrictionPrototype GetAbilitySlotRestrictionPrototype(PrototypeId abilityProtoRef)
        {
            if (abilityProtoRef == PrototypeId.Invalid)
                return null;

            Prototype abilityProto = abilityProtoRef.As<Prototype>();

            if (abilityProto is PowerPrototype powerProto)
                return powerProto.SlotRestriction;

            if (abilityProto is ItemPrototype itemProto)
                return itemProto.AbilitySettings.AbilitySlotRestriction;

            return null;
        }

        #endregion

        #region Resources

        public bool CanGainOrRegenEndurance(ManaType manaType)
        {
            if (Properties[PropertyEnum.ForceEnduranceRegen, manaType])
                return true;

            return Properties[PropertyEnum.DisableEnduranceGain, manaType] == false &&
                   Properties[PropertyEnum.DisableEnduranceRegen, manaType] == false;
        }

        public bool ResetResources(bool avatarSwap)
        {
            // Primary resources
            foreach (PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto in GetPrimaryResourceManaBehaviors())
            {
                ManaType manaType = primaryManaBehaviorProto.ManaType;
                float endurance = primaryManaBehaviorProto.StartsEmpty ? 0f : Properties[PropertyEnum.EnduranceMax, manaType];
                Properties[PropertyEnum.Endurance, manaType] = endurance;
            }

            // Secondary resources
            SecondaryResourceManaBehaviorPrototype secondaryResourceManaBehaviorProto = GetSecondaryResourceManaBehavior();
            if (secondaryResourceManaBehaviorProto == null)
                return true;
            
            if (avatarSwap == false || secondaryResourceManaBehaviorProto.ResetOnAvatarSwap)
            {
                float secondaryResource = secondaryResourceManaBehaviorProto.StartsEmpty ? 0f : Properties[PropertyEnum.SecondaryResourceMax];
                Properties[PropertyEnum.SecondaryResource] = secondaryResource;
            }

            return true;
        }

        private bool InitializePrimaryManaBehaviors()
        {
            foreach (PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto in GetPrimaryResourceManaBehaviors())
            {
                ManaType manaType = primaryManaBehaviorProto.ManaType;

                // Set base value
                Curve manaCurve = GameDatabase.GetCurve(primaryManaBehaviorProto.BaseEndurancePerLevel);
                if (manaCurve == null)
                {
                    Logger.Warn("InitializePrimaryResources(): manaCurve == null");
                    continue;
                }

                Properties[PropertyEnum.EnduranceBase, manaType] = manaCurve.GetAt(CharacterLevel);

                // Restore to full if needed
                if (primaryManaBehaviorProto.StartsEmpty == false)
                    Properties[PropertyEnum.Endurance, manaType] = Properties[PropertyEnum.EnduranceMax, manaType];

                // Start regen
                Properties[PropertyEnum.DisableEnduranceRegen, manaType] = primaryManaBehaviorProto.StartsWithRegenEnabled == false;

                // Do common mana init
                AssignManaBehaviorPowers(primaryManaBehaviorProto);
            }

            return true;
        }

        private bool InitializeSecondaryManaBehaviors()
        {
            // Secondary resource base is already present in the prototype's property collection as a curve property
            SecondaryResourceManaBehaviorPrototype secondaryManaBehaviorProto = GetSecondaryResourceManaBehavior();
            if (secondaryManaBehaviorProto == null)
                return false;

            AssignManaBehaviorPowers(secondaryManaBehaviorProto);
            return true;
        }

        private bool AssignManaBehaviorPowers(ManaBehaviorPrototype manaBehaviorProto)
        {
            if (manaBehaviorProto.Powers.IsNullOrEmpty())
                return true;

            PowerIndexProperties indexProps = new(0, CharacterLevel, CombatLevel);

            foreach (PrototypeId powerProtoRef in manaBehaviorProto.Powers)
            {
                if (GetPower(powerProtoRef) == null)
                {
                    if (AssignPower(powerProtoRef, indexProps) == null)
                        Logger.Warn($"AssignManaBehaviorPowers(): Failed to assign mana behavior power {powerProtoRef.GetName()} to [{this}]");
                }
            }

            return true;
        }

        private void UnassignManaBehaviorPowers(ManaBehaviorPrototype manaBehaviorProto)
        {
            if (manaBehaviorProto.Powers.IsNullOrEmpty())
                return;

            foreach (PrototypeId powerProtoRef in manaBehaviorProto.Powers)
            {
                if (GetPower(powerProtoRef) != null)
                {
                    if (UnassignPower(powerProtoRef) == false)
                        Logger.Warn($"UnassignManaBehaviorPowers(): Failed to unassign mana behavior power {powerProtoRef.GetName()} from [{this}]");
                }
            }
        }

        public PrimaryResourceManaBehaviorPrototype[] GetPrimaryResourceManaBehaviors()
        {
            PrimaryResourceManaBehaviorPrototype[] behaviors = AvatarPrototype?.PrimaryResourceBehaviorsCache;

            // Check if there are any primary resource behaviors (there should be!)
            if (behaviors.IsNullOrEmpty())
                return Logger.WarnReturn(Array.Empty<PrimaryResourceManaBehaviorPrototype>(), $"GetPrimaryResourceManaBehaviors(): behaviors.IsNullOrEmpty()");

            return behaviors;
        }

        private PrimaryResourceManaBehaviorPrototype GetPrimaryResourceManaBehavior(ManaType manaType)
        {
            PrimaryResourceManaBehaviorPrototype[] behaviors = AvatarPrototype?.PrimaryResourceBehaviorsCache;
            if (behaviors.IsNullOrEmpty())
                return Logger.WarnReturn<PrimaryResourceManaBehaviorPrototype>(null, $"GetPrimaryResourceManaBehaviors(): behaviors.IsNullOrEmpty()");

            foreach (PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto in behaviors)
            {
                if (primaryManaBehaviorProto.ManaType == manaType)
                    return primaryManaBehaviorProto;
            }

            return null;
        }

        private SecondaryResourceManaBehaviorPrototype GetSecondaryResourceManaBehavior()
        {
            PrototypeId secondaryResourceOverrideProtoRef = Properties[PropertyEnum.SecondaryResourceOverride];
            if (secondaryResourceOverrideProtoRef != PrototypeId.Invalid)
                return secondaryResourceOverrideProtoRef.As<SecondaryResourceManaBehaviorPrototype>();

            return AvatarPrototype?.SecondaryResourceBehaviorCache;
        }

        private float GetEnduranceMax(ManaType manaType)
        {
            float enduranceMax = Properties[PropertyEnum.EnduranceBase, manaType];
            enduranceMax *= 1f + Properties[PropertyEnum.EndurancePctBonus, manaType];
            enduranceMax += Properties[PropertyEnum.EnduranceAddBonus, manaType];
            enduranceMax += Properties[PropertyEnum.EnduranceAddBonus, ManaType.TypeAll];
            return enduranceMax;
        }

        private bool EnableEnduranceRegen(ManaType manaType)
        {
            // NOTE: Validation in GetPrimaryResourceManaBehavior() will ensure that we won't get an invalid mana type index here
            PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto = GetPrimaryResourceManaBehavior(manaType);
            if (primaryManaBehaviorProto == null) return Logger.WarnReturn(false, "EnableEnduranceRegen(): primaryManaBehaviorProto == null");

            // Remove the flag that prevents endurance regen
            Properties.RemoveProperty(new(PropertyEnum.DisableEnduranceRegen, manaType));

            // Initialize or reset the update event pointer for this mana type
            int index = (int)manaType;

            if (_updateEnduranceEvents[index] == null)
                _updateEnduranceEvents[index] = new();
            else
                Game.GameEventScheduler?.CancelEvent(_updateEnduranceEvents[index]);

            // Schedule the next update
            TimeSpan regenUpdateTime = TimeSpan.FromMilliseconds(primaryManaBehaviorProto.RegenUpdateTimeMS);
            ScheduleEntityEvent(_updateEnduranceEvents[index], regenUpdateTime, manaType);

            return true;
        }

        private void CancelEnduranceEvents()
        {
            EventScheduler scheduler = Game.GameEventScheduler;
            
            foreach (var enableEvent in _enableEnduranceRegenEvents)
            {
                if (enableEvent != null)
                    scheduler.CancelEvent(enableEvent);
            }

            foreach (var updateEvent in _updateEnduranceEvents)
            {
                if (updateEvent != null)
                    scheduler.CancelEvent(updateEvent);
            }
        }

        private bool UpdateEndurance(ManaType manaType)
        {
            // NOTE: Validation in GetPrimaryResourceManaBehavior() will ensure that we won't get an invalid mana type index here
            PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto = GetPrimaryResourceManaBehavior(manaType);
            if (primaryManaBehaviorProto == null) return Logger.WarnReturn(false, "UpdateEndurance(): primaryManaBehaviorProto == null");

            // Run the regen eval if regen is enabled
            EvalPrototype evalProto = primaryManaBehaviorProto.EvalOnEnduranceUpdate;
            if (CanGainOrRegenEndurance(manaType) && evalProto != null)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, Properties);
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Entity, Properties);

                if (Eval.RunBool(evalProto, evalContext) == false)
                    Logger.Warn($"UpdateEndurance(): The following EvalOnEnduranceUpdate failed:\nEval: [{evalProto.ExpressionString()}]");
            }

            // Initialize or reset the update event pointer for this mana type
            int index = (int)manaType;

            if (_updateEnduranceEvents[index] == null)
                _updateEnduranceEvents[index] = new();
            else
                Game.GameEventScheduler?.CancelEvent(_updateEnduranceEvents[index]);

            // Schedule the next update
            TimeSpan regenUpdateTime = TimeSpan.FromMilliseconds(primaryManaBehaviorProto.RegenUpdateTimeMS);
            ScheduleEntityEvent(_updateEnduranceEvents[index], regenUpdateTime, manaType);

            return true;
        }

        #endregion

        #region Conditions

        /// <summary>
        /// Pauses or unpauses boost <see cref="Condition"/> instances applied to this <see cref="Avatar"/>.
        /// </summary>
        public void UpdateBoostConditionPauseState(bool pause)
        {
            if (pause)
            {
                foreach (Condition condition in ConditionCollection)
                {
                    if (condition.IsBoost() == false)
                        continue;

                    if (condition.IsPaused)
                        continue;

                    ConditionCollection.PauseCondition(condition, true);
                }
            }
            else
            {
                foreach (Condition condition in ConditionCollection)
                {
                    if (condition.IsBoost() == false)
                        continue;

                    if (condition.IsPaused == false)
                        continue;

                    ConditionCollection.UnpauseCondition(condition, true);
                }
            }
        }

        #endregion

        #region Leveling

        public override void InitializeLevel(int newLevel)
        {
            base.InitializeLevel(newLevel);
            CombatLevel = newLevel;
        }

        public override long AwardXP(long amount, long minAmount, bool showXPAwardedText)
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(0L, "AwardXP(): player == null");

            // No more XP for capped starters
            if (player.HasAvatarAsCappedStarter(this))
                return 0;

            // The base method applies the cosmic prestige xp penalty, we use the original amount to calculate AA/legendary/team-up xp
            long awardedAmount = base.AwardXP(amount, minAmount, showXPAwardedText);

            // Award alternate advancement XP (omega or infinity)
            if (Game.InfinitySystemEnabled)
            {
                float infinityLiveTuningMult = 1f;
                if (LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_RespectLevelForInfinityXP) == 0f || player.CanUseLiveTuneBonuses())
                    infinityLiveTuningMult = Math.Max(LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_InfinityXPPct), 0f);

                player.AwardInfinityXP((long)(amount * infinityLiveTuningMult), true);
            }
            else
            {
                float omegaLiveTuningMult = 1f;
                if (LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_RespectLevelForOmegaXP) == 0f || player.CanUseLiveTuneBonuses())
                    omegaLiveTuningMult = Math.Max(LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_OmegaXPPct), 0f);

                player.AwardOmegaXP((long)(amount * omegaLiveTuningMult), true);
            }

            // Award XP to the equipped legendary item if there is one
            Inventory legendaryInventory = GetInventory(InventoryConvenienceLabel.AvatarLegendary);
            if (legendaryInventory != null)
            {
                ulong legendaryItemId = legendaryInventory.GetEntityInSlot(0);
                if (legendaryItemId != InvalidId)
                {
                    Item legendaryItem = Game.EntityManager.GetEntity<Item>(legendaryItemId);
                    if (legendaryItem != null)
                        legendaryItem.AwardAffixXP(amount);
                    else
                        Logger.Warn("AwardXP(): legendaryItem == null");
                }
            }

            // Award XP to the current team-up as well if there is one
            CurrentTeamUpAgent?.AwardXP(amount, 0, showXPAwardedText);

            return awardedAmount;
        }

        public static int GetAvatarLevelCap()
        {
            AdvancementGlobalsPrototype advancementProto = GameDatabase.AdvancementGlobalsPrototype;
            return advancementProto != null ? advancementProto.GetAvatarLevelCap() : 0;
        }

        public static int GetStarterAvatarLevelCap()
        {
            AdvancementGlobalsPrototype advancementProto = GameDatabase.AdvancementGlobalsPrototype;
            return advancementProto != null ? advancementProto.StarterAvatarLevelCap : 0;
        }

        public override long GetLevelUpXPRequirement(int level)
        {
            AdvancementGlobalsPrototype advancementProto = GameDatabase.AdvancementGlobalsPrototype;
            if (advancementProto == null) return Logger.WarnReturn(0, "GetLevelUpXPRequirement(): advancementProto == null");

            return advancementProto.GetAvatarLevelUpXPRequirement(level);
        }

        public override float GetPrestigeXPFactor()
        {
            int prestigeLevel = PrestigeLevel;
            if (prestigeLevel == 0)
                return 1f;

            AdvancementGlobalsPrototype advancementProto = GameDatabase.AdvancementGlobalsPrototype;

            Curve pctXPFromPrestigeLevelCurve = advancementProto.PctXPFromPrestigeLevelCurve.AsCurve();
            if (pctXPFromPrestigeLevelCurve == null) return Logger.WarnReturn(1f, "GetPrestigeXPFactor(): pctXPFromPrestigeLevelCurve == null");

            if (prestigeLevel == advancementProto.MaxPrestigeLevel)
            {
                float liveTuningXPPct = LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_CosmicPrestigeXPPct);
                if (liveTuningXPPct != 1f)
                    return liveTuningXPPct;
            }

            return pctXPFromPrestigeLevelCurve.GetAt(prestigeLevel);
        }

        public override int TryLevelUp(Player owner, bool isInitializing = false)
        {
            int levelDelta = base.TryLevelUp(owner, isInitializing);

            if (isInitializing)
            {
                CombatLevel = CharacterLevel;
                owner.OnAvatarCharacterLevelChanged(this);
            }
            else if (levelDelta != 0)
            {
                CombatLevel = Math.Clamp(CombatLevel + levelDelta, 1, GetAvatarLevelCap());

                owner.ScheduleCommunityBroadcast();
            }

            return levelDelta;
        }

        public long ApplyXPModifiers(long xp, bool applyKillBonus, TuningTable tuningTable = null)
        {
            if (IsInWorld == false)
                return 0;

            // Flat per kill bonus (optionally capped by a percentage)
            if (applyKillBonus)
            {
                long killBonus = Properties[PropertyEnum.ExperienceBonusPerKill];

                long killBonusMax = (long)(xp * (float)Properties[PropertyEnum.ExperienceBonusPerKillMaxPct]);
                if (killBonusMax > 0)
                    killBonus = Math.Min(killBonus, killBonusMax);

                xp += killBonus;
            }

            // Calculate the multiplier
            float xpMult = GetAvatarXPMultiplier();

            // Region bonus
            Region region = Region;
            if (region != null)
                xpMult *= 1f + region.Properties[PropertyEnum.ExperienceBonusPct];

            // Tuning table modifiers
            if (tuningTable != null)
            {
                TuningPrototype tuningProto = tuningTable.Prototype;
                if (tuningProto == null) return Logger.WarnReturn(0L, "ApplyXPModifiers(): tuningProto == null");

                // Apply difficulty index modifier
                Curve difficultyIndexCurve = tuningProto.PlayerXPByDifficultyIndexCurve.AsCurve();
                if (difficultyIndexCurve == null) return Logger.WarnReturn(0L, "ApplyXPModifiers(): difficultyIndexCurve == null");
                xpMult *= difficultyIndexCurve.GetAt(tuningTable.DifficultyIndex);

                // Apply unconditional tuning table multiplier
                xpMult *= tuningProto.PctXPMultiplier;

                // Party
                xpMult *= GetPartyXPMultiplier(tuningProto);
            }

            // Live tuning
            xpMult *= GetLiveTuningXPMultiplier();

            return (long)(xp * xpMult);
        }

        protected override bool OnLevelUp(int oldLevel, int newLevel, bool restoreHealthAndEndurance = true)
        {
            AvatarPrototype avatarProto = AvatarPrototype;
            if (avatarProto == null) return Logger.WarnReturn(false, "OnLevelUp(): avatarProto == null");

            // Check stat changes (this code also runs to initialize stats in ApplyInitialReplicationState())
            bool statsChanged = false;
            if (avatarProto.StatProgressionTable.HasValue())
            {
                foreach (PrototypeId statProgressionEntryProtoRef in avatarProto.StatProgressionTable)
                {
                    StatProgressionEntryPrototype statProgressionEntryProto = statProgressionEntryProtoRef.As<StatProgressionEntryPrototype>();
                    if (statProgressionEntryProto == null)
                    {
                        Logger.Warn("OnLevelUp(): statProgressionEntryProto == null");
                        continue;
                    }

                    if (newLevel < statProgressionEntryProto.Level)
                        continue;

                    statsChanged |= statProgressionEntryProto.TryUpdateStats(Properties);
                }
            }

            // Stat refreshes are scheduled on stat changes, but even if our stats didn't change,
            // we still need to refresh here, because some stats use avatar level in their formulas.
            if (statsChanged == false)
                ScheduleStatsPowerRefresh();

            if (IsInWorld)
                UpdateAvatarSynergyCondition();

            // Notify clients
            SendLevelUpMessage();

            // Unlock new powers
            if (IsInWorld)
            {
                UpdateTalentPowers();
                UpdatePowerProgressionPowers(false);
                UpdateTravelPower();
            }

            // Remove items that are no longer equippable (e.g. if we are leveling down via prestige)
            CheckEquipmentRestrictions();

            // Restore health if needed
            if (restoreHealthAndEndurance && IsDead == false)
                Properties[PropertyEnum.Health] = Properties[PropertyEnum.HealthMax];

            // Update endurance
            foreach (PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto in GetPrimaryResourceManaBehaviors())
            {
                ManaType manaType = primaryManaBehaviorProto.ManaType;

                // Update the base value
                Curve manaCurve = GameDatabase.GetCurve(primaryManaBehaviorProto.BaseEndurancePerLevel);
                if (manaCurve == null)
                {
                    Logger.Warn("OnLevelUp(): manaCurve == null");
                    continue;
                }

                Properties[PropertyEnum.EnduranceBase, manaType] = manaCurve.GetAt(newLevel);

                // Restore to max if needed
                if (restoreHealthAndEndurance && primaryManaBehaviorProto.RestoreToMaxOnLevelUp && IsDead == false)
                    Properties[PropertyEnum.Endurance, manaType] = Properties[PropertyEnum.EnduranceMax, manaType];
            }

            if (IsInWorld)
                AutoSlotPowers();

            var player = GetOwnerOfType<Player>();
            if (player == null) return false;

            player.ScheduleCommunityBroadcast();
            Region?.AvatarLeveledUpEvent.Invoke(new(player, PrototypeDataRef, newLevel));

            return true;
        }

        protected override void SetCharacterLevel(int characterLevel)
        {
            int oldLevel = CharacterLevel;

            base.SetCharacterLevel(characterLevel);

            if (characterLevel != oldLevel)
                UpdateAvatarSynergyUnlocks(oldLevel, characterLevel);

            Player player = GetOwnerOfType<Player>();
            if (player == null) return;

            player.OnAvatarCharacterLevelChanged(this);
            player.OnScoringEvent(new(ScoringEventType.AvatarLevel, characterLevel));

            if (IsAtLevelCap)
            {
                int count = ScoringEvents.GetPlayerAvatarsAtLevelCap(player);
                player.OnScoringEvent(new(ScoringEventType.AvatarsAtLevelCap, count));

                if (IsAtMaxPrestigeLevel())
                {
                    count = ScoringEvents.GetPlayerAvatarsAtPrestigeLevelCap(player);
                    player.OnScoringEvent(new(ScoringEventType.AvatarsAtPrestigeLevelCap, count));
                }
            }
        }

        protected override void SetCombatLevel(int combatLevel)
        {
            base.SetCombatLevel(combatLevel);

            Agent teamUpAgent = CurrentTeamUpAgent;
            if (teamUpAgent != null)
                teamUpAgent.CombatLevel = combatLevel;

            Agent cotrolledAgent = ControlledAgent;
            if (cotrolledAgent != null)
                cotrolledAgent.CombatLevel = combatLevel;
        }

        #endregion

        #region Interaction

        public override bool UseInteractableObject(ulong entityId, PrototypeId missionRef)
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "UseInteractableObject(): player == null");

            var region = Region;
            if (region == null)
            {
                // We need to send NetMessageMissionInteractRelease here, or the client UI will get locked
                player.MissionInteractRelease(this, missionRef);
                return false;
            }

            if (entityId == InvalidId)
            {
                region?.NotificationInteractEvent.Invoke(new(player, missionRef));
                return true;
            }

            var interactableObject = Game.EntityManager.GetEntity<WorldEntity>(entityId);
            if (interactableObject == null || CanInteract(player, interactableObject) == false)
            {
                player.MissionInteractRelease(this, missionRef);
                return false;
            }

            //Logger.Trace($"UseInteractableObject(): {this} => {interactableObject}");

            var objectProto = interactableObject.WorldEntityPrototype;
            if (objectProto.PreInteractPower != PrototypeId.Invalid)
            {
                ulong targetId = player.Properties[PropertyEnum.InteractReadyForTargetId];
                player.Properties.RemoveProperty(PropertyEnum.InteractReadyForTargetId);
                if (targetId != entityId) return Logger.WarnReturn(false, "UseInteractableObject(): targetId != entityId");
            }

            if (interactableObject.IsInWorld == false && interactableObject is Item item)
                item.InteractWithAvatar(this);

            region.PlayerInteractEvent.Invoke(new(player, interactableObject, missionRef));

            if (interactableObject.Properties[PropertyEnum.EntSelActHasInteractOption])
                interactableObject.TriggerEntityActionEvent(EntitySelectorActionEventType.OnPlayerInteract);

            player.OnScoringEvent(new(ScoringEventType.EntityInteract, interactableObject.Prototype));

            if (interactableObject is Transition transition)
                transition.UseTransition(player);

            interactableObject.OnInteractedWith(this);

            return true;
        }

        private bool CanInteract(Player player, WorldEntity interactableObject)
        {
            if (IsAliveInWorld == false) return false;

            if (interactableObject.IsInWorld)
            {
                if (InInteractRange(interactableObject, InteractionMethod.Use) == false) return false;
            }
            else
            {
                if (player.Owns(interactableObject.Id) == false) return false;
            }

            InteractData data = null;
            var iteractionStatus = InteractionManager.CallGetInteractionStatus(new EntityDesc(interactableObject), this, 
                InteractionOptimizationFlags.None, InteractionFlags.None, ref data);
            return iteractionStatus != InteractionMethod.None;
        }

        public override bool InInteractRange(WorldEntity interactee, InteractionMethod interaction, bool interactFallbackRange = false)
        {
            if (IsUsingGamepadInput)
            {
                if (IsSingleInteraction(interaction) == false && interaction.HasFlag(InteractionMethod.Throw)) return false;
                if (IsInWorld == false && interactee.IsInWorld == false) return false;
                return InGamepadInteractRange(interactee);
            }
            return base.InInteractRange(interactee, interaction, interactFallbackRange);
        }

        public bool InGamepadInteractRange(WorldEntity interactee)
        {
            var gamepadGlobals = GameDatabase.GamepadGlobalsPrototype;
            if (gamepadGlobals == null || RegionLocation.Region == null) return false;

            Vector3 direction = Forward;
            Vector3 interacteePosition = interactee.RegionLocation.Position;
            Vector3 avatarPosition = RegionLocation.Position;
            Vector3 velocity = Vector3.Normalize2D(interacteePosition - avatarPosition);

            float minAngle = Math.Abs(MathHelper.ToDegrees(Vector3.Angle2D(direction, velocity)));
            float distance = Vector3.Distance2D(interacteePosition, avatarPosition);

            if (distance < Bounds.Radius + gamepadGlobals.GamepadInteractBoundsIncrease)
                return true;

            if (minAngle < gamepadGlobals.GamepadInteractionHalfAngle)
            {
                Bounds capsuleBound = new();
                capsuleBound.InitializeCapsule(0.0f, 500, BoundsCollisionType.Overlapping, BoundsFlags.None);
                capsuleBound.Center = avatarPosition + (direction * gamepadGlobals.GamepadInteractionOffset);

                velocity *= gamepadGlobals.GamepadInteractRange + Bounds.Radius;
                float timeOfIntersection = 1.0f;
                Vector3? resultNormal = null;
                return capsuleBound.Sweep(interactee.Bounds, Vector3.Zero, velocity, ref timeOfIntersection, ref resultNormal);
            }

            return false;
        }

        #endregion

        #region Inventories

        public InventoryResult GetEquipmentInventoryAvailableStatus(PrototypeId invProtoRef)
        {
            AvatarPrototype avatarProto = AvatarPrototype;
            if (avatarProto == null) return Logger.WarnReturn(InventoryResult.UnknownFailure, "GetEquipmentInventoryAvailableStatus(): avatarProto == null");

            foreach (AvatarEquipInventoryAssignmentPrototype equipInvEntryProto in avatarProto.EquipmentInventories)
            {
                if (equipInvEntryProto == null)
                {
                    Logger.Warn("GetEquipmentInventoryAvailableStatus(): equipInvEntryProto == null");
                    continue;
                }

                if (equipInvEntryProto.Inventory == invProtoRef)
                {
                    if (CharacterLevel < equipInvEntryProto.UnlocksAtCharacterLevel)
                        return InventoryResult.InvalidEquipmentInventoryNotUnlocked;
                    else
                        return InventoryResult.Success;
                }
            }

            return InventoryResult.UnknownFailure;
        }

        /// <summary>
        /// Validates item movement when equipping items to an avatar.
        /// </summary>
        /// <remarks>
        /// In practice this validates only artifacts equipment.
        /// </remarks>
        public static InventoryResult ValidateEquipmentChange(Game game, Item itemToBeMoved, InventoryLocation fromInvLoc, InventoryLocation toInvLoc, out Item resultItem)
        {
            resultItem = null;

            if (itemToBeMoved.InventoryLocation.Equals(fromInvLoc) == false)
                return Logger.WarnReturn(InventoryResult.Invalid, "ValidateEquipmentChange(): itemToBeMoved.InventoryLocation.Equals(fromInvLoc) == false");
            
            // Validate only items that are being moved to avatar inventories (i.e. being equipped)
            Avatar containerAvatar = game.EntityManager.GetEntity<Avatar>(toInvLoc.ContainerId);
            if (containerAvatar == null)
                return InventoryResult.Success;

            // Validate only artifacts (TODO: make sure this is the case in other versions of the game)
            if (toInvLoc.IsArtifactInventory == false || fromInvLoc.IsArtifactInventory)
                return InventoryResult.Success;

            List<Inventory> otherArtifactInvs = ListPool<Inventory>.Instance.Get();

            switch (toInvLoc.InventoryConvenienceLabel)
            {
                case InventoryConvenienceLabel.AvatarArtifact1:
                    otherArtifactInvs.Add(containerAvatar.GetInventory(InventoryConvenienceLabel.AvatarArtifact2));
                    otherArtifactInvs.Add(containerAvatar.GetInventory(InventoryConvenienceLabel.AvatarArtifact3));
                    otherArtifactInvs.Add(containerAvatar.GetInventory(InventoryConvenienceLabel.AvatarArtifact4));
                    break;

                case InventoryConvenienceLabel.AvatarArtifact2:
                    otherArtifactInvs.Add(containerAvatar.GetInventory(InventoryConvenienceLabel.AvatarArtifact1));
                    otherArtifactInvs.Add(containerAvatar.GetInventory(InventoryConvenienceLabel.AvatarArtifact3));
                    otherArtifactInvs.Add(containerAvatar.GetInventory(InventoryConvenienceLabel.AvatarArtifact4));
                    break;

                case InventoryConvenienceLabel.AvatarArtifact3:
                    otherArtifactInvs.Add(containerAvatar.GetInventory(InventoryConvenienceLabel.AvatarArtifact1));
                    otherArtifactInvs.Add(containerAvatar.GetInventory(InventoryConvenienceLabel.AvatarArtifact2));
                    otherArtifactInvs.Add(containerAvatar.GetInventory(InventoryConvenienceLabel.AvatarArtifact4));
                    break;

                case InventoryConvenienceLabel.AvatarArtifact4:
                    otherArtifactInvs.Add(containerAvatar.GetInventory(InventoryConvenienceLabel.AvatarArtifact1));
                    otherArtifactInvs.Add(containerAvatar.GetInventory(InventoryConvenienceLabel.AvatarArtifact2));
                    otherArtifactInvs.Add(containerAvatar.GetInventory(InventoryConvenienceLabel.AvatarArtifact3));
                    break;
            }

            try
            {
                if (otherArtifactInvs[0] == null || otherArtifactInvs[1] == null || otherArtifactInvs[2] == null)
                    return Logger.WarnReturn(InventoryResult.Invalid, "ValidateEquipmentChange(): otherArtifactInvs[0] == null || otherArtifactInvs[1] == null || otherArtifactInvs[2] == null");

                EntityManager entityManager = game.EntityManager;
                for (int i = 0; i < otherArtifactInvs.Count; i++)
                {
                    if (otherArtifactInvs[i].Count == 0)
                        continue;

                    ulong otherArtifactId = otherArtifactInvs[i].GetEntityInSlot(0);
                    Item otherArtifact = entityManager.GetEntity<Item>(otherArtifactId);
                    if (otherArtifact == null) return Logger.WarnReturn(InventoryResult.Invalid, "ValidateEquipmentChange(): otherArtifact == null");

                    if (itemToBeMoved.PrototypeDataRef == otherArtifact.PrototypeDataRef)
                        return InventoryResult.InvalidTwoOfSameArtifact;

                    if (itemToBeMoved.CanBeEquippedWithItem(otherArtifact) == false)
                    {
                        resultItem = otherArtifact;
                        return InventoryResult.InvalidRestrictedByOtherItem;
                    }
                }

                return InventoryResult.Success;
            }
            finally
            {
                ListPool<Inventory>.Instance.Return(otherArtifactInvs);
            }
        }

        public override void OnOtherEntityAddedToMyInventory(Entity entity, InventoryLocation invLoc, bool unpackedArchivedEntity)
        {
            base.OnOtherEntityAddedToMyInventory(entity, invLoc, unpackedArchivedEntity);

            if (entity is not Item item)
                return;

            InventoryCategory category = invLoc.InventoryCategory;
            InventoryConvenienceLabel convenienceLabel = invLoc.InventoryConvenienceLabel;

            // Costume can be changed for library avatars
            if (convenienceLabel == InventoryConvenienceLabel.Costume)
                ChangeCostume(entity.PrototypeDataRef);

            if (IsInWorld == false)
                return;

            // Do things that require the avatar to be in play

            if (invLoc.InventoryPrototype?.IsEquipmentInventory != true)
                return;

            // Assign powers granted by equipped items
            if (item.GetPowerGranted(out PrototypeId powerProtoRef) && GetPower(powerProtoRef) == null)
            {
                int characterLevel = CharacterLevel;
                int combatLevel = CombatLevel;
                int itemLevel = item.Properties[PropertyEnum.ItemLevel];
                float itemVariation = item.Properties[PropertyEnum.ItemVariation];
                PowerIndexProperties indexProps = new(0, characterLevel, combatLevel, itemLevel, itemVariation);

                if (AssignPower(powerProtoRef, indexProps) == null)
                {
                    Logger.Warn($"OnOtherEntityAddedToMyInventory(): Failed to assign item power {powerProtoRef.GetName()} to avatar {this}");
                    return;
                }                
            }
            
            OnChangeInventory(item);
        }

        public override void OnOtherEntityRemovedFromMyInventory(Entity entity, InventoryLocation invLoc)
        {
            base.OnOtherEntityRemovedFromMyInventory(entity, invLoc);

            if (entity is not Item item)
                return;

            InventoryCategory category = invLoc.InventoryCategory;
            InventoryConvenienceLabel convenienceLabel = invLoc.InventoryConvenienceLabel;

            // Costume can be changed for library avatars
            if (convenienceLabel == InventoryConvenienceLabel.Costume)
                ChangeCostume(PrototypeId.Invalid);

            if (IsInWorld == false)
                return;

            // Do things that require the avatar to be in play

            if (invLoc.InventoryPrototype?.IsEquipmentInventory != true)
                return;

            // Unassign powers granted by equipped items
            if (item.GetPowerGranted(out PrototypeId powerProtoRef) && GetPower(powerProtoRef) != null)
                UnassignPower(powerProtoRef);

            OnChangeInventory(item);
        }

        private void OnChangeInventory(Item item)
        {
            var player = GetOwnerOfType<Player>();
            if (player == null) return;
            player.UpdateScoringEventContext();

            if (item.IsGear(AvatarPrototype))
            {
                int count = ScoringEvents.GetAvatarMinGearLevel(this);
                player.OnScoringEvent(new(ScoringEventType.MinGearLevel, Prototype, count));
            }
        }

        protected override bool InitInventories(bool populateInventories)
        {
            bool success = base.InitInventories(populateInventories);

            AvatarPrototype avatarProto = AvatarPrototype;
            foreach (AvatarEquipInventoryAssignmentPrototype equipInvAssignment in avatarProto.EquipmentInventories)
            {
                if (AddInventory(equipInvAssignment.Inventory, populateInventories ? equipInvAssignment.LootTable : PrototypeId.Invalid) == false)
                {
                    success = false;
                    Logger.Warn($"InitInventories(): Failed to add inventory {GameDatabase.GetPrototypeName(equipInvAssignment.Inventory)} to {this}");
                }
            }

            return success;
        }

        #endregion

        #region Costumes

        public override AssetId GetEntityWorldAsset()
        {
            AssetId result = AssetId.Invalid;

            TransformModePrototype transformModeProto = CurrentTransformMode.As<TransformModePrototype>();
            if (transformModeProto != null)
            {
                if (transformModeProto.UnrealClassOverrides.HasValue())
                {
                    AssetId currentCostumeAssetRef = GetCurrentCostumeAssetRef();

                    foreach (TransformModeUnrealOverridePrototype overrideProto in transformModeProto.UnrealClassOverrides)
                    {
                        if (overrideProto.IncomingUnrealClass == AssetId.Invalid)
                        {
                            Logger.Warn("GetEntityWorldAsset(): overrideProto.IncomingUnrealClass == AssetId.Invalid");
                            continue;
                        }

                        if (overrideProto.IncomingUnrealClass == currentCostumeAssetRef)
                        {
                            result = overrideProto.TransformedUnrealClass;
                            break;
                        }
                    }
                }

                if (result == AssetId.Invalid)
                    result = transformModeProto.UnrealClass;
            }
            else
            {
                result = GetCurrentCostumeAssetRef();
            }

            if (result == AssetId.Invalid)
                Logger.Warn($"GetEntityWorldAsset(): Unable to get a valid unreal class asset for avatar [{this}]");

            return result;
        }

        public PrototypeId GetCurrentCostumePrototypeRef()
        {
            PrototypeId equippedCostumeRef = EquippedCostumeRef;
            if (equippedCostumeRef != PrototypeId.Invalid)
                return equippedCostumeRef;

            return AvatarPrototype.GetStartingCostumeForPlatform(Platforms.PC);
        }

        public AssetId GetCurrentCostumeAssetRef()
        {
            // HACK: Return starting costume for Entity/Items/Costumes/Costume.defaults to avoid spam when forcing pre-VU costumes
            CostumePrototype equippedCostume = EquippedCostume;
            if (equippedCostume != null && equippedCostume.DataRef != (PrototypeId)10774581141289766864)
                return equippedCostume.CostumeUnrealClass;

            return GetStartingCostumeAssetRef();
        }

        public AssetId GetStartingCostumeAssetRef()
        {
            AvatarPrototype avatarProto = AvatarPrototype;
            if (avatarProto == null) return Logger.WarnReturn(AssetId.Invalid, "GetStartingCostumeAssetRef(): avatarProto == null");
            return avatarProto.GetStartingCostumeAssetRef(Platforms.PC);
        }

        public bool ChangeCostume(PrototypeId costumeProtoRef)
        {
            CostumePrototype costumeProto = null;

            if (costumeProtoRef != PrototypeId.Invalid)
            {
                // Make sure we have a valid costume prototype
                costumeProto = GameDatabase.GetPrototype<CostumePrototype>(costumeProtoRef);
                if (costumeProto == null)
                    return Logger.WarnReturn(false, $"ChangeCostume(): {costumeProtoRef} is not a valid costume prototype ref");
            }

            Properties[PropertyEnum.CostumeCurrent] = costumeProtoRef;

            // Update avatar library
            Player owner = GetOwnerOfType<Player>();
            if (owner == null) return Logger.WarnReturn(false, "ChangeCostume(): owner == null");

            // NOTE: Avatar mode is hardcoded to 0 since hardcore and ladder avatars never got implemented
            owner.Properties[PropertyEnum.AvatarLibraryCostume, 0, PrototypeDataRef] = costumeProtoRef;

            return true;
        }

        public bool GiveStartingCostume()
        {
            AvatarPrototype avatarProto = AvatarPrototype;
            if (avatarProto == null) return Logger.WarnReturn(false, "GiveStartingCostume(): avatarProto == null");

            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "GiveStartingCostume(): player == null");

            Inventory costumeInventory = GetInventory(InventoryConvenienceLabel.Costume);
            if (costumeInventory == null) return Logger.WarnReturn(false, "GiveStartingCostume(): costumeInventory == null");

            Inventory generalInventory = player.GetInventory(InventoryConvenienceLabel.General);
            if (generalInventory == null) return Logger.WarnReturn(false, "GiveStartingCostume(): generalInventory == null");

            Inventory deliveryBox = player.GetInventory(InventoryConvenienceLabel.DeliveryBox);
            if (deliveryBox == null) return Logger.WarnReturn(false, "GiveStartingCostume(): deliveryBox == null");

            Inventory errorRecovery = player.GetInventory(InventoryConvenienceLabel.ErrorRecovery);
            if (errorRecovery == null) return Logger.WarnReturn(false, "GiveStartingCostume(): errorRecovery == null");

            PrototypeId startingCostumeProtoRef = avatarProto.GetStartingCostumeForPlatform(Platforms.PC);
            if (startingCostumeProtoRef == PrototypeId.Invalid)
                return true;

            ItemSpec itemSpec = Game.LootManager.CreateItemSpec(startingCostumeProtoRef, LootContext.CashShop, player);

            using EntitySettings entitySettings = ObjectPoolManager.Instance.Get<EntitySettings>();
            entitySettings.EntityRef = itemSpec.ItemProtoRef;
            entitySettings.ItemSpec = itemSpec;

            Item costume = Game.EntityManager.CreateEntity(entitySettings) as Item;
            if (costume == null)
                return Logger.WarnReturn(false, $"GiveStartingCostume(): Failed to create starting costume for avatar [{this}]");

            InventoryResult result = costume.ChangeInventoryLocation(costumeInventory);

            if (result != InventoryResult.Success)
                result = costume.ChangeInventoryLocation(generalInventory);

            if (result != InventoryResult.Success)
                result = costume.ChangeInventoryLocation(deliveryBox);

            if (result != InventoryResult.Success)
            {
                Logger.Error($"GiveStartingCostume(): Failed to put costume [{costume}] into delivery box for avatar [{this}]");
                result = costume.ChangeInventoryLocation(errorRecovery);
            }

            if (result != InventoryResult.Success)
            {
                Logger.Error($"GiveStartingCostume(): Failed to put costume [{costume}] into error recovery for avatar [{this}]");
                costume.Destroy();
                return false;
            }

            return true;
        }

        #endregion

        #region Loot

        // NOTE: All these stacking functions are very copy-pasted, but that's client-accurate

        // Experience

        public float GetAvatarXPMultiplier()
        {
            float multiplier = 1f;

            multiplier += Properties[PropertyEnum.ExperienceBonusPct];
            multiplier += Properties[PropertyEnum.ExperienceBonusAvatarSynergy];
            multiplier += GetStackingExperienceBonusPct(Properties);

            return MathF.Max(-1f, multiplier);
        }

        public float GetPartyXPMultiplier(TuningPrototype tuningProto)
        {
            Party party = Party;
            if (party == null)
                return 1f;

            CurveId curveRef = party.Type == GroupType.GroupType_Raid ? tuningProto.PctXPFromRaid : tuningProto.PctXPFromParty;
            Curve curve = curveRef.AsCurve();
            if (curve == null) return Logger.WarnReturn(1f, "GetPartyXPMultiplier(): curve == null");

            float multiplier = 1f + curve.GetAt(CharacterLevel);
            multiplier += Math.Max(LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_PartyXPBonusPct) - 1f, 0f);
            return MathF.Max(multiplier, 0f);
        }

        public float GetLiveTuningXPMultiplier()
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(0f, "GetLiveTuningXPMultiplier(): player == null");

            RegionPrototype regionProto = Region?.Prototype;
            if (regionProto == null) return Logger.WarnReturn(0f, "GetLiveTuningXPMultiplier(): regionProto == null");

            bool canUseLiveTuneBonuses = player.CanUseLiveTuneBonuses();

            float avatarMultiplier = 1f;
            if (canUseLiveTuneBonuses || LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_RespectLevelForAvatarXP) == 0f)
                avatarMultiplier = LiveTuningManager.GetLiveAvatarTuningVar(AvatarPrototype, AvatarEntityTuningVar.eAETV_BonusXPPct);

            float regionMultiplier = 1f;
            if (canUseLiveTuneBonuses || LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_RespectLevelForRegionXP) == 0f)
                regionMultiplier = LiveTuningManager.GetLiveRegionTuningVar(regionProto, RegionTuningVar.eRT_BonusXPPct);

            return avatarMultiplier * regionMultiplier;
        }

        public static float GetStackingExperienceBonusPct(PropertyCollection properties)
        {
            float stackingExperienceBonusPct = 0f;

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.ExperienceBonusStackCount))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId powerProtoRef);
                int stackCount = kvp.Value;
                float multiplier = GetStackingExperienceBonusMultiplier(properties, powerProtoRef);

                stackingExperienceBonusPct += GetStackingExperienceBonusPct(stackCount) * multiplier;
            }

            return stackingExperienceBonusPct;
        }

        public static float GetStackingExperienceBonusPct(int stackCount)
        {
            if (stackCount <= 0)
                return 0f;

            Curve curve = GameDatabase.GlobalsPrototype.ExperienceBonusCurve.AsCurve();
            if (curve == null) return Logger.WarnReturn(0f, "GetStackingExperienceBonusPct(): curve == null");

            return curve.GetAt(stackCount);
        }

        public static float GetStackingExperienceBonusMultiplier(PropertyCollection properties, PrototypeId powerProtoRef)
        {
            float multiplier = 1f;

            if (powerProtoRef == PrototypeId.Invalid)
                return multiplier;

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.ExperienceBonusStackingMult, powerProtoRef))
            {
                multiplier = kvp.Value;
                break;
            }

            return multiplier;
        }

        // Rarity

        public static float GetStackingLootBonusRarityPct(PropertyCollection properties)
        {
            float stackingLootBonusRarityPct = 0f;

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.LootBonusRarityStackCount))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId powerProtoRef);
                int stackCount = kvp.Value;
                float multiplier = GetStackingLootBonusRarityMultiplier(properties, powerProtoRef);

                stackingLootBonusRarityPct += GetStackingLootBonusRarityPct(stackCount) * multiplier;
            }

            return stackingLootBonusRarityPct;
        }

        public static float GetStackingLootBonusRarityPct(int stackCount)
        {
            if (stackCount <= 0)
                return 0f;

            Curve curve = GameDatabase.LootGlobalsPrototype.LootBonusRarityCurve.AsCurve();
            if (curve == null) return Logger.WarnReturn(0f, "GetStackingLootBonusRarityPct(): curve == null");

            return curve.GetAt(stackCount);
        }

        public static float GetStackingLootBonusRarityMultiplier(PropertyCollection properties, PrototypeId powerProtoRef)
        {
            float multiplier = 1f;

            if (powerProtoRef == PrototypeId.Invalid)
                return multiplier;

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.LootBonusRarityStackingMult, powerProtoRef))
            {
                multiplier = kvp.Value;
                break;
            }

            return multiplier;
        }

        // Special

        public static float GetStackingLootBonusSpecialPct(PropertyCollection properties)
        {
            float stackingLootBonusSpecialPct = 0f;

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.LootBonusSpecialStackCount))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId powerProtoRef);
                int stackCount = kvp.Value;
                float multiplier = GetStackingLootBonusSpecialMultiplier(properties, powerProtoRef);

                stackingLootBonusSpecialPct += GetStackingLootBonusSpecialPct(stackCount) * multiplier;
            }

            return stackingLootBonusSpecialPct;
        }

        public static float GetStackingLootBonusSpecialPct(int stackCount)
        {
            if (stackCount <= 0)
                return 0f;

            Curve curve = GameDatabase.LootGlobalsPrototype.LootBonusSpecialCurve.AsCurve();
            if (curve == null) return Logger.WarnReturn(0f, "GetStackingLootBonusSpecialPct(): curve == null");

            return curve.GetAt(stackCount);
        }

        public static float GetStackingLootBonusSpecialMultiplier(PropertyCollection properties, PrototypeId powerProtoRef)
        {
            float multiplier = 1f;

            if (powerProtoRef == PrototypeId.Invalid)
                return multiplier;

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.LootBonusSpecialStackingMult, powerProtoRef))
            {
                multiplier = kvp.Value;
                break;
            }

            return multiplier;
        }

        // Flat Credits

        public static int GetFlatCreditsBonus(PropertyCollection properties)
        {
            int flatCreditsBonus = properties[PropertyEnum.LootBonusCreditsFlat];
            flatCreditsBonus += (int)GetStackingFlatCreditsBonus(properties);
            return flatCreditsBonus;
        }

        public static float GetStackingFlatCreditsBonus(PropertyCollection properties)
        {
            float stackingFlatCreditsBonus = 0f;

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.LootBonusCreditsStackCount))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId powerProtoRef);
                int stackCount = kvp.Value;
                float multiplier = GetStackingFlatCreditsBonusMultiplier(properties, powerProtoRef);

                stackingFlatCreditsBonus += GetStackingFlatCreditsBonus(stackCount) * multiplier;
            }

            return stackingFlatCreditsBonus;
        }

        public static float GetStackingFlatCreditsBonus(int stackCount)
        {
            if (stackCount <= 0)
                return 0f;

            Curve curve = GameDatabase.LootGlobalsPrototype.LootBonusFlatCreditsCurve.AsCurve();
            if (curve == null) return Logger.WarnReturn(0f, "GetStackingFlatCreditsBonus(): curve == null");

            return curve.GetAt(stackCount);
        }

        public static float GetStackingFlatCreditsBonusMultiplier(PropertyCollection properties, PrototypeId powerProtoRef)
        {
            float multiplier = 1f;

            if (powerProtoRef == PrototypeId.Invalid)
                return multiplier;

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.LootBonusCreditsStackingMult, powerProtoRef))
            {
                multiplier = kvp.Value;
                break;
            }

            return multiplier;
        }

        // Orb Aggro Range

        public static float GetOrbAggroRangeBonusPct(PropertyCollection properties)
        {
            float orbAggroRangePctBonus = properties[PropertyEnum.OrbAggroRangePctBonus];
            orbAggroRangePctBonus += GetStackingOrbAggroRangeBonusPct(properties);
            return MathF.Max(-1f, orbAggroRangePctBonus);
        }

        public static float GetStackingOrbAggroRangeBonusPct(PropertyCollection properties)
        {
            float stackingOrbAggroRangeBonus = 0f;

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.OrbAggroRangeBonusStackCount))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId powerProtoRef);
                int stackCount = kvp.Value;
                float multiplier = GetStackingOrbAggroRangeBonusMultiplier(properties, powerProtoRef);

                stackingOrbAggroRangeBonus += GetStackingOrbAggroRangeBonusPct(stackCount) * multiplier;
            }

            return stackingOrbAggroRangeBonus;
        }

        public static float GetStackingOrbAggroRangeBonusPct(int stackCount)
        {
            if (stackCount <= 0)
                return 0f;

            Curve curve = GameDatabase.AIGlobalsPrototype.OrbAggroRangeBonusCurve.AsCurve();
            if (curve == null) return Logger.WarnReturn(0f, "GetStackingFlatCreditsBonus(): curve == null");

            return curve.GetAt(stackCount);
        }

        public static float GetStackingOrbAggroRangeBonusMultiplier(PropertyCollection properties, PrototypeId powerProtoRef)
        {
            float multiplier = 1f;

            if (powerProtoRef == PrototypeId.Invalid)
                return multiplier;

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.OrbAggroRangeBonusStackingMult, powerProtoRef))
            {
                multiplier = kvp.Value;
                break;
            }

            return multiplier;
        }

        #endregion

        #region Team-Ups

        public void SelectTeamUpAgent(PrototypeId teamUpProtoRef)
        {
            if (Game.GameOptions.TeamUpSystemEnabled == false || IsInWorld == false) return;

            if (teamUpProtoRef == PrototypeId.Invalid || IsTeamUpAgentUnlocked(teamUpProtoRef) == false) return;
            var teamUpProto = GameDatabase.GetPrototype<WorldEntityPrototype>(teamUpProtoRef);
            if (teamUpProto.IsLiveTuningEnabled() == false) return;

            Agent oldTeamUp = CurrentTeamUpAgent;
            if (oldTeamUp != null)
                if (oldTeamUp.IsInWorld || oldTeamUp.PrototypeDataRef == teamUpProtoRef) return;

            Properties[PropertyEnum.AvatarTeamUpAgent] = teamUpProtoRef;
            Player player = GetOwnerOfType<Player>();
            player.Properties[PropertyEnum.AvatarLibraryTeamUp, 0, Prototype.DataRef] = teamUpProtoRef;

            if (oldTeamUp != null)
            {
                oldTeamUp.AssignTeamUpAgentPowers();
                oldTeamUp.RemoveTeamUpAffixesFromAvatar(this);
            }

            var currentTeamUp = CurrentTeamUpAgent;
            if (currentTeamUp == null) return;

            SetOwnerTeamUpAgent(currentTeamUp);
            currentTeamUp.AssignTeamUpAgentPowers();
            currentTeamUp.ApplyTeamUpAffixesToAvatar(this);
            currentTeamUp.SetTeamUpsAtMaxLevel(player);

            // event PlayerActivatedTeamUpGameEvent not used in missions
        }

        public void SummonTeamUpAgent(TimeSpan duration)
        {
            if (Game.GameOptions.TeamUpSystemEnabled == false) return;
            if (IsInWorld == false) return;

            Agent teamUpAgent = CurrentTeamUpAgent;
            if (teamUpAgent == null || teamUpAgent.IsLiveTuningEnabled == false) return;

            if (teamUpAgent.IsInWorld) 
            {
                if (teamUpAgent.IsDead == false) return;
                else DespawnTeamUpAgent();
            }

            // schedule Dissmiss event
            if (_dismissTeamUpAgentEvent.IsValid) return;

            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.CancelEvent(_dismissTeamUpAgentEvent);

            if (duration > TimeSpan.Zero && teamUpAgent.IsPermanentTeamUpStyle() == false)
                ScheduleEntityEvent(_dismissTeamUpAgentEvent, duration);

            SetTeamUpAgentDuration(true, Game.CurrentTime, duration);

            SpawnTeamUpAgent(true);

            TryActivateOnSummonPetProcs(teamUpAgent);
        }

        private void SpawnTeamUpAgent(bool newOnServer)
        {
            Agent teamUpAgent = CurrentTeamUpAgent;
            if (teamUpAgent == null) return;

            // Resurrect or restore team-up health
            if (teamUpAgent.IsDead)
                teamUpAgent.Resurrect();
            else
                teamUpAgent.Properties[PropertyEnum.Health] = teamUpAgent.Properties[PropertyEnum.HealthMax];

            teamUpAgent.RevealEquipmentToOwner();
            teamUpAgent.SetAsPersistent(this, newOnServer);
            teamUpAgent.AssignTeamUpAgentPowers();
            teamUpAgent.SetSummonedAllianceOverride(Alliance);
        }

        private bool RespawnTeamUpAgent()
        {
            if (IsInWorld == false) return false;
            var teamUp = CurrentTeamUpAgent;
            if (teamUp == null || teamUp.IsInWorld) return false;
            if (Properties[PropertyEnum.AvatarTeamUpIsSummoned] == false) return false;

            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return false;
            scheduler.CancelEvent(_dismissTeamUpAgentEvent);

            TimeSpan duration = Properties[PropertyEnum.AvatarTeamUpDuration];
            if (duration > TimeSpan.Zero && teamUp.IsPermanentTeamUpStyle() == false)
            {
                TimeSpan startTime = Properties[PropertyEnum.AvatarTeamUpStartTime];
                TimeSpan time = duration - (Game.CurrentTime - startTime);

                if (time <= TimeSpan.Zero)
                {
                    ResetTeamUpAgentDuration();
                    return false;
                }
                ScheduleEntityEvent(_dismissTeamUpAgentEvent, time);
            }
            SpawnTeamUpAgent(false);
            return true;
        }

        private void DespawnTeamUpAgent()
        {
            var teamup = CurrentTeamUpAgent;
            if (teamup == null) return;

            if (teamup.IsInWorld) teamup.ExitWorld();
            else teamup.SetDormant(true);
        }

        public void DismissTeamUpAgent(bool reset)
        {
            if (IsInWorld == false) return;

            if (reset) ResetTeamUpAgentDuration();

            Agent teamUpAgent = CurrentTeamUpAgent;
            if (teamUpAgent == null) return;

            if (teamUpAgent.IsAliveInWorld)
            {
                bool isSummoned = Properties[PropertyEnum.AvatarTeamUpIsSummoned];
                TimeSpan startTime = Properties[PropertyEnum.AvatarTeamUpStartTime];
                TimeSpan duration = Properties[PropertyEnum.AvatarTeamUpDuration];

                teamUpAgent.Kill();

                if (reset == false) 
                    SetTeamUpAgentDuration(isSummoned, startTime, duration);
            }
            else
            {
                DespawnTeamUpAgent();
            }
        }

        private void SetTeamUpAgentDuration(bool isSummoned, TimeSpan startTime, TimeSpan duration)
        {
            Properties[PropertyEnum.AvatarTeamUpIsSummoned] = isSummoned;
            Properties[PropertyEnum.AvatarTeamUpStartTime] = startTime;
            Properties[PropertyEnum.AvatarTeamUpDuration] = duration;
        }

        public void ResetTeamUpAgentDuration()
        {
            Properties.RemoveProperty(PropertyEnum.AvatarTeamUpIsSummoned);
            Properties.RemoveProperty(PropertyEnum.AvatarTeamUpStartTime);
            Properties.RemoveProperty(PropertyEnum.AvatarTeamUpDuration);
        }

        public void SetOwnerTeamUpAgent(Agent teamUpAgent)
        {
            Properties[PropertyEnum.AvatarTeamUpAgentId] = teamUpAgent.Id;
            teamUpAgent.Properties[PropertyEnum.TeamUpOwnerId] = Id;
            teamUpAgent.Properties[PropertyEnum.PowerUserOverrideID] = Id;

            teamUpAgent.CombatLevel = CombatLevel;
        }

        public bool IsTeamUpAgentUnlocked(PrototypeId teamUpProtoRef)
        {
            return GetTeamUpAgent(teamUpProtoRef) != null;
        }

        public Agent GetTeamUpAgent(PrototypeId teamUpProtoRef)
        {
            if (teamUpProtoRef == PrototypeId.Invalid) return null;
            Player player = GetOwnerOfType<Player>();
            return player?.GetTeamUpAgent(teamUpProtoRef);
        }

        public void OnEnteredWorldTeamUpAgent()
        {
            var player = GetOwnerOfType<Player>();
            player?.UpdateScoringEventContext();
        }

        public void OnExitedWorldTeamUpAgent(Agent teamUpAgent)
        {
            var player = GetOwnerOfType<Player>();
            if (player == null) return;

            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.CancelEvent(_dismissTeamUpAgentEvent);

            teamUpAgent.AssignTeamUpAgentPowers();
            teamUpAgent.SetDormant(true);

            player.UpdateScoringEventContext();
        }

        public void TryTeamUpStyleSelect(uint styleIndex)
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;

            var teamUpAgent = CurrentTeamUpAgent;
            if (teamUpAgent == null) return;

            if (teamUpAgent.Prototype is not AgentTeamUpPrototype teamUpProto) return;
            if (teamUpProto.Styles.IsNullOrEmpty()) return;
            if (styleIndex < 0 || styleIndex >= teamUpProto.Styles.Length) return;

            if (styleIndex == teamUpAgent.Properties[PropertyEnum.TeamUpStyle]) return;

            bool oldStyle = teamUpAgent.IsPermanentTeamUpStyle();
            teamUpAgent.Properties[PropertyEnum.TeamUpStyle] = styleIndex;
            bool newStyle = teamUpAgent.IsPermanentTeamUpStyle();
            teamUpAgent.AssignTeamUpAgentPowers();

            if (newStyle)
            {
                scheduler.CancelEvent(_dismissTeamUpAgentEvent);
            }
            else if (oldStyle && IsInWorld)
            {
                TimeSpan duration = Properties[PropertyEnum.AvatarTeamUpDuration];
                if (duration > TimeSpan.Zero)
                {
                    Properties[PropertyEnum.AvatarTeamUpStartTime] = Game.CurrentTime;
                    scheduler.CancelEvent(_dismissTeamUpAgentEvent);
                    ScheduleEntityEvent(_dismissTeamUpAgentEvent, duration);
                }
            }
        }

        #endregion

        #region PersistentAgents

        private Agent GetCurrentVanityPet()
        {
            var keywordGlobals = GameDatabase.KeywordGlobalsPrototype;
            if (keywordGlobals == null) return Logger.WarnReturn((Agent)null, "GetCurrentVanityPet(): keywordGlobals == null");

            foreach (var summoned in new SummonedEntityIterator(this))
                if (summoned is Agent pet && pet.HasKeyword(keywordGlobals.VanityPetKeywordPrototype))
                    return pet;

            return null;
        }

        private void RespawnPersistentAgents()
        {
            if (RespawnTeamUpAgent() == false)
            {
                var teamUpAgent = CurrentTeamUpAgent;
                if (teamUpAgent != null)
                {
                    SetOwnerTeamUpAgent(teamUpAgent);
                    teamUpAgent.AssignTeamUpAgentPowers();
                }
            }

            var controlledInventory = ControlledInventory;
            if (controlledInventory != null && controlledInventory.Count > 1)
                RemoveControlledAgentsFromInventory();

            if (ControlledAgentHasSummonDuration() == false)
                SummonControlledAgentWithDuration();

            if (IsInTown() == false) SetSummonWithLifespanRemaining();
        }

        private void DespawnPersistentAgents()
        {
            DespawnTeamUpAgent();
            DespawnControlledAgent();
            ResetSummonWithLifespanRemaining();
        }

        private void SetSummonWithLifespanRemaining()
        {
            foreach (var summoned in new SummonedEntityIterator(this))
                if (summoned.Properties[PropertyEnum.SummonedEntityIsRegionPersisted])
                {
                    summoned.SetAsPersistent(this, false);
                    summoned.Properties[PropertyEnum.DetachOnContainerDestroyed] = true;

                    var lifespan = TimeSpan.FromMilliseconds((int)summoned.Properties[PropertyEnum.SummonLifespanRemainingMS]);
                    if (lifespan > TimeSpan.Zero)
                    {
                        summoned.ResetLifespan(lifespan);
                        summoned.Properties.RemoveProperty(PropertyEnum.SummonLifespanRemainingMS);
                    }
                }
        }

        private void ResetSummonWithLifespanRemaining()
        {
            foreach (var summoned in new SummonedEntityIterator(this))
                if (summoned.Properties[PropertyEnum.SummonedEntityIsRegionPersisted])
                {
                    var lifespan = summoned.GetRemainingLifespan();
                    if (lifespan > TimeSpan.Zero)
                    {
                        summoned.Properties[PropertyEnum.SummonLifespanRemainingMS] = (long)lifespan.TotalMilliseconds;
                    }
                    summoned.Properties[PropertyEnum.DetachOnContainerDestroyed] = false;
                    summoned.ExitWorld();
                }
        }

        public void SummonControlledAgentWithDuration()
        {
            if (HasControlPowerEquipped() == false) return;

            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;

            var controlled = ControlledAgent;
            if (controlled == null) return;

            var aiGlobals = GameDatabase.AIGlobalsPrototype;

            if (controlled.HasKeyword(aiGlobals.CantBeControlledKeyword))
            {
                RemoveControlledAgentFromInventory(controlled);
                KillControlledAgent(controlled, KillFlags.None);
            }
            else
            {
                controlled.Properties[PropertyEnum.AIMasterAvatarDbGuid] = DatabaseUniqueId;
                controlled.SetAsPersistent(this, false);

                if (ControlledAgentHasSummonDuration())
                {
                    scheduler.CancelEvent(_despawnControlledEvent);
                    var duration = TimeSpan.FromMilliseconds(aiGlobals.ControlledAgentSummonDurationMS);
                    ScheduleEntityEvent(_despawnControlledEvent, duration);
                }
            }
        }

        private void DespawnControlledAgent()
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;

            scheduler.CancelEvent(_despawnControlledEvent);

            var controlled = ControlledAgent;
            if (controlled == null) return;

            controlled.KillSummonedOnOwnerDeath();
            controlled.ExitWorld();
        }

        public void RemoveAndKillControlledAgent()
        {
            var controlled = ControlledAgent;
            if (controlled == null) return;

            if (controlled.IsDestroyed || controlled.TestStatus(EntityStatus.PendingDestroy)) return;

            RemoveControlledAgentFromInventory(controlled);
            KillControlledAgent(controlled, KillFlags.Release);
        }

        private void KillControlledAgent(Agent controlled, KillFlags killFlags)
        {
            if (controlled.IsDestroyed || controlled.TestStatus(EntityStatus.PendingDestroy)) return;
            if (controlled.IsAliveInWorld)
            {
                if (killFlags.HasFlag(KillFlags.Release))
                    TryActivateOnControlledEntityReleasedProcs(controlled);

                controlled.Kill(null, killFlags);
            }
            else
            {
                controlled.Destroy();
            }
        }

        private bool ControlledAgentHasSummonDuration()
        {
            var controlledAgent = ControlledAgent;
            return controlledAgent != null && controlledAgent.Properties.HasProperty(PropertyEnum.ControlledAgentHasSummonDur);
        }

        private void RemoveControlledAgentsFromInventory()
        {
            List<Agent> destroyList = ListPool<Agent>.Instance.Get();

            var manager = Game.EntityManager;
            foreach (var entry in ControlledInventory)
            {
                var controlled = manager.GetEntity<Agent>(entry.Id);
                if (controlled == null) continue;
                destroyList.Add(controlled);
            }

            foreach (var controlled in destroyList)
                if (controlled.IsDestroyed == false && controlled.TestStatus(EntityStatus.PendingDestroy) == false)
                {
                    RemoveControlledAgentFromInventory(controlled);
                    controlled.Destroy();
                }

            ListPool<Agent>.Instance.Return(destroyList);
        }

        private void RemoveControlledAgentFromInventory(Agent controlled)
        {
            if (controlled.IsOwnedBy(Id) == false) return;
            if (controlled.IsDestroyed || controlled.TestStatus(EntityStatus.PendingDestroy)) return;

            controlled.Properties.RemoveProperty(PropertyEnum.AIMasterAvatarDbGuid);

            if (controlled.InventoryLocation.IsValid)
                controlled.ChangeInventoryLocation(null);
        }

        public Agent GetControlledAgent()
        {
            Agent controlledAgent = null;
            var controlledInventory = ControlledInventory;
            if (controlledInventory != null)
            {
                if (controlledInventory.Count > 1)
                    Logger.Warn($"Avatar has multiple controlled entities! Avatar: {ToString()}");

                var controlledId = controlledInventory.GetAnyEntity();
                if (controlledId != InvalidId)
                {
                    controlledAgent = Game.EntityManager.GetEntity<Agent>(controlledId);
                    if (controlledAgent == null)
                        Logger.Warn("Controlled agent is null!");
                }
            }
            return controlledAgent;
        }

        public bool SetControlledAgent(Agent controlled)
        {
            var controlledInventory = ControlledInventory;
            if (controlledInventory == null) return false;

            RemoveAndKillControlledAgent();

            if (controlledInventory.Count > 0) return false;

            // Trigger entity Death event for controlled
            var player = GetOwnerOfType<Player>();
            Region?.EntityDeadEvent.Invoke(new(controlled, this, player));

            controlled.Properties[PropertyEnum.AIMasterAvatarDbGuid] = DatabaseUniqueId;

            controlled.AwardKillLoot(this, KillFlags.None, this);
            controlled.SpawnSpec?.Defeat(this, true);

            controlled.DestroyEntityActionComponent();
            controlled.CancelDestroyEvent();

            var keywordSummonDuration = GameDatabase.KeywordGlobalsPrototype.ControlledSummonDurationKeyword;
            if (keywordSummonDuration == PrototypeId.Invalid) return false;

            bool hasSummonDuration = controlled.HasConditionWithKeyword(keywordSummonDuration);

            if (controlled.IsDead && hasSummonDuration == false)
                controlled.Properties[PropertyEnum.Health] = controlled.Properties[PropertyEnum.HealthMax];

            controlled.SetControlledProperties(this);
            controlled.Properties[PropertyEnum.PowerUserOverrideID] = Id;
            controlled.CombatLevel = CombatLevel;

            var rankRef = controlled.Properties[PropertyEnum.Rank];
            var rankProto = GameDatabase.GetPrototype<RankPrototype>(rankRef);
            if (rankProto.IsRankChampionOrEliteOrMiniBoss)
                controlled.Properties[PropertyEnum.MobRankOverride] = rankRef;

            var result = controlled.ChangeInventoryLocation(controlledInventory);
            if (result == InventoryResult.Success)
            {
                var invLocation = controlled.InventoryLocation;
                var message = NetMessageInventoryMove.CreateBuilder()
                            .SetEntityId(controlled.Id)
                            .SetInvLocContainerEntityId(invLocation.ContainerId)
                            .SetInvLocInventoryPrototypeId((ulong)invLocation.InventoryRef)
                            .SetInvLocSlot(invLocation.Slot)
                            .SetRequiredNoOwnerOnClient(false)
                            .Build();
                Game.NetworkManager.SendMessageToInterested(message, this, AOINetworkPolicyValues.AOIChannelOwner);
            }
            else
            {
                controlled.Kill();
            }

            if (hasSummonDuration)
            {
                controlled.Properties[PropertyEnum.ControlledAgentHasSummonDur] = true;
                var scheduler = Game.GameEventScheduler;
                if (scheduler == null) return false;
                scheduler.CancelEvent(_despawnControlledEvent);
                ScheduleEntityEvent(_despawnControlledEvent, TimeSpan.Zero);
            }

            return result == InventoryResult.Success;
        }

        public int RemoveSummonedAgentsWithKeywords(float count, KeywordsMask keywordsMask)
        {
            int removed = 0;

            List<WorldEntity> summons = ListPool<WorldEntity>.Instance.Get();

            foreach (var summoned in new SummonedEntityIterator(this))
            {
                if (summoned.IsDead) continue;
                if (summoned.IsDestroyed || summoned.TestStatus(EntityStatus.PendingDestroy)) continue;

                if (summoned.KeywordsMask.TestAll(keywordsMask))
                {
                    summons.Add(summoned);
                    removed++;
                }

                if (removed != 0 && removed == count) break;
            }

            var killFlags = KillFlags.NoExp | KillFlags.NoLoot | KillFlags.NoDeadEvent;
            foreach (var summoned in summons)
                summoned.Kill(null, killFlags);

            ListPool<WorldEntity>.Instance.Return(summons);

            return removed;
        }

        #endregion

        #region Synergies

        public bool UpdateAvatarSynergyCondition()
        {
            PrototypeId avatarSynergyConditionRef = GameDatabase.GlobalsPrototype.AvatarSynergyCondition;
            if (avatarSynergyConditionRef == PrototypeId.Invalid)
                return true;

            ConditionPrototype avatarSynergyConditionProto = avatarSynergyConditionRef.As<ConditionPrototype>();
            if (avatarSynergyConditionProto == null) return Logger.WarnReturn(false, "UpdateAvatarSynergyCondition(): avatarSynergyConditionProto == null");

            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "UpdateAvatarSynergyCondition(): player == null");

            using PropertyCollection avatarSynergyProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();

            // Ignoring avatar mode here
            foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.AvatarLibraryLevel, (int)AvatarMode.Normal))
            {
                int level = kvp.Value;

                Property.FromParam(kvp.Key, 1, out PrototypeId avatarProtoRef);
                if (Properties[PropertyEnum.AvatarSynergySelected, avatarProtoRef] == false)
                    continue;

                AvatarPrototype avatarProto = avatarProtoRef.As<AvatarPrototype>();
                if (avatarProto == null)
                {
                    Logger.Warn("UpdateAvatarSynergyCondition(): avatarProto == null");
                    continue;
                }

                bool canUseSynergy = false;
                foreach (AvatarSynergyEntryPrototype synergyProto in avatarProto.SynergyTable)
                {
                    if (synergyProto is not AvatarSynergyEvalEntryPrototype evalSynergyProto)
                    {
                        Logger.Warn("UpdateAvatarSynergyCondition(): synergyProto is not AvatarSynergyEvalEntryPrototype evalSynergyProto");
                        continue;
                    }

                    if (level < evalSynergyProto.Level)
                        continue;

                    canUseSynergy |= true;

                    if (evalSynergyProto.SynergyEval == null)
                        continue;

                    using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                    evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, avatarSynergyProperties);
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);

                    Eval.RunBool(evalSynergyProto.SynergyEval, evalContext);
                }

                if (canUseSynergy == false)
                    Properties.RemoveProperty(new(PropertyEnum.AvatarSynergySelected, avatarProtoRef));
            }

            // See if there is a synergy condition we don't know about
            if (_avatarSynergyConditionId == ConditionCollection.InvalidConditionId)
                _avatarSynergyConditionId = ConditionCollection.GetConditionIdByRef(avatarSynergyConditionRef);

            // Remove the existing synergy condition
            if (_avatarSynergyConditionId != ConditionCollection.InvalidConditionId)
            {
                ConditionCollection.RemoveCondition(_avatarSynergyConditionId);
                _avatarSynergyConditionId = ConditionCollection.InvalidConditionId;
            }

            // Add a new synergy condition
            Condition avatarSynergyCondition = ConditionCollection.AllocateCondition();
            if (avatarSynergyCondition.InitializeFromConditionPrototype(ConditionCollection.NextConditionId, Game,
                Id, Id, Id, avatarSynergyConditionProto, TimeSpan.Zero, avatarSynergyProperties))
            {
                ConditionCollection.AddCondition(avatarSynergyCondition);
                _avatarSynergyConditionId = avatarSynergyCondition.Id;
            }
            else
            {
                ConditionCollection.DeleteCondition(avatarSynergyCondition);
            }

            return true;
        }

        public bool UpdateAvatarSynergyExperienceBonus()
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "UpdateAvatarSynergyExperienceBonus(): player == null");

            if (player.GameplayOptions.GetOptionSetting(Options.GameplayOptionSetting.DisableHeroSynergyBonusXP) == 1)
            {
                Properties.RemoveProperty(PropertyEnum.ExperienceBonusAvatarSynergy);
                return true;
            }

            // Get requirements from advancement globals
            AdvancementGlobalsPrototype advancementGlobals = GameDatabase.AdvancementGlobalsPrototype;
            Curve normalBonusCurve = advancementGlobals.ExperienceBonusAvatarSynergy.AsCurve();
            Curve cappedBonusMaxCurve = advancementGlobals.ExperienceBonusLevel60Synergy.AsCurve();
            int originalMaxLevel = advancementGlobals.OriginalMaxLevel;

            float experienceBonus = 0f;
            int numLevelCappedAvatars = 0;

            // Ignoring avatar mode here
            foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.AvatarLibraryLevel, (int)AvatarMode.Normal))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId avatarProtoRef);
                if (avatarProtoRef == PrototypeId.Invalid)
                {
                    Logger.Warn("UpdateAvatarSynergyExperienceBonus(): avatarProtoRef == PrototypeId.Invalid");
                    continue;
                }

                // Level cap bonus is applied below based on the total number of capped avatars
                int level = player.GetMaxCharacterLevelAttainedForAvatar(avatarProtoRef);
                if (level < originalMaxLevel)
                    experienceBonus += normalBonusCurve.GetAt(level);
                else
                    numLevelCappedAvatars++;
            }

            experienceBonus += cappedBonusMaxCurve.GetAt(numLevelCappedAvatars);
            experienceBonus = Math.Min(experienceBonus, advancementGlobals.ExperienceBonusAvatarSynergyMax);

            Properties[PropertyEnum.ExperienceBonusAvatarSynergy] = experienceBonus;

            return true;
        }

        private bool UpdateAvatarSynergyUnlocks(int oldLevel, int newLevel)
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "UpdateAvatarSynergyUnlocks(): player == null");

            AvatarPrototype avatarProto = AvatarPrototype;
            if (avatarProto == null) return Logger.WarnReturn(false, "UpdateAvatarSynergyUnlocks(): avatarProto == null");

            // No synergies to unlock
            if (avatarProto.SynergyTable.IsNullOrEmpty())
                return true;

            foreach (AvatarSynergyEntryPrototype synergyProto in avatarProto.SynergyTable)
            {
                if (oldLevel < synergyProto.Level && newLevel >= synergyProto.Level)
                {
                    player.Properties[PropertyEnum.AvatarSynergyNewUnlock, PrototypeDataRef] = true;
                    break;
                }
            }

            return true;
        }

        #endregion

        #region Prestige

        public bool ResetMissions()
        {
            Logger.Trace($"ResetMissions(): [{this}]");

            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "ResetMissions(): player == null");

            MissionManager missionManager = player.MissionManager;
            if (missionManager == null) return Logger.WarnReturn(false, "ResetMissions(): missionManager == null");

            RegionConnectionTargetPrototype targetProto = GameDatabase.GlobalsPrototype.DefaultStartTargetPrestigeRegion.As<RegionConnectionTargetPrototype>();
            if (targetProto == null) return Logger.WarnReturn(false, "ResetMissions(): targetProto == null");

            PrototypeId chapterProtoRef = GameDatabase.MissionGlobalsPrototype.InitialChapter;
            
            if (IsInWorld)
            {
                player.QueueLoadingScreen(targetProto.Region);
                ExitWorld();
            }

            Properties.RemoveProperty(PropertyEnum.LastTownRegion);

            player.RemoveBodysliderProperties();

            if (missionManager.ResetAvatarMissionsForStoryWarp(chapterProtoRef, true) == false)
                Logger.Warn($"ResetMissions(): Failed to reset missions for avatar [{this}]");

            player.ResetMapDiscoveryForStoryWarp();

            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.DifficultyTierRef = GameDatabase.GlobalsPrototype.DifficultyTierDefault;
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_StoryWarp);
            return teleporter.TeleportToTarget(targetProto.DataRef);
        }

        public bool ActivatePrestigeMode()
        {
            Logger.Trace($"ActivatePrestigeMode(): [{this}]");

            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "ActivatePrestigeMode(): player == null");

            RegionConnectionTargetPrototype targetProto = GameDatabase.GlobalsPrototype.DefaultStartTargetPrestigeRegion.As<RegionConnectionTargetPrototype>();
            if (targetProto == null) return Logger.WarnReturn(false, "ActivatePrestigeMode(): targetProto == null");

            if (CanActivatePrestigeMode() == false) return Logger.WarnReturn(false, "ActivatePrestigeMode(): CanActivatePrestigeMode() == false");

            player.QueueLoadingScreen(targetProto.Region);

            // Clear transform mode
            PrototypeId currentTransformMode = CurrentTransformMode;
            if (currentTransformMode != PrototypeId.Invalid)
                OnTransformModeChange(PrototypeId.Invalid, currentTransformMode, false);

            // Exit
            ExitWorld();

            // Respec
            UnassignAllMappedPowers();

            int unlockedSpec = GetPowerSpecIndexUnlocked();
            for (int i = 0; i <= unlockedSpec; i++)
                RespecPowerSpec(i, PowersRespecReason.Prestige, true);

            // Adjust properties
            Properties.AdjustProperty(1, PropertyEnum.AvatarPrestigeLevel);
            Properties[PropertyEnum.NumberOfDeaths] = 0;
            Properties.RemoveProperty(PropertyEnum.DifficultyTierPreference);

            // Get rid of controlled agents
            RemoveAndKillControlledAgent();

            // Reset level (this also removes equipment)
            InitializeLevel(1);
            ResetResources(false);

            // Loot!
            int prestigeLevel = PrestigeLevel;
            AwardPrestigeLoot(prestigeLevel);

            ResetMissions();

            player.ScheduleCommunityBroadcast();

            // Invoke achievement events
            PrestigeLevelPrototype prestigeLevelProto = GameDatabase.AdvancementGlobalsPrototype.GetPrestigeLevelPrototype(prestigeLevel);
            if (prestigeLevelProto == null) return Logger.WarnReturn(false, "ActivatePrestigeMode(): prestigeLevelProto == null");

            player.OnScoringEvent(new(ScoringEventType.AvatarPrestigeLevel, prestigeLevel));

            int avatarsAtPrestigeLevelCount = ScoringEvents.GetPlayerAvatarsAtPrestigeLevel(player, prestigeLevel);
            player.OnScoringEvent(new(ScoringEventType.AvatarsAtPrestigeLevel, prestigeLevelProto, avatarsAtPrestigeLevelCount));

            return true;
        }

        public bool IsAtMaxPrestigeLevel()
        {
            AdvancementGlobalsPrototype advancementProto = GameDatabase.AdvancementGlobalsPrototype;
            if (advancementProto == null) return false;
            return PrestigeLevel >= advancementProto.MaxPrestigeLevel;
        }

        public bool CanActivatePrestigeMode()
        {
            if (PartyId != InvalidId)
                return false;

            if (CharacterLevel < GetAvatarLevelCap())
                return false;

            if (IsAtMaxPrestigeLevel())
                return false;

            return IsInTown();
        }

        private bool CheckEquipmentRestrictions()
        {
            // This can be called during initialization before this avatar has a player
            Player player = GetOwnerOfType<Player>();
            if (player == null)
                return true;

            Inventory deliveryBox = player.GetInventory(InventoryConvenienceLabel.DeliveryBox);
            if (deliveryBox == null) return Logger.WarnReturn(false, "CheckEquipmentRestrictions(): deliveryBox == null");

            Inventory errorRecovery = player.GetInventory(InventoryConvenienceLabel.ErrorRecovery);
            if (errorRecovery == null) return Logger.WarnReturn(false, "CheckEquipmentRestrictions(): errorRecovery == null");

            EntityManager entityManager = Game.EntityManager;

            InventoryCollection inventoryCollection = player.InventoryCollection;

            foreach (Inventory inventory in new InventoryIterator(this, InventoryIterationFlags.Equipment))
            {
                Inventory.Enumerator enumerator = inventory.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Item item = entityManager.GetEntity<Item>(enumerator.Current.Id);
                    if (item == null)
                    {
                        Logger.Warn("CheckEquipmentRestrictions(): item == null");
                        continue;
                    }

                    if (inventory.PassesEquipmentRestrictions(item, out _) == InventoryResult.Success)
                        continue;

                    // Unequip items that don't pass the restrictions
                    InventoryResult result = InventoryResult.Invalid;

                    // General
                    inventoryCollection.GetInventoryForItem(item, InventoryCategory.PlayerGeneral, out Inventory general);
                    if (general != null)
                        result = item.ChangeInventoryLocation(general);

                    // Delivery Box
                    if (result != InventoryResult.Success)
                        result = item.ChangeInventoryLocation(deliveryBox);

                    // Error Recovery
                    if (result != InventoryResult.Success)
                        result = item.ChangeInventoryLocation(errorRecovery);

                    if (result != InventoryResult.Success)
                    {
                        Logger.Warn($"CheckEquipmentRestrictions(): Failed to remove equipped item [{item}] from avatar [{this}]");
                        continue;
                    }

                    // Restart iteration on successful removal
                    enumerator = inventory.GetEnumerator();
                }
            }

            return true;
        }

        private bool AwardPrestigeLoot(int prestigeLevel)
        {
            PrestigeLevelPrototype prestigeLevelProto = GameDatabase.AdvancementGlobalsPrototype.GetPrestigeLevelPrototype(prestigeLevel);
            if (prestigeLevelProto == null) return Logger.WarnReturn(false, "AwardPrestigeLoot(): prestigeLevelProto == null");

            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "AwardPrestigeLoot(): player == null");

            if (Game.CustomGameOptions.UsePrestigeLootTable)
            {
                // Award loot from the prestige loot table (same as BIF boxes by default), it appears this was never fully implemented
                PrototypeId prestigeLootTableProtoRef = prestigeLevelProto.Reward;
                if (prestigeLootTableProtoRef != PrototypeId.Invalid)
                {
                    using LootInputSettings settings = ObjectPoolManager.Instance.Get<LootInputSettings>();
                    settings.Initialize(LootContext.Initialization, player, null, 1);

                    Span<(PrototypeId, LootActionType)> tables = stackalloc (PrototypeId, LootActionType)[]
                    {
                        (prestigeLootTableProtoRef, LootActionType.Give)
                    };

                    Game.LootManager.AwardLootFromTables(tables, settings, 1);
                }
            }
            else
            {
                // Grant a copy of the starting costume, original behavior
                GiveStartingCostume();
            }

            return true;
        }

        #endregion

        #region Event Handlers

        public override void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            base.OnPropertyChange(id, newValue, oldValue, flags);
            if (flags.HasFlag(SetPropertyFlags.Refresh)) return;

            int manaTypeValue;
            ManaType manaType;

            switch (id.Enum)
            {
                case PropertyEnum.AvatarPowerUltimatePoints:
                    if (IsInWorld)
                    {
                        if (GetPowerProgressionInfo(UltimatePowerRef, out PowerProgressionInfo powerInfo) == false)
                        {
                            Logger.Warn($"OnPropertyChange(): Failed to get ultimate power progression info for [{this}]");
                            return;
                        }

                        UpdatePowerRank(ref powerInfo, false);
                    }
                    break;

                case PropertyEnum.AvatarMappedPower:
                    Property.FromParam(id, 0, out PrototypeId originalPowerRef);
                    PowerPrototype originalPowerProto = originalPowerRef.As<PowerPrototype>();
                    if (originalPowerProto == null)
                    {
                        Logger.Warn("OnPropertyChange(): originalPowerProto == null");
                        return;
                    }

                    if (originalPowerProto.IsTravelPower)
                    {
                        PrototypeId mappedPowerRef = GetMappedPowerFromOriginalPower(originalPowerRef);
                        SetTravelPowerOverride(mappedPowerRef);

                        _currentAbilityKeyMapping?.SetAbilityInAbilitySlot(GetTravelPowerRef(), AbilitySlot.TravelPower);
                    }

                    break;

                case PropertyEnum.OmegaRank:
                case PropertyEnum.InfinityGemBonusRank:
                case PropertyEnum.PvPUpgrades:
                    if (IsSimulated)
                    {
                        Property.FromParam(id, 0, out PrototypeId modProtoRef);
                        if (modProtoRef == PrototypeId.Invalid)
                        {
                            Logger.Warn("OnPropertyChange(): modProtoRef == PrototypeId.Invalid");
                            return;
                        }

                        ModChangeModEffects(modProtoRef, newValue);
                    }
                    break;

                case PropertyEnum.Knockback:
                case PropertyEnum.Knockdown:
                case PropertyEnum.Knockup:
                case PropertyEnum.Mesmerized:
                case PropertyEnum.Stunned:
                case PropertyEnum.StunnedByHitReact:
                    if (newValue == true)
                    {
                        // Clear pending actions / continuous powers on loss of control
                        if (IsInPendingActionState(PendingActionState.WaitingForPrevPower) || IsInPendingActionState(PendingActionState.AfterPowerMove))
                            CancelPendingAction();

                        SetContinuousPower(PrototypeId.Invalid, _continuousPowerData.TargetId, Vector3.Zero, 0, true);
                    }
                        
                    break;

                case PropertyEnum.AllianceOverride:
                case PropertyEnum.Confused:

                    var alliance = Alliance;
                    ControlledAgent?.SetSummonedAllianceOverride(alliance);
                    CurrentTeamUpAgent?.SetSummonedAllianceOverride(alliance);
                    break;

                case PropertyEnum.PetHealthPctBonus:
                case PropertyEnum.PetDamagePctBonus:

                    var controlledAgent = ControlledAgent;
                    if (controlledAgent != null)
                    {
                        controlledAgent.Properties[PropertyEnum.PetHealthPctBonus] = Properties[PropertyEnum.PetHealthPctBonus];
                        controlledAgent.Properties[PropertyEnum.PetDamagePctBonus] = Properties[PropertyEnum.PetDamagePctBonus];
                    }
                    break;

                case PropertyEnum.EnduranceAddBonus:
                case PropertyEnum.EnduranceBase:
                case PropertyEnum.EndurancePctBonus:
                    Property.FromParam(id, 0, out manaTypeValue);
                    manaType = (ManaType)manaTypeValue;

                    if (manaType == ManaType.TypeAll)
                    {
                        // Update max for all mana types
                        foreach (PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto in GetPrimaryResourceManaBehaviors())
                        {
                            ManaType protoManaType = primaryManaBehaviorProto.ManaType;
                            Properties[PropertyEnum.EnduranceMax, protoManaType] = GetEnduranceMax(protoManaType);
                        }
                    }
                    else
                    {
                        // Update max just for the mana type that was affected
                        Properties[PropertyEnum.EnduranceMax, manaType] = GetEnduranceMax(manaType);
                    }

                    break;

                case PropertyEnum.EnduranceMax:
                    Property.FromParam(id, 0, out manaTypeValue);
                    manaType = (ManaType)manaTypeValue;

                    // Rescale current endurance
                    if (IsAliveInWorld && flags.HasFlag(SetPropertyFlags.Deserialized) == false)
                    {
                        float endurance = Properties[PropertyEnum.Endurance, manaType];

                        // 0 max endurance is treated as having full endurance (this will be reset to 0 later if needed)
                        float ratio = oldValue > 0f ? Math.Min(endurance / oldValue, 1f) : 1f;
                        Properties[PropertyEnum.Endurance, manaType] = newValue * ratio;
                    }

                    // Update client value
                    Properties[PropertyEnum.EnduranceMaxOther, manaType] = newValue;
                    break;

                case PropertyEnum.SecondaryResource:
                    TryActivateOnSecondaryResourceValueChangeProcs(newValue);
                    break;

                case PropertyEnum.SecondaryResourcePips:
                    TryActivateOnSecondaryResourcePipsChangeProcs(newValue, oldValue);
                    break;

                case PropertyEnum.SecondaryResourceMax:
                    // Clamp current value to new max
                    float secondaryResourceMax = newValue;
                    if (secondaryResourceMax != 0f && secondaryResourceMax < Properties[PropertyEnum.SecondaryResource])
                        Properties[PropertyEnum.SecondaryResource] = secondaryResourceMax;
                    break;

                case PropertyEnum.SecondaryResourceMaxBase:
                    Properties[PropertyEnum.SecondaryResourceMax] = (float)newValue + Properties[PropertyEnum.SecondaryResourceMaxChange];
                    break;

                case PropertyEnum.SecondaryResourceMaxChange:
                    Properties[PropertyEnum.SecondaryResourceMax] = Properties[PropertyEnum.SecondaryResourceMaxBase] + (float)newValue;
                    break;

                case PropertyEnum.SecondaryResourceMaxPipsBase:
                    Properties[PropertyEnum.SecondaryResourceMaxPips] = (int)newValue + Properties[PropertyEnum.SecondaryResourceMaxPipsChg];
                    break;

                case PropertyEnum.SecondaryResourceMaxPipsChg:
                    Properties[PropertyEnum.SecondaryResourceMaxPips] = Properties[PropertyEnum.SecondaryResourceMaxPipsBase] + (int)newValue;
                    break;

                case PropertyEnum.SecondaryResourceOverride:
                    if (oldValue != PrototypeId.Invalid)
                    {
                        // Clear old override behavior
                        SecondaryResourceManaBehaviorPrototype manaBehaviorOverrideProto = GameDatabase.GetPrototype<SecondaryResourceManaBehaviorPrototype>(oldValue);
                        if (manaBehaviorOverrideProto == null)
                        {
                            Logger.Warn("OnPropertyChange(): manaBehaviorOverrideProto == null");
                            break;
                        }

                        UnassignManaBehaviorPowers(manaBehaviorOverrideProto);
                    }
                    else
                    {
                        // Clear default behavior (if any)
                        SecondaryResourceManaBehaviorPrototype defaultManaBehaviorProto = AvatarPrototype?.SecondaryResourceBehaviorCache;
                        if (defaultManaBehaviorProto != null)
                            UnassignManaBehaviorPowers(defaultManaBehaviorProto);
                    }

                    // Initialize the new override
                    InitializeSecondaryManaBehaviors();
                    break;

                case PropertyEnum.StatAllModifier:
                case PropertyEnum.StatDurability:
                case PropertyEnum.StatDurabilityDmgPctPerPoint:
                case PropertyEnum.StatDurabilityModifier:
                case PropertyEnum.StatStrength:
                case PropertyEnum.StatStrengthDmgPctPerPoint:
                case PropertyEnum.StatStrengthModifier:
                case PropertyEnum.StatFightingSkills:
                case PropertyEnum.StatFightingSkillsDmgPctPerPoint:
                case PropertyEnum.StatFightingSkillsModifier:
                case PropertyEnum.StatSpeed:
                case PropertyEnum.StatSpeedDmgPctPerPoint:
                case PropertyEnum.StatSpeedModifier:
                case PropertyEnum.StatEnergyProjection:
                case PropertyEnum.StatEnergyDmgPctPerPoint:
                case PropertyEnum.StatEnergyProjectionModifier:
                case PropertyEnum.StatIntelligence:
                case PropertyEnum.StatIntelligenceDmgPctPerPoint:
                case PropertyEnum.StatIntelligenceModifier:
                    if (IsInWorld)
                        ScheduleStatsPowerRefresh();
                    break;

                case PropertyEnum.PowerChargesMax:
                {
                    Property.FromParam(id, 0, out PrototypeId powerProtoRef);
                    if (powerProtoRef == PrototypeId.Invalid)
                    {
                        Logger.Warn("OnPropertyChange(): powerProtoRef == PrototypeId.Invalid");
                        break;
                    }

                    int chargesAvailable = Properties[PropertyEnum.PowerChargesAvailable, powerProtoRef];
                    if (newValue.RawLong < oldValue.RawLong)
                    {
                        // Remove extra charges if the max number went down
                        if (newValue >= chargesAvailable)
                            break;

                        if (newValue > 0 && TestStatus(EntityStatus.ExitingWorld) == false)
                            Properties[PropertyEnum.PowerChargesAvailable, powerProtoRef] = newValue;

                        // Cancel generation of the next charge by resetting the cooldown
                        foreach (PropertyEnum cooldownProperty in Property.CooldownProperties)
                            Properties.RemoveProperty(new(cooldownProperty, powerProtoRef));
                    }
                    else if (chargesAvailable < newValue)
                    {
                        // Start generating charges if we can now have more of them
                        Power power = GetPower(powerProtoRef);
                        if (power != null && power.IsOnCooldown() == false)
                            power.StartCooldown();
                    }

                    break;
                }

                case PropertyEnum.PowerChargesMaxBonus:
                {
                    Property.FromParam(id, 0, out PrototypeId powerProtoRef);

                    int chargesMaxOld = Properties[PropertyEnum.PowerChargesMax, powerProtoRef];
                    int chargesMaxNew = chargesMaxOld - oldValue + newValue;

                    Power power = GetPower(powerProtoRef);

                    // This indicates whether this power has charges on its own or all extra charges are coming from bonuses
                    bool hasBaselineCharges = power != null && power.Properties.HasProperty(PropertyEnum.PowerChargesMax);

                    if (chargesMaxOld == 0)
                    {
                        // Initialize charges for powers that don't have baseline charges
                        PropertyId chargesAvailableProp = new(PropertyEnum.PowerChargesAvailable, powerProtoRef);
                        if (power != null && power.IsOnCooldown() == false && Properties.HasProperty(chargesAvailableProp) == false)
                            Properties[chargesAvailableProp] = 1;

                        if (chargesMaxNew == 1 && hasBaselineCharges == false)
                            chargesMaxNew++;
                    }
                    else if (chargesMaxNew == 1 && hasBaselineCharges == false)
                    {
                        // Revert to normal behavior if this power doesn't have baseline charges
                        chargesMaxNew = 0;
                    }

                    Properties[PropertyEnum.PowerChargesMax, powerProtoRef] = chargesMaxNew;
                    break;
                }

                case PropertyEnum.PowerChargesMaxBonusForKwd:
                {
                    // These will be applied when the power is assigned if currently not in the world
                    if (IsAliveInWorld == false)
                        break;

                    Property.FromParam(id, 0, out PrototypeId keywordProtoRef);

                    // Apply bonus to power progression powers
                    List<PowerProgressionInfo> powerInfoList = ListPool<PowerProgressionInfo>.Instance.Get();
                    GetPowerProgressionInfos(powerInfoList);

                    foreach (PowerProgressionInfo powerInfo in powerInfoList)
                    {
                        PowerPrototype powerProto = powerInfo.PowerPrototype;
                        if (powerProto == null)
                        {
                            Logger.Warn("OnPropertyChange(): powerProto == null");
                            continue;
                        }

                        if (HasPowerWithKeyword(powerProto, keywordProtoRef) == false)
                            continue;

                        Properties[PropertyEnum.PowerChargesMaxBonus, powerProto.DataRef] = newValue;
                    }

                    ListPool<PowerProgressionInfo>.Instance.Return(powerInfoList);

                    // Apply bonus to mapped powers
                    Dictionary<PropertyId, PropertyValue> mappedPowerDict = DictionaryPool<PropertyId, PropertyValue>.Instance.Get();

                    foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.AvatarMappedPower))
                        mappedPowerDict.Add(kvp.Key, kvp.Value);

                    foreach (var kvp in mappedPowerDict)
                    {
                        PowerPrototype mappedPowerProto = GameDatabase.GetPrototype<PowerPrototype>(kvp.Value);
                        if (mappedPowerProto == null)
                        {
                            Logger.Warn("OnPropertyChange(): mappedPowerProto == null");
                            continue;
                        }

                        if (HasPowerWithKeyword(mappedPowerProto, keywordProtoRef) == false)
                            continue;

                        Properties[PropertyEnum.PowerChargesMaxBonus, mappedPowerProto.DataRef] = newValue;
                    }

                    DictionaryPool<PropertyId, PropertyValue>.Instance.Return(mappedPowerDict);

                    break;
                }

                case PropertyEnum.PowerCooldownDuration:
                    if (IsInWorld)
                    {
                        Property.FromParam(id, 0, out PrototypeId powerProtoRef);
                        PowerPrototype powerProto = powerProtoRef.As<PowerPrototype>();
                        if (powerProto == null)
                        {
                            Logger.Warn("OnPropertyChange(): powerProto == null");
                            break;
                        }

                        if (Power.IsCooldownPersistent(powerProto))
                            Properties[PropertyEnum.PowerCooldownDurationPersistent, powerProtoRef] = newValue;
                    }

                    break;

                case PropertyEnum.PowerCooldownStartTime:
                    if (IsInWorld)
                    {
                        Property.FromParam(id, 0, out PrototypeId powerProtoRef);
                        PowerPrototype powerProto = powerProtoRef.As<PowerPrototype>();
                        if (powerProto == null)
                        {
                            Logger.Warn("OnPropertyChange(): powerProto == null");
                            break;
                        }

                        if (Power.IsCooldownPersistent(powerProto))
                            Properties[PropertyEnum.PowerCooldownStartTimePersistent, powerProtoRef] = newValue;
                    }

                    break;

                case PropertyEnum.DifficultyTierPreference:
                    {
                        Player player = GetOwnerOfType<Player>();
                        if (player != null)
                        {
                            player.SendDifficultyTierPreferenceToPlayerManager();
                            player.UpdatePartyDifficulty(newValue);
                        }
                    }
                    break;
            }
        }

        public override void OnAreaChanged(RegionLocation oldLocation, RegionLocation newLocation)
        {
            base.OnAreaChanged(oldLocation, newLocation);

            var oldArea = oldLocation.Area;
            var newArea = newLocation.Area;
            if (oldArea == newArea) return;

            var player = GetOwnerOfType<Player>();
            if (player == null) return;

            if (oldArea != null)
            {
                PlayerLeftAreaGameEvent evt = new(player, oldArea.PrototypeDataRef);
                oldArea.PopulationArea?.OnPlayerLeft();
                oldArea.PlayerLeftAreaEvent.Invoke(evt);
                oldArea.Region.PlayerLeftAreaEvent.Invoke(evt);
            }

            if (newArea != null)
            {
                PlayerEnteredAreaGameEvent evt = new(player, newArea.PrototypeDataRef);
                newArea.PopulationArea?.OnPlayerEntered();
                newArea.PlayerEnteredAreaEvent.Invoke(evt);
                newArea.Region.PlayerEnteredAreaEvent.Invoke(evt);
                player.OnScoringEvent(new(ScoringEventType.AreaEnter, newArea.Prototype));
            }
        }

        public override void OnCellChanged(RegionLocation oldLocation, RegionLocation newLocation, ChangePositionFlags flags)
        {
            base.OnCellChanged(oldLocation, newLocation, flags);

            Cell oldCell = oldLocation.Cell;
            Cell newCell = newLocation.Cell;
            if (oldCell == newCell) return;

            var player = GetOwnerOfType<Player>();
            if (player == null) return;

            if (oldCell != null)
            {
                PlayerLeftCellGameEvent evt = new(player, oldCell.PrototypeDataRef);
                oldCell.PlayerLeftCellEvent.Invoke(evt);
                oldCell.Region.PlayerLeftCellEvent.Invoke(evt);
            }

            if (newCell != null)
            {
                PlayerEnteredCellGameEvent evt = new(player, newCell.PrototypeDataRef);
                newCell.PlayerEnteredCellEvent.Invoke(evt);
                newCell.Region.PlayerEnteredCellEvent.Invoke(evt);
            }
        }

        public override void OnEnteredWorld(EntitySettings settings)
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null)
            {
                Logger.Warn("OnEnteredWorld(): player == null");
                return;
            }

            Region region = Region;
            if (region == null)
            {
                Logger.Warn("OnEnteredWorld(): region == null");
                return;
            }

            player.UpdateScoringEventContext();

            var teamUpAgent = CurrentTeamUpAgent;
            if (teamUpAgent != null)
            {
                if (teamUpAgent.IsLiveTuningEnabled)
                {
                    SetOwnerTeamUpAgent(teamUpAgent);
                    teamUpAgent.ApplyTeamUpAffixesToAvatar(this);
                }
                else
                    Properties.RemoveProperty(PropertyEnum.AvatarTeamUpAgent);
            }

            InitAbilityKeyMappings();

            base.OnEnteredWorld(settings);

            Properties[PropertyEnum.AvatarTimePlayedStart] = Game.CurrentTime;

            // Enable primary resource regen (this will be disabled by mana behavior initialization if needed)
            foreach (PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto in GetPrimaryResourceManaBehaviors())
                EnableEnduranceRegen(primaryManaBehaviorProto.ManaType);

            // Assign powers
            InitializePowers();

            if (Game.InfinitySystemEnabled)
                InitializeInfinityBonuses();
            else
                InitializeOmegaBonuses();

            OnEnteredWorldSetTransformMode();

            // Last active time is checked in onEnteredWorldSetTransformMode() and ObjectiveTracker::doTrackerUpdate()
            Properties[PropertyEnum.AvatarLastActiveTime] = Game.CurrentTime;

            UpdateAvatarSynergyCondition();
            UpdateAvatarSynergyExperienceBonus();
            CurrentTeamUpAgent?.SetTeamUpsAtMaxLevel(player);   // Needed to calculate team-up synergies

            ApplyLiveTuneServerConditions();

            RestoreSelfAppliedPowerConditions();     // This needs to happen after we assign powers
            UpdateBoostConditionPauseState(region.PausesBoostConditions());

            // Unlock chapters and waypoints that should be unlocked by default
            player.UnlockChapters();
            player.UnlockWaypoints();

            RegionPrototype regionProto = region.Prototype;
            if (regionProto != null)
            {
                var waypointRef = regionProto.WaypointAutoUnlock;
                if (waypointRef != PrototypeId.Invalid)
                    player.UnlockWaypoint(waypointRef);
                if (regionProto.WaypointAutoUnlockList.HasValue())
                    foreach(var waypointUnlockRef in regionProto.WaypointAutoUnlockList)
                        player.UnlockWaypoint(waypointUnlockRef);
            }

            UpdateTalentPowers();

            var missionManager = player.MissionManager;
            if (missionManager != null)
            {
                // Restore missions from Avatar
                missionManager.RestoreAvatarMissions(this);
                // Update interest
                missionManager.UpdateMissionInterest();
            }

            // summoner condition
            foreach (var summon in new SummonedEntityIterator(this))
                summon.AddSummonerCondition(Id);

            // Finish the switch (if there was one)
            player.Properties.RemovePropertyRange(PropertyEnum.AvatarSwitchPending);

            // update achievement score
            player.AchievementManager.UpdateScore();

            // Update AOI of the owner player
            AreaOfInterest aoi = player.AOI;
            aoi.Update(RegionLocation.Position, true);

            // Update party
            Party party = Party;
            if (party != null)
            {
                AssignPartyBonusPower();
                SetPartySize(party.NumMembers);
                SyncPartyBoostConditions();
            }

            // Assign region passive powers (e.g. min health tutorial power)
            AssignRegionPowers();

            // Spawn team-up / controlled entities
            RespawnPersistentAgents();

            if (regionProto != null)
            {
                if (regionProto.Chapter != PrototypeId.Invalid)
                    player.SetActiveChapter(regionProto.Chapter);

                if (regionProto.IsNPE == false)
                    player.UnlockNewPlayerUISystems();
            }

            ScheduleEntityEvent(_avatarEnteredRegionEvent, TimeSpan.Zero);

            player.TryDoVendorXPCapRollover();
        }

        private void ApplyLiveTuneServerConditions()
        {
            foreach (var conditionRef in GameDatabase.GlobalsPrototype.LiveTuneServerConditions)
            {
                var conditionProto = GameDatabase.GetPrototype<ConditionPrototype>(conditionRef);

                if (LiveTuningManager.GetLiveConditionTuningVar(conditionProto, ConditionTuningVar.eCTV_Enabled) != 1.0f)
                {
                    if (ConditionCollection.GetConditionByRef(conditionRef) != null) continue;
                    var condition = ConditionCollection.AllocateCondition();
                    condition.InitializeFromConditionPrototype(ConditionCollection.NextConditionId, Game, Id, Id, Id, conditionProto, TimeSpan.Zero);
                    ConditionCollection.AddCondition(condition);
                }
                else
                {
                    ConditionCollection.RemoveConditionsWithConditionPrototypeRef(conditionRef);
                }
            }
        }

        private void RemoveLiveTuneServerConditions()
        {
            foreach (var conditionRef in GameDatabase.GlobalsPrototype.LiveTuneServerConditions)
                ConditionCollection.RemoveConditionsWithConditionPrototypeRef(conditionRef);
        }

        public override void OnExitedWorld()
        {
            base.OnExitedWorld();

            // Clear dialog target
            Player player = GetOwnerOfType<Player>();
            player?.SetDialogTargetId(InvalidId, InvalidId);

            // despawn teamups / controlled entities
            DespawnPersistentAgents();

            if (PartyId != 0)
            {
                UnassignPartyBonusPower();
                SetPartySize(1);
            }

            CancelEnduranceEvents();

            // Pause boosts while not in the world
            UpdateBoostConditionPauseState(true);

            // Remove the avatar synergy condition
            if (_avatarSynergyConditionId != ConditionCollection.InvalidConditionId)
            {
                ConditionCollection.RemoveCondition(_avatarSynergyConditionId);
                _avatarSynergyConditionId = ConditionCollection.InvalidConditionId;
            }

            RemoveLiveTuneServerConditions();

            UpdateTimePlayed(player);

            Properties.RemoveProperty(PropertyEnum.NumMissionAllies);
            Properties.RemovePropertyRange(PropertyEnum.PowersRespecResult);

            // Store missions to Avatar
            player?.MissionManager?.StoreAvatarMissions(this);

            // Cancel events
            EventScheduler scheduler = Game.GameEventScheduler;
            scheduler.CancelEvent(_refreshStatsPowerEvent);
            scheduler.CancelEvent(_transformModeExitPowerEvent);
            scheduler.CancelEvent(_transformModeChangeEvent);
            scheduler.CancelEvent(_deathDialogEvent);

            // Remove summoner conditions
            foreach (var summon in new SummonedEntityIterator(this))
                summon.RemoveSummonerCondition(Id);
        }

        public override void OnLocomotionStateChanged(LocomotionState oldState, LocomotionState newState)
        {
            base.OnLocomotionStateChanged(oldState, newState);
        }

        #endregion

        #region Time

        public TimeSpan GetTimePlayed()
        {
            TimeSpan savedTimePlayed = Properties[PropertyEnum.AvatarTotalTimePlayed];

            TimeSpan currentTimePlayed = TimeSpan.Zero;
            TimeSpan startTime = Properties[PropertyEnum.AvatarTimePlayedStart];
            if (startTime != TimeSpan.Zero)
                currentTimePlayed = Game.CurrentTime - startTime;

            return savedTimePlayed + currentTimePlayed;
        }

        private void UpdateTimePlayed(Player player)
        {
            player?.UpdateTimePlayed();

            Properties[PropertyEnum.AvatarTotalTimePlayed] = GetTimePlayed();
            Properties[PropertyEnum.AvatarTimePlayedStart] = TimeSpan.Zero;

            // AvatarLastActiveCalendarTime is used by the client to choose the voice line to play when the client logs in
            Properties[PropertyEnum.AvatarLastActiveTime] = Game.CurrentTime;
            Properties[PropertyEnum.AvatarLastActiveCalendarTime] = (long)Clock.UnixTime.TotalMilliseconds;
        }

        #endregion

        #region Party

        // PartyBoost is a power assigned to players in party. This is used in 1.10 and maybe other versions too.

        public void AssignPartyBonusPower()
        {
            if (IsInWorld == false)
                return;

            PrototypeId partyBonusPower = AvatarPrototype.PartyBonusPower;
            if (partyBonusPower == PrototypeId.Invalid)
                return;

            if (GetPower(partyBonusPower) != null)
                return;

            PowerIndexProperties indexProps = new(0, CharacterLevel, CombatLevel);
            AssignPower(partyBonusPower, indexProps);
        }

        public void UnassignPartyBonusPower()
        {
            if (IsInWorld == false)
                return;

            PrototypeId partyBonusPower = AvatarPrototype.PartyBonusPower;
            if (partyBonusPower == PrototypeId.Invalid)
                return;

            UnassignPower(partyBonusPower);
        }

        public void SetPartySize(int partySize)
        {
            Properties[PropertyEnum.PartySize] = partySize;

            // Potentially move this to OnPropertyChange?
            foreach (Condition condition in ConditionCollection)
            {
                if (condition.Properties.HasProperty(PropertyEnum.PartySize))
                    condition.Properties[PropertyEnum.PartySize] = partySize;
            }

            // This eval doesn't seem to be used in any data for version 1.52, but it may have been used in older versions.
            EvalPrototype evalOnPartySizeChange = AvatarPrototype.OnPartySizeChange;
            if (evalOnPartySizeChange != null)
                Logger.Debug("SetPartySize(): evalOnPartySizeChange != null");
        }

        // PartyBoostCondition is a condition that scales with the number of party members that have this condition (e.g. Avengers Assemble boosts).

        public void OnPartyBoostConditionAdded(Condition condition)
        {
            if (condition.IsPartyBoost() == false)
                return;

            Player player = GetOwnerOfType<Player>();
            if (player != null && player.IsSwitchingAvatar)
                return;

            if (player != null && player.PartyId != 0)
            {
                SyncPartyBoostConditions();
            }
            else
            {
                condition.Properties[PropertyEnum.PartyBoostCount] = 1;
                condition.RunEvalPartyBoost();
            }
        }

        public void OnPartyBoostConditionRemoved(Condition condition)
        {
            if (condition.IsPartyBoost() == false)
                return;

            Player player = GetOwnerOfType<Player>();
            if (player != null && player.IsSwitchingAvatar)
                return;

            if (player != null && player.PartyId != 0)
                SyncPartyBoostConditions();
        }

        public void ResetPartyBoostConditions()
        {
            foreach (Condition condition in ConditionCollection)
            {
                if (condition.IsPartyBoost() == false)
                    continue;

                if (condition.Properties[PropertyEnum.PartyBoostCount] <= 1)
                    continue;

                condition.Properties[PropertyEnum.PartyBoostCount] = 1;
                condition.RunEvalPartyBoost();
            }
        }

        public bool SyncPartyBoostConditions()
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "SyncPartyBoostConditions(): player == null");

            List<ulong> boosts = null;  // allocate on demand

            foreach (Condition condition in ConditionCollection)
            {
                if (condition.IsPartyBoost() == false)
                    continue;

                if (condition.ConditionPrototypeRef == PrototypeId.Invalid)
                {
                    Logger.Warn($"SyncPartyBoostConditions(): Non-standalone [{condition}] is flagged as a party boost, which is not supported");
                    continue;
                }

                boosts ??= new();
                PrototypeGuid conditionGuid = GameDatabase.GetPrototypeGuid(condition.ConditionPrototypeRef);
                boosts.Add((ulong)conditionGuid);
            }

            // Even if there are no party boosts currently, notify anyway to clear the conditions that may have previously been applied.
            ServiceMessage.PartyBoostUpdate message = new(player.DatabaseUniqueId, boosts);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);

            return true;
        }

        #endregion

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"{nameof(_playerName)}: {_playerName}");
            sb.AppendLine($"{nameof(_ownerPlayerDbId)}: 0x{OwnerPlayerDbId:X}");

            if (_guildId != GuildMember.InvalidGuildId)
            {
                sb.AppendLine($"{nameof(_guildId)}: {_guildId}");
                sb.AppendLine($"{nameof(_guildName)}: {_guildName}");
                sb.AppendLine($"{nameof(_guildMembership)}: {_guildMembership}");
            }

            for (int i = 0; i < _abilityKeyMappings.Count; i++)
                sb.AppendLine($"{nameof(_abilityKeyMappings)}[{i}]: {_abilityKeyMappings[i]}");
        }

        #region Scheduled Events

        private void AvatarEnteredRegion()
        {
            var player = GetOwnerOfType<Player>();
            if (player == null) return;

            var region = Region;
            region?.AvatarEnteredRegionEvent.Invoke(new(player, region.PrototypeDataRef));
            player.OnScoringEvent(new(ScoringEventType.RegionEnter));
        }

        public void ScheduleSwapInPower()
        {
            ScheduleEntityEventCustom(_activateSwapInPowerEvent, TimeSpan.FromMilliseconds(700));
            _activateSwapInPowerEvent.Get().Initialize(this);
        }

        private void ScheduleRecheckContinuousPower(TimeSpan delay)
        {
            if (_recheckContinuousPowerEvent.IsValid)
            {
                Game.GameEventScheduler.RescheduleEvent(_recheckContinuousPowerEvent, delay);
                return;
            }

            ScheduleEntityEvent(_recheckContinuousPowerEvent, delay);
        }

        private class AvatarEnteredRegionEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Avatar)t).AvatarEnteredRegion();
        }

        private class RefreshStatsPowersEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Avatar)t).RefreshStatsPower();
        }

        private class RecheckContinuousPowerEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Avatar)t).CheckContinuousPower();
        }

        private class DismissTeamUpAgentEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Avatar)t).DismissTeamUpAgent(true);
        }

        private class DespawnControlledEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Avatar)t).DespawnControlledAgent();
        }

        private class DelayedPowerActivationEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Avatar)t).DelayedPowerActivation();
        }

        private class ActivateSwapInPowerEvent : TargetedScheduledEvent<Entity>
        {
            public void Initialize(Avatar avatar)
            {
                _eventTarget = avatar;
            }

            public override bool OnTriggered()
            {
                Avatar avatar = (Avatar)_eventTarget;
                PrototypeId swapInPowerRef = GameDatabase.GlobalsPrototype.AvatarSwapInPower;

                PowerActivationSettings settings = new(avatar.Id, avatar.RegionLocation.Position, avatar.RegionLocation.Position);
                settings.Flags = PowerActivationSettingsFlags.NotifyOwner;

                return avatar.ActivatePower(swapInPowerRef, ref settings) == PowerUseResult.Success;
            }
        }

        private class EnableEnduranceRegenEvent : CallMethodEventParam1<Entity, ManaType>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => ((Avatar)t).EnableEnduranceRegen(p1);
        }

        private class UpdateEnduranceEvent : CallMethodEventParam1<Entity, ManaType>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => ((Avatar)t).UpdateEndurance(p1);
        }

        private class TransformModeChangeEvent : CallMethodEventParam2<Entity, PrototypeId, PrototypeId>
        {
            protected override CallbackDelegate GetCallback() => (t, p1, p2) => ((Avatar)t).DoTransformModeChangeCallback(p1, p2);
        }

        private class TransformModeExitPowerEvent : CallMethodEventParam1<Entity, PrototypeId>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => ((Avatar)t).DoTransformModeExitPowerCallback(p1);
        }

        private class UnassignMappedPowersForRespecEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Avatar)t).UnassignMappedPowersForRespec();
        }

        private class BodyslideTeleportToTownEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Avatar)t).DoBodyslideTeleportToTown();
        }

        private class BodyslideTeleportFromTownEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Avatar)t).DoBodyslideTeleportFromTown();
        }

        private class PowerTeleportEvent : CallMethodEventParam1<Entity, PrototypeId>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => ((Avatar)t).DoPowerTeleport(p1);
        }

        private class DeathDialogEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Avatar)t).DeathDialogCallback();
        }

        #endregion
    }
}
