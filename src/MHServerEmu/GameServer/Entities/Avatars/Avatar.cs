using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Powers;

namespace MHServerEmu.GameServer.Entities.Avatars
{
    public class Avatar : WorldEntity
    {
        public ReplicatedString PlayerName { get; set; }
        public ulong OwnerPlayerDbId { get; set; }
        public string GuildName { get; set; }
        public bool IsRuntimeInfo { get; set; }
        public AbilityKeyMapping[] AbilityKeyMappings { get; set; }

        public Avatar(byte[] archiveData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);
            BoolDecoder boolDecoder = new();

            ReadEntityFields(stream);
            ReadWorldEntityFields(stream);

            PlayerName = new(stream);
            OwnerPlayerDbId = stream.ReadRawVarint64();

            GuildName = stream.ReadRawString();
            //Gazillion::GuildMember::SerializeReplicationRuntimeInfo

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            IsRuntimeInfo = boolDecoder.ReadBool();

            if (IsRuntimeInfo)
            {
                throw new("RuntimeInfo decoding not implemented!");
                // u64
                // string
                // int zigzag
            }

            AbilityKeyMappings = new AbilityKeyMapping[stream.ReadRawVarint64()];
            for (int i = 0; i < AbilityKeyMappings.Length; i++)
                AbilityKeyMappings[i] = new(stream, boolDecoder);
        }

        public Avatar(Condition[] conditions, int unknownPowerVar, ReplicatedString playerName, ulong ownerPlayerDbId,
            string guildName, bool isRuntimeInfo, AbilityKeyMapping[] abilityKeyMappings)
        {
            Conditions = conditions;
            UnknownPowerVar = unknownPowerVar;
            PlayerName = playerName;
            OwnerPlayerDbId = ownerPlayerDbId;
            GuildName = guildName;
            IsRuntimeInfo = isRuntimeInfo;
            AbilityKeyMappings = abilityKeyMappings;
        }

        public override byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                // Prepare bool encoder
                BoolEncoder boolEncoder = new();
                byte bitBuffer;

                boolEncoder.WriteBool(IsRuntimeInfo);
                foreach (AbilityKeyMapping keyMap in AbilityKeyMappings) boolEncoder.WriteBool(keyMap.ShouldPersist);

                boolEncoder.Cook();

                // Encode
                WriteEntityFields(stream);
                WriteWorldEntityFields(stream);

                stream.WriteRawBytes(PlayerName.Encode());
                stream.WriteRawVarint64(OwnerPlayerDbId);
                stream.WriteRawString(GuildName);

                bitBuffer = boolEncoder.GetBitBuffer();             // IsRuntimeInfo
                if (bitBuffer != 0) stream.WriteRawByte(bitBuffer);

                stream.WriteRawVarint64((ulong)AbilityKeyMappings.Length);
                foreach (AbilityKeyMapping keyMap in AbilityKeyMappings) stream.WriteRawBytes(keyMap.Encode(boolEncoder));

                stream.Flush();
                return memoryStream.ToArray();
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
            sb.AppendLine($"IsRuntimeInfo: {IsRuntimeInfo}");
            for (int i = 0; i < AbilityKeyMappings.Length; i++) sb.AppendLine($"AbilityKeyMapping{i}: {AbilityKeyMappings[i]}");

            return sb.ToString();
        }
    }
}
