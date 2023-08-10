using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities.Archives
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
            return Array.Empty<byte>();
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"Id: 0x{AchievementId.ToString("X")}");
                streamWriter.WriteLine($"Count: 0x{Count.ToString("X")}");
                streamWriter.WriteLine($"CompletionDate: 0x{CompletionDate.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
