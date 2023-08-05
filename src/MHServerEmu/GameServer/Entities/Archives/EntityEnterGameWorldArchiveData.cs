using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class EntityEnterGameWorldArchiveData
    {
        // TODO: field interpretation based on flag

        // player 01B2F8FD06A021F0A301BC40902E9103BC05000001
        // waypoint 010C028043E06BD82AC801
        // something with 0xA0 01BEF8FD06A001B6A501D454902E0094050000
        private static readonly Logger Logger = LogManager.CreateLogger();

        public ulong Header { get; }
        public ulong EntityId { get; set; }
        public ulong Flag { get; set; }     // 0x02 == mini (waypoint, etc), 0x10A0 == players, 0xA0 == ??
        public Vector3 Position { get; set; }
        public Vector3 Orientation { get; set; }
        public float LocomotionState { get; set; }
        public ulong[] UnknownFields { get; } = Array.Empty<ulong>();

        public EntityEnterGameWorldArchiveData(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            Header = stream.ReadRawVarint64();
            EntityId = stream.ReadRawVarint64();

            Flag = stream.ReadRawVarint64();

            Position = new(stream.ReadRawVarint32().ZigZagDecode32(3),
                stream.ReadRawVarint32().ZigZagDecode32(3),
                stream.ReadRawVarint32().ZigZagDecode32(3));

            Orientation = new(stream.ReadRawVarint32().ZigZagDecode32(6), 0f, 0f);

            if (Flag == 0x10A0)
            {
                LocomotionState = stream.ReadRawVarint32().ZigZagDecode32(0);
            }

            List<ulong> fieldList = new();
            while (!stream.IsAtEnd)
            {
                fieldList.Add(stream.ReadRawVarint64());
            }

            UnknownFields = fieldList.ToArray();
        }

        public EntityEnterGameWorldArchiveData(ulong entityId, Vector3 position, float orientation)
        {
            Header = 0x01;
            EntityId = entityId;
            Flag = 0x02;
            Position = position;
            Orientation = new(orientation, 0f, 0f);
        }

        public EntityEnterGameWorldArchiveData(ulong entityId, Vector3 position, float orientation, float locomotionState)
        {
            Header = 0x01;
            EntityId = entityId;
            Flag = 0x10A0;
            Position = position;
            Orientation = new(orientation, 0f, 0f);
            LocomotionState = locomotionState;
            UnknownFields = new ulong[] { 0x00, 0x00, 0x01 };
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(Header);
                stream.WriteRawVarint64(EntityId);
                stream.WriteRawVarint64(Flag);

                stream.WriteRawVarint32(Position.X.ZigZagEncode32(3));
                stream.WriteRawVarint32(Position.Y.ZigZagEncode32(3));
                stream.WriteRawVarint32(Position.Z.ZigZagEncode32(3));
                stream.WriteRawVarint32(Orientation.X.ZigZagEncode32(6));

                if (Flag != 0x02) stream.WriteRawVarint32(LocomotionState.ZigZagEncode32(0));

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
                streamWriter.WriteLine($"EntityId: 0x{EntityId.ToString("X")}");
                streamWriter.WriteLine($"Flag: 0x{Flag.ToString("X")}");
                streamWriter.WriteLine($"Position: {Position}");
                streamWriter.WriteLine($"Orientation: {Orientation}");
                streamWriter.WriteLine($"LocomotionState: {LocomotionState}");
                for (int i = 0; i < UnknownFields.Length; i++) streamWriter.WriteLine($"UnknownField{i}: 0x{UnknownFields[i].ToString("X")} ({((uint)UnknownFields[i]).ZigZagDecode32(6)})");

                streamWriter.Flush();

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
