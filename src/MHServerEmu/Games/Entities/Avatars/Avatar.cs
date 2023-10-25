using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Social;

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
