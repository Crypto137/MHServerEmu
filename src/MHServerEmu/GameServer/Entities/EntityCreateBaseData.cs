using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities
{
    public class EntityCreateBaseData
    {
        // Note: in old client builds (July 2014 and earlier) this used to be a protobuf message with a lot of fields.
        // It was probably converted to an archive for optimization reasons.
        public uint ReplicationPolicy { get; set; }
        public ulong EntityId { get; set; }
        public ulong EntityPrototypeEnum { get; set; }
        public uint Flags { get; set; }         // the original message contained bools, they might have been packed with field flags
        public uint LocFieldFlags { get; set; }
        public uint InterestPolicies { get; set; }
        public uint AvatarWorldInstanceId { get; set; }
        public uint DbId { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Orientation { get; set; }
        public LocomotionState LocomotionState { get; set; }
        public float BoundsScaleOverride { get; }
        public ulong SourceEntityId { get; }
        public Vector3 SourcePosition { get; }
        public ulong ActivePowerPrototypeId { get; }
        public InventoryLocation InvLocPrev { get; }
        public InventoryLocation InvLoc { get; }
        public ulong[] Vector { get; }

        public EntityCreateBaseData(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            ReplicationPolicy = stream.ReadRawVarint32();
            EntityId = stream.ReadRawVarint64();
            EntityPrototypeEnum = stream.ReadRawVarint64();
            Flags = stream.ReadRawVarint32();
            LocFieldFlags = stream.ReadRawVarint32();

            if ((Flags & 0x20) > 0) InterestPolicies = stream.ReadRawVarint32();
            if ((Flags & 0x200) > 0) AvatarWorldInstanceId = stream.ReadRawVarint32();
            if ((Flags & 0x100) > 0) DbId = stream.ReadRawVarint32();

            // Location
            if ((Flags & 0x1) > 0)
            {
                Position = new(stream, 3);

                if ((LocFieldFlags & 0x1) > 0)
                    Orientation = new(stream.ReadRawFloat(6), stream.ReadRawFloat(6), stream.ReadRawFloat(6));
                else
                    Orientation = new(stream.ReadRawFloat(6), 0f, 0f);
            }

            if ((LocFieldFlags & 0x2) == 0) LocomotionState = new(stream, LocFieldFlags);
            if ((Flags & 0x800) > 0) BoundsScaleOverride = stream.ReadRawFloat(8);
            if ((Flags & 0x8) > 0) SourceEntityId = stream.ReadRawVarint64();
            if ((Flags & 0x10) > 0) SourcePosition = new(stream, 3);
            if ((Flags & 0x2) > 0) ActivePowerPrototypeId = stream.ReadRawVarint64();
            if ((Flags & 0x40) > 0) InvLocPrev = new(stream);
            if ((Flags & 0x80) > 0) InvLoc = new(stream);

            if ((Flags & 0x4000) > 0)
            {
                Vector = new ulong[stream.ReadRawVarint64()];
                for (int i = 0; i < Vector.Length; i++)
                    Vector[i] = stream.ReadRawVarint64();
            }
        }

        public EntityCreateBaseData()
        {
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint32(ReplicationPolicy);
                stream.WriteRawVarint64(EntityId);
                stream.WriteRawVarint64(EntityPrototypeEnum);
                stream.WriteRawVarint32(Flags);
                stream.WriteRawVarint32(LocFieldFlags);

                if ((Flags & 0x20) > 0) stream.WriteRawVarint32(InterestPolicies);
                if ((Flags & 0x200) > 0) stream.WriteRawVarint32(AvatarWorldInstanceId);
                if ((Flags & 0x100) > 0) stream.WriteRawVarint32(DbId);

                // Location
                if ((Flags & 0x1) > 0)
                {
                    stream.WriteRawBytes(Position.Encode(3));

                    if ((LocFieldFlags & 0x1) > 0)
                        stream.WriteRawBytes(Orientation.Encode(6));
                    else
                        stream.WriteRawFloat(Orientation.X, 6);
                }

                if ((LocFieldFlags & 0x2) == 0) stream.WriteRawBytes(LocomotionState.Encode(LocFieldFlags));
                if ((Flags & 0x800) > 0) stream.WriteRawFloat(BoundsScaleOverride, 8);
                if ((Flags & 0x8) > 0) stream.WriteRawVarint64(SourceEntityId);
                if ((Flags & 0x10) > 0) stream.WriteRawBytes(SourcePosition.Encode(3));
                if ((Flags & 0x2) > 0) stream.WriteRawVarint64(ActivePowerPrototypeId);
                if ((Flags & 0x40) > 0) stream.WriteRawBytes(InvLocPrev.Encode());
                if ((Flags & 0x80) > 0) stream.WriteRawBytes(InvLoc.Encode());

                if ((Flags & 0x4000) > 0)
                {
                    stream.WriteRawVarint64((ulong)Vector.Length);
                    for (int i = 0; i < Vector.Length; i++)
                        stream.WriteRawVarint64(Vector[i]);
                }

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
                streamWriter.WriteLine($"EntityPrototypeEnum: 0x{EntityPrototypeEnum.ToString("X")}");
                streamWriter.WriteLine($"Flags: 0x{Flags.ToString("X")}");
                streamWriter.WriteLine($"LocFieldFlags: 0x{LocFieldFlags.ToString("X")}");
                streamWriter.WriteLine($"InterestPolicies: 0x{InterestPolicies.ToString("X")}");
                streamWriter.WriteLine($"AvatarWorldInstanceId: 0x{AvatarWorldInstanceId.ToString("X")}");
                streamWriter.WriteLine($"DbId: 0x{DbId.ToString("X")}");
                streamWriter.WriteLine($"Position: {Position}");
                streamWriter.WriteLine($"Orientation: {Orientation}");
                streamWriter.WriteLine($"LocomotionState: {LocomotionState}");
                streamWriter.WriteLine($"BoundsScaleOverride: {BoundsScaleOverride}");
                streamWriter.WriteLine($"SourceEntityId: 0x{SourceEntityId}");
                streamWriter.WriteLine($"SourcePosition: {SourcePosition}");
                streamWriter.WriteLine($"ActivePowerPrototypeId: 0x{ActivePowerPrototypeId.ToString("X")}");
                streamWriter.WriteLine($"InvLocPrev: {InvLocPrev}");
                streamWriter.WriteLine($"InvLoc: {InvLoc}");
                for (int i = 0; i < Vector.Length; i++) streamWriter.WriteLine($"Vector{i}: 0x{Vector[i].ToString("X")}");

                streamWriter.Flush();

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
