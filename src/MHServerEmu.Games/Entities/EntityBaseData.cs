using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities
{
    // Unused flag names from old protocols:
    // isNewOnServer, initConditionComponent, isClientEntityHidden, startFullInWorldHierarchyUpdate
    [Flags]
    public enum EntityCreateMessageFlags : uint
    {
        None                        = 0,
        HasPositionAndOrientation   = 1 << 0,
        HasActivePowerPrototypeId   = 1 << 1,
        Flag2                       = 1 << 2,
        HasSourceEntityId           = 1 << 3,
        HasSourcePosition           = 1 << 4,
        HasInterestPolicies         = 1 << 5,
        HasInvLoc                   = 1 << 6,
        HasInvLocPrev               = 1 << 7,
        HasDbId                     = 1 << 8,
        HasAvatarWorldInstanceId    = 1 << 9,
        OverrideSnapToFloorOnSpawn  = 1 << 10,
        HasBoundsScaleOverride      = 1 << 11,
        Flag12                      = 1 << 12,
        Flag13                      = 1 << 13,
        HasUnkVector                = 1 << 14,
        Flag15                      = 1 << 15
    }

    public class EntityBaseData
    {
        // Note: in old client builds (July 2014 and earlier) this used to be a protobuf message with 20+ fields.
        // It was probably converted to an archive for optimization reasons.
        public AOINetworkPolicyValues ReplicationPolicy { get; set; }
        public ulong EntityId { get; set; }
        public PrototypeId PrototypeId { get; set; }
        public EntityCreateMessageFlags FieldFlags { get; set; }
        public LocomotionMessageFlags LocoFieldFlags { get; set; }
        public AOINetworkPolicyValues InterestPolicies { get; set; }
        public uint AvatarWorldInstanceId { get; set; }
        public uint DbId { get; set; }
        public Vector3 Position { get; set; }
        public Orientation Orientation { get; set; }
        public LocomotionState LocomotionState { get; set; }
        public float BoundsScaleOverride { get; }
        public ulong SourceEntityId { get; }
        public Vector3 SourcePosition { get; }
        public PrototypeId ActivePowerPrototypeId { get; }
        public InventoryLocation InvLoc { get; set; }
        public InventoryLocation InvLocPrev { get; set; }
        public ulong[] UnkVector { get; } = Array.Empty<ulong>();

        public EntityBaseData(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());

            ReplicationPolicy = (AOINetworkPolicyValues)stream.ReadRawVarint32();
            EntityId = stream.ReadRawVarint64();
            PrototypeId = stream.ReadPrototypeRef<EntityPrototype>();
            FieldFlags = (EntityCreateMessageFlags)stream.ReadRawVarint32();
            LocoFieldFlags = (LocomotionMessageFlags)stream.ReadRawVarint32();

            InterestPolicies = FieldFlags.HasFlag(EntityCreateMessageFlags.HasInterestPolicies)
                ? (AOINetworkPolicyValues)stream.ReadRawVarint32()
                : AOINetworkPolicyValues.AOIChannelProximity;    // This defaults to 0x1 if no policies are specified

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasAvatarWorldInstanceId))
                AvatarWorldInstanceId = stream.ReadRawVarint32();

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasDbId))
                DbId = stream.ReadRawVarint32();

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasPositionAndOrientation))
            {
                Position = new(stream);

                Orientation = LocoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation)
                    ? new(stream)
                    : new(stream.ReadRawZigZagFloat(6), 0f, 0f);
            }

            if (LocoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
            {
                LocomotionState = new();
                LocomotionState.Decode(stream, LocoFieldFlags);
            }

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasBoundsScaleOverride))
                BoundsScaleOverride = stream.ReadRawZigZagFloat(8);
            
            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasSourceEntityId))
                SourceEntityId = stream.ReadRawVarint64();

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasSourcePosition))
                SourcePosition = new(stream);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasActivePowerPrototypeId))
                ActivePowerPrototypeId = stream.ReadPrototypeRef<PowerPrototype>();

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLoc))
                InvLoc = new(stream);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLocPrev))
                InvLocPrev = new(stream);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasUnkVector))
            {
                UnkVector = new ulong[stream.ReadRawVarint64()];
                for (int i = 0; i < UnkVector.Length; i++)
                    UnkVector[i] = stream.ReadRawVarint64();
            }
        }

        public EntityBaseData() { }

        public EntityBaseData(ulong entityId, PrototypeId prototypeId, Vector3 position, Orientation orientation, bool snap = false)
        {
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelDiscovery;
            EntityId = entityId;
            PrototypeId = prototypeId;
            LocomotionState = new(0f);

            FieldFlags = EntityCreateMessageFlags.None;
            LocoFieldFlags = LocomotionMessageFlags.None;

            if (position != null && orientation != null)
            {
                Position = position;
                Orientation = orientation;
                FieldFlags |= EntityCreateMessageFlags.HasPositionAndOrientation;
            }

            if (snap) FieldFlags |= EntityCreateMessageFlags.OverrideSnapToFloorOnSpawn;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint32((uint)ReplicationPolicy);
            stream.WriteRawVarint64(EntityId);
            stream.WritePrototypeRef<EntityPrototype>(PrototypeId);
            stream.WriteRawVarint32((uint)FieldFlags);
            stream.WriteRawVarint32((uint)LocoFieldFlags);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasInterestPolicies))
                stream.WriteRawVarint32((uint)InterestPolicies);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasAvatarWorldInstanceId))
                stream.WriteRawVarint32(AvatarWorldInstanceId);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasDbId))
                stream.WriteRawVarint32(DbId);

            // Location
            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasPositionAndOrientation))
            {
                Position.Encode(stream);

                if (LocoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation))
                    Orientation.Encode(stream);
                else
                    stream.WriteRawZigZagFloat(Orientation.Yaw, 6);
            }

            if (LocoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
                LocomotionState.Encode(stream, LocoFieldFlags);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasBoundsScaleOverride))
                stream.WriteRawZigZagFloat(BoundsScaleOverride, 8);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasSourceEntityId))
                stream.WriteRawVarint64(SourceEntityId);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasSourcePosition))
                SourcePosition.Encode(stream);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasActivePowerPrototypeId))
                stream.WritePrototypeRef<PowerPrototype>(ActivePowerPrototypeId);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLoc))
                InvLoc.Encode(stream);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLocPrev))
                InvLocPrev.Encode(stream);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasUnkVector))
            {
                stream.WriteRawVarint64((ulong)UnkVector.Length);
                for (int i = 0; i < UnkVector.Length; i++)
                    stream.WriteRawVarint64(UnkVector[i]);
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
            sb.AppendLine($"FieldFlags: {FieldFlags}");
            sb.AppendLine($"LocoFieldFlags: {LocoFieldFlags}");
            sb.AppendLine($"InterestPolicies: {InterestPolicies}");
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
            for (int i = 0; i < UnkVector.Length; i++) sb.AppendLine($"UnkVector{i}: 0x{UnkVector[i]:X}");
            return sb.ToString();
        }
    }
}
