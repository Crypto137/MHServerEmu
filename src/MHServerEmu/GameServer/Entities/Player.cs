using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Entities.Archives;

namespace MHServerEmu.GameServer.Entities
{
    public class Player : Entity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public ulong EnumValue { get; set; }
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

        public Player(byte[] archiveData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);
            BoolBuffer boolBuffer = new();

            ReadHeader(stream);
            ReadProperties(stream);

            EnumValue = stream.ReadRawVarint64();

            ReadMissions(stream, boolBuffer);
            ReadQuests(stream);

            UnknownCollectionRepId = stream.ReadRawVarint64();
            UnknownCollectionSize = stream.ReadRawUInt32();
            ShardId = stream.ReadRawVarint64();
            ReplicatedString1 = new(stream.ReadRawVarint64(), stream.ReadRawString());
            Community1 = stream.ReadRawVarint64();
            Community2 = stream.ReadRawVarint64();
            ReplicatedString2 = new(stream.ReadRawVarint64(), stream.ReadRawString());
            MatchQueueStatus = stream.ReadRawVarint64();

            if (boolBuffer.IsEmpty())
                boolBuffer.SetBits(stream.ReadRawByte());
            ReplicationPolicyBool = boolBuffer.ReadBool();

            DateTime = stream.ReadRawVarint64();
            Community = new(stream, boolBuffer);

            ReadUnknownFields(stream);
        }

        // note: this is ugly
        public Player(ulong replicationPolicy, ulong replicationId, Property[] properties,
            ulong enumValue, Mission[] missions, Quest[] quests, ulong unknownCollectionRepId, uint unknownCollectionSize,
            ulong shardId, ReplicatedString replicatedString1, ulong community1, ulong community2, ReplicatedString replicatedString2,
            ulong matchQueueStatus, bool replicationPolicyBool, ulong dateTime, Community community, ulong[] unknownFields)
            : base(replicationPolicy, replicationId, properties, unknownFields)
        {
            EnumValue = enumValue;
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

                stream.WriteRawVarint64(ReplicationPolicy);
                stream.WriteRawVarint64(ReplicationId);

                stream.WriteRawBytes(BitConverter.GetBytes(Properties.Length));
                foreach (Property property in Properties) stream.WriteRawBytes(property.Encode());

                stream.WriteRawVarint64(EnumValue);

                stream.WriteRawVarint64((ulong)Missions.Length);
                //foreach (ulong field in MissionFields) stream.WriteRawVarint64(field);

                stream.WriteRawVarint64((ulong)(Quests.Length << 1));
                foreach (Quest quest in Quests) stream.WriteRawBytes(quest.Encode());

                stream.WriteRawVarint64(UnknownCollectionRepId);
                stream.WriteRawUInt32(UnknownCollectionSize);
                stream.WriteRawVarint64(ShardId);
                stream.WriteRawBytes(ReplicatedString1.Encode());
                stream.WriteRawVarint64(Community1);
                stream.WriteRawVarint64(Community2);
                stream.WriteRawBytes(ReplicatedString2.Encode());
                stream.WriteRawVarint64(MatchQueueStatus);
                stream.WriteRawVarint64(DateTime);
                stream.WriteRawBytes(Community.Encode());

                foreach (ulong field in UnknownFields) stream.WriteRawVarint64(field);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"Header: 0x{ReplicationPolicy.ToString("X")}");
                streamWriter.WriteLine($"RepId: 0x{ReplicationId.ToString("X")}");
                for (int i = 0; i < Properties.Length; i++) streamWriter.WriteLine($"Property{i}: {Properties[i]}");
                streamWriter.WriteLine($"EnumValue: 0x{EnumValue.ToString("X")}");
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

                for (int i = 0; i < UnknownFields.Length; i++) streamWriter.WriteLine($"UnknownField{i}: 0x{UnknownFields[i].ToString("X")}");

                streamWriter.Flush();

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        private void ReadMissions(CodedInputStream stream, BoolBuffer boolBuffer)
        {
            Missions = new Mission[stream.ReadRawVarint64()];
            //MissionFields = new ulong[4664];    // hardcoded size for skipping
            for (int i = 0; i < Missions.Length; i++)
            {
                Missions[i] = new(stream, boolBuffer);
            }
        }

        private void ReadQuests(CodedInputStream stream)
        {
            Quests = new Quest[stream.ReadRawVarint64() >> 1];
            for (int i = 0; i < Quests.Length; i++)
            {
                Quests[i] = new(stream);
            }
        }
    }
}
