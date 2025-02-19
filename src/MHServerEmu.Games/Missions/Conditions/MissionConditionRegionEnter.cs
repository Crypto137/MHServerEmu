using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionRegionEnter : MissionPlayerCondition
    {
        private MissionConditionRegionEnterPrototype _proto;
        private Action<AvatarEnteredRegionGameEvent> _avatarEnteredRegionAction;
        private Action<CinematicFinishedGameEvent> _cinematicFinishedAction;
        private Action<LoadingScreenFinishedGameEvent> _loadingScreenFinishedAction;

        public MissionConditionRegionEnter(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CH00RaftTutorial
            _proto = prototype as MissionConditionRegionEnterPrototype;
            _avatarEnteredRegionAction = OnAvatarEnteredRegion;
            _cinematicFinishedAction = OnCinematicFinished;
            _loadingScreenFinishedAction = OnLoadingScreenFinished;
        }

        public override bool OnReset()
        {
            bool entered = false;

            List<Player> participants = ListPool<Player>.Instance.Get();
            if (Mission.GetParticipants(participants))
            {
                foreach (var player in participants)
                {
                    if (_proto.WaitForCinematicFinished && player.IsFullscreenObscured) continue;
                    var region = player.CurrentAvatar?.Region;
                    if (region != null && FilterRegion(region.Prototype))
                    {
                        entered = true;
                        break;
                    }
                }
            }
            ListPool<Player>.Instance.Return(participants);

            SetCompletion(entered);
            return true;
        }

        private bool FilterRegion(RegionPrototype regionProto)
        {
            if (regionProto == null) return false;
            if (regionProto.FilterRegion(_proto.RegionPrototype, _proto.RegionIncludeChildren, null)) return true;
            if (_proto.Keywords.HasValue() && regionProto.Keywords.HasValue())
                foreach (var keyword in _proto.Keywords)
                    if (regionProto.HasKeyword(keyword)) return true;

            return false;
        }

        private bool EvaluateRegion(Player player, PrototypeId regionRef)
        {
            if (player == null || IsMissionPlayer(player) == false) return false;
            if (_proto.WaitForCinematicFinished && player.IsFullscreenObscured) return false;
            var regionProto = GameDatabase.GetPrototype<RegionPrototype>(regionRef);
            return FilterRegion(regionProto);
        }

        private void OnAvatarEnteredRegion(AvatarEnteredRegionGameEvent evt)
        {
            var player = evt.Player;
            var regionRef = evt.RegionRef;
            if (EvaluateRegion(player, regionRef) == false) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        private void OnLoadingScreenFinished(LoadingScreenFinishedGameEvent evt)
        {
            var player = evt.Player;
            var regionRef = evt.RegionRef;
            if (EvaluateRegion(player, regionRef) == false) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        private void OnCinematicFinished(CinematicFinishedGameEvent evt)
        {
            var player = evt.Player;
            var region = player?.GetRegion();
            if (region == null) return;
            if (EvaluateRegion(player, region.PrototypeDataRef) == false) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.AvatarEnteredRegionEvent.AddActionBack(_avatarEnteredRegionAction);
            if (_proto.WaitForCinematicFinished)
            {
                region.CinematicFinishedEvent.AddActionBack(_cinematicFinishedAction);
                region.LoadingScreenFinishedEvent.AddActionBack(_loadingScreenFinishedAction);
            }
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.AvatarEnteredRegionEvent.RemoveAction(_avatarEnteredRegionAction);
            if (_proto.WaitForCinematicFinished)
            {
                region.CinematicFinishedEvent.RemoveAction(_cinematicFinishedAction);
                region.LoadingScreenFinishedEvent.RemoveAction(_loadingScreenFinishedAction);
            }
        }
    }
}
