using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities
{
    public class EnterGameWorldArchive
    {
        // examples
        // player 01B2F8FD06A021F0A301BC40902E9103BC05000001
        // waypoint 010C028043E06BD82AC801
        // something with 0xA0 01BEF8FD06A001B6A501D454902E0094050000

        // known flag values: 0x02 == mini (waypoint, etc), 0x10A0 == players, 0xA0 == ??

        private static readonly Logger Logger = LogManager.CreateLogger();

        public ulong ReplicationPolicy { get; }
        public ulong EntityId { get; set; }
        public int LocFlags { get; set; }     // Contains bits that determine fields
        public int LocMsgFlags { get; set; }
        public ulong EnumEntityPrototype { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Orientation { get; set; }
        public ulong LocBitset { get; set; }
        public uint LocField8 { get; set; }
        public float LocField9 { get; set; }    // previously called LocomotionState
        public uint LocField10 { get; set; }
        public ulong LocField11 { get; set; }
        public Vector2 LocField12 { get; set; }
        public uint LocNaviPath { get; set; }
        public LocomotionPathNode[] LocomotionPathNodes { get; set; } = Array.Empty<LocomotionPathNode>();
        public uint UnknownSetting { get; set; }

        public EnterGameWorldArchive(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            ReplicationPolicy = stream.ReadRawVarint64();
            EntityId = stream.ReadRawVarint64();

            LocFlags = (int)stream.ReadRawVarint64();
            LocMsgFlags = LocFlags >> 12;

            if ((LocFlags & 0x800) > 0) EnumEntityPrototype = stream.ReadRawVarint64();

            Position = new(stream.ReadRawFloat(3),
                stream.ReadRawFloat(3),
                stream.ReadRawFloat(3));

            if ((LocFlags & 0x1) > 0)
            {
                Orientation = new(stream.ReadRawFloat(6), stream.ReadRawFloat(6), stream.ReadRawFloat(6));
            }
            else
            {
                Orientation = new(stream.ReadRawFloat(6), 0f, 0f);
            }

            if ((LocFlags & 0x2) == 0)
            {
                if ((LocFlags & 0x8) > 0) LocBitset = stream.ReadRawVarint64();
                if ((LocFlags & 0x10) > 0) LocField8 = stream.ReadRawVarint32();
                if ((LocFlags & 0x80) > 0) LocField9 = stream.ReadRawFloat(0);
                if ((LocFlags & 0x100) > 0) LocField10 = stream.ReadRawVarint32();
                if ((LocFlags & 0x200) > 0) LocField11 = stream.ReadRawVarint64();
                if ((LocFlags & 0x400) > 0) LocField12 = new(stream.ReadRawFloat(0), stream.ReadRawFloat(0));

                if ((LocFlags & 0x20) > 0)
                {
                    LocNaviPath = stream.ReadRawVarint32();
                    LocomotionPathNodes = new LocomotionPathNode[stream.ReadRawVarint64()];
                    for (int i = 0; i < LocomotionPathNodes.Length; i++)
                        LocomotionPathNodes[i] = new(stream);
                }
            }

            if ((LocMsgFlags & 0x1) > 0) UnknownSetting = stream.ReadRawVarint32();

            if (stream.IsAtEnd == false) Logger.Warn("Archive contains unknown fields!");
        }

        public EnterGameWorldArchive(ulong entityId, Vector3 position, float orientation)
        {
            ReplicationPolicy = 0x01;
            EntityId = entityId;
            LocFlags = 0x02;
            Position = position;
            Orientation = new(orientation, 0f, 0f);
        }

        public EnterGameWorldArchive(ulong entityId, Vector3 position, float orientation, float locField9)
        {
            ReplicationPolicy = 0x01;
            EntityId = entityId;
            LocFlags = 0x10A0;
            Position = position;
            Orientation = new(orientation, 0f, 0f);
            LocField9 = locField9;

            LocNaviPath = 0;
            LocomotionPathNodes = new LocomotionPathNode[0];
            UnknownSetting = 1;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(ReplicationPolicy);
                stream.WriteRawVarint64(EntityId);
                stream.WriteRawVarint64((ulong)LocFlags);

                if ((LocFlags & 0x800) > 0) stream.WriteRawVarint64(EnumEntityPrototype);
                stream.WriteRawBytes(Position.Encode(3));

                if ((LocFlags & 0x1) > 0)
                    stream.WriteRawBytes(Orientation.Encode(6));
                else
                    stream.WriteRawFloat(Orientation.X, 6);

                if ((LocFlags & 0x2) == 0)
                {
                    if ((LocFlags & 0x8) > 0) stream.WriteRawVarint64(LocBitset);
                    if ((LocFlags & 0x10) > 0) stream.WriteRawVarint32(LocField8);
                    if ((LocFlags & 0x80) > 0) stream.WriteRawFloat(LocField9, 0);
                    if ((LocFlags & 0x100) > 0) stream.WriteRawVarint32(LocField10);
                    if ((LocFlags & 0x200) > 0) stream.WriteRawVarint64(LocField11);
                    if ((LocFlags & 0x400) > 0) stream.WriteRawBytes(LocField12.Encode(0));

                    if ((LocFlags & 0x20) > 0)
                    {
                        stream.WriteRawVarint32(LocNaviPath);
                        stream.WriteRawVarint64((ulong)LocomotionPathNodes.Length);
                        foreach (LocomotionPathNode naviVector in LocomotionPathNodes) stream.WriteRawBytes(naviVector.Encode());
                    }
                }

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
                streamWriter.WriteLine($"LocFlags: 0x{LocFlags.ToString("X")}");
                streamWriter.WriteLine($"EnumEntityPrototype: 0x{EnumEntityPrototype.ToString("X")}");
                streamWriter.WriteLine($"Position: {Position}");
                streamWriter.WriteLine($"Orientation: {Orientation}");
                streamWriter.WriteLine($"LocBitSet: 0x{LocBitset.ToString("X")}");
                streamWriter.WriteLine($"LocField8: 0x{LocField8.ToString("X")}");
                streamWriter.WriteLine($"LocField9: {LocField9}");
                streamWriter.WriteLine($"LocField10: 0x{LocField10.ToString("X")}");
                streamWriter.WriteLine($"LocField11: 0x{LocField11.ToString("X")}");
                streamWriter.WriteLine($"LocField12: {LocField12}");
                streamWriter.WriteLine($"LocNaviPath: 0x{LocNaviPath.ToString("X")}");
                for (int i = 0; i < LocomotionPathNodes.Length; i++) streamWriter.WriteLine($"LocomotionPathNode{i}: {LocomotionPathNodes[i]}");
                streamWriter.WriteLine($"UnknownSetting: 0x{UnknownSetting.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
