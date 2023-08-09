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
        public ulong[] MissionFields { get; set; }
        public Quest[] Quests { get; set; }
        public ulong[] UnknownFields { get; set; }

        public Player(byte[] archiveData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);

            ReadHeader(stream);
            ReadProperties(stream);

            EnumValue = stream.ReadRawVarint64();

            Missions = new Mission[stream.ReadRawVarint64()];

            MissionFields = new ulong[4664];    // hardcoded size for skipping
            for (int i = 0; i < MissionFields.Length; i++)
            {
                MissionFields[i] = stream.ReadRawVarint64();
            }

            ulong questsSize = stream.ReadRawVarint64();
            Quests = new Quest[questsSize / 2];     // quest array size is 9 instead of 18?
            for (int i = 0; i < Quests.Length; i++)
            {
                ulong prototypeId = stream.ReadRawVarint64();
                ulong[] fields = new ulong[stream.ReadRawVarint64()];
                for (int j = 0; j < fields.Length; j++)
                {
                    fields[j] = stream.ReadRawVarint64();
                }

                Quests[i] = new(prototypeId, fields);
            }

            ReadUnknownFields(stream);
        }

        public Player(ulong replicationPolicy, ulong replicationId, Property[] properties,
            ulong enumValue, Mission[] missions, ulong[] missionFields, Quest[] quests, ulong[] unknownFields)
            : base(replicationPolicy, replicationId, properties, unknownFields)
        {
            EnumValue = enumValue;
            Missions = missions;
            MissionFields = missionFields;
            Quests = quests;
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
                foreach (ulong field in MissionFields) stream.WriteRawVarint64(field);

                stream.WriteRawVarint64((ulong)(Quests.Length * 2));
                foreach (Quest quest in Quests) stream.WriteRawBytes(quest.Encode());

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
                for (int i = 0; i < MissionFields.Length; i++) streamWriter.WriteLine($"MissionField{i}: 0x{MissionFields[i].ToString("X")}");

                for (int i = 0; i < Quests.Length; i++) streamWriter.WriteLine($"Quest{i}: {Quests[i]}");

                for (int i = 0; i < UnknownFields.Length; i++) streamWriter.WriteLine($"UnknownField{i}: 0x{UnknownFields[i].ToString("X")}");

                streamWriter.Flush();

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
