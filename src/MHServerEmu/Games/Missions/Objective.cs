using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.Missions
{
    public enum MissionObjectiveState
    {
        Invalid = 0,
        Available = 1,
        Active = 2,
        Completed = 3,
        Failed = 4,
        Skipped = 5
    }

    public class Objective
    {
        public ulong ObjectivesIndex { get; set; }
        public ulong ObjectiveIndex { get; set; }                   // NetMessageMissionObjectiveUpdate
        public MissionObjectiveState ObjectiveState { get; set; }
        public ulong ObjectiveStateExpireTime { get; set; }
        public InteractionTag[] InteractedEntities { get; set; }
        public ulong CurrentCount { get; set; }
        public ulong RequiredCount { get; set; }
        public ulong FailCurrentCount { get; set; }
        public ulong FailRequiredCount { get; set; }

        public Objective(CodedInputStream stream)
        {
            ObjectivesIndex = stream.ReadRawByte();
            ObjectiveIndex = stream.ReadRawByte();
            ObjectiveState = (MissionObjectiveState)stream.ReadRawInt32();
            ObjectiveStateExpireTime = stream.ReadRawVarint64();

            InteractedEntities = new InteractionTag[stream.ReadRawVarint64()];
            for (int i = 0; i < InteractedEntities.Length; i++)
                InteractedEntities[i] = new(stream);

            CurrentCount = stream.ReadRawVarint64();
            RequiredCount = stream.ReadRawVarint64();
            FailCurrentCount = stream.ReadRawVarint64();
            FailRequiredCount = stream.ReadRawVarint64();
        }

        public Objective(ulong objectiveIndex, MissionObjectiveState objectiveState, ulong objectiveStateExpireTime,
            InteractionTag[] interactedEntities, ulong currentCount, ulong requiredCount, ulong failCurrentCount, 
            ulong failRequiredCount)
        {
            ObjectivesIndex = objectiveIndex;
            ObjectiveIndex = objectiveIndex;            
            ObjectiveState = objectiveState;
            ObjectiveStateExpireTime = objectiveStateExpireTime;
            InteractedEntities = interactedEntities;
            CurrentCount = currentCount;
            RequiredCount = requiredCount;
            FailCurrentCount = failCurrentCount;
            FailRequiredCount = failRequiredCount;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawByte((byte)ObjectivesIndex);
            stream.WriteRawByte((byte)ObjectiveIndex);
            stream.WriteRawInt32((int)ObjectiveState);
            stream.WriteRawVarint64(ObjectiveStateExpireTime);
            stream.WriteRawVarint64((ulong)InteractedEntities.Length);
            foreach (InteractionTag tag in InteractedEntities) tag.Encode(stream);
            stream.WriteRawVarint64(CurrentCount);
            stream.WriteRawVarint64(RequiredCount);
            stream.WriteRawVarint64(FailCurrentCount);
            stream.WriteRawVarint64(FailRequiredCount);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ObjectivesIndex: 0x{ObjectivesIndex:X}");
            sb.AppendLine($"ObjectiveIndex: 0x{ObjectiveIndex:X}");
            sb.AppendLine($"ObjectiveState: {ObjectiveState}");
            sb.AppendLine($"ObjectiveStateExpireTime: 0x{ObjectiveStateExpireTime:X}");
            for (int i = 0; i < InteractedEntities.Length; i++) sb.AppendLine($"InteractedEntity{i}: {InteractedEntities[i]}");
            sb.AppendLine($"CurrentCount: 0x{CurrentCount:X}");
            sb.AppendLine($"RequiredCount: 0x{RequiredCount:X}");
            sb.AppendLine($"FailCurrentCount: 0x{FailCurrentCount:X}");
            sb.AppendLine($"FailRequiredCount: 0x{FailRequiredCount:X}");
            return sb.ToString();
        }
    }
}
