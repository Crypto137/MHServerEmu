using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionSequencer : MetaState
    {
	    private MetaStateMissionSequencerPrototype _proto;
        private Action<OpenMissionCompleteGameEvent> _openMissionCompleteAction;
        private Action<OpenMissionFailedGameEvent> _openMissionFailedAction;
        private EventPointer<SpawnEntryEvent> _spawnEntryEvent;
        private EventPointer<MissionCompleteEvent> _missionCompleteEvent;
        private List<MetaMissionEntryPrototype> _entries;
        private int _index;

        public MetaStateMissionSequencer(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateMissionSequencerPrototype;
            _openMissionCompleteAction = OnOpenMissionComplete;
            _openMissionFailedAction = OnOpenMissionFailed;
            _missionCompleteEvent = new();
            _spawnEntryEvent = new();
            _entries = new();
            _index = 0;
        }

        public override void OnApply()
        {
            var region = Region;
            if (region == null) return;

            ScheduleSpawnEntry(TimeSpan.Zero, 0);

            region.OpenMissionCompleteEvent.AddActionBack(_openMissionCompleteAction);
            region.OpenMissionFailedEvent.AddActionBack(_openMissionFailedAction);
        }

        private void ScheduleSpawnEntry(TimeSpan timeOffset, int index)
        {
            var scheduler = GameEventScheduler;
            if (scheduler == null || timeOffset < TimeSpan.Zero) return;
            if (_spawnEntryEvent.IsValid) return;

            scheduler.ScheduleEvent(_spawnEntryEvent, timeOffset, _pendingEvents);
            _spawnEntryEvent.Get().Initialize(this, index);
        }

        private void OnSpawnEntry(int index)
        {
            if (_proto.Sequence.IsNullOrEmpty()) return;
            if (index < 0 || index >= _proto.Sequence.Length) return;

            _index = index;

            var entryProto = _proto.Sequence[_index];
            if (entryProto == null) return;

            var region = Region;
            if (region == null) return;

            var missionRef = entryProto.Mission;
            if (missionRef == PrototypeId.Invalid) return;

            _entries.Add(entryProto);
            
            ActivateMission(missionRef);

            var spawnEvent = MetaGame.GetSpawnEvent(PrototypeDataRef);
            if (spawnEvent == null) return;

            var spawnLocation = new SpawnLocation(region, _proto.PopulationAreaRestriction, null);
            spawnLocation.AddAreaRefs(entryProto.PopulationAreaRestriction);
            spawnEvent.AddRequiredObjects(entryProto.PopulationObjects, spawnLocation, missionRef, false, false);
            spawnEvent.Schedule();
        }

        public override void OnRemove()
        {
            var region = Region;
            if (region == null) return;

            if (_proto.Sequence.IsNullOrEmpty()) return;

            region.OpenMissionCompleteEvent.RemoveAction(_openMissionCompleteAction);
            region.OpenMissionFailedEvent.RemoveAction(_openMissionFailedAction);

            foreach(var state in _proto.Sequence)
                SetMissionFailedState(state.Mission);

            if (_proto.RemovePopulationOnDeactivate)
            {
                foreach (var entry in _entries)
                {
                    MetaGame.RemoveSpawnEvent(entry.Mission);
                    var popManager = region.PopulationManager;
                    popManager.DespawnSpawnGroups(entry.Mission);
                    popManager.ResetEncounterSpawnPhase(entry.Mission);
                }
            }

            GameEventScheduler?.CancelEvent(_missionCompleteEvent);

            base.OnRemove();
        }

        private void OnOpenMissionComplete(OpenMissionCompleteGameEvent evt)
        {
            if (_proto.Sequence.IsNullOrEmpty()) return;
            if (_index < 0 || _index >= _proto.Sequence.Length) return;

            var entryProto = _proto.Sequence[_index];
            if (entryProto == null) return;

            if (evt.MissionRef != entryProto.Mission) return;

            int last = _proto.Sequence.Length - 1;

            if (_index < last)
            {
                ScheduleSpawnEntry(TimeSpan.FromMilliseconds(_proto.SequenceAdvanceDelayMS), _index + 1);
            }
            else if (_index == last) 
            {
                PlayerMetaStateComplete();
                MetaGame.ScheduleActivateGameMode(_proto.OnSequenceCompleteSetMode);

                if (_missionCompleteEvent.IsValid == false && _proto.DeactivateOnMissionCompDelayMS > 0)
                    ScheduleMissionComplete(TimeSpan.FromMilliseconds(_proto.DeactivateOnMissionCompDelayMS));
                else
                    OnMissionComplete();
            }
        }

        private void OnOpenMissionFailed(OpenMissionFailedGameEvent evt)
        {
            if (_proto.Sequence.IsNullOrEmpty()) return;
            if (_index < 0 || _index >= _proto.Sequence.Length) return;

            var entryProto = _proto.Sequence[_index];
            if (entryProto == null) return;

            if (evt.MissionRef != entryProto.Mission) return;

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

        public void OnMissionComplete()
        {
            var region = Region;
            if (region == null) return;

            MetaGame.RemoveState(PrototypeDataRef);

            var missionManager = region.MissionManager;
            if (missionManager == null) return;

            if (_proto.Sequence.IsNullOrEmpty()) return;
            if (_index < 0 || _index >= _proto.Sequence.Length) return;

            var entryProto = _proto.Sequence[_index];
            if (entryProto == null) return;

            var mission = missionManager.FindMissionByDataRef(entryProto.Mission);
            if (mission != null)
            {
                var missionState = mission.State;
                if (missionState == MissionState.Completed)
                    MetaGame.ApplyStates(_proto.OnMissionCompletedApplyStates);
                else if (missionState == MissionState.Failed)
                    MetaGame.ApplyStates(_proto.OnMissionFailedApplyStates);
            }
        }

        protected class MissionCompleteEvent : CallMethodEvent<MetaStateMissionSequencer>
        {
            protected override CallbackDelegate GetCallback() => (metaState) => metaState.OnMissionComplete();
        }

        protected class SpawnEntryEvent : CallMethodEventParam1<MetaStateMissionSequencer, int>
        {
            protected override CallbackDelegate GetCallback() => (metaState, index) => metaState.OnSpawnEntry(index);
        }
    }
}
