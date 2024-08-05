using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionPlayerCondition : MissionCondition
    {
        protected MissionPlayerConditionPrototype PlayerProto => Prototype as MissionPlayerConditionPrototype;
        protected virtual PrototypeId MissionProtoRef => PrototypeId.Invalid;
        protected Player Player => Mission.MissionManager.Player;

        private long _count;
        public long Count { get => _count; set => SetCount(value); }
        protected virtual long MaxCount => 1;

        public MissionPlayerCondition(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype)
            : base(mission, owner, prototype)
        {
            _count = 0;
        }

        public override bool Initialize(int conditionIndex)
        {
            ConditionIndex = conditionIndex++;
            return base.Initialize(conditionIndex);
        }

        protected bool IsMissionPlayer(Player player)
        {
            var missionPlayer = Player;
            if (missionPlayer == null) return false;
            if (player == missionPlayer) return true;
            var playerProto = PlayerProto;
            if (playerProto != null && playerProto.PartyMembersGetCredit)
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
                var playerProto = PlayerProto;
                if (playerProto != null && playerProto.OpenMissionContributionValue != 0.0f)
                    SetPlayerContribution(player, (float)playerProto.OpenMissionContributionValue * count);
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

        public override bool IsCompleted() => Count >= MaxCount;
        public override void SetCompleted() => SetCount(Count);

        protected virtual bool GetCompletion() => false;

        protected virtual void SetCompletion(bool completed)
        {
            if (completed) SetCompleted();
            else ResetCompleted();
        }

        protected virtual void SetCount(long count)
        {
            _count = Math.Clamp(count, 0, MaxCount);
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
