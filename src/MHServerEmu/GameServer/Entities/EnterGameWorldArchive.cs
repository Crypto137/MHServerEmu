using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities
{
    public class EnterGameWorldArchive
    {
        // examples
        // player 01B2F8FD06A021F0A301BC40902E9103BC05000001
        // waypoint 010C028043E06BD82AC801
        // something with 0xA0 01BEF8FD06A001B6A501D454902E0094050000

        // known FieldFlags: 0x02 == mini (waypoint, etc), 0x10A0 == players, 0xA0 == ??

        private static readonly Logger Logger = LogManager.CreateLogger();

        public ulong ReplicationPolicy { get; }
        public ulong EntityId { get; set; }
        public uint FieldFlags { get; set; }
        public uint LocMsgFlags { get; set; }
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

            FieldFlags = stream.ReadRawVarint32();
            LocMsgFlags = FieldFlags >> 12;

            if ((FieldFlags & 0x800) > 0) EnumEntityPrototype = stream.ReadRawVarint64();

            Position = new(stream.ReadRawFloat(3),
                stream.ReadRawFloat(3),
                stream.ReadRawFloat(3));

            if ((FieldFlags & 0x1) > 0)
                Orientation = new(stream.ReadRawFloat(6), stream.ReadRawFloat(6), stream.ReadRawFloat(6));
            else
                Orientation = new(stream.ReadRawFloat(6), 0f, 0f);

            if ((FieldFlags & 0x2) == 0) LocomotionState = new(stream, FieldFlags);
            if ((LocMsgFlags & 0x1) > 0) UnknownSetting = stream.ReadRawVarint32();

            if (stream.IsAtEnd == false) Logger.Warn("Archive contains unknown fields!");
        }

        public EnterGameWorldArchive(ulong entityId, Vector3 position, float orientation)
        {
            ReplicationPolicy = 0x01;
            EntityId = entityId;
            FieldFlags = 0x02;
            Position = position;
            Orientation = new(orientation, 0f, 0f);
        }

        public EnterGameWorldArchive(ulong entityId, Vector3 position, float orientation, float moveSpeed)
        {
            ReplicationPolicy = 0x01;
            EntityId = entityId;
            FieldFlags = 0x10A0;
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
                stream.WriteRawVarint64((ulong)FieldFlags);

                if ((FieldFlags & 0x800) > 0) stream.WriteRawVarint64(EnumEntityPrototype);
                stream.WriteRawBytes(Position.Encode(3));

                if ((FieldFlags & 0x1) > 0)
                    stream.WriteRawBytes(Orientation.Encode(6));
                else
                    stream.WriteRawFloat(Orientation.X, 6);

                if ((FieldFlags & 0x2) == 0) stream.WriteRawBytes(LocomotionState.Encode(FieldFlags));
                if ((LocMsgFlags & 0x1) > 0) stream.WriteRawVarint32(UnknownSetting);

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
                streamWriter.WriteLine($"FieldFlags: 0x{FieldFlags.ToString("X")}");
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
