using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionProgression : MetaState
    {
	    private MetaStateMissionProgressionPrototype _proto;
        private Action<OpenMissionCompleteGameEvent> _openMissionCompleteAction;
        private Action<OpenMissionFailedGameEvent> _openMissionFailedAction;
        private Action<PlayerDeathLimitHitGameEvent> _playerDeathLimitHitAction;
        private EventPointer<StatePickIntervalEvent> _stateIntervalEvent = new();
        private EventPointer<PlayerDeathLimitEvent> _playerDeathLimitEvent = new();
        private PrototypeId _stateRef;
        private PrototypeId _lastStateRef;

        public MetaStateMissionProgression(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateMissionProgressionPrototype;
            _openMissionCompleteAction = OnOpenMissionComplete;
            _openMissionFailedAction = OnOpenMissionFailed;
            _playerDeathLimitHitAction = OnPlayerDeathLimitHit;
        }

        public override void OnApply()
        {
            if (_proto.BeforeFirstStateDelayMS > 0)
                ScheduleStateInterval(TimeSpan.FromMilliseconds(_proto.BeforeFirstStateDelayMS));
            else
                ScheduleStateInterval(TimeSpan.FromMilliseconds(1));

            var region = Region;
            if (region == null) return;

            region.OpenMissionCompleteEvent.AddActionBack(_openMissionCompleteAction);
            region.OpenMissionFailedEvent.AddActionBack(_openMissionFailedAction);
            region.PlayerDeathLimitHitEvent.AddActionBack(_playerDeathLimitHitAction);
        }

        public override void OnRemove()
        {
            if (_stateRef != PrototypeId.Invalid)
                MetaGame.RemoveState(_stateRef);

            var region = Region;
            if (region == null) return;

            region.OpenMissionCompleteEvent.RemoveAction(_openMissionCompleteAction);
            region.OpenMissionFailedEvent.RemoveAction(_openMissionFailedAction);
            region.PlayerDeathLimitHitEvent.RemoveAction(_playerDeathLimitHitAction);

            base.OnRemove();
        }

        private void OnOpenMissionComplete(OpenMissionCompleteGameEvent evt)
        {
            var missionRef = evt.MissionRef;
            if (missionRef == PrototypeId.Invalid || _proto.StatesProgression.IsNullOrEmpty()) return;

            foreach(var stateRef in _proto.StatesProgression)
            {
                var stateProto = GameDatabase.GetPrototype<MetaStateMissionActivatePrototype>(stateRef);
                if (stateProto == null || stateProto.Mission != missionRef) continue;

                _lastStateRef = stateRef;
                if (_proto.SaveProgressionStateToDb)
                    SaveProgressionStateToDb(_lastStateRef);

                ScheduleStateInterval(TimeSpan.FromMilliseconds(_proto.BetweenStatesIntervalMS));
                _stateRef = PrototypeId.Invalid;
                break;
            }
        }

        private void SaveProgressionStateToDb(PrototypeId stateRef)
        {
            var region = Region;
            if (region == null) return;

            foreach(var player in MetaGame.Players)
            {
                var avatar = player.CurrentAvatar;
                if (avatar != null) MetaGame.SaveMetaStateProgress(avatar, region.PrototypeDataRef, region.DifficultyTierRef, stateRef);
            }
        }

        private void OnOpenMissionFailed(OpenMissionFailedGameEvent evt)
        {
            var missionRef = evt.MissionRef; 
            if (missionRef == PrototypeId.Invalid || _proto.StatesProgression.IsNullOrEmpty()) return;

            foreach (var stateRef in _proto.StatesProgression)
            {
                var stateProto = GameDatabase.GetPrototype<MetaStateMissionActivatePrototype>(stateRef);
                if (stateProto == null || stateProto.Mission != missionRef) continue;
                _stateRef = PrototypeId.Invalid;
                break;
            }
        }

        private void OnPlayerDeathLimitHit(PlayerDeathLimitHitGameEvent evt)
        {
            var scheduler = GameEventScheduler;
            if (scheduler == null) return;
            if (_playerDeathLimitEvent.IsValid) return;

            scheduler.ScheduleEvent(_playerDeathLimitEvent, TimeSpan.Zero, _pendingEvents);
            _playerDeathLimitEvent.Get().Initialize(this);
        }

        private void OnPlayerDeathLimit()
        {
            var manager = Region?.MissionManager;
            if (manager == null) return;

            TeleportPlayersToStart();

            var mode = MetaGame.CurrentMode;
            if (mode == null) return;
            mode.ResetStates();

            if (_stateRef != PrototypeId.Invalid)
            {
                MetaGame.Properties[PropertyEnum.MetaStateWaveForce, PrototypeDataRef] = _stateRef;
                var stateProto = GameDatabase.GetPrototype<MetaStateMissionActivatePrototype>(_stateRef);
                if (stateProto == null) return;

                var mission = manager.FindMissionByDataRef(stateProto.Mission);
                if (mission != null)
                {
                    var missionState = mission.State;
                    if (missionState != MissionState.Failed)
                        mission.SetState(MissionState.Failed);
                }

                // clear State
                MetaGame.RemoveState(_stateRef);
                _stateRef = PrototypeId.Invalid;
                _lastStateRef = PrototypeId.Invalid;
            }
            
            ScheduleStateInterval(TimeSpan.FromMilliseconds(1));
        }

        private void TeleportPlayersToStart()
        {
            var players = MetaGame.Players;
            foreach (var player in players.ToArray())
            {
                var avatar = player.CurrentAvatar;
                if (avatar == null) continue;

                // reset avatar status
                if (avatar.IsDead) avatar.Resurrect();

                foreach (var primaryManaBehaviorProto in avatar.GetPrimaryResourceManaBehaviors())
                {
                    float endurance = avatar.Properties[PropertyEnum.EnduranceMax, primaryManaBehaviorProto.ManaType];
                    avatar.Properties[PropertyEnum.Endurance, primaryManaBehaviorProto.ManaType] = endurance;
                }

                avatar.Properties[PropertyEnum.Health] = avatar.Properties[PropertyEnum.HealthMax];

                // teleport to start target
                var startTarget = Region.GetStartTarget(player);
                if (startTarget != PrototypeId.Invalid)
                    Transition.TeleportToLocalTarget(player, startTarget);
            }
        }

        private void ScheduleStateInterval(TimeSpan interval)
        {
            var scheduler = GameEventScheduler;
            if (scheduler == null || interval <= TimeSpan.Zero) return;
            if (_stateIntervalEvent.IsValid) return;

            scheduler.ScheduleEvent(_stateIntervalEvent, interval, _pendingEvents);
            _stateIntervalEvent.Get().Initialize(this);
        }

        private void OnStateInterval()
        {
            var region = Region;
            if (region == null) return;

            var stateRef = PrototypeId.Invalid;

            var forsePropId = new PropertyId(PropertyEnum.MetaStateWaveForce, PrototypeDataRef);
            if (MetaGame.Properties.HasProperty(forsePropId))
            {
                stateRef = MetaGame.Properties[forsePropId];
                MetaGame.Properties.RemoveProperty(forsePropId);
            }

            if (stateRef == PrototypeId.Invalid)
                stateRef = _proto.NextState(_lastStateRef);

            if (stateRef != PrototypeId.Invalid)
            {
                if (MetaGame.ApplyMetaState(_stateRef))
                {
                    _lastStateRef = stateRef;
                    _stateRef = stateRef;
                }
                else
                {
                    _lastStateRef = PrototypeId.Invalid;
                    _stateRef = stateRef;
                    ScheduleStateInterval(TimeSpan.FromMilliseconds(_proto.BetweenStatesIntervalMS));
                }
            }
            else MetaGame.ActivateNextMode();
        }

        protected class StatePickIntervalEvent : CallMethodEvent<MetaStateMissionProgression>
        {
            protected override CallbackDelegate GetCallback() => (metaState) => metaState.OnStateInterval();
        }

        protected class PlayerDeathLimitEvent : CallMethodEvent<MetaStateMissionProgression>
        {
            protected override CallbackDelegate GetCallback() => (metaState) => metaState.OnPlayerDeathLimit();
        }
    }
}
