using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionPlayerCondition : MissionCondition
    {
        private MissionPlayerConditionPrototype _proto;
        protected virtual PrototypeId MissionProtoRef => PrototypeId.Invalid;
        protected Player Player => Mission.MissionManager.Player;

        private long _count;
        public long Count { get => _count; set => SetCount(value); }
        protected virtual long RequiredCount => 1;

        public MissionPlayerCondition(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype)
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionPlayerConditionPrototype;
            _count = 0;
        }

        public override bool Serialize(Archive archive)
        {
            return Serializer.Transfer(archive, ref _count);
        }

        public override bool Initialize(int conditionIndex)
        {
            ConditionIndex = conditionIndex++;
            return base.Initialize(conditionIndex);
        }

        public override bool GetCompletionCount(ref long currentCount, ref long requiredCount, bool isRequired)
        {
            if (RequiredCount > 1 || isRequired)
            {
                currentCount += Count;
                requiredCount += RequiredCount;
            }
            return requiredCount > 0;
        }

        protected bool IsMissionPlayer(Player player)
        {
            var missionPlayer = Player;
            if (missionPlayer == null || player == missionPlayer) return true;
            if (_proto.PartyMembersGetCredit)
            {
                var party = missionPlayer.Party;
                if (party != null && party.IsMember(player.DatabaseUniqueId)) return true;
            }
            return false;
        }

        protected void UpdatePlayerContribution(Player player, float count = 1.0f)
        {
            if (count == 0) return;
            if (Mission.IsOpenMission)
            {
                if (_proto.OpenMissionContributionValue != 0.0f)
                    SetPlayerContribution(player, (float)_proto.OpenMissionContributionValue * count);
            }
        }

        protected void SetPlayerContribution(Player player, float contributionValue)
        {
            if (contributionValue == 0.0f) return;
            if (Mission.IsOpenMission) 
                Mission.AddContribution(player, contributionValue);
        }

        protected Mission GetMission()
        {
            var missionRef = MissionProtoRef;
            if (missionRef != PrototypeId.Invalid)
            {
                var missionRegion = Mission.MissionManager.GetRegion();
                var manager = MissionManager.FindMissionManagerForMission(Player, missionRegion, missionRef);
                return manager?.FindMissionByDataRef(missionRef);
            }
            else
                return Mission;
        }

        public override bool IsCompleted() => Count >= RequiredCount;
        public override void SetCompleted() => SetCount(RequiredCount);

        protected virtual bool GetCompletion() => false;

        protected virtual void SetCompletion(bool completed)
        {
            if (completed) SetCompleted();
            else ResetCompleted();
        }

        protected virtual void SetCount(long count)
        {
            _count = Math.Clamp(count, 0, RequiredCount);
            OnUpdate();
        }

        protected void ResetCompleted()
        {
            SetCount(0);
        }

        public override bool OnReset()
        {
            ResetCompleted();
            return true;
        }
    }
}
