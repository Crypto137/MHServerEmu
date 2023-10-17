using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Powers
{
    public class ActivatePowerArchive
    {
        private const int FlagCount = 8;

        public uint ReplicationPolicy { get; set; }
        public bool[] Flags { get; set; }
        public ulong IdUserEntity { get; set; }
        public ulong IdTargetEntity { get; set; }
        public ulong PowerPrototypeId { get; set; }
        public ulong TriggeringPowerPrototypeId { get; set; }
        public Vector3 UserPosition { get; set; }
        public Vector3 TargetPosition { get; set; }
        public ulong MovementTimeMS { get; set; }   // should this be uint or ulong?
        public uint UnknownTimeMS { get; set; }
        public uint PowerRandomSeed { get; set; }
        public uint FXRandomSeed { get; set; }

        public ActivatePowerArchive(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            ReplicationPolicy = stream.ReadRawVarint32();
            Flags = stream.ReadRawVarint32().ToBoolArray(FlagCount);
            IdUserEntity = stream.ReadRawVarint64();
            if (Flags[0] == false) IdTargetEntity = stream.ReadRawVarint64();
            PowerPrototypeId = stream.ReadPrototypeId(PrototypeEnumType.Power);
            if (Flags[1]) TriggeringPowerPrototypeId = stream.ReadPrototypeId(PrototypeEnumType.Power);
            UserPosition = new(stream, 2);
            if (Flags[2]) TargetPosition = new Vector3(stream, 2) + UserPosition;      // TargetPosition is relative to UserPosition when encoded
            else if (Flags[3]) TargetPosition = UserPosition;
            if (Flags[4]) MovementTimeMS = stream.ReadRawVarint64();
            if (Flags[5]) UnknownTimeMS = stream.ReadRawVarint32();
            if (Flags[6]) PowerRandomSeed = stream.ReadRawVarint32();
            if (Flags[7]) FXRandomSeed = stream.ReadRawVarint32();
        }

        public ActivatePowerArchive(NetMessageTryActivatePower tryActivatePower, Vector3 userPosition)
        {
            ReplicationPolicy = 0x1;
            Flags = 0u.ToBoolArray(FlagCount);

            IdUserEntity = tryActivatePower.IdUserEntity;
            PowerPrototypeId = tryActivatePower.PowerPrototypeId;
            UserPosition = userPosition;        // derive this from tryActivatePower.MovementSpeed?

            // IdTargetEntity
            if (tryActivatePower.HasIdTargetEntity)                 
                if (tryActivatePower.IdTargetEntity == IdUserEntity)
                    Flags[0] = true;    // flag0 means the user is the target
                else
                    IdTargetEntity = tryActivatePower.IdTargetEntity;

            // TriggeringPowerPrototypeId
            if (tryActivatePower.HasTriggeringPowerPrototypeId)
            {
                TriggeringPowerPrototypeId = tryActivatePower.TriggeringPowerPrototypeId;
                Flags[1] = true;
            }

            // TargetPosition
            if (tryActivatePower.HasTargetPosition)
            {
                TargetPosition = new(tryActivatePower.TargetPosition);
                Flags[2] = true;
            }
            else
            {
                Flags[3] = true;    // TargetPosition == UserPosition
            }

            // MovementTimeMS
            if (tryActivatePower.HasMovementTimeMS)
            {
                MovementTimeMS = tryActivatePower.MovementTimeMS;
                Flags[4] = true;
            }

            // UnknownTimeMS (Flag5) - where does this come from?

            // PowerRandomSeed
            if (tryActivatePower.HasPowerRandomSeed)
            {
                PowerRandomSeed = tryActivatePower.PowerRandomSeed;
                Flags[6] = true;
            }

            // FXRandomSeed - NOTE: FXRandomSeed is marked as required in protobuf, so it should always be present
            if (tryActivatePower.HasFxRandomSeed)
            {
                FXRandomSeed = tryActivatePower.FxRandomSeed;
                Flags[7] = true;
            }
        }

        public ActivatePowerArchive() { }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint32(ReplicationPolicy);
                cos.WriteRawVarint32(Flags.ToUInt32());
                cos.WriteRawVarint64(IdUserEntity);
                if (Flags[0] == false) cos.WriteRawVarint64(IdTargetEntity);
                cos.WritePrototypeId(PowerPrototypeId, PrototypeEnumType.Power);
                if (Flags[1]) cos.WritePrototypeId(TriggeringPowerPrototypeId, PrototypeEnumType.Power);
                cos.WriteRawBytes(UserPosition.Encode(2));
                if (Flags[2]) cos.WriteRawBytes((TargetPosition - UserPosition).Encode(2));
                if (Flags[4]) cos.WriteRawVarint64(MovementTimeMS);
                if (Flags[5]) cos.WriteRawVarint32(UnknownTimeMS);
                if (Flags[6]) cos.WriteRawVarint32(PowerRandomSeed);
                if (Flags[7]) cos.WriteRawVarint32(FXRandomSeed);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationPolicy: 0x{ReplicationPolicy:X}");

            sb.Append("Flags: ");
            for (int i = 0; i < Flags.Length; i++) if (Flags[i]) sb.Append($"{i} ");
            sb.AppendLine();

            sb.AppendLine($"IdUserEntity: {IdUserEntity}");
            sb.AppendLine($"IdTargetEntity: {IdTargetEntity}");
            sb.AppendLine($"PowerPrototypeId: {GameDatabase.GetPrototypeName(PowerPrototypeId)}");
            sb.AppendLine($"TriggeringPowerPrototypeId: {GameDatabase.GetPrototypeName(TriggeringPowerPrototypeId)}");
            sb.AppendLine($"UserPosition: {UserPosition}");
            sb.AppendLine($"TargetPosition: {TargetPosition}");
            sb.AppendLine($"MovementTimeMS: {MovementTimeMS}");
            sb.AppendLine($"UnknownTimeMS: {UnknownTimeMS}");
            sb.AppendLine($"PowerRandomSeed: {PowerRandomSeed}");
            sb.AppendLine($"FXRandomSeed: {FXRandomSeed}");

            return sb.ToString();
        }
    }
}
