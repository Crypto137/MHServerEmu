using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.GameServer.Achievements
{
    public class AchievementState
    {
        public ulong AchievementId { get; set; }
        public ulong Count { get; set; }
        public ulong CompletionDate { get; set; }

        public AchievementState(CodedInputStream stream)
        {
            AchievementId = stream.ReadRawVarint64();
            Count = stream.ReadRawVarint64();
            CompletionDate = stream.ReadRawVarint64();
        }

        public AchievementState(ulong achievementId, ulong count, ulong completionDate)
        {
            AchievementId = achievementId;
            Count = count;
            CompletionDate = completionDate;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(AchievementId);
                stream.WriteRawVarint64(Count);
                stream.WriteRawVarint64(CompletionDate);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Id: 0x{AchievementId:X}");
            sb.AppendLine($"Count: 0x{Count:X}");
            sb.AppendLine($"CompletionDate: 0x{CompletionDate:X}");
            return sb.ToString();
        }
    }
}
