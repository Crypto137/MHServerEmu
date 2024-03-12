using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core;
using MHServerEmu.Core.Extensions;

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
        public TimeSpan ObjectiveStateExpireTime { get; set; }
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
            ObjectiveStateExpireTime = Clock.GameTimeMicrosecondsToTimeSpan(stream.ReadRawInt64());

            InteractedEntities = new InteractionTag[stream.ReadRawVarint64()];
            for (int i = 0; i < InteractedEntities.Length; i++)
                InteractedEntities[i] = new(stream);

            CurrentCount = stream.ReadRawVarint64();
            RequiredCount = stream.ReadRawVarint64();
            FailCurrentCount = stream.ReadRawVarint64();
            FailRequiredCount = stream.ReadRawVarint64();
        }

        public Objective(ulong objectiveIndex, MissionObjectiveState objectiveState, TimeSpan objectiveStateExpireTime,
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
            stream.WriteRawInt64(ObjectiveStateExpireTime.Ticks / 10);
            stream.WriteRawVarint64((ulong)InteractedEntities.Length);
            foreach (InteractionTag tag in InteractedEntities) tag.Encode(stream);
            stream.WriteRawVarint64(CurrentCount);
            stream.WriteRawVarint64(RequiredCount);
            stream.WriteRawVarint64(FailCurrentCount);
            stream.WriteRawVarint64(FailRequiredCount);
        }

        public override string ToString()
        {
            string expireTime = ObjectiveStateExpireTime != TimeSpan.Zero ? Clock.GameTimeToDateTime(ObjectiveStateExpireTime).ToString() : "0";
            return $"state={ObjectiveState}, expireTime={expireTime}, count={CurrentCount}/{RequiredCount}, failCount={FailCurrentCount}/{FailRequiredCount}";
        }
    }
}
