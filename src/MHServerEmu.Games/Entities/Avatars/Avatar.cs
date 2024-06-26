using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
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

        private EventPointer<TEMP_SendActivatePowerMessageEvent> _swapInPowerEvent = new();

        private ReplicatedVariable<string> _playerName = new(0, string.Empty);
        private ulong _ownerPlayerDbId;
        private List<AbilityKeyMapping> _abilityKeyMappingList = new();

        private ulong _guildId = GuildMember.InvalidGuildId;
        private string _guildName = string.Empty;
        private GuildMembership _guildMembership = GuildMembership.eGMNone;

        public uint AvatarWorldInstanceId { get; } = 1;
        public string PlayerName { get => _playerName.Value; }
        public ulong OwnerPlayerDbId { get => _ownerPlayerDbId; }
        public AbilityKeyMapping CurrentAbilityKeyMapping { get => _abilityKeyMappingList.FirstOrDefault(); }
        public Agent CurrentTeamUpAgent { get => GetTeamUpAgent(Properties[PropertyEnum.AvatarTeamUpAgent]); }
        public AvatarPrototype AvatarPrototype { get => Prototype as AvatarPrototype; }
        public int PrestigeLevel { get => Properties[PropertyEnum.AvatarPrestigeLevel]; }

        public bool IsUsingGamepadInput { get; set; } = false;
        public PrototypeId CurrentTransformMode { get; private set; } = PrototypeId.Invalid;

        public override bool IsMovementAuthoritative => false;
        public override bool CanBeRepulsed => false;
        public override bool CanRepulseOthers => false;

        public PrototypeId TeamUpPowerRef { get => GameDatabase.GlobalsPrototype.TeamUpSummonPower; }

        public Avatar(Game game) : base(game) { }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);

            return true;
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);

            success &= Serializer.Transfer(archive, ref _playerName);
            success &= Serializer.Transfer(archive, ref _ownerPlayerDbId);

            // There is an unused string here that is always empty
            string emptyString = string.Empty;
            success &= Serializer.Transfer(archive, ref emptyString);
            if (emptyString != string.Empty)
                Logger.Warn($"Serialize(): emptyString is not empty!");

            //if (archive.IsReplication)
            success &= GuildMember.SerializeReplicationRuntimeInfo(archive, ref _guildId, ref _guildName, ref _guildMembership);

            success &= Serializer.Transfer(archive, ref _abilityKeyMappingList);

            return success;
        }

        public void SetPlayer(Player player)
        {
            _playerName.Value = player.GetName();
            _ownerPlayerDbId = player.DatabaseUniqueId;
        }

        /// <summary>
        /// Initializes this <see cref="Avatar"/> from data contained in the provided <see cref="DBAccount"/>.
        /// </summary>
        public void InitializeFromDBAccount(PrototypeId prototypeId, DBAccount account)
        {
            DBAvatar dbAvatar = account.GetAvatar((long)prototypeId);
            AvatarPrototype prototype = GameDatabase.GetPrototype<AvatarPrototype>(prototypeId);

            // Archive Data
            _playerName.Value = account.PlayerName;

            // Properties
            // AvatarLastActiveTime is needed for missions to show up in the tracker
            Properties[PropertyEnum.AvatarLastActiveCalendarTime] = 1509657924421;  // Nov 02 2017 21:25:24 GMT+0000
            Properties[PropertyEnum.AvatarLastActiveTime] = 161351646299;

            Properties[PropertyEnum.CostumeCurrent] = dbAvatar.RawCostume;
            Properties[PropertyEnum.CharacterLevel] = 60;
            Properties[PropertyEnum.CombatLevel] = 60;
            Properties[PropertyEnum.AvatarPowerUltimatePoints] = 19;

            // Health
            Properties[PropertyEnum.Health] = Properties[PropertyEnum.HealthMaxOther];

            // Resources
            // Ger primary resources defaults from PrimaryResourceBehaviors
            foreach (PrototypeId manaBehaviorId in prototype.PrimaryResourceBehaviors)
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
            foreach (PrototypeId entryId in prototype.StatProgressionTable)
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

            // Unlock all stealable powers for Rogue
            if (prototypeId == (PrototypeId)6514650100102861856)
            {
                foreach (PrototypeId stealablePowerInfoRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<StealablePowerInfoPrototype>(PrototypeIterateFlags.NoAbstract))
                {
                    var stealablePowerInfo = stealablePowerInfoRef.As<StealablePowerInfoPrototype>();
                    Properties[PropertyEnum.StolenPowerAvailable, stealablePowerInfo.Power] = true;
                }
            }

            // We need 10 synergies active to remove the in-game popup
            int synergyCount = 0;
            foreach (PrototypeId avatarRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                Properties[PropertyEnum.AvatarSynergySelected, avatarRef] = true;
                if (++synergyCount >= 10) break;
            }

            // Initialize AbilityKeyMapping
            _abilityKeyMappingList.Clear();
            AbilityKeyMapping abilityKeyMapping = new();
            if (dbAvatar.RawAbilityKeyMapping != null)
            {
                // Deserialize existing saved mapping if there is one
                using (Archive archive = new(ArchiveSerializeType.Database, dbAvatar.RawAbilityKeyMapping))
                    abilityKeyMapping.Serialize(archive);
            }
            else
            {
                // Initialize a new mapping
                abilityKeyMapping.SlotDefaultAbilities(this);
            }

            _abilityKeyMappingList.Add(abilityKeyMapping);
        }

        public override void OnEnteredWorld(EntitySettings settings)
        {
            base.OnEnteredWorld(settings);
            AssignDefaultAvatarPowers();
            SetSimulated(false); // For AI
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
            if (avatarPrototype.DataRef == (PrototypeId)AvatarPrototypeId.Rogue)
            {
                foreach (PrototypeId stealablePowerInfoRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<StealablePowerInfoPrototype>(PrototypeIterateFlags.NoAbstract))
                {
                    var stealablePowerInfo = stealablePowerInfoRef.As<StealablePowerInfoPrototype>();
                    AssignPower(stealablePowerInfo.Power, indexProps);
                }
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

        public override void OnLocomotionStateChanged(LocomotionState oldState, LocomotionState newState)
        {
            base.OnLocomotionStateChanged(oldState, newState);
        }

        public void ScheduleSwapInPower()
        {
            ScheduleEntityEvent(_swapInPowerEvent, TimeSpan.FromMilliseconds(700), GameDatabase.GlobalsPrototype.AvatarSwapInPower);
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

        internal bool IsValidTargetForCurrentPower(WorldEntity target)
        {
            throw new NotImplementedException();
        }

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
            EntitySettings setting = new()
            { OptionFlags = EntitySettingsOptionFlags.IsNewOnServer | EntitySettingsOptionFlags.IsClientEntityHidden };            
            teamUp.EnterWorld(RegionLocation.Region, teamUp.GetPositionNearAvatar(this), RegionLocation.Orientation, setting);
            teamUp.AIController.Blackboard.PropertyCollection[PropertyEnum.AIAssistedEntityID] = Id; // link to owner
        }

        public void DismissTeamUpAgent()
        {
            Agent teamUp = CurrentTeamUpAgent;
            if (teamUp == null) return;
            if (teamUp.IsAliveInWorld)
            {
                // TODO: teamUp.Kill(null);

                var killMessage = NetMessageEntityKill.CreateBuilder()
                    .SetIdEntity(teamUp.Id)
                    .SetIdKillerEntity(0)
                    .SetKillFlags(0)
                    .Build();
                Game.NetworkManager.SendMessageToInterested(killMessage, teamUp, AOINetworkPolicyValues.AOIChannelProximity);             
                Properties.RemoveProperty(PropertyEnum.AvatarTeamUpIsSummoned);
                Properties.RemoveProperty(PropertyEnum.AvatarTeamUpStartTime);
                Properties.RemoveProperty(PropertyEnum.AvatarTeamUpDuration);
                teamUp.AIController.SetIsEnabled(false);
                teamUp.ScheduleExitWorldEvent(TimeSpan.FromMilliseconds(teamUp.WorldEntityPrototype.RemoveFromWorldTimerMS));
            }
        }

        public void LinkTeamUpAgent(Agent teamUpAgent)
        {
            Properties[PropertyEnum.AvatarTeamUpAgentId] = teamUpAgent.Id;
            teamUpAgent.Properties[PropertyEnum.TeamUpOwnerId] = Id;
            teamUpAgent.Properties[PropertyEnum.PowerUserOverrideID] = Id;
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

        public override void OnExitedWorld()
        {
            base.OnExitedWorld();
            SetSimulated(false); // put it here for test
            if (CurrentTeamUpAgent != null) DismissTeamUpAgent();
        }

    }
}
