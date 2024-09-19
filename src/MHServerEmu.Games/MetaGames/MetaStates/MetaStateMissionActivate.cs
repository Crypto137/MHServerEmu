using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.UI;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionActivate : MetaState
    {
        private MetaStateMissionActivatePrototype _proto;
        private Action<OpenMissionCompleteGameEvent> _openMissionCompleteAction;
        private Action<OpenMissionFailedGameEvent> _openMissionFailedAction;
        private EventPointer<MissionCompleteEvent> _missionCompleteEvent;

        public MetaStateMissionActivate(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateMissionActivatePrototype;
            _openMissionCompleteAction = OnOpenMissionComplete;
            _openMissionFailedAction = OnOpenMissionFailed;
            _missionCompleteEvent = new();
        }

        public override void OnApply()
        {
            var region = Region;
            if (region == null) return;

            var missionRef = _proto.Mission;
            bool hasMission = missionRef != PrototypeId.Invalid;

            if (hasMission)
                ActivateMission(missionRef);

            MetaGame.RemoveSpawnEvent(PrototypeDataRef);

            if (hasMission)
            {
                var popManager = Region.PopulationManager;
                popManager.DespawnSpawnGroups(missionRef);
                popManager.ResetEncounterSpawnPhase(missionRef);
            }

            var spawnEvent = MetaGame.GetSpawnEvent(PrototypeDataRef);
            if (spawnEvent == null) return;

            var spawnLocation = new SpawnLocation(region, _proto.PopulationAreaRestriction, null);
            spawnEvent.AddRequiredObjects(_proto.PopulationObjects, spawnLocation, missionRef, false, false);
            spawnEvent.Schedule();

            if (hasMission)
            {
                region.OpenMissionCompleteEvent.AddActionBack(_openMissionCompleteAction);
                region.OpenMissionFailedEvent.AddActionBack(_openMissionFailedAction);
            }
        }

        public override void OnRemove()
        {
            var region = Region;
            if (region == null) return;

            var missionRef = _proto.Mission;
            bool hasMission = missionRef != PrototypeId.Invalid;

            if (hasMission)
            {
                region.OpenMissionCompleteEvent.RemoveAction(_openMissionCompleteAction);
                region.OpenMissionFailedEvent.RemoveAction(_openMissionFailedAction);
            }

            if (_proto.RemovePopulationOnDeactivate)
            {
                MetaGame.RemoveSpawnEvent(PrototypeDataRef);
                var popManager = Region.PopulationManager;
                popManager.DespawnSpawnGroups(missionRef);
                popManager.ResetEncounterSpawnPhase(missionRef);
            }

            GameEventScheduler?.CancelEvent(_missionCompleteEvent);

            base.OnRemove();
        }

        public override void OnAddPlayer(Player player)
        {
            var missionRef = _proto.Mission;
            if (missionRef == PrototypeId.Invalid) return;

            var region = Region;
            if (region == null) return;

            var uiDataProvider = region.UIDataProvider;
            if (uiDataProvider == null) return;

            var missionManager = region.MissionManager;
            if (missionManager == null) return;

            var mission = missionManager.FindMissionByDataRef(_proto.Mission);
            if (mission == null) return;
            
            var missionState = mission.State;
            if (missionState != MissionState.Active) return;

            foreach (var objective in mission.Objectives)
            {
                if (objective.State != MissionObjectiveState.Active) continue;
                var objProto = objective.Prototype;
                if (objProto.MetaGameWidget != PrototypeId.Invalid && objProto.TimeLimitSeconds > 0)
                {
                    var syncData = uiDataProvider.GetWidget<UISyncData>(objProto.MetaGameWidget, missionRef);
                    if (syncData != null) 
                    {
                        syncData.SetTimeRemaining((long)objective.TimeRemainingForObjective.TotalMilliseconds);
                        syncData.SetAreaContext(missionRef);
                    }
                }

            }
        }

        private void ActivateMission(PrototypeId missionRef)
        {
            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
            if (missionProto is not OpenMissionPrototype) return;

            var manager = Region?.MissionManager;
            if (manager?.ShouldCreateMission(missionProto) == true)
                manager.ActivateMission(missionRef);
        }

        private void OnOpenMissionComplete(OpenMissionCompleteGameEvent evt)
        {
            if (evt.MissionRef != _proto.Mission) return;
            PlayerMetaStateComplete();

            if (_missionCompleteEvent.IsValid == false && _proto.DeactivateOnMissionCompDelayMS > 0)
                ScheduleMissionComplete(TimeSpan.FromMilliseconds(_proto.DeactivateOnMissionCompDelayMS));
            else
                OnMissionComplete();
        }

        private void OnOpenMissionFailed(OpenMissionFailedGameEvent evt)
        {
            if (evt.MissionRef != _proto.Mission) return;

            if (_missionCompleteEvent.IsValid == false && _proto.DeactivateOnMissionCompDelayMS > 0)
                ScheduleMissionComplete(TimeSpan.FromMilliseconds(_proto.DeactivateOnMissionCompDelayMS));
            else
                OnMissionComplete();
        }

        private void ScheduleMissionComplete(TimeSpan timeOffset)
        {
            var scheduler = GameEventScheduler;
            if (scheduler == null || timeOffset <= TimeSpan.Zero) return;
            if (_missionCompleteEvent.IsValid) return;

            scheduler.ScheduleEvent(_missionCompleteEvent, timeOffset, _pendingEvents);
            _missionCompleteEvent.Get().Initialize(this);
        }

        private void OnMissionComplete()
        {
            var region = Region;
            if (region == null) return;

            MetaGame.RemoveState(PrototypeDataRef);

            var missionManager = region.MissionManager;
            if (missionManager == null) return;

            var mission = missionManager.FindMissionByDataRef(_proto.Mission);
            if (mission != null)
            {
                var missionState = mission.State;
                if (missionState == MissionState.Completed)
                    MetaGame.ApplyStates(_proto.OnMissionCompletedApplyStates);
                else if (missionState == MissionState.Failed)
                    MetaGame.ApplyStates(_proto.OnMissionFailedApplyStates);
            }
        }

        protected class MissionCompleteEvent : CallMethodEvent<MetaStateMissionActivate>
        {
            protected override CallbackDelegate GetCallback() => (metaState) => metaState.OnMissionComplete();
        }
    }
}
