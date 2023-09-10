using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Entities.Locomotion;

namespace MHServerEmu.GameServer.Entities
{
    public class EnterGameWorldArchive
    {
        // examples
        // player 01B2F8FD06A021F0A301BC40902E9103BC05000001
        // waypoint 010C028043E06BD82AC801
        // something with 0xA0 01BEF8FD06A001B6A501D454902E0094050000
        // known Flags: 0x02 == mini (waypoint, etc), 0x10A0 == players, 0xA0 == ??

        private const int FieldFlagCount = 16;  // keep flag count a bit higher than we need just in case so we don't miss anything

        private static readonly Logger Logger = LogManager.CreateLogger();

        public ulong ReplicationPolicy { get; }
        public ulong EntityId { get; set; }
        public bool[] Flags { get; set; }   // mystery flags: 2, 6
        public ulong EnumEntityPrototype { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Orientation { get; set; }
        public LocomotionState LocomotionState { get; set; }
        public uint UnknownSetting { get; set; }

        public EnterGameWorldArchive(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            ReplicationPolicy = stream.ReadRawVarint64();
            EntityId = stream.ReadRawVarint64();

            Flags = stream.ReadRawVarint32().ToBoolArray(FieldFlagCount);
            //LocMsgFlags = Flags >> 12;

            if (Flags[11]) EnumEntityPrototype = stream.ReadRawVarint64();

            Position = new(stream, 3);

            if (Flags[0])
                Orientation = new(stream.ReadRawFloat(6), stream.ReadRawFloat(6), stream.ReadRawFloat(6));
            else
                Orientation = new(stream.ReadRawFloat(6), 0f, 0f);

            if (Flags[1] == false) LocomotionState = new(stream, Flags);
            if (Flags[12]) UnknownSetting = stream.ReadRawVarint32();          // LocMsgFlags[0]

            if (stream.IsAtEnd == false) Logger.Warn("Archive contains unknown fields!");
        }

        public EnterGameWorldArchive(ulong entityId, Vector3 position, float orientation)
        {
            ReplicationPolicy = 0x01;
            EntityId = entityId;
            Flags = 0x02u.ToBoolArray(FieldFlagCount);
            Position = position;
            Orientation = new(orientation, 0f, 0f);
        }

        public EnterGameWorldArchive(ulong entityId, Vector3 position, float orientation, float moveSpeed)
        {
            ReplicationPolicy = 0x01;
            EntityId = entityId;
            Flags = 0x10A0u.ToBoolArray(FieldFlagCount);
            Position = position;
            Orientation = new(orientation, 0f, 0f);
            LocomotionState = new(moveSpeed);
            UnknownSetting = 1;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(ReplicationPolicy);
                stream.WriteRawVarint64(EntityId);
                stream.WriteRawVarint32(Flags.ToUInt32());

                if (Flags[11]) stream.WriteRawVarint64(EnumEntityPrototype);
                stream.WriteRawBytes(Position.Encode(3));

                if (Flags[0])
                    stream.WriteRawBytes(Orientation.Encode(6));
                else
                    stream.WriteRawFloat(Orientation.X, 6);

                if (Flags[1] == false) stream.WriteRawBytes(LocomotionState.Encode(Flags));
                if (Flags[12]) stream.WriteRawVarint32(UnknownSetting);

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
                streamWriter.WriteLine($"EntityId: 0x{EntityId.ToString("X")}");
                for (int i = 0; i < Flags.Length; i++) streamWriter.WriteLine($"Flag{i}: {Flags[i]}");
                streamWriter.WriteLine($"EnumEntityPrototype: 0x{EnumEntityPrototype.ToString("X")}");
                streamWriter.WriteLine($"Position: {Position}");
                streamWriter.WriteLine($"Orientation: {Orientation}");
                streamWriter.WriteLine($"LocomotionState: {LocomotionState}");
                streamWriter.WriteLine($"UnknownSetting: 0x{UnknownSetting.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
