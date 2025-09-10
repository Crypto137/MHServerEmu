using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMissionFailed : MissionPlayerCondition
    {
        private MissionConditionMissionFailedPrototype _proto;
        protected override PrototypeId MissionProtoRef => _proto.MissionPrototype;
        protected override long RequiredCount => _proto.Count;

        private Event<OpenMissionFailedGameEvent>.Action _openMissionFailedAction;
        private Event<PlayerFailedMissionGameEvent>.Action _playerFailedMissionAction;
        private Event<AvatarEnteredRegionGameEvent>.Action _avatarEnteredRegionAction;

        public MissionConditionMissionFailed(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // IronManCombatController
            _proto = prototype as MissionConditionMissionFailedPrototype;
            _openMissionFailedAction = OnOpenMissionFailed;
            _playerFailedMissionAction = OnPlayerFailedMission;
            _avatarEnteredRegionAction = OnAvatarEnteredRegion;
        }

        public override bool OnReset()
        {
            bool completed = EvaluateOnReset() && GetCompletion();
            SetCompletion(completed);
            return true;
        }

        protected override bool GetCompletion()
        {
            Mission mission = GetMission();
            if (mission == null) return false;
            return mission.State == MissionState.Failed;
        }

        public override bool EvaluateOnReset()
        {
            if (_proto.Count != 1) return false;
            if (_proto.MissionPrototype == PrototypeId.Invalid) return false;
            if (_proto.WithinRegions.HasValue()) return false;
            if (GameDatabase.GetPrototype<MissionPrototype>(MissionProtoRef) is OpenMissionPrototype) return false;

            return _proto.EvaluateOnReset;
        }

        private bool FilterMission(PrototypeId missionRef)
        {
            if (MissionProtoRef != PrototypeId.Invalid)
                return MissionProtoRef == missionRef;

            if (_proto.MissionKeyword != PrototypeId.Invalid)
            {
                var missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
                if (missionProto == null) return false;
                var missionKeyword = GameDatabase.GetPrototype<KeywordPrototype>(_proto.MissionKeyword);
                return missionProto.HasKeyword(missionKeyword);
            }

            return false;
        }

        private bool EvaluatePlayer(Player player, PrototypeId missionRef, bool participant, bool contributor)
        {
            if (player == null || IsMissionPlayer(player) == false) return false;

            if (FilterMission(missionRef) == false) return false;

            var manager = Mission.MissionManager;
            if (_proto.WithinRegions.HasValue())
            {
                var region = manager.GetRegion();
                if (region == null || region.FilterRegions(_proto.WithinRegions) == false) return false;
            }

            return _proto.CreditTo switch
            {
                DistributionType.Participants => participant,
                DistributionType.Contributors => contributor,
                _ => true
            };
        }

        private void OnOpenMissionFailed(in OpenMissionFailedGameEvent evt)
        {
            var missionRef = evt.MissionRef;
            if (FilterMission(missionRef) == false) return;

            var manager = Mission.MissionManager;
            if (_proto.WithinRegions.HasValue())
            {
                var region = manager.GetRegion();
                if (region == null || region.FilterRegions(_proto.WithinRegions) == false) return;
            }

            if (manager.IsPlayerMissionManager())
            {
                var player = manager.Player;
                if (player == null) return;
                var regionManager = player.GetRegion()?.MissionManager;
                if (regionManager == null) return;
                var mission = regionManager.FindMissionByDataRef(missionRef);
                if (mission == null || mission.IsOpenMission == false) return;

                bool isParticipant = false;
                bool isContributor = false;
                List<Player> participants = ListPool<Player>.Instance.Get();
                mission.GetParticipants(participants);

                var party = player.GetParty();
                if (party != null)
                {
                    foreach (var kvp in party)
                    {
                        Player member = Game.Current.EntityManager.GetEntityByDbGuid<Player>(kvp.Key);
                        if (member == null)
                            continue;

                        isParticipant |= participants.Contains(member);
                        isContributor |= mission.GetContribution(member) > 0.0f;
                    }
                }
                else
                {
                    isParticipant = participants.Contains(player);
                    isContributor = mission.GetContribution(player) > 0.0f;
                }

                if (EvaluatePlayer(player, missionRef, isParticipant, isContributor))
                    UpdatePlayerContribution(player);

                ListPool<Player>.Instance.Return(participants);
            }

            Count++;
        }

        private void OnPlayerFailedMission(in PlayerFailedMissionGameEvent evt)
        {
            var player = evt.Player;
            var missionRef = evt.MissionRef;

            if (GameDatabase.GetPrototype<OpenMissionPrototype>(missionRef) != null) return;

            bool participant = evt.Participant;
            bool contributor = evt.Contributor;

            if (EvaluatePlayer(player, missionRef, participant, contributor) == false) return;

            UpdatePlayerContribution(player);
            Count++;
        }

        private void OnAvatarEnteredRegion(in AvatarEnteredRegionGameEvent evt)
        {
            var player = evt.Player;

            if (player == null || IsMissionPlayer(player) == false) return;

            var missionRef = MissionProtoRef;
            var region = Mission.MissionManager.GetRegion();

            var manager = MissionManager.FindMissionManagerForMission(Player, region, missionRef);
            var mission = manager?.FindMissionByDataRef(missionRef);
            if (mission == null) return;
            if (mission.State != MissionState.Failed) return;

            UpdatePlayerContribution(player);
            Count++;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;

            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(MissionProtoRef);
            bool isOpenMission = missionProto is OpenMissionPrototype;

            if (missionProto == null || isOpenMission)
                region.OpenMissionFailedEvent.AddActionBack(_openMissionFailedAction);
            if (missionProto == null || isOpenMission == false)
                region.PlayerFailedMissionEvent.AddActionBack(_playerFailedMissionAction);

            if (_proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.AddActionBack(_avatarEnteredRegionAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;

            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(MissionProtoRef);
            bool isOpenMission = missionProto is OpenMissionPrototype;

            if (missionProto == null || isOpenMission)
                region.OpenMissionFailedEvent.RemoveAction(_openMissionFailedAction);
            if (missionProto == null || isOpenMission == false)
                region.PlayerFailedMissionEvent.RemoveAction(_playerFailedMissionAction);

            if (_proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.RemoveAction(_avatarEnteredRegionAction);
        }
    }
}
