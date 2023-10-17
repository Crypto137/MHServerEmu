using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Social
{
    public enum CommunityMemberOnlineStatus
    {
        Status0,
        Online,
        Offline
    }

    public class CommunityMember
    {
        public string Name { get; set; }
        public ulong DbId { get; set; }
        public ulong RegionRef { get; set; }
        public ulong DifficultyRef { get; set; }
        public AvatarSlotInfo[] Slots { get; set; }
        public CommunityMemberOnlineStatus OnlineStatus { get; set; }
        public string MemberName { get; set; }
        public string UnkName { get; set; }
        public ulong ConsoleAccountId1 { get; set; }   
        public ulong ConsoleAccountId2 { get; set; }
        public int[] ArchiveCircleIds { get; set; }

        public CommunityMember(CodedInputStream stream)
        {
            Name = stream.ReadRawString();
            DbId = stream.ReadRawVarint64();
            RegionRef = stream.ReadPrototypeId(PrototypeEnumType.All);
            DifficultyRef = stream.ReadPrototypeId(PrototypeEnumType.All);
            Slots = new AvatarSlotInfo[stream.ReadRawByte()];  
            for (int i = 0; i < Slots.Length; i++)
                Slots[i] = new(stream);
            OnlineStatus = (CommunityMemberOnlineStatus)stream.ReadRawInt32();
            MemberName = stream.ReadRawString();
            UnkName = stream.ReadRawString();
            ConsoleAccountId1 = stream.ReadRawVarint64();
            ConsoleAccountId2 = stream.ReadRawVarint64();
            ArchiveCircleIds = new int[stream.ReadRawInt32()];
            for (int i = 0; i < ArchiveCircleIds.Length; i++)
                ArchiveCircleIds[i] = stream.ReadRawInt32();
        }

        public CommunityMember(string name, ulong dbId, ulong regionRef, ulong difficultyRef, 
            AvatarSlotInfo[] slots, CommunityMemberOnlineStatus onlineStatus, string unkName, int[] archiveCircleIds)
        {
            Name = name;
            DbId = dbId;
            RegionRef = regionRef;
            DifficultyRef = difficultyRef;
            Slots = slots;
            OnlineStatus = onlineStatus;
            MemberName = name;
            UnkName = unkName;
            ConsoleAccountId1 = 0;
            ConsoleAccountId2 = 0;
            ArchiveCircleIds = archiveCircleIds;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawString(Name);
                cos.WriteRawVarint64(DbId);
                cos.WriteRawVarint64(RegionRef);
                cos.WriteRawVarint64(DifficultyRef);

                cos.WriteRawByte((byte)Slots.Length);
                foreach (AvatarSlotInfo slot in Slots)
                    cos.WriteRawBytes(slot.Encode());

                cos.WriteRawInt32((int)OnlineStatus);
                cos.WriteRawString(MemberName);
                cos.WriteRawString(UnkName);
                cos.WriteRawVarint64(ConsoleAccountId1);
                cos.WriteRawVarint64(ConsoleAccountId2);

                cos.WriteRawInt32(ArchiveCircleIds.Length);
                foreach (int circleId in ArchiveCircleIds)
                    cos.WriteRawInt32(circleId);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"DbId: 0x{DbId:X}");
            sb.AppendLine($"RegionRef: {GameDatabase.GetPrototypeName(RegionRef)}");
            sb.AppendLine($"DifficultyRef: {GameDatabase.GetPrototypeName(DifficultyRef)}");
            for (int i = 0; i < Slots.Length; i++) sb.AppendLine($"Slot{i}: {Slots[i]}");
            sb.AppendLine($"OnlineStatus: {OnlineStatus}");
            sb.AppendLine($"MemberName: {MemberName}");
            sb.AppendLine($"UnkName: {UnkName}");
            sb.AppendLine($"ConsoleAccountId1: 0x{ConsoleAccountId1:X}");
            sb.AppendLine($"ConsoleAccountId2: 0x{ConsoleAccountId2:X}");
            for (int i = 0; i < ArchiveCircleIds.Length; i++) sb.AppendLine($"ArchiveCircleId{i}: {ArchiveCircleIds[i]}");
            return sb.ToString();
        }
    }
}
