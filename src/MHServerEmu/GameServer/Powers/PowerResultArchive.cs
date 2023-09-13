using System.Text;
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
        public ulong PowerPrototype { get; set; }
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
            PowerPrototype = stream.ReadPrototypeId(PrototypeEnumType.Power);
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

        public PowerResultArchive() { }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint32(ReplicationPolicy);
                cos.WriteRawVarint32(Flags.ToUInt32());
                cos.WritePrototypeId(PowerPrototype, PrototypeEnumType.Power);
                cos.WriteRawVarint64(TargetId);
                if (Flags[1] == false && Flags[0] == false) cos.WriteRawVarint64(PowerOwnerId);
                if (Flags[3] == false && Flags[2] == false) cos.WriteRawVarint64(UltimateOwnerId);
                if (Flags[4]) cos.WriteRawVarint64(ResultFlags);
                if (Flags[6]) cos.WriteRawVarint32(Damage0);
                if (Flags[7]) cos.WriteRawVarint32(Damage1);
                if (Flags[8]) cos.WriteRawVarint32(Damage2);
                if (Flags[9]) cos.WriteRawVarint32(Healing);
                if (Flags[10]) cos.WriteRawVarint64(AssetGuid);
                if (Flags[5]) cos.WriteRawBytes(Position.Encode(2));
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

            sb.AppendLine($"PowerPrototype: {GameDatabase.GetPrototypePath(PowerPrototype)}");
            sb.AppendLine($"TargetId: 0x{TargetId:X}");
            sb.AppendLine($"PowerOwnerId: 0x{PowerOwnerId:X}");
            sb.AppendLine($"UltimateOwnerId: 0x{UltimateOwnerId:X}");
            sb.AppendLine($"ResultFlags: 0x{ResultFlags:X}");
            sb.AppendLine($"Damage0: 0x{Damage0:X}");
            sb.AppendLine($"Damage1: 0x{Damage1:X}");
            sb.AppendLine($"Damage2: 0x{Damage2:X}");
            sb.AppendLine($"Healing: 0x{Healing:X}");
            sb.AppendLine($"AssetGuid: 0x{AssetGuid:X}");
            sb.AppendLine($"Position: {Position}");
            sb.AppendLine($"TransferToId: 0x{TransferToId:X}");

            return sb.ToString();
        }
    }
}
