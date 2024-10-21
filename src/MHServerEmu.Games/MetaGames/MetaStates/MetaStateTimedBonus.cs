using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions.Actions;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateTimedBonus : MetaState
    {
        private MetaStateTimedBonusPrototype _proto;
        private Action<OpenMissionCompleteGameEvent> _openMissionCompleteAction;
        private EventPointer<TimerEvent> _timerEvent = new();
        private readonly MissionActionList[] _onSuccessActionsList;
        private readonly MissionActionList[] _onFailActionsList;
        private MetaStateTimedBonusEntryPrototype _entryProto;
        private int _length;
        private int _index;
        private bool _success;

        public MetaStateTimedBonus(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateTimedBonusPrototype;
            _openMissionCompleteAction = OnOpenMissionComplete;

            if (_proto.Entries.HasValue())
            {
                _length = _proto.Entries.Length;
                _onSuccessActionsList = new MissionActionList[_length];
                _onFailActionsList = new MissionActionList[_length];
            }
        }        

        public override void OnApply()
        {
            if (_length == 0) return;

            _index = 0;
            StartTimer();

            var region = Region;
            if (region == null) return;

            region.OpenMissionCompleteEvent.AddActionBack(_openMissionCompleteAction); 
        }

        public override void OnReset()
        {
            _index = 0;

            var region = Region;
            if (region == null) return;

            var windgetRef = _proto.UIWidget;
            if (windgetRef != PrototypeId.Invalid)
                region.UIDataProvider?.DeleteWidget(windgetRef);
        }

        public override void OnRemove()
        {
            var region = Region;
            if (region == null) return;

            region.OpenMissionCompleteEvent.RemoveAction(_openMissionCompleteAction);

            var windgetRef = _proto.UIWidget;
            if (windgetRef != PrototypeId.Invalid)
                region.UIDataProvider?.DeleteWidget(windgetRef);

            if (_length > 0)
            {
                foreach (var action in _onSuccessActionsList) action?.Destroy();
                foreach (var action in _onFailActionsList) action?.Destroy();
                _length = 0;
            }

            base.OnRemove();
        }

        private void StartTimer()
        {
            var scheduler = GameEventScheduler;
            if (scheduler == null || _index >= _length) return;

            _entryProto = _proto.Entries[_index];
            _success = false;

            if (_entryProto.TimerForEntryMS < 0) return;

            scheduler.ScheduleEvent(_timerEvent, TimeSpan.FromMilliseconds(_entryProto.TimerForEntryMS), _pendingEvents);
            _timerEvent.Get().Initialize(this);

            var widgetRef = _entryProto.UIWidget;
            if (widgetRef == PrototypeId.Invalid) return;
            
            var widget = Region?.UIDataProvider?.GetWidget<UIWidgetGenericFraction>(widgetRef);
            widget?.SetTimeRemaining(_entryProto.TimerForEntryMS);
        }

        private void OnOpenMissionComplete(OpenMissionCompleteGameEvent evt)
        {
            var missionRef = evt.MissionRef;
            if (missionRef == PrototypeId.Invalid) return;
            if (_entryProto == null || _entryProto.MissionsToWatch.IsNullOrEmpty() || _success) return;

            var manager = Region?.MissionManager;
            if (manager == null) return;

            var scheduler = GameEventScheduler;
            if (scheduler == null) return;

            bool remove = false;
            if (_entryProto.MissionsToWatch.Contains(missionRef))
            {
                _success = true;
                var mission = manager.MissionByDataRef(missionRef);
                if (mission == null || _index >= _length) return;
                MissionActionList.CreateActionList(ref _onSuccessActionsList[_index], _entryProto.ActionsOnSuccess, mission);

                if (_entryProto.RemoveStateOnSuccess)
                {
                    scheduler.CancelEvent(_timerEvent);
                    remove = true;
                }
            }

            if (remove) MetaGame.RemoveState(PrototypeDataRef);
        }

        private void OnTimer()
        {
            var region = Region;

            var manager = region?.MissionManager;
            if (manager == null || _entryProto == null) return;

            bool remove = false;
            if (_success == false)
            {
                if (_entryProto.ActionsOnSuccess.HasValue())
                {
                    if (_entryProto.MissionsToWatch.IsNullOrEmpty()) return;
                    var missionRef = _entryProto.MissionsToWatch[0];

                    var mission = manager.MissionByDataRef(missionRef);
                    if (mission == null || _index >= _length) return;
                    MissionActionList.CreateActionList(ref _onFailActionsList[_index], _entryProto.ActionsOnFail, mission);
                }

                remove = _entryProto.RemoveStateOnFail;
            }

            if (remove == false)
            {
                var windgetRef = _entryProto.UIWidget;
                if (windgetRef != PrototypeId.Invalid)
                    region.UIDataProvider?.DeleteWidget(windgetRef);

                _index++;
                if (_index >= _length)
                    remove = true;
                else
                    StartTimer();
            }

            if (remove) MetaGame.RemoveState(PrototypeDataRef);
        }

        protected class TimerEvent : CallMethodEvent<MetaStateTimedBonus>
        {
            protected override CallbackDelegate GetCallback() => (metaState) => metaState?.OnTimer();
        }
    }
}
