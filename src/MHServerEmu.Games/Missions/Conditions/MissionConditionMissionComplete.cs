using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMissionComplete : MissionPlayerCondition
    {
        private MissionConditionMissionCompletePrototype _proto;
        protected override PrototypeId MissionProtoRef => _proto.MissionPrototype;
        protected override long RequiredCount => _proto.Count;

        private Event<OpenMissionCompleteGameEvent>.Action _openMissionCompleteAction;
        private Event<PlayerCompletedMissionGameEvent>.Action _playerCompletedMissionAction;
        private Event<AvatarEnteredRegionGameEvent>.Action _avatarEnteredRegionAction;

        public MissionConditionMissionComplete(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CH00RaftTutorial
            _proto = prototype as MissionConditionMissionCompletePrototype;
            _openMissionCompleteAction = OnOpenMissionComplete;
            _playerCompletedMissionAction = OnPlayerCompletedMission;
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
            return mission.State == MissionState.Completed;
        }

        public override bool EvaluateOnReset()
        {
            if (_proto.Count != 1) return false;
            if (_proto.MissionPrototype == PrototypeId.Invalid) return false;
            if (_proto.WithinRegions.HasValue()) return false;
            if (_proto.WithinAreas.HasValue()) return false;
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

            if (_proto.WithinAreas.HasValue())
            {
                var area = player.CurrentAvatar?.Area;
                if (area == null || _proto.WithinAreas.Contains(area.PrototypeDataRef) == false) return false;
            }

            return _proto.CreditTo switch
            {
                DistributionType.Participants => participant,
                DistributionType.Contributors => contributor,
                _ => true
            };
        }

        private void OnOpenMissionComplete(in OpenMissionCompleteGameEvent evt)
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

        private void OnPlayerCompletedMission(in PlayerCompletedMissionGameEvent evt)
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
            if (mission.State != MissionState.Completed) return;

            UpdatePlayerContribution(player);
            Count++;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;

            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(_proto.MissionPrototype);
            bool isOpenMission = missionProto is OpenMissionPrototype;

            if (missionProto == null || isOpenMission)
                region.OpenMissionCompleteEvent.AddActionBack(_openMissionCompleteAction); 
            if (missionProto == null || isOpenMission == false)
                region.PlayerCompletedMissionEvent.AddActionBack(_playerCompletedMissionAction);

            if (_proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.AddActionBack(_avatarEnteredRegionAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;

            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(_proto.MissionPrototype);
            bool isOpenMission = missionProto is OpenMissionPrototype;

            if (missionProto == null || isOpenMission)
                region.OpenMissionCompleteEvent.RemoveAction(_openMissionCompleteAction);
            if (missionProto == null || isOpenMission == false)
                region.PlayerCompletedMissionEvent.RemoveAction(_playerCompletedMissionAction);

            if (_proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.RemoveAction(_avatarEnteredRegionAction);
        }
    }
}
