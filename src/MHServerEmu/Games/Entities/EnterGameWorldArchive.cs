using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities
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

        public uint ReplicationPolicy { get; }
        public ulong EntityId { get; set; }
        public bool[] Flags { get; set; }   // mystery flags: 2, 6
        public ulong PrototypeId { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Orientation { get; set; }
        public LocomotionState LocomotionState { get; set; }
        public uint UnknownSetting { get; set; }

        public EnterGameWorldArchive(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());

            ReplicationPolicy = stream.ReadRawVarint32();
            EntityId = stream.ReadRawVarint64();

            Flags = stream.ReadRawVarint32().ToBoolArray(FieldFlagCount);
            //LocMsgFlags = Flags >> 12;

            if (Flags[11]) PrototypeId = stream.ReadPrototypeEnum(PrototypeEnumType.Entity);

            Position = new(stream, 3);

            if (Flags[0])
                Orientation = new(stream.ReadRawZigZagFloat(6), stream.ReadRawZigZagFloat(6), stream.ReadRawZigZagFloat(6));
            else
                Orientation = new(stream.ReadRawZigZagFloat(6), 0f, 0f);

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

        public ByteString Serialize()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(ReplicationPolicy);
                cos.WriteRawVarint64(EntityId);
                cos.WriteRawVarint32(Flags.ToUInt32());

                if (Flags[11]) cos.WritePrototypeEnum(PrototypeId, PrototypeEnumType.Entity);
                Position.Encode(cos, 3);

                if (Flags[0])
                    Orientation.Encode(cos, 6);
                else
                    cos.WriteRawZigZagFloat(Orientation.X, 6);

                if (Flags[1] == false) LocomotionState.Encode(cos, Flags);
                if (Flags[12]) cos.WriteRawVarint32(UnknownSetting);

                cos.Flush();
                return ByteString.CopyFrom(ms.ToArray());
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationPolicy: 0x{ReplicationPolicy:X}");
            sb.AppendLine($"EntityId: {EntityId}");

            sb.Append("Flags: ");
            for (int i = 0; i < Flags.Length; i++) if (Flags[i]) sb.Append($"{i} ");
            sb.AppendLine();

            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypeName(PrototypeId)}");
            sb.AppendLine($"Position: {Position}");
            sb.AppendLine($"Orientation: {Orientation}");
            sb.AppendLine($"LocomotionState: {LocomotionState}");
            sb.AppendLine($"UnknownSetting: 0x{UnknownSetting:X}");
            return sb.ToString();
        }
    }
}
