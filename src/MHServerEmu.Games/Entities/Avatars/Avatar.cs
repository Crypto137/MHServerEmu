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
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Guilds;

namespace MHServerEmu.Games.Entities.Avatars
{
    public class Avatar : Agent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly TimeSpan StandardContinuousPowerRecheckDelay = TimeSpan.FromMilliseconds(150);

        private readonly EventPointer<ActivateSwapInPowerEvent> _activateSwapInPowerEvent = new();
        private readonly EventPointer<RecheckContinuousPowerEvent> _recheckContinuousPowerEvent = new();
        private readonly EventPointer<AvatarEnteredRegionEvent> _avatarEnteredRegionEvent = new();

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
        public AvatarPrototype AvatarPrototype { get => Prototype as AvatarPrototype; }
        public int PrestigeLevel { get => Properties[PropertyEnum.AvatarPrestigeLevel]; }
        public override bool IsAtLevelCap { get => CharacterLevel >= GetAvatarLevelCap(); }

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

        public Avatar(Game game) : base(game) { }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);


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

            // HACK: Set health to max for new avatars
            if (Properties[PropertyEnum.Health] == 0)
                Properties[PropertyEnum.Health] = Properties[PropertyEnum.HealthMaxOther];

            // Resources
            // Ger primary resources defaults from PrimaryResourceBehaviors
            foreach (PrototypeId manaBehaviorId in avatarProto.PrimaryResourceBehaviors)
            {
                var behaviorPrototype = GameDatabase.GetPrototype<PrimaryResourceManaBehaviorPrototype>(manaBehaviorId);
                Curve manaCurve = GameDatabase.GetCurve(behaviorPrototype.BaseEndurancePerLevel);
                Properties[PropertyEnum.EnduranceBase, (int)behaviorPrototype.ManaType] = manaCurve.GetAt(60);
            }
;
            // Set primary resources
            Properties[PropertyEnum.EnduranceMaxOther] = Properties[PropertyEnum.EnduranceBase];
            Properties[PropertyEnum.EnduranceMax] = Properties[PropertyEnum.EnduranceMaxOther];
            Properties[PropertyEnum.Endurance] = Properties[PropertyEnum.EnduranceMax];
            Properties[PropertyEnum.EnduranceMaxOther, (int)ManaType.Type2] = Properties[PropertyEnum.EnduranceBase, (int)ManaType.Type2];
            Properties[PropertyEnum.EnduranceMax, (int)ManaType.Type2] = Properties[PropertyEnum.EnduranceMaxOther, (int)ManaType.Type2];
            Properties[PropertyEnum.Endurance, (int)ManaType.Type2] = Properties[PropertyEnum.EnduranceMax, (int)ManaType.Type2];

            // Secondary resource base is already present in the prototype's property collection as a curve property
            Properties[PropertyEnum.SecondaryResourceMax] = Properties[PropertyEnum.SecondaryResourceMaxBase];
            Properties[PropertyEnum.SecondaryResource] = Properties[PropertyEnum.SecondaryResourceMax];

            // Stats
            foreach (PrototypeId entryId in avatarProto.StatProgressionTable)
            {
                var entry = entryId.As<StatProgressionEntryPrototype>();

                if (entry.DurabilityValue > 0)
                    Properties[PropertyEnum.StatDurability] = entry.DurabilityValue;

                if (entry.StrengthValue > 0)
                    Properties[PropertyEnum.StatStrength] = entry.StrengthValue;

                if (entry.FightingSkillsValue > 0)
                    Properties[PropertyEnum.StatFightingSkills] = entry.FightingSkillsValue;

                if (entry.SpeedValue > 0)
                    Properties[PropertyEnum.StatSpeed] = entry.SpeedValue;

                if (entry.EnergyProjectionValue > 0)
                    Properties[PropertyEnum.StatEnergyProjection] = entry.EnergyProjectionValue;

                if (entry.IntelligenceValue > 0)
                    Properties[PropertyEnum.StatIntelligence] = entry.IntelligenceValue;
            }

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
                // Do a normal position change and update AOI if the position is loaded
                result = base.ChangeRegionPosition(position, orientation, flags);
                if (result == ChangePositionResult.PositionChanged)
                    player.AOI.Update(RegionLocation.Position);
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

        public override PowerUseResult ActivatePower(PrototypeId powerRef, ref PowerActivationSettings settings)
        {
            PowerUseResult rusult = base.ActivatePower(powerRef, ref settings);

            var player = GetOwnerOfType<Player>();
            if (player != null) 
                Region?.AvatarUsedPowerEvent.Invoke(new(player, this, powerRef, settings.TargetEntityId));

            return rusult;
        }

        public override void ActivatePostPowerAction(Power power, EndPowerFlags flags)
        {
            base.ActivatePostPowerAction(power, flags);

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

            AvatarPrototype avatarPrototype = AvatarPrototype;
            if (avatarPrototype.PrimaryResourceBehaviors.IsNullOrEmpty())
                return Logger.WarnReturn(false, "ShouldContinueRecurringPower(): avatarPrototype.PrimaryResourceBehaviors.IsNullOrEmpty()");

            // Check endurance (mana) costs
            foreach (PrototypeId primaryManaBehaviorProtoRef in avatarPrototype.PrimaryResourceBehaviors)
            {
                var primaryManaBehaviorProto = primaryManaBehaviorProtoRef.As<PrimaryResourceManaBehaviorPrototype>();
                if (primaryManaBehaviorProto == null)
                {
                    Logger.Warn("ShouldContinueRecurringPower(): primaryManaBehaviorProto == null");
                    continue;
                }

                float endurance = Properties[PropertyEnum.Endurance, (int)primaryManaBehaviorProto.ManaType];
                float enduranceCost = power.GetEnduranceCost(primaryManaBehaviorProto.ManaType, true);

                if (endurance < enduranceCost)
                {
                    flags |= EndPowerFlags.ExplicitCancel;
                    flags |= EndPowerFlags.NotEnoughEndurance;
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

            // Check power's CanTriggerEval
            return power.CheckCanTriggerEval();
        }

        public void SetContinuousPower(PrototypeId powerProtoRef, ulong targetId, Vector3 targetPosition, uint randomSeed, bool notifyOwner = false)
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
            IEnumerable<PlayerConnection> interestedClients = networkManager.GetInterestedClients(this, AOINetworkPolicyValues.AOIChannelProximity, notifyOwner == false);
            if (interestedClients.Any() == false) return;

            // NOTE: Although NetMessageCancelPower is not an archive, it uses power prototype enums
            var continuousPowerUpdateMessage = NetMessageContinuousPowerUpdateToClient.CreateBuilder()
                .SetIdAvatar(Id)
                .SetPowerPrototypeId((ulong)powerProtoRef)
                .SetIdTargetEntity(targetId)
                .SetTargetPosition(targetPosition.ToNetStructPoint3())
                .SetRandomSeed(randomSeed)
                .Build();

            networkManager.SendMessageToMultiple(interestedClients, continuousPowerUpdateMessage);
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
                            GRandom random = new((int)_continuousPowerData.RandomSeed);
                            _continuousPowerData.RandomSeed = (uint)random.Next(0, 10000);

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
            //Logger.Debug("CancelPendingAction()");
            _pendingAction.Clear();
        }

        public bool IsCombatActive()
        {
            // TODO: Check PropertyEnum.LastInflictedDamageTime
            return true;
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
            List<Inventory> inventoryList = ListPool<Inventory>.Instance.Rent();

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

        private bool AssignDefaultAvatarPowers()
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "AssignHardcodedPowers(): player == null");

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
            AssignPower(GameDatabase.GlobalsPrototype.AvatarHealPower, indexProps);

            AssignItemPowers();

            // Progression table powers
            foreach (var powerProgressionEntry in avatarPrototype.GetPowersUnlockedAtLevel(-1, true))
                AssignPower(powerProgressionEntry.PowerAssignment.Ability, indexProps);

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
                    AssignPower(stealablePowerInfo.Power, indexProps);
                }
            }

            // Assign hidden passive powers
            if (avatarPrototype.HiddenPassivePowers.HasValue())
            {
                foreach (AbilityAssignmentPrototype abilityAssignmentProto in avatarPrototype.HiddenPassivePowers)
                    AssignPower(abilityAssignmentProto.Ability, indexProps);
            }

            // Travel
            AssignPower(avatarPrototype.TravelPower, indexProps);

            // Emotes
            // Starting emotes
            foreach (AbilityAssignmentPrototype emoteAssignment in playerPrototype.StartingEmotes)
            {
                PrototypeId emoteProtoRef = emoteAssignment.Ability;
                if (GetPower(emoteProtoRef) != null) continue;
                if (AssignPower(emoteProtoRef, indexProps) == null)
                    Logger.Warn($"AssignDefaultAvatarPowers(): Failed to assign starting emote {GameDatabase.GetPrototypeName(emoteProtoRef)} to {this}");
            }

            // Unlockable emotes
            foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.AvatarEmoteUnlocked, PrototypeDataRef))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId emoteProtoRef);
                if (GetPower(emoteProtoRef) != null) continue;
                if (AssignPower(emoteProtoRef, indexProps) == null)
                    Logger.Warn($"AssignDefaultAvatarPowers(): Failed to assign unlockable emote {GameDatabase.GetPrototypeName(emoteProtoRef)} to {this}");
            }

            return true;
        }

        private bool AssignItemPowers()
        {
            // This has similar structure to FindAbilityItem()
            Player playerOwner = GetOwnerOfType<Player>();
            if (playerOwner == null) return Logger.WarnReturn(false, "AssignItemPowers(): playerOwner == null");

            List<Inventory> inventoryList = ListPool<Inventory>.Instance.Rent();

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

        #endregion

        #region Progression

        public override long AwardXP(long amount, bool showXPAwardedText)
        {
            long awardedAmount = base.AwardXP(amount, showXPAwardedText);

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

            return (long)MathF.Round(xp * xpMult);
        }

        protected override bool OnLevelUp(int oldLevel, int newLevel)
        {
            Properties[PropertyEnum.Health] = Properties[PropertyEnum.HealthMaxOther];

            // Slot unlocked default abilities
            AbilityKeyMapping currentAbilityKeyMapping = CurrentAbilityKeyMapping;
            if (CurrentAbilityKeyMapping != null)
            {
                foreach (HotkeyData hotkeyData in currentAbilityKeyMapping.GetDefaultAbilities(this, oldLevel))
                {
                    Logger.Debug($"OnLevelUp(): {hotkeyData}");
                    currentAbilityKeyMapping.SetAbilityInAbilitySlot(hotkeyData.AbilityProtoRef, hotkeyData.AbilitySlot);
                }
            }

            SendLevelUpMessage();

            var player = GetOwnerOfType<Player>();
            if (player == null) return false;
            Region?.AvatarLeveledUpEvent.Invoke(new(player, PrototypeDataRef, newLevel));

            return true;
        }

        protected override void SetCharacterLevel(int characterLevel)
        {
            base.SetCharacterLevel(characterLevel);

            Player owner = GetOwnerOfType<Player>();
            owner?.OnAvatarCharacterLevelChanged(this);
        }

        protected override void SetCombatLevel(int combatLevel)
        {
            base.SetCombatLevel(combatLevel);

            Agent teamUpAgent = CurrentTeamUpAgent;
            if (teamUpAgent != null)
                teamUpAgent.CombatLevel = combatLevel;
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

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.LootBonusRarityStackCount))
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

        #region Omega and Infinity

        public long GetInfinityPointsSpentOnBonus(PrototypeId infinityGemBonusRef, bool getTempPoints)
        {
            if (getTempPoints)
            {
                long pointsSpent = Properties[PropertyEnum.InfinityPointsSpentTemp, infinityGemBonusRef];
                if (pointsSpent >= 0) return pointsSpent;
            }

            return Properties[PropertyEnum.InfinityPointsSpentTemp, infinityGemBonusRef];
        }

        public int GetOmegaPointsSpentOnBonus(PrototypeId omegaBonusRef, bool getTempPoints)
        {
            if (getTempPoints)
            {
                int pointsSpent = Properties[PropertyEnum.OmegaSpecTemp, omegaBonusRef];
                if (pointsSpent >= 0) return pointsSpent;
            }

            return Properties[PropertyEnum.OmegaSpec, omegaBonusRef];
        }

        #endregion

        #region Team-Ups

        public void SelectTeamUpAgent(PrototypeId teamUpProtoRef)
        {
            if (teamUpProtoRef == PrototypeId.Invalid || IsTeamUpAgentUnlocked(teamUpProtoRef) == false) return;
            Agent currentTeamUp = CurrentTeamUpAgent;
            if (currentTeamUp != null)
                if (currentTeamUp.IsInWorld || currentTeamUp.PrototypeDataRef == teamUpProtoRef) return;

            Properties[PropertyEnum.AvatarTeamUpAgent] = teamUpProtoRef;
            LinkTeamUpAgent(CurrentTeamUpAgent);
            Player player = GetOwnerOfType<Player>();
            player.Properties[PropertyEnum.AvatarLibraryTeamUp, 0, Prototype.DataRef] = teamUpProtoRef;

            // TODO affixes, event PlayerActivatedTeamUpGameEvent
        }

        public void SummonTeamUpAgent()
        {
            Agent teamUp = CurrentTeamUpAgent;
            if (teamUp == null) return;
            if (teamUp.IsInWorld) return;

            Properties[PropertyEnum.AvatarTeamUpIsSummoned] = true;
            Properties[PropertyEnum.AvatarTeamUpStartTime] = (long)Game.CurrentTime.TotalMilliseconds;
            //Power power = GetPower(TeamUpPowerRef);
            //Properties[PropertyEnum.AvatarTeamUpDuration] = power.GetCooldownDuration();

            ActivateTeamUpAgent(true);
        }

        public bool ClearSummonedTeamUpAgent(Agent teamUpAgent)
        {
            if (teamUpAgent != CurrentTeamUpAgent)
                return Logger.WarnReturn(false, "CleanUpSummonedTeamUpAgent(): teamUpAgent != CurrentTeamUpAgent");

            Properties.RemoveProperty(PropertyEnum.AvatarTeamUpIsSummoned);
            Properties.RemoveProperty(PropertyEnum.AvatarTeamUpStartTime);
            //Properties.RemoveProperty(PropertyEnum.AvatarTeamUpDuration);

            return true;
        }

        public void DismissTeamUpAgent()
        {
            Agent teamUp = CurrentTeamUpAgent;
            if (teamUp == null) return;
            if (teamUp.IsAliveInWorld)
            {
                teamUp.Kill();
            }
        }

        public void LinkTeamUpAgent(Agent teamUpAgent)
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

        private void ActivateTeamUpAgent(bool playIntro)
        {
            Agent teamUp = CurrentTeamUpAgent;
            if (teamUp == null) return;

            // Resurrect or restore team-up health
            if (teamUp.IsDead)
                teamUp.Resurrect();
            else
                teamUp.Properties[PropertyEnum.Health] = teamUp.Properties[PropertyEnum.HealthMax];

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            if (playIntro)
                settings.OptionFlags = EntitySettingsOptionFlags.IsNewOnServer | EntitySettingsOptionFlags.IsClientEntityHidden;

            teamUp.EnterWorld(RegionLocation.Region, teamUp.GetPositionNearAvatar(this), RegionLocation.Orientation, settings);
            teamUp.AIController.Blackboard.PropertyCollection[PropertyEnum.AIAssistedEntityID] = Id; // link to owner
        }

        private void DeactivateTeamUpAgent()
        {
            CurrentTeamUpAgent?.ExitWorld();
        }

        #endregion

        #region Event Handlers

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
                // TODO Achievement?
                PlayerEnteredAreaGameEvent evt = new(player, newArea.PrototypeDataRef);
                newArea.PopulationArea?.OnPlayerEntered();
                newArea.PlayerEnteredAreaEvent.Invoke(evt);
                newArea.Region.PlayerEnteredAreaEvent.Invoke(evt);
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

            base.OnEnteredWorld(settings);
            AssignDefaultAvatarPowers();

            // auto unlock chapters and Waypoinst
            player.UnlockChapters();
            player.UnlockWaypoints();

            var region = Region;
            var regionProto = region?.Prototype;
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

            // Update AOI of the owner player
            AreaOfInterest aoi = player.AOI;
            aoi.Update(RegionLocation.Position, true);

            if (Properties[PropertyEnum.AvatarTeamUpAgent] != PrototypeId.Invalid)
            {
                LinkTeamUpAgent(CurrentTeamUpAgent);
                if (Properties[PropertyEnum.AvatarTeamUpIsSummoned])
                    ActivateTeamUpAgent(true);  // We may want to disable the intro animation in some cases
            }

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
            player?.SetDialogTarget(InvalidId, InvalidId);

            DeactivateTeamUpAgent();

            Inventory summonedInventory = GetInventory(InventoryConvenienceLabel.Summoned);
            summonedInventory?.DestroyContained();

            // Store missions to Avatar
            player?.MissionManager?.StoreAvatarMissions(this);
        }

        public override void OnLocomotionStateChanged(LocomotionState oldState, LocomotionState newState)
        {
            base.OnLocomotionStateChanged(oldState, newState);
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

        private class RecheckContinuousPowerEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Avatar)t).CheckContinuousPower();
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

        #endregion
    }
}
