using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Powers;
using MHServerEmu.GameServer.Social;

namespace MHServerEmu.GameServer.Entities.Avatars
{
    public class Avatar : Agent
    {
        public ReplicatedString PlayerName { get; set; }
        public ulong OwnerPlayerDbId { get; set; }
        public string GuildName { get; set; }
        public bool HasGuildInfo { get; set; }
        public GuildMemberReplicationRuntimeInfo GuildInfo { get; set; }
        public AbilityKeyMapping[] AbilityKeyMappings { get; set; }

        public Avatar(EntityBaseData baseData, byte[] archiveData) : base(baseData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);
            BoolDecoder boolDecoder = new();

            DecodeEntityFields(stream);
            DecodeWorldEntityFields(stream);

            PlayerName = new(stream);
            OwnerPlayerDbId = stream.ReadRawVarint64();

            GuildName = stream.ReadRawString();

            //Gazillion::GuildMember::SerializeReplicationRuntimeInfo
            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            HasGuildInfo = boolDecoder.ReadBool();

            if (HasGuildInfo) GuildInfo = new(stream);

            AbilityKeyMappings = new AbilityKeyMapping[stream.ReadRawVarint64()];
            for (int i = 0; i < AbilityKeyMappings.Length; i++)
                AbilityKeyMappings[i] = new(stream, boolDecoder);
        }

        public Avatar(EntityBaseData baseData, EntityTrackingContextMap[] trackingContextMap, Condition[] conditionCollection, PowerCollectionRecord[] powerCollection, int unkEvent,
            ReplicatedString playerName, ulong ownerPlayerDbId, string guildName, bool hasGuildInfo, GuildMemberReplicationRuntimeInfo guildInfo, AbilityKeyMapping[] abilityKeyMappings)
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

        public override byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                // Prepare bool encoder
                BoolEncoder boolEncoder = new();

                boolEncoder.EncodeBool(HasGuildInfo);
                foreach (AbilityKeyMapping keyMap in AbilityKeyMappings) keyMap.EncodeBools(boolEncoder);

                boolEncoder.Cook();

                // Encode
                EncodeEntityFields(cos);
                EncodeWorldEntityFields(cos);

                cos.WriteRawBytes(PlayerName.Encode());
                cos.WriteRawVarint64(OwnerPlayerDbId);
                cos.WriteRawString(GuildName);

                boolEncoder.WriteBuffer(cos);   // HasGuildInfo  
                if (HasGuildInfo) cos.WriteRawBytes(GuildInfo.Encode());

                cos.WriteRawVarint64((ulong)AbilityKeyMappings.Length);
                foreach (AbilityKeyMapping keyMap in AbilityKeyMappings) cos.WriteRawBytes(keyMap.Encode(boolEncoder));

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            WriteEntityString(sb);
            WriteWorldEntityString(sb);

            sb.AppendLine($"PlayerName: {PlayerName}");
            sb.AppendLine($"OwnerPlayerDbId: 0x{OwnerPlayerDbId:X}");
            sb.AppendLine($"GuildName: {GuildName}");
            sb.AppendLine($"HasGuildInfo: {HasGuildInfo}");
            sb.AppendLine($"GuildInfo: {GuildInfo}");
            for (int i = 0; i < AbilityKeyMappings.Length; i++) sb.AppendLine($"AbilityKeyMapping{i}: {AbilityKeyMappings[i]}");

            return sb.ToString();
        }
    }
}
