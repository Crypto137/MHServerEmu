using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.Missions
{
    public struct InteractionTag
    {
        public ulong EntityId;
        public ulong RegionId;
        public ulong GameTime; // not used
    }
    public class Objective
    {
        public ulong ObjectivesIndex { get; set; }
        public ulong ObjectiveIndex { get; set; }   // NetMessageMissionObjectiveUpdate
        public int ObjectiveState { get; set; }
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
            ObjectiveState = stream.ReadRawInt32();
            ObjectiveStateExpireTime = stream.ReadRawVarint64();
            InteractedEntities = new InteractionTag[stream.ReadRawVarint64()];
            for (int i = 0; i < InteractedEntities.Length; i++) { 
                InteractedEntities[i].EntityId = stream.ReadRawVarint64();
                InteractedEntities[i].RegionId = stream.ReadRawVarint64();
            }
            CurrentCount = stream.ReadRawVarint64();
            RequiredCount = stream.ReadRawVarint64();
            FailCurrentCount = stream.ReadRawVarint64();
            FailRequiredCount = stream.ReadRawVarint64();
        }

        public Objective(ulong objectiveIndex, int objectiveState, ulong objectiveStateExpireTime,
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

        public byte[] Encode()
        {
            using (MemoryStream ms = new ())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawByte((byte)ObjectivesIndex);
                cos.WriteRawByte((byte)ObjectiveIndex);
                cos.WriteRawInt32(ObjectiveState);
                cos.WriteRawVarint64(ObjectiveStateExpireTime);
                cos.WriteRawVarint64((ulong)InteractedEntities.Length);
                foreach (InteractionTag Tag in InteractedEntities)
                {
                    cos.WriteRawVarint64(Tag.EntityId);
                    cos.WriteRawVarint64(Tag.RegionId);
                }
                cos.WriteRawVarint64(CurrentCount);
                cos.WriteRawVarint64(RequiredCount);
                cos.WriteRawVarint64(FailCurrentCount);
                cos.WriteRawVarint64(FailRequiredCount);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Index: 0x{ObjectivesIndex:X}");
            sb.AppendLine($"ObjectiveIndex: 0x{ObjectiveIndex:X}");
            sb.AppendLine($"ObjectiveState: 0x{ObjectiveState:X}");
            sb.AppendLine($"ObjectiveStateExpireTime: 0x{ObjectiveStateExpireTime:X}");
            sb.AppendLine($"InteractedEntities: {InteractedEntities.Length}");
            sb.AppendLine($"CurrentCount: 0x{CurrentCount:X}");
            sb.AppendLine($"RequiredCount: 0x{RequiredCount:X}");
            sb.AppendLine($"FailCurrentCount: 0x{FailCurrentCount:X}");
            sb.AppendLine($"FailRequiredCount: 0x{FailRequiredCount:X}");
            return sb.ToString();
        }
    }
}
