using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions
{
    [AssetEnum((int)Invalid)]
    public enum MissionState
    {
        Invalid = 0,
        Inactive = 1,
        Available = 2,
        Active = 3,
        Completed = 4,
        Failed = 5,
    }

    public class Mission
    {
        public MissionState State { get; set; }
        public TimeSpan TimeExpireCurrentState { get; set; }
        public PrototypeId PrototypeId { get; set; }
        public int Random { get; set; }
        public Objective[] Objectives { get; set; }
        public ulong[] Participants { get; set; }
        public bool Suspended { get; set; }

        public MissionManager MissionManager { get; }
        public Game Game { get; }

        public Mission(CodedInputStream stream, BoolDecoder boolDecoder)
        {            
            State = (MissionState)stream.ReadRawInt32();
            TimeExpireCurrentState = new(stream.ReadRawInt64() * 10);
            PrototypeId = stream.ReadPrototypeRef<Prototype>();
            Random = stream.ReadRawInt32();

            Objectives = new Objective[stream.ReadRawVarint64()];
            for (int i = 0; i < Objectives.Length; i++)
                Objectives[i] = new(stream);

            Participants = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < Participants.Length; i++)
                Participants[i] = stream.ReadRawVarint64();

            Suspended = boolDecoder.ReadBool(stream);
        }

        public Mission(MissionState state, TimeSpan timeExpireCurrentState, PrototypeId prototypeId,
            int random, Objective[] objectives, ulong[] participants, bool suspended)
        {
            State = state;
            TimeExpireCurrentState = timeExpireCurrentState;
            PrototypeId = prototypeId;
            Random = random;
            Objectives = objectives;
            Participants = participants;
            Suspended = suspended;
        }

        public Mission(PrototypeId prototypeId, int random)
        {
            State = MissionState.Active;
            TimeExpireCurrentState = TimeSpan.Zero;
            PrototypeId = prototypeId;
            Random = random;
            Objectives = new Objective[] { new(0x0, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) };
            Participants = Array.Empty<ulong>();
            Suspended = false;
        }

        public Mission(MissionManager missionManager, PrototypeId missionRef)
        {
            MissionManager = missionManager;
            Game = MissionManager.Game;
            PrototypeId = missionRef;

            // TODO other fields
        }

        public void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {            
            stream.WriteRawInt32((int)State);
            stream.WriteRawInt64(TimeExpireCurrentState.Ticks / 10);
            stream.WritePrototypeRef<Prototype>(PrototypeId);
            stream.WriteRawInt32(Random);

            stream.WriteRawVarint64((ulong)Objectives.Length);
            foreach (Objective objective in Objectives) objective.Encode(stream);

            stream.WriteRawVarint64((ulong)Participants.Length);
            foreach (ulong Participant in Participants) stream.WriteRawVarint64(Participant);

            boolEncoder.WriteBuffer(stream);   // Suspended
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"State: {State}");
            string expireTime = TimeExpireCurrentState != TimeSpan.Zero ? Clock.GameTimeToDateTime(TimeExpireCurrentState).ToString() : "0";
            sb.AppendLine($"TimeExpireCurrentState: {expireTime}");
            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypeName(PrototypeId)}");
            sb.AppendLine($"Random: 0x{Random:X}");

            for (int i = 0; i < Objectives.Length; i++)
                sb.AppendLine($"Objectives[{i}]: {Objectives[i]}");

            sb.Append("Participants: ");
            for (int i = 0; i < Participants.Length; i++)
                sb.Append($"{Participants[i]} ");
            sb.AppendLine();

            sb.AppendLine($"Suspended: {Suspended}");
            return sb.ToString();
        }

    }
}
