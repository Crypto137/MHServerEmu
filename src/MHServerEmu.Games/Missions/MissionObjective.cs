using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;

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

    public class MissionObjective : ISerialize
    {
        // Relevant protobuf: NetMessageMissionObjectiveUpdate

        private static readonly Logger Logger = LogManager.CreateLogger();

        private byte _prototypeIndex;

        private MissionObjectiveState _objectiveState;
        private TimeSpan _objectiveStateExpireTime;

        private readonly List<InteractionTag> _interactedEntityList = new();

        private ushort _currentCount;
        private ushort _requiredCount;
        private ushort _failCurrentCount;
        private ushort _failRequiredCount;

        public Mission Mission { get; }
        public Game Game { get => Mission.Game; }

        public byte PrototypeIndex { get => _prototypeIndex; }
        public MissionObjectiveState State { get => _objectiveState; }
        public TimeSpan TimeExpire { get => _objectiveStateExpireTime; }
        public TimeSpan TimeRemainingForObjective { get => _objectiveStateExpireTime - Clock.GameTime; }

        public MissionObjective(Mission mission, byte prototypeIndex)
        {
            Mission = mission;
            _prototypeIndex = prototypeIndex;
        }

        public MissionObjective(byte prototypeIndex, MissionObjectiveState objectiveState, TimeSpan objectiveStateExpireTime,
            IEnumerable<InteractionTag> interactedEntities, ushort currentCount, ushort requiredCount, ushort failCurrentCount, 
            ushort failRequiredCount)
        {
            _prototypeIndex = prototypeIndex;            
            _objectiveState = objectiveState;
            _objectiveStateExpireTime = objectiveStateExpireTime;
            _interactedEntityList.AddRange(interactedEntities);
            _currentCount = currentCount;
            _requiredCount = requiredCount;
            _failCurrentCount = failCurrentCount;
            _failRequiredCount = failRequiredCount;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _prototypeIndex);

            int state = (int)_objectiveState;
            success &= Serializer.Transfer(archive, ref state);
            _objectiveState = (MissionObjectiveState)state;

            success &= Serializer.Transfer(archive, ref _objectiveStateExpireTime);

            uint numInteractedEntities = (uint)_interactedEntityList.Count;
            success &= Serializer.Transfer(archive, ref numInteractedEntities);

            if (archive.IsPacking)
            {
                foreach (InteractionTag tag in _interactedEntityList)
                {
                    ulong entityId = tag.EntityId;
                    ulong regionId = tag.RegionId;
                    success &= Serializer.Transfer(archive, ref entityId);
                    success &= Serializer.Transfer(archive, ref regionId);
                    // timestamp - ignored in replication
                }
            }
            else
            {
                _interactedEntityList.Clear();

                for (uint i = 0; i < numInteractedEntities; i++)
                {
                    ulong entityId = 0;
                    ulong regionId = 0;
                    success &= Serializer.Transfer(archive, ref entityId);
                    success &= Serializer.Transfer(archive, ref regionId);
                    // timestamp - ignored in replication
                }
            }

            // Counts are serialized only in replication
            success &= Serializer.Transfer(archive, ref _currentCount);
            success &= Serializer.Transfer(archive, ref _requiredCount);
            success &= Serializer.Transfer(archive, ref _failCurrentCount);
            success &= Serializer.Transfer(archive, ref _failRequiredCount);

            return success;
        }

        public void Decode(CodedInputStream stream)
        {
            _interactedEntityList.Clear();

            _prototypeIndex = stream.ReadRawByte();
            _objectiveState = (MissionObjectiveState)stream.ReadRawInt32();
            _objectiveStateExpireTime = new(stream.ReadRawInt64() * 10);

            ulong numInteractedEntities = stream.ReadRawVarint64();
            for (ulong i = 0; i < numInteractedEntities; i++)
            {
                ulong entityId = stream.ReadRawVarint64();
                ulong regionId = stream.ReadRawVarint64();
                // timestamp - ignored in replication
                _interactedEntityList.Add(new(entityId, regionId));
            }

            _currentCount = (ushort)stream.ReadRawVarint32();
            _requiredCount = (ushort)stream.ReadRawVarint32();
            _failCurrentCount = (ushort)stream.ReadRawVarint32();
            _failRequiredCount = (ushort)stream.ReadRawVarint32();
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawByte((byte)_prototypeIndex);
            stream.WriteRawInt32((int)_objectiveState);
            stream.WriteRawInt64(_objectiveStateExpireTime.Ticks / 10);
            stream.WriteRawVarint32((uint)_interactedEntityList.Count);

            foreach (InteractionTag tag in _interactedEntityList)
            {
                stream.WriteRawVarint64(tag.EntityId);
                stream.WriteRawVarint64(tag.RegionId);
                // timestamp - ignored in replication
            }
                
            stream.WriteRawVarint32(_currentCount);
            stream.WriteRawVarint32(_requiredCount);
            stream.WriteRawVarint32(_failCurrentCount);
            stream.WriteRawVarint32(_failRequiredCount);
        }

        public override string ToString()
        {
            string expireTime = _objectiveStateExpireTime != TimeSpan.Zero ? Clock.GameTimeToDateTime(_objectiveStateExpireTime).ToString() : "0";
            return $"state={_objectiveState}, expireTime={expireTime}, numInteractions={_interactedEntityList.Count}, count={_currentCount}/{_requiredCount}, failCount={_failCurrentCount}/{_failRequiredCount}";
        }

        public bool HasInteractedWithEntity(WorldEntity entity)
        {
            ulong entityId = entity.Id;
            ulong regionId = 0;     // TODO: WorldEntity::IsInWorld, WorldEntity::GetRegionLocation(), WorldEntity::GetExitWorldRegionLocation

            if (_interactedEntityList.Count >= 20)
                Logger.Warn("HasInteractedWithEntity(): _interactedEntityList.Count >= 20");    // same check as the client

            foreach (InteractionTag tag in _interactedEntityList)
            {
                if (tag.EntityId == entityId && tag.RegionId == regionId)
                    return true;
            }

            return false;
        }

        public bool GetCompletionCount(ref ushort currentCount, ref ushort requiredCount)
        {
            currentCount = _currentCount;
            requiredCount = _requiredCount;
            return requiredCount > 1;
        }

        public bool GetFailCount(ref ushort currentCount, ref ushort requiredCount)
        {
            currentCount = _failCurrentCount;
            requiredCount = _failRequiredCount;
            return requiredCount > 1;
        }
    }
}
