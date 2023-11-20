using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities
{
    public class EntityBaseData
    {
        private const int FlagCount = 16;  // keep flag count a bit higher than we need just in case so we don't miss anything

        // Note: in old client builds (July 2014 and earlier) this used to be a protobuf message with a lot of fields.
        // It was probably converted to an archive for optimization reasons.
        public AoiNetworkPolicyValues ReplicationPolicy { get; set; }
        public ulong EntityId { get; set; }
        public PrototypeId PrototypeId { get; set; }
        public bool[] Flags { get; set; }         // mystery flags: 2, 10, 12, 13
        public LocomotionMessageFlags LocoFieldFlags { get; set; }
        public uint InterestPolicies { get; set; }
        public uint AvatarWorldInstanceId { get; set; }
        public uint DbId { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Orientation { get; set; }
        public LocomotionState LocomotionState { get; set; }
        public float BoundsScaleOverride { get; }
        public ulong SourceEntityId { get; }
        public Vector3 SourcePosition { get; }
        public PrototypeId ActivePowerPrototypeId { get; }
        public InventoryLocation InvLoc { get; set; }
        public InventoryLocation InvLocPrev { get; set; }
        public ulong[] Vector { get; } = Array.Empty<ulong>();

        public EntityBaseData(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());

            ReplicationPolicy = (AoiNetworkPolicyValues)stream.ReadRawVarint32();
            EntityId = stream.ReadRawVarint64();
            PrototypeId = stream.ReadPrototypeEnum(PrototypeEnumType.Entity);
            Flags = stream.ReadRawVarint32().ToBoolArray(FlagCount);
            LocoFieldFlags = (LocomotionMessageFlags)stream.ReadRawVarint32();

            if (Flags[5]) InterestPolicies = stream.ReadRawVarint32();
            if (Flags[9]) AvatarWorldInstanceId = stream.ReadRawVarint32();
            if (Flags[8]) DbId = stream.ReadRawVarint32();

            // Location
            if (Flags[0])
            {
                Position = new(stream, 3);

                Orientation = LocoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation)
                    ? new(stream, 6)
                    : new(stream.ReadRawZigZagFloat(6), 0f, 0f);
            }

            if (LocoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
                LocomotionState = new(stream, LocoFieldFlags);

            if (Flags[11]) BoundsScaleOverride = stream.ReadRawZigZagFloat(8);
            if (Flags[3]) SourceEntityId = stream.ReadRawVarint64();
            if (Flags[4]) SourcePosition = new(stream, 3);
            if (Flags[1]) ActivePowerPrototypeId = stream.ReadPrototypeEnum(PrototypeEnumType.Power);
            if (Flags[6]) InvLoc = new(stream);
            if (Flags[7]) InvLocPrev = new(stream);

            if (Flags[14])
            {
                Vector = new ulong[stream.ReadRawVarint64()];
                for (int i = 0; i < Vector.Length; i++)
                    Vector[i] = stream.ReadRawVarint64();
            }
        }

        public EntityBaseData() { }

        public EntityBaseData(ulong entityId, PrototypeId prototypeId, Vector3 position, Vector3 orientation, bool snap = false)
        {
            ReplicationPolicy = AoiNetworkPolicyValues.AoiChannel5;
            EntityId = entityId;
            PrototypeId = prototypeId;
            LocomotionState = new(0f);

            Flags = new bool[FlagCount];
            LocoFieldFlags = LocomotionMessageFlags.None;

            if (position != null && orientation != null)
            {
                Position = position;
                Orientation = orientation;
                Flags[0] = true;
            }

            Flags[10] = snap;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint32((uint)ReplicationPolicy);
            stream.WriteRawVarint64(EntityId);
            stream.WritePrototypeEnum(PrototypeId, PrototypeEnumType.Entity);
            stream.WriteRawVarint32(Flags.ToUInt32());
            stream.WriteRawVarint32((uint)LocoFieldFlags);

            if (Flags[5]) stream.WriteRawVarint32(InterestPolicies);
            if (Flags[9]) stream.WriteRawVarint32(AvatarWorldInstanceId);
            if (Flags[8]) stream.WriteRawVarint32(DbId);

            // Location
            if (Flags[0])
            {
                Position.Encode(stream, 3);

                if (LocoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation))
                    Orientation.Encode(stream, 6);
                else
                    stream.WriteRawZigZagFloat(Orientation.X, 6);
            }

            if (LocoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false) LocomotionState.Encode(stream, LocoFieldFlags);
            if (Flags[11]) stream.WriteRawZigZagFloat(BoundsScaleOverride, 8);
            if (Flags[3]) stream.WriteRawVarint64(SourceEntityId);
            if (Flags[4]) SourcePosition.Encode(stream, 3);
            if (Flags[1]) stream.WritePrototypeEnum(ActivePowerPrototypeId, PrototypeEnumType.Power);
            if (Flags[6]) InvLoc.Encode(stream);
            if (Flags[7]) InvLocPrev.Encode(stream);

            if (Flags[14])
            {
                stream.WriteRawVarint64((ulong)Vector.Length);
                for (int i = 0; i < Vector.Length; i++)
                    stream.WriteRawVarint64(Vector[i]);
            }
        }

        public ByteString Serialize()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
                Encode(cos);
                cos.Flush();
                return ByteString.CopyFrom(ms.ToArray());
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationPolicy: {ReplicationPolicy}");
            sb.AppendLine($"EntityId: {EntityId}");
            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypeName(PrototypeId)}");

            sb.Append("Flags: ");
            for (int i = 0; i < Flags.Length; i++) if (Flags[i]) sb.Append($"{i} ");
            sb.AppendLine();

            sb.AppendLine($"LocoFlags: {LocoFieldFlags}");
            sb.AppendLine($"InterestPolicies: 0x{InterestPolicies:X}");
            sb.AppendLine($"AvatarWorldInstanceId: {AvatarWorldInstanceId}");
            sb.AppendLine($"DbId: {DbId}");
            sb.AppendLine($"Position: {Position}");
            sb.AppendLine($"Orientation: {Orientation}");
            sb.AppendLine($"LocomotionState: {LocomotionState}");
            sb.AppendLine($"BoundsScaleOverride: {BoundsScaleOverride}");
            sb.AppendLine($"SourceEntityId: {SourceEntityId}");
            sb.AppendLine($"SourcePosition: {SourcePosition}");
            sb.AppendLine($"ActivePowerPrototypeId: {GameDatabase.GetPrototypeName(ActivePowerPrototypeId)}");
            sb.AppendLine($"InvLoc: {InvLoc}");
            sb.AppendLine($"InvLocPrev: {InvLocPrev}");
            for (int i = 0; i < Vector.Length; i++) sb.AppendLine($"Vector{i}: 0x{Vector[i]:X}");
            return sb.ToString();
        }
    }
}
