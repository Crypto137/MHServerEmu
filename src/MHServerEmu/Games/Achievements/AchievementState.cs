using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.Achievements
{
    public class AchievementState
    {
        public uint Id { get; set; }
        public uint Count { get; set; }
        public long CompletedDate { get; set; }     // DateTime in microseconds

        public AchievementState(CodedInputStream stream)
        {
            Id = stream.ReadRawVarint32();
            Count = stream.ReadRawVarint32();
            CompletedDate = stream.ReadRawInt64();
        }

        public AchievementState(uint id, uint count, long completedDate)
        {
            Id = id;
            Count = count;
            CompletedDate = completedDate;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(Id);
            stream.WriteRawVarint64(Count);
            stream.WriteRawInt64(CompletedDate);
        }

        public NetMessageAchievementStateUpdate.Types.AchievementState ToNetStruct()
        {
            return NetMessageAchievementStateUpdate.Types.AchievementState.CreateBuilder()
                .SetId(Id)
                .SetCount(Count)
                .SetCompleteddate((ulong)CompletedDate)
                .Build();
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Id: {Id}");
            sb.AppendLine($"Count: {Count}");
            sb.AppendLine($"CompletionDate: {Clock.DateTimeMicrosecondsToDateTime(CompletedDate)}");
            return sb.ToString();
        }
    }
}
