using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoding;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Achievements;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Misc;
using MHServerEmu.GameServer.Missions;
using MHServerEmu.GameServer.Properties;
using MHServerEmu.GameServer.Social;

namespace MHServerEmu.GameServer.Entities
{
    public class Player : Entity
    {
        public ulong PrototypeId { get; set; }
        public Mission[] Missions { get; set; }
        public Quest[] Quests { get; set; }
        public ulong UnknownCollectionRepId { get; set;}
        public uint UnknownCollectionSize { get; set; }
        public ulong ShardId { get; set; }
        public ReplicatedString ReplicatedString1 { get; set; }
        public ulong Community1 { get; set; }
        public ulong Community2 { get; set; }
        public ReplicatedString ReplicatedString2 { get; set; }
        public ulong MatchQueueStatus { get; set; }
        public bool ReplicationPolicyBool { get; set; }
        public ulong DateTime { get; set; }
        public Community Community { get; set; }
        public bool Flag3 { get; set; }
        public ulong[] StashInventories { get; set; }
        public uint[] AvailableBadges { get; set; }
        public ChatChannelOption[] ChatChannelOptions { get; set; }
        public ulong[] ChatChannelOptions2 { get; set; }
        public ulong[] UnknownOptions { get; set; }
        public EquipmentInvUISlot[] EquipmentInvUISlots { get; set; }
        public AchievementState[] AchievementStates { get; set; }
        public StashTabOption[] StashTabOptions { get; set; }

        public Player(byte[] archiveData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);
            BoolDecoder boolDecoder = new();

            ReadHeader(stream);
            ReadProperties(stream);

            PrototypeId = stream.ReadPrototypeId(PrototypeEnumType.Property);

            Missions = new Mission[stream.ReadRawVarint64()];
            for (int i = 0; i < Missions.Length; i++)
                Missions[i] = new(stream, boolDecoder);
            Quests = new Quest[stream.ReadRawInt32()];
            for (int i = 0; i < Quests.Length; i++)
                Quests[i] = new(stream);

            UnknownCollectionRepId = stream.ReadRawVarint64();
            UnknownCollectionSize = stream.ReadRawUInt32();
            ShardId = stream.ReadRawVarint64();
            ReplicatedString1 = new(stream);
            Community1 = stream.ReadRawVarint64();
            Community2 = stream.ReadRawVarint64();
            ReplicatedString2 = new(stream);
            MatchQueueStatus = stream.ReadRawVarint64();

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            ReplicationPolicyBool = boolDecoder.ReadBool();

            DateTime = stream.ReadRawVarint64();
            Community = new(stream, boolDecoder);

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            Flag3 = boolDecoder.ReadBool();

            StashInventories = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < StashInventories.Length; i++)
                StashInventories[i] = stream.ReadPrototypeId(PrototypeEnumType.Property);

            AvailableBadges = new uint[stream.ReadRawVarint64()];

            ChatChannelOptions = new ChatChannelOption[stream.ReadRawVarint64()];
            for (int i = 0; i < ChatChannelOptions.Length; i++)
                ChatChannelOptions[i] = new(stream, boolDecoder);

            ChatChannelOptions2 = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < ChatChannelOptions2.Length; i++)
                ChatChannelOptions2[i] = stream.ReadPrototypeId(PrototypeEnumType.Property);

            UnknownOptions = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < UnknownOptions.Length; i++)
                UnknownOptions[i] = stream.ReadRawVarint64();

            EquipmentInvUISlots = new EquipmentInvUISlot[stream.ReadRawVarint64()];
            for (int i = 0; i < EquipmentInvUISlots.Length; i++)
                EquipmentInvUISlots[i] = new(stream);

            AchievementStates = new AchievementState[stream.ReadRawVarint64()];
            for (int i = 0; i < AchievementStates.Length; i++)
                AchievementStates[i] = new(stream);

            StashTabOptions = new StashTabOption[stream.ReadRawVarint64()];
            for (int i = 0; i < StashTabOptions.Length; i++)
                StashTabOptions[i] = new(stream);
        }

        // note: this is ugly
        public Player(ulong replicationPolicy, ulong replicationId, Property[] properties,
            ulong prototypeId, Mission[] missions, Quest[] quests, ulong unknownCollectionRepId, uint unknownCollectionSize,
            ulong shardId, ReplicatedString replicatedString1, ulong community1, ulong community2, ReplicatedString replicatedString2,
            ulong matchQueueStatus, bool replicationPolicyBool, ulong dateTime, Community community, ulong[] unknownFields)
            : base(replicationPolicy, replicationId, properties, unknownFields)
        {
            PrototypeId = prototypeId;
            Missions = missions;
            Quests = quests;
            UnknownCollectionRepId = unknownCollectionRepId;
            UnknownCollectionSize = unknownCollectionSize;
            ShardId = shardId;
            ReplicatedString1 = replicatedString1;
            Community1 = community1;
            Community2 = community2;
            ReplicatedString2 = replicatedString2;
            MatchQueueStatus = matchQueueStatus;
            ReplicationPolicyBool = replicationPolicyBool;
            DateTime = dateTime;
            Community = community;
            UnknownFields = unknownFields;
        }

        public override byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                // Prepare bool encoder
                BoolEncoder boolEncoder = new();
                byte bitBuffer;

                foreach (Mission mission in Missions)
                    boolEncoder.WriteBool(mission.BoolField);
                boolEncoder.WriteBool(ReplicationPolicyBool);
                boolEncoder.WriteBool(Community.GmBool);
                boolEncoder.WriteBool(Community.Flag3);
                boolEncoder.WriteBool(Flag3);
                foreach (ChatChannelOption option in ChatChannelOptions)
                    boolEncoder.WriteBool(option.Value);

                boolEncoder.Cook();

                // Encode
                stream.WriteRawVarint64(ReplicationPolicy);
                stream.WriteRawVarint64(ReplicationId);

                stream.WriteRawBytes(BitConverter.GetBytes(Properties.Length));
                foreach (Property property in Properties)
                    stream.WriteRawBytes(property.Encode());

                stream.WritePrototypeId(PrototypeId, PrototypeEnumType.Property);

                stream.WriteRawVarint64((ulong)Missions.Length);
                foreach (Mission mission in Missions)
                    stream.WriteRawBytes(mission.Encode(boolEncoder));

                stream.WriteRawInt32(Quests.Length);
                foreach (Quest quest in Quests)
                    stream.WriteRawBytes(quest.Encode());

                stream.WriteRawVarint64(UnknownCollectionRepId);
                stream.WriteRawUInt32(UnknownCollectionSize);
                stream.WriteRawVarint64(ShardId);
                stream.WriteRawBytes(ReplicatedString1.Encode());
                stream.WriteRawVarint64(Community1);
                stream.WriteRawVarint64(Community2);
                stream.WriteRawBytes(ReplicatedString2.Encode());
                stream.WriteRawVarint64(MatchQueueStatus);

                bitBuffer = boolEncoder.GetBitBuffer();             //ReplicationPolicyBool
                if (bitBuffer != 0) stream.WriteRawByte(bitBuffer);

                stream.WriteRawVarint64(DateTime);
                stream.WriteRawBytes(Community.Encode(boolEncoder));

                bitBuffer = boolEncoder.GetBitBuffer();             //Flag3
                if (bitBuffer != 0) stream.WriteRawByte(bitBuffer);

                stream.WriteRawVarint64((ulong)StashInventories.Length);
                foreach (ulong stashInventory in StashInventories) stream.WritePrototypeId(stashInventory, PrototypeEnumType.Property);

                stream.WriteRawVarint64((ulong)AvailableBadges.Length);
                foreach (uint badge in AvailableBadges)
                    stream.WriteRawVarint64(badge);

                stream.WriteRawVarint64((ulong)ChatChannelOptions.Length);
                foreach (ChatChannelOption option in ChatChannelOptions)
                    stream.WriteRawBytes(option.Encode(boolEncoder));

                stream.WriteRawVarint64((ulong)ChatChannelOptions2.Length);
                foreach (ulong option in ChatChannelOptions2)
                    stream.WritePrototypeId(option, PrototypeEnumType.Property);

                stream.WriteRawVarint64((ulong)UnknownOptions.Length);
                foreach (ulong option in UnknownOptions)
                    stream.WriteRawVarint64(option);

                stream.WriteRawVarint64((ulong)EquipmentInvUISlots.Length);
                foreach (EquipmentInvUISlot slot in EquipmentInvUISlots)
                    stream.WriteRawBytes(slot.Encode());

                stream.WriteRawVarint64((ulong)AchievementStates.Length);
                foreach (AchievementState state in AchievementStates)
                    stream.WriteRawBytes(state.Encode());

                stream.WriteRawVarint64((ulong)StashTabOptions.Length);
                foreach (StashTabOption option in StashTabOptions)
                    stream.WriteRawBytes(option.Encode());

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"ReplicationPolicy: 0x{ReplicationPolicy.ToString("X")}");
                streamWriter.WriteLine($"ReplicationId: 0x{ReplicationId.ToString("X")}");
                for (int i = 0; i < Properties.Length; i++) streamWriter.WriteLine($"Property{i}: {Properties[i]}");
                streamWriter.WriteLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
                for (int i = 0; i < Missions.Length; i++) streamWriter.WriteLine($"Mission{i}: {Missions[i]}");
                for (int i = 0; i < Quests.Length; i++) streamWriter.WriteLine($"Quest{i}: {Quests[i]}");

                streamWriter.WriteLine($"UnknownCollectionRepId: 0x{UnknownCollectionRepId.ToString("X")}");
                streamWriter.WriteLine($"UnknownCollectionSize: 0x{UnknownCollectionSize.ToString("X")}");
                streamWriter.WriteLine($"ShardId: 0x{ShardId.ToString("X")}");
                streamWriter.WriteLine($"ReplicatedString1: {ReplicatedString1}");
                streamWriter.WriteLine($"Community1: 0x{Community1.ToString("X")}");
                streamWriter.WriteLine($"Community2: 0x{Community2.ToString("X")}");
                streamWriter.WriteLine($"ReplicatedString2: {ReplicatedString2}");
                streamWriter.WriteLine($"MatchQueueStatus: 0x{MatchQueueStatus.ToString("X")}");
                streamWriter.WriteLine($"ReplicationPolicyBool: 0x{DateTime.ToString("X")}");
                streamWriter.WriteLine($"DateTime: 0x{DateTime.ToString("X")}");
                streamWriter.WriteLine($"Community: {Community}");

                streamWriter.WriteLine($"Flag3: {Flag3}");
                for (int i = 0; i < StashInventories.Length; i++) streamWriter.WriteLine($"StashInventory{i}: {GameDatabase.GetPrototypePath(StashInventories[i])}");
                for (int i = 0; i < AvailableBadges.Length; i++) streamWriter.WriteLine($"AvailableBadge{i}: 0x{AvailableBadges[i].ToString("X")}");

                for (int i = 0; i < ChatChannelOptions.Length; i++) streamWriter.WriteLine($"ChatChannelOption{i}: {ChatChannelOptions[i]}");

                for (int i = 0; i < ChatChannelOptions2.Length; i++) streamWriter.WriteLine($"ChatChannelOptions2_{i}: {GameDatabase.GetPrototypePath(ChatChannelOptions2[i])}");

                for (int i = 0; i < UnknownOptions.Length; i++) streamWriter.WriteLine($"UnknownOption{i}: 0x{UnknownOptions[i].ToString("X")}");

                for (int i = 0; i < EquipmentInvUISlots.Length; i++) streamWriter.WriteLine($"EquipmentInvUISlot{i}: {EquipmentInvUISlots[i]}");

                for (int i = 0; i < AchievementStates.Length; i++) streamWriter.WriteLine($"AchievementState{i}: {AchievementStates[i]}");

                for (int i = 0; i < StashTabOptions.Length; i++) streamWriter.WriteLine($"StashTabOption{i}: {StashTabOptions[i]}");

                streamWriter.Flush();

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
