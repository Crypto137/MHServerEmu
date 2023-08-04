using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class Player
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public ulong Header { get; set; }
        public ulong RepId { get; set; }
        public Property[] Properties { get; set; }
        public ulong EnumValue { get; set; }
        public Mission[] Missions { get; set; }
        public ulong[] MissionFields { get; set; }
        public Quest[] Quests { get; set; }
        public ulong[] UnknownFields { get; set; }

        public Player(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            Header = stream.ReadRawVarint64();
            RepId = stream.ReadRawVarint64();

            Properties = new Property[BitConverter.ToUInt32(stream.ReadRawBytes(4))];
            for (int i = 0; i < Properties.Length; i++)
            {
                ulong id = stream.ReadRawVarint64();
                ulong value = stream.ReadRawVarint64();
                Properties[i] = new(id, value);
            }

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

            List<ulong> fieldList = new();
            while (!stream.IsAtEnd)
            {
                fieldList.Add(stream.ReadRawVarint64());
            }

            UnknownFields = fieldList.ToArray();
        }

        public Player(ulong header, ulong repId, Property[] properties, ulong enumValue, Mission[] missions, ulong[] missionFields, Quest[] quests, ulong[] unknownFields)
        {
            Header = header;
            RepId = repId;
            Properties = properties;
            EnumValue = enumValue;
            Missions = missions;
            MissionFields = missionFields;
            Quests = quests;
            UnknownFields = unknownFields;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(Header);
                stream.WriteRawVarint64(RepId);

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
                streamWriter.WriteLine($"Header: 0x{Header.ToString("X")}");
                streamWriter.WriteLine($"RepId: 0x{RepId.ToString("X")}");
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
