using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Powers
{
    [Flags]
    public enum ActivatePowerMessageFlags
    {
        None                            = 0,
        TargetIsUser                    = 1 << 0,
        HasTriggeringPowerPrototypeId   = 1 << 1,
        HasTargetPosition               = 1 << 2,
        TargetPositionIsUserPosition    = 1 << 3,
        HasMovementTimeMS               = 1 << 4,
        HasVariableActivationTime       = 1 << 5,
        HasPowerRandomSeed              = 1 << 6,
        HasFXRandomSeed                 = 1 << 7
    }

    public class ActivatePowerArchive
    {
        public AOINetworkPolicyValues ReplicationPolicy { get; set; }
        public ActivatePowerMessageFlags Flags { get; set; }
        public ulong IdUserEntity { get; set; }
        public ulong IdTargetEntity { get; set; }
        public PrototypeId PowerPrototypeId { get; set; }
        public PrototypeId TriggeringPowerPrototypeId { get; set; }     // AKA parentPowerPrototypeId
        public Vector3 UserPosition { get; set; }
        public Vector3 TargetPosition { get; set; }
        public ulong MovementTimeMS { get; set; }   // AKA moveTimeSeconds, should this be uint or ulong?
        public uint VariableActivationTime { get; set; }
        public uint PowerRandomSeed { get; set; }
        public uint FXRandomSeed { get; set; }

        public ActivatePowerArchive(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());

            ReplicationPolicy = (AOINetworkPolicyValues)stream.ReadRawVarint32();
            Flags = (ActivatePowerMessageFlags)stream.ReadRawVarint32();
            IdUserEntity = stream.ReadRawVarint64();

            if (Flags.HasFlag(ActivatePowerMessageFlags.TargetIsUser) == false)
                IdTargetEntity = stream.ReadRawVarint64();

            PowerPrototypeId = stream.ReadPrototypeEnum<PowerPrototype>();

            if (Flags.HasFlag(ActivatePowerMessageFlags.HasTriggeringPowerPrototypeId))
                TriggeringPowerPrototypeId = stream.ReadPrototypeEnum<PowerPrototype>();

            UserPosition = new(stream, 2);

            if (Flags.HasFlag(ActivatePowerMessageFlags.HasTargetPosition))
                TargetPosition = new Vector3(stream, 2) + UserPosition;      // TargetPosition is relative to UserPosition when encoded
            else if (Flags.HasFlag(ActivatePowerMessageFlags.TargetPositionIsUserPosition))
                TargetPosition = UserPosition;

            if (Flags.HasFlag(ActivatePowerMessageFlags.HasMovementTimeMS))
                MovementTimeMS = stream.ReadRawVarint64();

            if (Flags.HasFlag(ActivatePowerMessageFlags.HasVariableActivationTime))
                VariableActivationTime = stream.ReadRawVarint32();

            if (Flags.HasFlag(ActivatePowerMessageFlags.HasPowerRandomSeed))
                PowerRandomSeed = stream.ReadRawVarint32();

            if (Flags.HasFlag(ActivatePowerMessageFlags.HasFXRandomSeed))
                FXRandomSeed = stream.ReadRawVarint32();
        }

        public ActivatePowerArchive(NetMessageTryActivatePower tryActivatePower, Vector3 userPosition)
        {
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity;
            Flags = ActivatePowerMessageFlags.None;

            IdUserEntity = tryActivatePower.IdUserEntity;
            PowerPrototypeId = (PrototypeId)tryActivatePower.PowerPrototypeId;
            UserPosition = userPosition;        // derive this from tryActivatePower.MovementSpeed?

            // IdTargetEntity
            if (tryActivatePower.HasIdTargetEntity)                 
                if (tryActivatePower.IdTargetEntity == IdUserEntity)
                    Flags |= ActivatePowerMessageFlags.TargetIsUser;    // flag0 means the user is the target
                else
                    IdTargetEntity = tryActivatePower.IdTargetEntity;

            // TriggeringPowerPrototypeId
            if (tryActivatePower.HasTriggeringPowerPrototypeId)
            {
                TriggeringPowerPrototypeId = (PrototypeId)tryActivatePower.TriggeringPowerPrototypeId;
                Flags |= ActivatePowerMessageFlags.HasTriggeringPowerPrototypeId;
            }

            // TargetPosition
            if (tryActivatePower.HasTargetPosition)
            {
                TargetPosition = new(tryActivatePower.TargetPosition);
                Flags |= ActivatePowerMessageFlags.HasTargetPosition;
            }
            else
            {
                Flags |= ActivatePowerMessageFlags.TargetPositionIsUserPosition;    // TargetPosition == UserPosition
            }

            // MovementTimeMS
            if (tryActivatePower.HasMovementTimeMS)
            {
                MovementTimeMS = tryActivatePower.MovementTimeMS;
                Flags |= ActivatePowerMessageFlags.HasMovementTimeMS;
            }

            // VariableActivationTime - where does this come from?

            // PowerRandomSeed
            if (tryActivatePower.HasPowerRandomSeed)
            {
                PowerRandomSeed = tryActivatePower.PowerRandomSeed;
                Flags |= ActivatePowerMessageFlags.HasPowerRandomSeed;
            }

            // FXRandomSeed - NOTE: FXRandomSeed is marked as required in protobuf, so it should always be present
            if (tryActivatePower.HasFxRandomSeed)
            {
                FXRandomSeed = tryActivatePower.FxRandomSeed;
                Flags |= ActivatePowerMessageFlags.HasFXRandomSeed;
            }
        }

        public ActivatePowerArchive() { }

        public ByteString Serialize()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint32((uint)ReplicationPolicy);
                cos.WriteRawVarint32((uint)Flags);
                cos.WriteRawVarint64(IdUserEntity);

                if (Flags.HasFlag(ActivatePowerMessageFlags.TargetIsUser) == false)
                    cos.WriteRawVarint64(IdTargetEntity);

                cos.WritePrototypeEnum<PowerPrototype>(PowerPrototypeId);

                if (Flags.HasFlag(ActivatePowerMessageFlags.HasTriggeringPowerPrototypeId))
                    cos.WritePrototypeEnum<PowerPrototype>(TriggeringPowerPrototypeId);

                UserPosition.Encode(cos, 2);

                if (Flags.HasFlag(ActivatePowerMessageFlags.HasTargetPosition))
                    (TargetPosition - UserPosition).Encode(cos, 2);

                if (Flags.HasFlag(ActivatePowerMessageFlags.HasMovementTimeMS))
                    cos.WriteRawVarint64(MovementTimeMS);

                if (Flags.HasFlag(ActivatePowerMessageFlags.HasVariableActivationTime))
                    cos.WriteRawVarint32(VariableActivationTime);

                if (Flags.HasFlag(ActivatePowerMessageFlags.HasPowerRandomSeed))
                    cos.WriteRawVarint32(PowerRandomSeed);

                if (Flags.HasFlag(ActivatePowerMessageFlags.HasFXRandomSeed))
                    cos.WriteRawVarint32(FXRandomSeed);

                cos.Flush();
                return ByteString.CopyFrom(ms.ToArray());
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationPolicy: {ReplicationPolicy}");
            sb.AppendLine($"Flags: {Flags}");
            sb.AppendLine($"IdUserEntity: {IdUserEntity}");
            sb.AppendLine($"IdTargetEntity: {IdTargetEntity}");
            sb.AppendLine($"PowerPrototypeId: {GameDatabase.GetPrototypeName(PowerPrototypeId)}");
            sb.AppendLine($"TriggeringPowerPrototypeId: {GameDatabase.GetPrototypeName(TriggeringPowerPrototypeId)}");
            sb.AppendLine($"UserPosition: {UserPosition}");
            sb.AppendLine($"TargetPosition: {TargetPosition}");
            sb.AppendLine($"MovementTimeMS: {MovementTimeMS}");
            sb.AppendLine($"VariableActivationTime: {VariableActivationTime}");
            sb.AppendLine($"PowerRandomSeed: {PowerRandomSeed}");
            sb.AppendLine($"FXRandomSeed: {FXRandomSeed}");
            return sb.ToString();
        }
    }
}
