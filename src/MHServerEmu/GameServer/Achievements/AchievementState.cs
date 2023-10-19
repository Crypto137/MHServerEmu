using System.Text;
using Google.ProtocolBuffers;
using Gazillion;

namespace MHServerEmu.GameServer.Achievements
{
    public class AchievementState
    {
        public uint Id { get; set; }
        public uint Count { get; set; }
        public ulong CompletedDate { get; set; }

        public AchievementState(CodedInputStream stream)
        {
            Id = stream.ReadRawVarint32();
            Count = stream.ReadRawVarint32();
            CompletedDate = stream.ReadRawVarint64();
        }

        public AchievementState(uint id, uint count, ulong completedDate)
        {
            Id = id;
            Count = count;
            CompletedDate = completedDate;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(Id);
            stream.WriteRawVarint64(Count);
            stream.WriteRawVarint64(CompletedDate);
        }

        public NetMessageAchievementStateUpdate.Types.AchievementState ToNetStruct()
        {
            return NetMessageAchievementStateUpdate.Types.AchievementState.CreateBuilder()
                .SetId(Id)
                .SetCount(Count)
                .SetCompleteddate(CompletedDate)
                .Build();
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Id: {Id}");
            sb.AppendLine($"Count: {Count}");
            sb.AppendLine($"CompletionDate: 0x{CompletedDate:X}");
            return sb.ToString();
        }
    }
}
