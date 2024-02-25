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
using MHServerEmu.PlayerManagement.Accounts.DBModels;

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
        public void InitializeFromDBAccount(DBAccount account)
        {
            PlayerName.Value = account.PlayerName;

            Properties[PropertyEnum.CostumeCurrent] = (PrototypeId)account.CurrentAvatar.Costume;
            Properties[PropertyEnum.CharacterLevel] = 60;
            Properties[PropertyEnum.CombatLevel] = 60;
            Properties[PropertyEnum.AvatarPrestigeLevel] = 0;
            Properties[PropertyEnum.AvatarVanityTitle] = PrototypeId.Invalid;
            Properties[PropertyEnum.AvatarPowerUltimatePoints] = 19;
            Properties[PropertyEnum.Endurance] = Properties[PropertyEnum.EnduranceMax];
            Properties[PropertyEnum.Endurance, (int)ManaType.Type2] = Properties[PropertyEnum.EnduranceMax, (int)ManaType.Type2];
            Properties[PropertyEnum.SecondaryResource] = Properties[PropertyEnum.SecondaryResourceMax];

            // We need 10 synergies active to remove the in-game popup
            Properties.RemovePropertyRange(PropertyEnum.AvatarSynergySelected);
            for (int i = 0; i < 10; i++)
                Properties[PropertyEnum.AvatarSynergySelected, (PrototypeId)account.Avatars[i].Prototype] = true;

            if (account.CurrentAvatar.AbilityKeyMapping == null)
            {
                account.CurrentAvatar.AbilityKeyMapping = new(0);
                account.CurrentAvatar.AbilityKeyMapping.SlotDefaultAbilities(this);
            }

            AbilityKeyMappings = new AbilityKeyMapping[] { account.CurrentAvatar.AbilityKeyMapping };
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
