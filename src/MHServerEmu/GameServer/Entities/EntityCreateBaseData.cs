using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Entities.Locomotion;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Entities
{
    public class EntityCreateBaseData
    {
        private const int FlagCount = 16;  // keep flag count a bit higher than we need just in case so we don't miss anything
        private const int LocFlagCount = 16;

        // Note: in old client builds (July 2014 and earlier) this used to be a protobuf message with a lot of fields.
        // It was probably converted to an archive for optimization reasons.
        public uint ReplicationPolicy { get; set; }
        public ulong EntityId { get; set; }
        public ulong PrototypeId { get; set; }
        public bool[] Flags { get; set; }         // mystery flags: 2, 10, 12, 13
        public bool[] LocFlags { get; set; }
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
        public InventoryLocation InvLoc { get; }
        public InventoryLocation InvLocPrev { get; }
        public ulong[] Vector { get; } = Array.Empty<ulong>();

        public EntityCreateBaseData(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            ReplicationPolicy = stream.ReadRawVarint32();
            EntityId = stream.ReadRawVarint64();
            PrototypeId = stream.ReadPrototypeId(PrototypeEnumType.Entity);
            Flags = stream.ReadRawVarint32().ToBoolArray(FlagCount);
            LocFlags = stream.ReadRawVarint32().ToBoolArray(LocFlagCount);

            if (Flags[5]) InterestPolicies = stream.ReadRawVarint32();
            if (Flags[9]) AvatarWorldInstanceId = stream.ReadRawVarint32();
            if (Flags[8]) DbId = stream.ReadRawVarint32();

            // Location
            if (Flags[0])
            {
                Position = new(stream, 3);

                if (LocFlags[0])
                    Orientation = new(stream.ReadRawFloat(6), stream.ReadRawFloat(6), stream.ReadRawFloat(6));
                else
                    Orientation = new(stream.ReadRawFloat(6), 0f, 0f);
            }

            if (LocFlags[1] == false) LocomotionState = new(stream, LocFlags);
            if (Flags[11]) BoundsScaleOverride = stream.ReadRawFloat(8);
            if (Flags[3]) SourceEntityId = stream.ReadRawVarint64();
            if (Flags[4]) SourcePosition = new(stream, 3);
            if (Flags[1]) ActivePowerPrototypeId = stream.ReadPrototypeId(PrototypeEnumType.Power);
            if (Flags[6]) InvLoc = new(stream);
            if (Flags[7]) InvLocPrev = new(stream);

            if (Flags[14])
            {
                Vector = new ulong[stream.ReadRawVarint64()];
                for (int i = 0; i < Vector.Length; i++)
                    Vector[i] = stream.ReadRawVarint64();
            }
        }

        public EntityCreateBaseData() { }

        public EntityCreateBaseData(ulong entityId, ulong prototypeId, Vector3 position, Vector3 orientation)
        {
            ReplicationPolicy = 0x20;
            EntityId = entityId;
            PrototypeId = prototypeId;
            LocomotionState = new(0f);

            Flags = new bool[FlagCount];
            LocFlags = new bool[LocFlagCount];

            if (position != null && orientation != null)
            {
                Position = position;
                Orientation = orientation;
                Flags[0] = true;
            }
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint32(ReplicationPolicy);
                stream.WriteRawVarint64(EntityId);
                stream.WritePrototypeId(PrototypeId, PrototypeEnumType.Entity);
                stream.WriteRawVarint32(Flags.ToUInt32());
                stream.WriteRawVarint32(LocFlags.ToUInt32());

                if (Flags[5]) stream.WriteRawVarint32(InterestPolicies);
                if (Flags[9]) stream.WriteRawVarint32(AvatarWorldInstanceId);
                if (Flags[8]) stream.WriteRawVarint32(DbId);

                // Location
                if (Flags[0])
                {
                    stream.WriteRawBytes(Position.Encode(3));

                    if (LocFlags[0])
                        stream.WriteRawBytes(Orientation.Encode(6));
                    else
                        stream.WriteRawFloat(Orientation.X, 6);
                }

                if (LocFlags[1] == false) stream.WriteRawBytes(LocomotionState.Encode(LocFlags));
                if (Flags[11]) stream.WriteRawFloat(BoundsScaleOverride, 8);
                if (Flags[3]) stream.WriteRawVarint64(SourceEntityId);
                if (Flags[4]) stream.WriteRawBytes(SourcePosition.Encode(3));
                if (Flags[1]) stream.WritePrototypeId(ActivePowerPrototypeId, PrototypeEnumType.Power);
                if (Flags[6]) stream.WriteRawBytes(InvLoc.Encode());
                if (Flags[7]) stream.WriteRawBytes(InvLocPrev.Encode());

                if (Flags[14])
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
                streamWriter.WriteLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
                for (int i = 0; i < Flags.Length; i++) streamWriter.WriteLine($"Flag{i}: {Flags[i]}");
                for (int i = 0; i < LocFlags.Length; i++) streamWriter.WriteLine($"LocFlag{i}: {LocFlags[i]}");
                streamWriter.WriteLine($"InterestPolicies: 0x{InterestPolicies.ToString("X")}");
                streamWriter.WriteLine($"AvatarWorldInstanceId: 0x{AvatarWorldInstanceId.ToString("X")}");
                streamWriter.WriteLine($"DbId: 0x{DbId.ToString("X")}");
                streamWriter.WriteLine($"Position: {Position}");
                streamWriter.WriteLine($"Orientation: {Orientation}");
                streamWriter.WriteLine($"LocomotionState: {LocomotionState}");
                streamWriter.WriteLine($"BoundsScaleOverride: {BoundsScaleOverride}");
                streamWriter.WriteLine($"SourceEntityId: 0x{SourceEntityId}");
                streamWriter.WriteLine($"SourcePosition: {SourcePosition}");
                streamWriter.WriteLine($"ActivePowerPrototypeId: {GameDatabase.GetPrototypePath(ActivePowerPrototypeId)}");
                streamWriter.WriteLine($"InvLoc: {InvLoc}");
                streamWriter.WriteLine($"InvLocPrev: {InvLocPrev}");
                for (int i = 0; i < Vector.Length; i++) streamWriter.WriteLine($"Vector{i}: 0x{Vector[i].ToString("X")}");

                streamWriter.Flush();

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
