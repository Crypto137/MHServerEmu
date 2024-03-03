using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Social;
using MHServerEmu.Games.Network;
using MHServerEmu.PlayerManagement.Accounts.DBModels;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.Entities.Avatars
{
    public class Avatar : Agent
    {
        public ReplicatedVariable<string> PlayerName { get; set; }
        public ulong OwnerPlayerDbId { get; set; }
        public string GuildName { get; set; }
        public bool HasGuildInfo { get; set; }
        public GuildMemberReplicationRuntimeInfo GuildInfo { get; set; }
        public AbilityKeyMapping[] AbilityKeyMappings { get; set; }

        public Avatar(ulong entityId, ulong replicationId) : base(new())
        {
            // Entity
            BaseData.ReplicationPolicy = AOINetworkPolicyValues.AOIChannelOwner;
            BaseData.LocomotionState = new(0f);
            BaseData.EntityId = entityId;
            BaseData.InterestPolicies = AOINetworkPolicyValues.AOIChannelOwner;
            BaseData.FieldFlags = EntityCreateMessageFlags.HasInterestPolicies | EntityCreateMessageFlags.HasInvLoc | EntityCreateMessageFlags.HasAvatarWorldInstanceId;
            
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelOwner;
            Properties = new(replicationId);

            // WorldEntity
            TrackingContextMap = Array.Empty<EntityTrackingContextMap>();
            ConditionCollection = Array.Empty<Condition>();
            PowerCollection = Array.Empty<PowerCollectionRecord>();
            UnkEvent = 134463198;

            // Avatar
            PlayerName = new(++replicationId, string.Empty);
            OwnerPlayerDbId = 0x20000000000D3D03;
            GuildName = string.Empty;
        }

        public Avatar(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public Avatar(EntityBaseData baseData, EntityTrackingContextMap[] trackingContextMap, Condition[] conditionCollection, PowerCollectionRecord[] powerCollection, int unkEvent,
            ReplicatedVariable<string> playerName, ulong ownerPlayerDbId, string guildName, bool hasGuildInfo, GuildMemberReplicationRuntimeInfo guildInfo, AbilityKeyMapping[] abilityKeyMappings)
            : base(baseData)
        {
            TrackingContextMap = trackingContextMap;
            ConditionCollection = conditionCollection;
            PowerCollection = powerCollection;
            UnkEvent = unkEvent;
            PlayerName = playerName;
            OwnerPlayerDbId = ownerPlayerDbId;
            GuildName = guildName;
            HasGuildInfo = hasGuildInfo;
            GuildInfo = guildInfo;
            AbilityKeyMappings = abilityKeyMappings;
        }

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);

            BoolDecoder boolDecoder = new();

            PlayerName = new(stream);
            OwnerPlayerDbId = stream.ReadRawVarint64();

            GuildName = stream.ReadRawString();

            //Gazillion::GuildMember::SerializeReplicationRuntimeInfo
            HasGuildInfo = boolDecoder.ReadBool(stream);
            if (HasGuildInfo) GuildInfo = new(stream);

            AbilityKeyMappings = new AbilityKeyMapping[stream.ReadRawVarint64()];
            for (int i = 0; i < AbilityKeyMappings.Length; i++)
                AbilityKeyMappings[i] = new(stream, boolDecoder);
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);

            // Prepare bool encoder
            BoolEncoder boolEncoder = new();

            boolEncoder.EncodeBool(HasGuildInfo);
            foreach (AbilityKeyMapping keyMap in AbilityKeyMappings) keyMap.EncodeBools(boolEncoder);

            boolEncoder.Cook();

            // Encode
            PlayerName.Encode(stream);
            stream.WriteRawVarint64(OwnerPlayerDbId);
            stream.WriteRawString(GuildName);

            boolEncoder.WriteBuffer(stream);   // HasGuildInfo  
            if (HasGuildInfo) GuildInfo.Encode(stream);

            stream.WriteRawVarint64((ulong)AbilityKeyMappings.Length);
            foreach (AbilityKeyMapping keyMap in AbilityKeyMappings) keyMap.Encode(stream, boolEncoder);
        }

        /// <summary>
        /// Initializes this <see cref="Avatar"/> from data contained in the provided <see cref="DBAccount"/>.
        /// </summary>
        public void InitializeFromDBAccount(PrototypeId prototypeId, DBAccount account)
        {
            DBAvatar dbAvatar = account.GetAvatar(prototypeId);
            AvatarPrototype prototype = GameDatabase.GetPrototype<AvatarPrototype>(prototypeId);

            // Base Data
            BaseData.PrototypeId = prototypeId;

            // Archive Data
            PlayerName.Value = account.PlayerName;

            // Properties
            Properties.FlattenCopyFrom(prototype.Properties, true);

            // AvatarLastActiveTime is needed for missions to show up in the tracker
            Properties[PropertyEnum.AvatarLastActiveCalendarTime] = 1509657924421;  // Nov 02 2017 21:25:24 GMT+0000
            Properties[PropertyEnum.AvatarLastActiveTime] = 161351646299;

            Properties[PropertyEnum.CostumeCurrent] = (PrototypeId)dbAvatar.Costume;
            Properties[PropertyEnum.CharacterLevel] = 60;
            Properties[PropertyEnum.CombatLevel] = 60;
            Properties[PropertyEnum.AvatarPowerUltimatePoints] = 19;

            // Health
            Properties[PropertyEnum.HealthMaxOther] = (int)(float)Properties[PropertyEnum.HealthBase];
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
                foreach (PrototypeId stealablePowerInfoId in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(StealablePowerInfoPrototype), PrototypeIterateFlags.NoAbstract))
                {
                    var stealablePowerInfo = stealablePowerInfoId.As<StealablePowerInfoPrototype>();
                    Properties[PropertyEnum.StolenPowerAvailable, stealablePowerInfo.Power] = true;
                }
            }

            // We need 10 synergies active to remove the in-game popup
            int synergyCount = 0;
            foreach (PrototypeId avatarId in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(AvatarPrototype),
                PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
            {
                Properties[PropertyEnum.AvatarSynergySelected, avatarId] = true;
                if (++synergyCount >= 10) break;
            }

            // Initialize AbilityKeyMapping if needed
            if (dbAvatar.AbilityKeyMapping == null)
            {
                dbAvatar.AbilityKeyMapping = new(0);
                dbAvatar.AbilityKeyMapping.SlotDefaultAbilities(this);
            }

            AbilityKeyMappings = new AbilityKeyMapping[] { dbAvatar.AbilityKeyMapping };
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"PlayerName: {PlayerName}");
            sb.AppendLine($"OwnerPlayerDbId: 0x{OwnerPlayerDbId:X}");
            sb.AppendLine($"GuildName: {GuildName}");
            sb.AppendLine($"HasGuildInfo: {HasGuildInfo}");
            sb.AppendLine($"GuildInfo: {GuildInfo}");
            for (int i = 0; i < AbilityKeyMappings.Length; i++) sb.AppendLine($"AbilityKeyMapping{i}: {AbilityKeyMappings[i]}");
        }
    }
}
