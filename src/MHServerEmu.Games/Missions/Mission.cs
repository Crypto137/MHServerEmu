using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
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

    public class Mission : ISerialize
    {
        // Relevant protobuf: NetMessageMissionUpdate

        private static readonly Logger Logger = LogManager.CreateLogger();

        private MissionState _state;
        private TimeSpan _timeExpireCurrentState;
        private PrototypeId _prototypeDataRef;
        private int _unkRandom;     // random integer rolled for each mission
        private SortedDictionary<byte, MissionObjective> _objectiveDict = new();
        private SortedSet<ulong> _participants = new();
        private bool _isSuspended;

        public MissionState State { get => _state; }
        public TimeSpan TimeExpireCurrentState { get => _timeExpireCurrentState; }
        public TimeSpan TimeRemainingForCurrentState { get => _timeExpireCurrentState - Clock.GameTime; }
        public PrototypeId PrototypeDataRef { get => _prototypeDataRef; }
        public MissionPrototype Prototype { get; }
        public int UnkRandom { get => _unkRandom; }
        public SortedSet<ulong> Participants { get => _participants; }
        public bool IsSuspended { get => _isSuspended; }

        public MissionManager MissionManager { get; }
        public Game Game { get; }

        public Mission(MissionManager missionManager, PrototypeId missionRef)
        {
            MissionManager = missionManager;
            Game = MissionManager.Game;
            _prototypeDataRef = missionRef;
            Prototype = GameDatabase.GetPrototype<MissionPrototype>(_prototypeDataRef);
        }

        public Mission(MissionState state, TimeSpan timeExpireCurrentState, PrototypeId prototypeDataRef,
            int unkRandom, IEnumerable<MissionObjective> objectives, IEnumerable<ulong> participants, bool isSuspended)
        {
            _state = state;
            _timeExpireCurrentState = timeExpireCurrentState;
            _prototypeDataRef = prototypeDataRef;
            Prototype = GameDatabase.GetPrototype<MissionPrototype>(_prototypeDataRef);
            _unkRandom = unkRandom;

            foreach (MissionObjective objective in objectives)
                _objectiveDict.Add(objective.PrototypeIndex, objective);

            _participants.UnionWith(participants);
            _isSuspended = isSuspended;
        }

        public Mission(PrototypeId prototypeDataRef, int unkRandom)
        {
            _state = MissionState.Active;
            _timeExpireCurrentState = TimeSpan.Zero;
            _prototypeDataRef = prototypeDataRef;
            Prototype = GameDatabase.GetPrototype<MissionPrototype>(_prototypeDataRef);
            _unkRandom = unkRandom;

            _objectiveDict.Add(0, new(0x0, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0));
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            int state = (int)_state;
            success &= Serializer.Transfer(archive, ref state);
            _state = (MissionState)state;

            success &= Serializer.Transfer(archive, ref _timeExpireCurrentState);
            success &= Serializer.Transfer(archive, ref _prototypeDataRef);
            // old versions contain an ItemSpec map here
            success &= Serializer.Transfer(archive, ref _unkRandom);

            // Objectives, participants, and suspension status are serialized only for replication
            success &= SerializeObjectives(archive);
            success &= Serializer.Transfer(archive, ref _participants);
            success &= Serializer.Transfer(archive, ref _isSuspended);

            return success;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_state)}: {_state}");
            string expireTime = TimeExpireCurrentState != TimeSpan.Zero ? Clock.GameTimeToDateTime(TimeExpireCurrentState).ToString() : "0";
            sb.AppendLine($"{nameof(_timeExpireCurrentState)}: {expireTime}");
            sb.AppendLine($"{nameof(_prototypeDataRef)}: {GameDatabase.GetPrototypeName(_prototypeDataRef)}");
            sb.AppendLine($"{nameof(_unkRandom)}: 0x{_unkRandom:X}");

            foreach (var kvp in _objectiveDict)
                sb.AppendLine($"{nameof(_objectiveDict)}[{kvp.Key}]: {kvp.Value}");

            sb.Append($"{nameof(_participants)}: ");
            foreach (ulong participantId in _participants)
                sb.Append($"{participantId} ");
            sb.AppendLine();

            sb.AppendLine($"{nameof(_isSuspended)}: {_isSuspended}");
            return sb.ToString();
        }
        
        public void SetState(MissionState newState)
        {
            _state = newState;
        }

        public MissionObjective GetObjectiveByObjectiveIndex(byte objectiveIndex)
        {
            if (_objectiveDict.TryGetValue(objectiveIndex, out MissionObjective objective) == false)
                return Logger.WarnReturn<MissionObjective>(null, $"GetObjectiveByObjectiveIndex(): Objective index {objectiveIndex} is not valid");

            return objective;
        }

        public MissionObjective CreateObjective(byte objectiveIndex)
        {
            return new(this, objectiveIndex);
        }

        public MissionObjective InsertObjective(byte objectiveIndex, MissionObjective objective)
        {
            if (_objectiveDict.TryAdd(objectiveIndex, objective) == false)
                return Logger.WarnReturn<MissionObjective>(null, $"InsertObjective(): Failed to insert objective with index {objectiveIndex}");

            return objective;
        }

        public bool AddParticipant(Player player)
        {
            return _participants.Add(player.Id);
        }

        private bool SerializeObjectives(Archive archive)
        {
            bool success = true;

            ulong numObjectives = (ulong)_objectiveDict.Count;
            success &= Serializer.Transfer(archive, ref numObjectives);

            if (archive.IsPacking)
            {
                foreach (var kvp in _objectiveDict)
                {
                    byte index = kvp.Key;
                    MissionObjective objective = kvp.Value;
                    success &= Serializer.Transfer(archive, ref index);
                    success &= Serializer.Transfer(archive, ref objective);
                }
            }
            else
            {
                for (uint i = 0; i < numObjectives; i++)
                {
                    byte index = 0;
                    success &= Serializer.Transfer(archive, ref index);

                    MissionObjective objective = CreateObjective(index);
                    success &= Serializer.Transfer(archive, ref objective);

                    InsertObjective(index, objective);
                }
            }

            return success;
        }

        public bool HasParticipant(Player player)
        {
            return Participants.Contains(player.Id);
        }

        public bool ShouldShowInteractIndicators()
        {
            if (Prototype == null) return false;
            return Prototype.ShowInteractIndicators;
        }
    }
}
