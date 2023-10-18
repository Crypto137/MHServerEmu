using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Powers
{
    public class PowerResultArchive
    {
        private const int FlagCount = 12;

        public uint ReplicationPolicy { get; set; }
        public bool[] Flags { get; set; }
        public ulong PowerPrototypeId { get; set; }
        public ulong TargetId { get; set; }
        public ulong PowerOwnerId { get; set; }
        public ulong UltimateOwnerId { get; set; }
        public ulong ResultFlags { get; set; }
        public uint Damage0 { get; set; }
        public uint Damage1 { get; set; }
        public uint Damage2 { get; set; }
        public uint Healing { get; set; }
        public ulong AssetGuid { get; set; }
        public Vector3 Position { get; set; }
        public ulong TransferToId { get; set; }

        public PowerResultArchive(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            ReplicationPolicy = stream.ReadRawVarint32();
            Flags = stream.ReadRawVarint32().ToBoolArray(FlagCount);
            PowerPrototypeId = stream.ReadPrototypeEnum(PrototypeEnumType.Power);
            TargetId = stream.ReadRawVarint64();

            if (Flags[1])
                PowerOwnerId = TargetId;
            else if (Flags[0] == false)
                PowerOwnerId = stream.ReadRawVarint64();
            
            if (Flags[3])
                UltimateOwnerId = PowerOwnerId;
            else if (Flags[2] == false)
                UltimateOwnerId = stream.ReadRawVarint64();

            if (Flags[4]) ResultFlags = stream.ReadRawVarint64();
            if (Flags[6]) Damage0 = stream.ReadRawVarint32();
            if (Flags[7]) Damage1 = stream.ReadRawVarint32();
            if (Flags[8]) Damage2 = stream.ReadRawVarint32();
            if (Flags[9]) Healing = stream.ReadRawVarint32();
            if (Flags[10]) AssetGuid = stream.ReadRawVarint64();
            if (Flags[5]) Position = new(stream, 2);
            if (Flags[11]) TransferToId = stream.ReadRawVarint64();
        }

        public PowerResultArchive(NetMessageTryActivatePower tryActivatePower)
        {
            // damage test
            ReplicationPolicy = 0x1;
            Flags = 0u.ToBoolArray(FlagCount);
            PowerPrototypeId = tryActivatePower.PowerPrototypeId;
            TargetId = tryActivatePower.IdTargetEntity;
            PowerOwnerId = tryActivatePower.IdUserEntity;
            Flags[3] = true;    // UltimateOwnerId same as PowerOwnerId
            
            if (tryActivatePower.IdTargetEntity != 0)
            {
                //ResultFlags = 0x85;   // brutal strike
                //Flags[4] = true;

                Position = new(tryActivatePower.TargetPosition);
                Flags[5] = true;

                if (TargetId != PowerOwnerId) Damage0 = 100;
                Flags[6] = true;
            }
        }

        public PowerResultArchive(NetMessageContinuousPowerUpdateToServer continuousPowerUpdate)
        {
            // damage test
            ReplicationPolicy = 0x1;
            Flags = 0u.ToBoolArray(FlagCount);
            PowerPrototypeId = continuousPowerUpdate.PowerPrototypeId;
            TargetId = continuousPowerUpdate.IdTargetEntity;
            Flags[3] = true;    // UltimateOwnerId same as PowerOwnerId

            if (continuousPowerUpdate.IdTargetEntity != 0)
            {
                //ResultFlags = 0x85;   // brutal strike
                //Flags[4] = true;

                Position = new(continuousPowerUpdate.TargetPosition);
                Flags[5] = true;

                Damage0 = 100;
                Flags[6] = true;
            }
        }

        public PowerResultArchive() { }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint32(ReplicationPolicy);
                cos.WriteRawVarint32(Flags.ToUInt32());
                cos.WritePrototypeEnum(PowerPrototypeId, PrototypeEnumType.Power);
                cos.WriteRawVarint64(TargetId);
                if (Flags[1] == false && Flags[0] == false) cos.WriteRawVarint64(PowerOwnerId);
                if (Flags[3] == false && Flags[2] == false) cos.WriteRawVarint64(UltimateOwnerId);
                if (Flags[4]) cos.WriteRawVarint64(ResultFlags);
                if (Flags[6]) cos.WriteRawVarint32(Damage0);
                if (Flags[7]) cos.WriteRawVarint32(Damage1);
                if (Flags[8]) cos.WriteRawVarint32(Damage2);
                if (Flags[9]) cos.WriteRawVarint32(Healing);
                if (Flags[10]) cos.WriteRawVarint64(AssetGuid);
                if (Flags[5]) Position.Encode(cos, 2);
                if (Flags[11]) cos.WriteRawVarint64(TransferToId);

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

            sb.AppendLine($"PowerPrototype: {GameDatabase.GetPrototypeName(PowerPrototypeId)}");
            sb.AppendLine($"TargetId: {TargetId}");
            sb.AppendLine($"PowerOwnerId: {PowerOwnerId}");
            sb.AppendLine($"UltimateOwnerId: {UltimateOwnerId}");
            sb.AppendLine($"ResultFlags: 0x{ResultFlags:X}");
            sb.AppendLine($"Damage0: {Damage0}");
            sb.AppendLine($"Damage1: {Damage1}");
            sb.AppendLine($"Damage2: {Damage2}");
            sb.AppendLine($"Healing: {Healing}");
            sb.AppendLine($"AssetGuid: {AssetGuid}");
            sb.AppendLine($"Position: {Position}");
            sb.AppendLine($"TransferToId: {TransferToId}");

            return sb.ToString();
        }
    }
}
