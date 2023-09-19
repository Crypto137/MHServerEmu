using System;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
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
        public ReplicatedString Name { get; set; }
        public ulong ConsoleAccountId1 { get; set; }
        public ulong ConsoleAccountId2 { get; set; }
        public ReplicatedString UnkName { get; set; }
        public ulong MatchQueueStatus { get; set; }
        public bool EmailVerified { get; set; }
        public ulong AccountCreationTimestamp { get; set; }
        public ulong PartyRepId { get; set; }
        public ulong PartyId { get; set; }
        public string UnknownString { get; set; }
        public bool IsGuidInfo { get; set; } 
        public bool IsCommunity { get; set; }
        public Community Community { get; set; }
        public bool UnkBool { get; set; }
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

            ReadEntityFields(stream);

            PrototypeId = stream.ReadPrototypeId(PrototypeEnumType.All);

            Missions = new Mission[stream.ReadRawVarint64()];
            for (int i = 0; i < Missions.Length; i++)
                Missions[i] = new(stream, boolDecoder);
            Quests = new Quest[stream.ReadRawInt32()];
            for (int i = 0; i < Quests.Length; i++)
                Quests[i] = new(stream);

            UnknownCollectionRepId = stream.ReadRawVarint64();
            UnknownCollectionSize = stream.ReadRawUInt32();

            ShardId = stream.ReadRawVarint64();
            Name = new(stream);
            ConsoleAccountId1 = stream.ReadRawVarint64();
            ConsoleAccountId2 = stream.ReadRawVarint64();
            UnkName = new(stream);
            MatchQueueStatus = stream.ReadRawVarint64();

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            EmailVerified = boolDecoder.ReadBool();

            AccountCreationTimestamp = stream.ReadRawVarint64();

            PartyRepId = stream.ReadRawVarint64();
            PartyId = stream.ReadRawVarint64();
            
            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            IsGuidInfo = boolDecoder.ReadBool();
            if (IsGuidInfo) // GuildMember::SerializeReplicationRuntimeInfo
            {
                throw new("IsGuidInfo parsing not implemented.");
                // ulong GuildId
                // string GuildList
                // int GuildMembership
            }

            UnknownString = stream.ReadRawString();

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            IsCommunity = boolDecoder.ReadBool();
            if (IsCommunity)
                Community = new(stream);

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            UnkBool = boolDecoder.ReadBool();

            StashInventories = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < StashInventories.Length; i++)
                StashInventories[i] = stream.ReadPrototypeId(PrototypeEnumType.All);

            AvailableBadges = new uint[stream.ReadRawVarint64()];

            ChatChannelOptions = new ChatChannelOption[stream.ReadRawVarint64()];
            for (int i = 0; i < ChatChannelOptions.Length; i++)
                ChatChannelOptions[i] = new(stream, boolDecoder);

            ChatChannelOptions2 = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < ChatChannelOptions2.Length; i++)
                ChatChannelOptions2[i] = stream.ReadPrototypeId(PrototypeEnumType.All);

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
        public Player(uint replicationPolicy, ReplicatedPropertyCollection propertyCollection,
            ulong prototypeId, Mission[] missions, Quest[] quests, 
            ulong shardId, ReplicatedString playerName, ReplicatedString unkName,
            ulong matchQueueStatus, bool emailVerified, ulong accountCreationTimestamp, 
            Community community, ulong[] unknownFields)
            : base(replicationPolicy, propertyCollection, unknownFields)
        {
            PrototypeId = prototypeId;
            Missions = missions;
            Quests = quests;
            UnknownCollectionRepId = 0;
            UnknownCollectionSize = 0;
            ShardId = shardId;
            Name = playerName;
            ConsoleAccountId1 = 0;
            ConsoleAccountId2 = 0;
            UnkName = unkName;
            MatchQueueStatus = matchQueueStatus;
            EmailVerified = emailVerified;
            AccountCreationTimestamp = accountCreationTimestamp;
            Community = community;
            UnknownFields = unknownFields;
        }

        public override byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                // Prepare bool encoder
                BoolEncoder boolEncoder = new();
                byte bitBuffer;

                foreach (Mission mission in Missions)
                    boolEncoder.WriteBool(mission.BoolField);
                boolEncoder.WriteBool(EmailVerified);
                boolEncoder.WriteBool(IsGuidInfo);
                boolEncoder.WriteBool(IsCommunity);
                boolEncoder.WriteBool(UnkBool);
                foreach (ChatChannelOption option in ChatChannelOptions)
                    boolEncoder.WriteBool(option.Value);

                boolEncoder.Cook();

                // Encode
                WriteEntityFields(cos);

                cos.WritePrototypeId(PrototypeId, PrototypeEnumType.All);

                cos.WriteRawVarint64((ulong)Missions.Length);
                foreach (Mission mission in Missions)
                    cos.WriteRawBytes(mission.Encode(boolEncoder));

                cos.WriteRawInt32(Quests.Length);
                foreach (Quest quest in Quests)
                    cos.WriteRawBytes(quest.Encode());

                cos.WriteRawVarint64(UnknownCollectionRepId);
                cos.WriteRawUInt32(UnknownCollectionSize);
                cos.WriteRawVarint64(ShardId);
                cos.WriteRawBytes(Name.Encode());
                cos.WriteRawVarint64(ConsoleAccountId1);
                cos.WriteRawVarint64(ConsoleAccountId2);
                cos.WriteRawBytes(UnkName.Encode());
                cos.WriteRawVarint64(MatchQueueStatus);

                bitBuffer = boolEncoder.GetBitBuffer();             // EmailVerified
                if (bitBuffer != 0) cos.WriteRawByte(bitBuffer);

                cos.WriteRawVarint64(AccountCreationTimestamp);

                cos.WriteRawVarint64(PartyRepId);
                cos.WriteRawVarint64(PartyId);

                bitBuffer = boolEncoder.GetBitBuffer();             // IsGuidInfo
                if (bitBuffer != 0) cos.WriteRawByte(bitBuffer);

                cos.WriteRawString(UnknownString);

                bitBuffer = boolEncoder.GetBitBuffer();             // IsCommunity
                if (bitBuffer != 0)
                {
                    cos.WriteRawByte(bitBuffer);
                    cos.WriteRawBytes(Community.Encode());
                }

                bitBuffer = boolEncoder.GetBitBuffer();             // UnkBool
                if (bitBuffer != 0) cos.WriteRawByte(bitBuffer);

                cos.WriteRawVarint64((ulong)StashInventories.Length);
                foreach (ulong stashInventory in StashInventories) cos.WritePrototypeId(stashInventory, PrototypeEnumType.All);

                cos.WriteRawVarint64((ulong)AvailableBadges.Length);
                foreach (uint badge in AvailableBadges)
                    cos.WriteRawVarint64(badge);

                cos.WriteRawVarint64((ulong)ChatChannelOptions.Length);
                foreach (ChatChannelOption option in ChatChannelOptions)
                    cos.WriteRawBytes(option.Encode(boolEncoder));

                cos.WriteRawVarint64((ulong)ChatChannelOptions2.Length);
                foreach (ulong option in ChatChannelOptions2)
                    cos.WritePrototypeId(option, PrototypeEnumType.All);

                cos.WriteRawVarint64((ulong)UnknownOptions.Length);
                foreach (ulong option in UnknownOptions)
                    cos.WriteRawVarint64(option);

                cos.WriteRawVarint64((ulong)EquipmentInvUISlots.Length);
                foreach (EquipmentInvUISlot slot in EquipmentInvUISlots)
                    cos.WriteRawBytes(slot.Encode());

                cos.WriteRawVarint64((ulong)AchievementStates.Length);
                foreach (AchievementState state in AchievementStates)
                    cos.WriteRawBytes(state.Encode());

                cos.WriteRawVarint64((ulong)StashTabOptions.Length);
                foreach (StashTabOption option in StashTabOptions)
                    cos.WriteRawBytes(option.Encode());

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            WriteEntityString(sb);

            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
            for (int i = 0; i < Missions.Length; i++) sb.AppendLine($"Mission{i}: {Missions[i]}");
            for (int i = 0; i < Quests.Length; i++) sb.AppendLine($"Quest{i}: {Quests[i]}");
            sb.AppendLine($"UnknownCollectionRepId: 0x{UnknownCollectionRepId:X}");
            sb.AppendLine($"UnknownCollectionSize: 0x{UnknownCollectionSize:X}");
            sb.AppendLine($"ShardId: {ShardId}");
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"ConsoleAccountId1: 0x{ConsoleAccountId1:X}");
            sb.AppendLine($"ConsoleAccountId2: 0x{ConsoleAccountId2:X}");
            sb.AppendLine($"UnkName: {UnkName}");
            sb.AppendLine($"MatchQueueStatus: 0x{MatchQueueStatus:X}");
            sb.AppendLine($"EmailVerified: {EmailVerified}");
            sb.AppendLine($"AccountCreationTimestamp: 0x{AccountCreationTimestamp:X}");
            sb.AppendLine($"IsGuidInfo: {IsGuidInfo}");
            sb.AppendLine($"UnknownString: {UnknownString}");
            sb.AppendLine($"IsCommunity: {IsCommunity}");
            sb.AppendLine($"Community: {Community}");
            sb.AppendLine($"UnkBool: {UnkBool}");
            for (int i = 0; i < StashInventories.Length; i++) sb.AppendLine($"StashInventory{i}: {GameDatabase.GetPrototypePath(StashInventories[i])}");
            for (int i = 0; i < AvailableBadges.Length; i++) sb.AppendLine($"AvailableBadge{i}: 0x{AvailableBadges[i]:X}");
            for (int i = 0; i < ChatChannelOptions.Length; i++) sb.AppendLine($"ChatChannelOption{i}: {ChatChannelOptions[i]}");
            for (int i = 0; i < ChatChannelOptions2.Length; i++) sb.AppendLine($"ChatChannelOptions2_{i}: {GameDatabase.GetPrototypePath(ChatChannelOptions2[i])}");
            for (int i = 0; i < UnknownOptions.Length; i++) sb.AppendLine($"UnknownOption{i}: 0x{UnknownOptions[i]:X}");
            for (int i = 0; i < EquipmentInvUISlots.Length; i++) sb.AppendLine($"EquipmentInvUISlot{i}: {EquipmentInvUISlots[i]}");
            for (int i = 0; i < AchievementStates.Length; i++) sb.AppendLine($"AchievementState{i}: {AchievementStates[i]}");
            for (int i = 0; i < StashTabOptions.Length; i++) sb.AppendLine($"StashTabOption{i}: {StashTabOptions[i]}");

            return sb.ToString();
        }
    }
}
