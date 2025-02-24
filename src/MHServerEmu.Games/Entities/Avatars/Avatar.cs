using System.Text;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Random;
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
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Guilds;

namespace MHServerEmu.Games.Entities.Avatars
{
    public partial class Avatar : Agent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly TimeSpan StandardContinuousPowerRecheckDelay = TimeSpan.FromMilliseconds(150);

        private readonly EventPointer<ActivateSwapInPowerEvent> _activateSwapInPowerEvent = new();
        private readonly EventPointer<RecheckContinuousPowerEvent> _recheckContinuousPowerEvent = new();
        private readonly EventPointer<DelayedPowerActivationEvent> _delayedPowerActivationEvent = new();
        private readonly EventPointer<AvatarEnteredRegionEvent> _avatarEnteredRegionEvent = new();
        private readonly EventPointer<RefreshStatsPowersEvent> _refreshStatsPowerEvent = new();
        private readonly EventPointer<DismissTeamUpAgentEvent> _dismissTeamUpAgentEvent = new();
        private readonly EventPointer<DespawnControlledEvent> _despawnControlledEvent = new();

        private readonly EventPointer<EnableEnduranceRegenEvent>[] _enableEnduranceRegenEvents = new EventPointer<EnableEnduranceRegenEvent>[(int)ManaType.NumTypes];
        private readonly EventPointer<UpdateEnduranceEvent>[] _updateEnduranceEvents = new EventPointer<UpdateEnduranceEvent>[(int)ManaType.NumTypes];

        private RepString _playerName = new();
        private ulong _ownerPlayerDbId;
        private List<AbilityKeyMapping> _abilityKeyMappingList = new();

        private ulong _guildId = GuildMember.InvalidGuildId;
        private string _guildName = string.Empty;
        private GuildMembership _guildMembership = GuildMembership.eGMNone;
        private readonly PendingPowerData _continuousPowerData = new();
        private readonly PendingAction _pendingAction = new();

        public uint AvatarWorldInstanceId { get; } = 1;
        public string PlayerName { get => _playerName.Get(); }
        public ulong OwnerPlayerDbId { get => _ownerPlayerDbId; }
        public AbilityKeyMapping CurrentAbilityKeyMapping { get => _abilityKeyMappingList.FirstOrDefault(); }   // TODO: Save reference
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
        public Inventory ControlledInventory { get => GetInventory(InventoryConvenienceLabel.Controlled); }
        public Agent ControlledAgent { get => GetControlledAgent(); }

        public Avatar(Game game) : base(game) { }

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

        public override void OnPostInit(EntitySettings settings)
        {
            base.OnPostInit(settings);

            // TODO: Clean up this hardcoded mess

            AvatarPrototype avatarProto = AvatarPrototype;

            // Properties
            // AvatarLastActiveTime is needed for missions to show up in the tracker
            Properties[PropertyEnum.AvatarLastActiveCalendarTime] = 1509657924421;  // Nov 02 2017 21:25:24 GMT+0000
            Properties[PropertyEnum.AvatarLastActiveTime] = 161351646299;

            Properties[PropertyEnum.CombatLevel] = CharacterLevel;

            // REMOVEME
            // Unlock all stealable powers for Rogue
            if (avatarProto.StealablePowersAllowed.HasValue())
            {
                foreach (PrototypeId stealablePowerInfoProtoRef in avatarProto.StealablePowersAllowed)
                {
                    var stealablePowerInfo = stealablePowerInfoProtoRef.As<StealablePowerInfoPrototype>();
                    Properties[PropertyEnum.StolenPowerAvailable, stealablePowerInfo.Power] = true;
                }
            }

            // Initialize AbilityKeyMapping
            if (_abilityKeyMappingList.Count == 0)
            {
                AbilityKeyMapping abilityKeyMapping = new();
                abilityKeyMapping.SlotDefaultAbilities(this);
                _abilityKeyMappingList.Add(abilityKeyMapping);
            }
        }

        public override bool ApplyInitialReplicationState(ref EntitySettings settings)
        {
            if (base.ApplyInitialReplicationState(ref settings) == false)
                return false;

            ResetResources(false);

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

            success &= Serializer.Transfer(archive, ref _abilityKeyMappingList);

            return success;
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
        }

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
                    AvatarOnKilledInfoPrototype avatarOnKilledInfo = region.GetAvatarOnKilledInfo();
                    if (avatarOnKilledInfo == null) return Logger.WarnReturn(false, "DoDeathRelease(): avatarOnKilledInfo == null");

                    if (avatarOnKilledInfo.DeathReleaseBehavior == DeathReleaseBehavior.ReturnToWaypoint)
                    {
                        // Find the target for our respawn teleport
                        PrototypeId deathReleaseTarget = FindDeathReleaseTarget();
                        Logger.Trace($"DoDeathRelease(): {deathReleaseTarget.GetName()}");
                        if (deathReleaseTarget == PrototypeId.Invalid)
                            return Logger.WarnReturn(false, "DoDeathRelease(): Failed to find a target to move to");

                        Transition.TeleportToLocalTarget(owner, deathReleaseTarget);
                    }
                    else 
                    {
                        return Logger.WarnReturn(false, $"DoDeathRelease(): Unimplemented behavior {avatarOnKilledInfo.DeathReleaseBehavior}");
                    }

                    break;

                default:
                    return Logger.WarnReturn(false, $"DoDeathRelease(): Unimplemented request type {requestType}");
            }

            return true;
        }

        private PrototypeId FindDeathReleaseTarget()
        {
            Region region = Region;
            if (region == null) return Logger.WarnReturn(PrototypeId.Invalid, "FindDeathReleaseTarget(): region == null");

            Area area = Area;
            if (area == null) return Logger.WarnReturn(PrototypeId.Invalid, "FindDeathReleaseTarget(): area == null");

            Cell cell = Cell;
            if (cell == null) return Logger.WarnReturn(PrototypeId.Invalid, "FindDeathReleaseTarget(): cell == null");

            var player = GetOwnerOfType<Player>();

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

            // TODO: More handling

            // Failed validation despite everything above, clean up and bail out
            if (result != PowerUseResult.Success && result != PowerUseResult.TargetIsMissing)
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
                var player = GetOwnerOfType<Player>();
                if (player != null)
                    Region?.AvatarUsedPowerEvent.Invoke(new(player, this, powerRef, settings.TargetEntityId));
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

            // TODO: IsPowerAllowedInCurrentTransformMode()

            return base.CanTriggerPower(powerProto, power, flags);
        }

        public override void ActivatePostPowerAction(Power power, EndPowerFlags flags)
        {
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

        public AbilitySlot GetPowerSlot(PrototypeId powerProtoRef)
        {
            AbilityKeyMapping keyMapping = CurrentAbilityKeyMapping;
            if (keyMapping == null)
                return Logger.WarnReturn(AbilitySlot.Invalid, $"GetPowerSlot(): No current keyMapping when calling GetPowerSlot [{powerProtoRef.GetName()}]");

            List<AbilitySlot> abilitySlotList = ListPool<AbilitySlot>.Instance.Get();
            keyMapping.GetActiveAbilitySlotsContainingProtoRef(powerProtoRef, abilitySlotList);
            AbilitySlot result = abilitySlotList.Count > 0 ? abilitySlotList[0] : AbilitySlot.Invalid;

            ListPool<AbilitySlot>.Instance.Return(abilitySlotList);
            return result;
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

        public override bool HasPowerInPowerProgression(PrototypeId powerRef)
        {
            if (GameDataTables.Instance.PowerOwnerTable.GetPowerProgressionEntry(PrototypeDataRef, powerRef) != null)
                return true;

            if (GameDataTables.Instance.PowerOwnerTable.GetTalentEntry(PrototypeDataRef, powerRef) != null)
                return true;

            return false;
        }

        public override bool GetPowerProgressionInfo(PrototypeId powerProtoRef, out PowerProgressionInfo info)
        {
            info = new();

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

                info.InitForAvatar(powerProgressionEntry, mappedPowerRef, powerTabRef);
                return info.IsValid;
            }

            // Case 2 - Talent
            var talentEntryPair = powerOwnerTable.GetTalentEntryPair(avatarProto.DataRef, progressionInfoPower);
            var talentGroupPair = powerOwnerTable.GetTalentGroupPair(avatarProto.DataRef, progressionInfoPower);
            if (talentEntryPair.Item1 != null && talentGroupPair.Item1 != null)
            {
                info.InitForAvatar(talentEntryPair.Item1, talentGroupPair.Item1, talentEntryPair.Item2, talentGroupPair.Item2);
                return info.IsValid;
            }

            // Case 3 - Non-Progression Power
            info.InitNonProgressionPower(powerProtoRef);
            return info.IsValid;
        }

        public override int GetLatestPowerProgressionVersion()
        {
            if (AvatarPrototype == null) return 0;
            return AvatarPrototype.PowerProgressionVersion;
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
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "InitializePowers(): player == null");

            PlayerPrototype playerPrototype = player.Prototype as PlayerPrototype;
            AvatarPrototype avatarPrototype = AvatarPrototype;

            PowerIndexProperties indexProps = new(0, CharacterLevel, CombatLevel);

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

            // Initialize resources (TODO: Separate InitializePowers() into multiple methods and move this out of here)
            InitializePrimaryManaBehaviors();
            InitializeSecondaryManaBehaviors();

            // Item Powers
            AssignItemPowers();

            // Emotes
            // Starting emotes
            foreach (AbilityAssignmentPrototype emoteAssignment in playerPrototype.StartingEmotes)
            {
                PrototypeId emoteProtoRef = emoteAssignment.Ability;
                if (GetPower(emoteProtoRef) != null) continue;
                if (AssignPower(emoteProtoRef, indexProps) == null)
                    Logger.Warn($"InitializePowers(): Failed to assign starting emote {GameDatabase.GetPrototypeName(emoteProtoRef)} to {this}");
            }

            // Unlockable emotes
            foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.AvatarEmoteUnlocked, PrototypeDataRef))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId emoteProtoRef);
                if (GetPower(emoteProtoRef) != null) continue;
                if (AssignPower(emoteProtoRef, indexProps) == null)
                    Logger.Warn($"InitializePowers(): Failed to assign unlockable emote {GameDatabase.GetPrototypeName(emoteProtoRef)} to {this}");
            }

            // Assign hidden passive powers (these need to be assigned before progression table powers)
            if (avatarPrototype.HiddenPassivePowers.HasValue())
            {
                foreach (AbilityAssignmentPrototype abilityAssignmentProto in avatarPrototype.HiddenPassivePowers)
                {
                    if (GetPower(abilityAssignmentProto.Ability) == null)
                        AssignPower(abilityAssignmentProto.Ability, indexProps);
                }
            }

            // Progression table powers
            indexProps = new(1, CharacterLevel, CombatLevel);   // use rank 1 for power progression (todo: remove this when we have everything working properly)

            List<PowerProgressionEntryPrototype> powerProgEntryList = ListPool<PowerProgressionEntryPrototype>.Instance.Get();
            if (avatarPrototype.GetPowersUnlockedAtLevel(powerProgEntryList, -1, true))
            {
                foreach (PowerProgressionEntryPrototype powerProgEntry in powerProgEntryList)
                    AssignPower(powerProgEntry.PowerAssignment.Ability, indexProps);
            }

            ListPool<PowerProgressionEntryPrototype>.Instance.Return(powerProgEntryList);

            // Mapped powers (power replacements from talents)
            // AvatarPrototype -> TalentGroups -> Talents -> Talent -> ActionsTriggeredOnPowerEvent -> PowerEventContext -> MappedPower
            foreach (var talentGroup in avatarPrototype.TalentGroups)
            {
                foreach (var talentEntry in talentGroup.Talents)
                {
                    var talent = talentEntry.Talent.As<SpecializationPowerPrototype>();

                    foreach (var powerEventAction in talent.ActionsTriggeredOnPowerEvent)
                    {
                        if (powerEventAction.PowerEventContext is PowerEventContextMapPowersPrototype mapPowerEvent)
                        {
                            foreach (MapPowerPrototype mapPower in mapPowerEvent.MappedPowers)
                            {
                                AssignPower(mapPower.MappedPower, indexProps);
                            }
                        }
                    }
                }
            }

            // Stolen powers for Rogue
            if (avatarPrototype.StealablePowersAllowed.HasValue())
            {
                foreach (PrototypeId stealablePowerInfoProtoRef in avatarPrototype.StealablePowersAllowed)
                {
                    var stealablePowerInfo = stealablePowerInfoProtoRef.As<StealablePowerInfoPrototype>();
                    
                    // Skip assigning stealable passives for now
                    PowerPrototype powerProto = stealablePowerInfo.Power.As<PowerPrototype>();
                    if (powerProto.Activation == PowerActivationType.Passive)
                        continue;

                    AssignPower(stealablePowerInfo.Power, indexProps);
                }
            }

            // Travel
            AssignPower(avatarPrototype.TravelPower, indexProps);

            return true;
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
                        Logger.Warn($"InitializeManaBehaviorPowers(): Failed to assign mana behavior power {powerProtoRef.GetName()} to [{this}]");
                }
            }

            return true;
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

        #region Progression

        public override long AwardXP(long amount, bool showXPAwardedText)
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(0L, "AwardXP(): player == null");

            long awardedAmount = base.AwardXP(amount, showXPAwardedText);

            // Award alternate advancement XP (omega or infinity)
            // TODO: Remove the cosmic prestige experience penalty for omega / infinity
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
            CurrentTeamUpAgent?.AwardXP(amount, showXPAwardedText);

            return awardedAmount;
        }

        public static int GetAvatarLevelCap()
        {
            AdvancementGlobalsPrototype advancementProto = GameDatabase.AdvancementGlobalsPrototype;
            return advancementProto != null ? advancementProto.GetAvatarLevelCap() : 0;
        }

        public bool IsAtMaxPrestigeLevel()
        {
            AdvancementGlobalsPrototype advancementProto = GameDatabase.AdvancementGlobalsPrototype;
            if (advancementProto == null) return false;
            return PrestigeLevel >= advancementProto.MaxPrestigeLevel;
        }

        public override long GetLevelUpXPRequirement(int level)
        {
            AdvancementGlobalsPrototype advancementProto = GameDatabase.AdvancementGlobalsPrototype;
            if (advancementProto == null) return Logger.WarnReturn(0, "GetLevelUpXPRequirement(): advancementProto == null");

            return advancementProto.GetAvatarLevelUpXPRequirement(level);
        }

        public override int TryLevelUp(Player owner)
        {
            int levelDelta = base.TryLevelUp(owner);

            if (levelDelta != 0)
                CombatLevel = Math.Clamp(CombatLevel + levelDelta, 1, GetAvatarLevelCap());

            return levelDelta;
        }

        public long ApplyXPModifiers(long xp, bool applyKillBonus, TuningTable tuningTable = null)
        {
            if (IsInWorld == false)
                return 0;

            // TODO: Prestige multiplier
            // TODO: Party bonus

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

            // Notify clients
            SendLevelUpMessage();

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
            {
                // Slot ALL default abilities
                // (this is dumb, but we have to do this to avoid desync with the client, see CAvatar::autoSlotPowers())
                AbilityKeyMapping currentAbilityKeyMapping = CurrentAbilityKeyMapping;
                if (CurrentAbilityKeyMapping != null)
                {
                    List<HotkeyData> hotkeyDataList = ListPool<HotkeyData>.Instance.Get();
                    if (currentAbilityKeyMapping.GetDefaultAbilities(hotkeyDataList, this))
                    {
                        // TODO: Avatar.SlotAbility()
                        foreach (HotkeyData hotkeyData in hotkeyDataList)
                            currentAbilityKeyMapping.SetAbilityInAbilitySlot(hotkeyData.AbilityProtoRef, hotkeyData.AbilitySlot);
                    }

                    ListPool<HotkeyData>.Instance.Return(hotkeyDataList);
                }
            }

            var player = GetOwnerOfType<Player>();
            if (player == null) return false;
            Region?.AvatarLeveledUpEvent.Invoke(new(player, PrototypeDataRef, newLevel));

            return true;
        }

        protected override void SetCharacterLevel(int characterLevel)
        {
            base.SetCharacterLevel(characterLevel);

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

        public InteractionValidateResult CanUpgradeUltimate()
        {
            // TODO
            return InteractionValidateResult.AvatarUltimateAlreadyMaxedOut;
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
                if (summoned is Agent pet && pet.HasKeyword(keywordGlobals.VanityPetKeyword))
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

            if (IsInTown()) SetSummonWithLifespanRemaining();
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
            if (IsControlPowerSlot() == false) return;

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

        public bool IsControlPowerSlot()
        {
            var keyMapping = CurrentAbilityKeyMapping;
            if (keyMapping == null) return false;

            // TODO Crypto do this

            return true;
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


        #region Event Handlers

        public override void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            base.OnPropertyChange(id, newValue, oldValue, flags);
            if (flags.HasFlag(SetPropertyFlags.Refresh)) return;

            int manaTypeValue;
            ManaType manaType;

            switch (id.Enum)
            {
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

        public override void OnExitedWorld()
        {
            base.OnExitedWorld();

            // Clear dialog target
            Player player = GetOwnerOfType<Player>();
            player?.SetDialogTargetId(InvalidId, InvalidId);

            // despawn teamups / controlled entities
            DespawnPersistentAgents();

            CancelEnduranceEvents();

            if (player != null) player.Properties[PropertyEnum.AvatarTotalTimePlayed] = player.TimePlayed();
            Properties[PropertyEnum.AvatarTotalTimePlayed] = TimePlayed();
            Properties[PropertyEnum.AvatarTimePlayedStart] = TimeSpan.Zero;

            // Pause boosts while not in the world
            UpdateBoostConditionPauseState(true);

            // Store missions to Avatar
            player?.MissionManager?.StoreAvatarMissions(this);

            // Cancel events
            EventScheduler scheduler = Game.GameEventScheduler;
            scheduler.CancelEvent(_refreshStatsPowerEvent);

            // summoner condition
            foreach (var summon in new SummonedEntityIterator(this))
                summon.RemoveSummonerCondition(Id);
        }

        public TimeSpan TimePlayed()
        {
            TimeSpan timePlayed = TimeSpan.Zero;
            TimeSpan totalTimePlayed = Properties[PropertyEnum.AvatarTotalTimePlayed];
            TimeSpan startTime = Properties[PropertyEnum.AvatarTimePlayedStart];

            if (startTime != TimeSpan.Zero)
                timePlayed = Game.CurrentTime - startTime;

            return totalTimePlayed + timePlayed;
        }

        public override void OnLocomotionStateChanged(LocomotionState oldState, LocomotionState newState)
        {
            base.OnLocomotionStateChanged(oldState, newState);
        }

        #endregion

        public override string ToString()
        {
            return $"{base.ToString()}, Player={_playerName?.Get()} (0x{_ownerPlayerDbId:X})";
        }

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

            for (int i = 0; i < _abilityKeyMappingList.Count; i++)
                sb.AppendLine($"{nameof(_abilityKeyMappingList)}[{i}]: {_abilityKeyMappingList[i]}");
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

        #endregion
    }
}
