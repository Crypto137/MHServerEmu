using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateWaveInstance : MetaState
    {
	    private MetaStateWaveInstancePrototype _proto;
        private PrototypeId _waveRef;
        private EventPointer<StatePickIntervalEvent> _statePickIntervalEvent = new();

        public MetaStateWaveInstance(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateWaveInstancePrototype;
        }

        public override void OnApply()
        {
            if (_proto.StatePickIntervalMS > 0)
                ScheduleStatePickInterval(TimeSpan.FromMilliseconds(_proto.StatePickIntervalMS));
        }

        public override void OnRemove()
        {
            MetaGame.RemoveState(_waveRef);
        }

        public override void OnRemovedState(PrototypeId removedStateRef)
        {
            if (removedStateRef != PrototypeId.Invalid && _proto.States.HasValue())
                if (_proto.States.Contains(removedStateRef) || _waveRef == removedStateRef)
                {
                    ScheduleStatePickInterval(TimeSpan.FromMilliseconds(_proto.StatePickIntervalMS));
                    _waveRef = PrototypeId.Invalid;
                }
        }

        private void ScheduleStatePickInterval(TimeSpan interval)
        {
            var scheduler = GameEventScheduler;
            if (scheduler == null || interval <= TimeSpan.Zero) return;
            if (_statePickIntervalEvent.IsValid) return;

            if (_proto.UIWidget != PrototypeId.Invalid) 
            {
                var widget = Region.UIDataProvider.GetWidget<UIWidgetGenericFraction>(_proto.UIWidget, PrototypeId.Invalid);
                if (widget != null)
                {
                    int count = MetaGame.Properties[PropertyEnum.MetaStateWaveCount];
                    widget.SetCount(count, count + 1);
                    widget.SetTimeRemaining((long)interval.TotalMilliseconds);
                }
            }

            scheduler.ScheduleEvent(_statePickIntervalEvent, interval, _pendingEvents);
            _statePickIntervalEvent.Get().Initialize(this);
        }

        private void OnStatePickInterval()
        {
            var wavePropId = new PropertyId(PropertyEnum.MetaStateWaveCount, PrototypeDataRef);

            MetaGame.Properties.AdjustProperty(1, wavePropId);

            bool applyState = false;
            _waveRef = PrototypeId.Invalid;

            var forsePropId = new PropertyId(PropertyEnum.MetaStateWaveForce, PrototypeDataRef);
            if (MetaGame.Properties.HasProperty(forsePropId)) 
            {
                _waveRef = MetaGame.Properties[forsePropId];
                applyState = MetaGame.ApplyMetaState(_waveRef, true);
                if (applyState) MetaGame.Properties.RemoveProperty(forsePropId);
                _waveRef = PrototypeId.Invalid;
            }

            if (applyState == false && _proto.States.HasValue())
                applyState = PickState();

            if (applyState == false)
            {
                MetaGame.Properties.AdjustProperty(-1, wavePropId);
                bool hasState = false;
                if (_proto.States.HasValue())
                    foreach(var state in _proto.States)
                        if (MetaGame.HasState(state))
                        {
                            hasState = true;
                            break;
                        }

                if (hasState == false)
                    ScheduleStatePickInterval(TimeSpan.FromMilliseconds(_proto.StatePickIntervalMS));
            }

            if (_proto.UIWidget != PrototypeId.Invalid)
            {
                var uiDataProvider = Region.UIDataProvider;
                var widget = uiDataProvider.GetWidget<UIWidgetGenericFraction>(_proto.UIWidget, PrototypeId.Invalid);
                if (widget != null)
                    uiDataProvider.DeleteWidget(_proto.UIWidget, PrototypeId.Invalid);
            }
        }

        private bool PickState()
        {
            if (_proto.StatesWeighted.HasValue())
            {
                Picker<PrototypeId> picker = new(MetaGame.Random);
                foreach (var state in _proto.StatesWeighted)
                    picker.Add(state.Ref, state.Weight);

                while (!picker.Empty())
                {
                    picker.PickRemove(out _waveRef);
                    if (MetaGame.ApplyMetaState(_waveRef)) return true;
                    _waveRef = PrototypeId.Invalid;
                }
            }
            else if (_proto.States.HasValue())
            {
                int count = _proto.States.Length;
                int pick = MetaGame.Random.Next(0, count);
                for (int i = 0; i < count; i++)
                {
                    int index = (pick + i) % count;
                    _waveRef = _proto.States[index];
                    if (MetaGame.ApplyMetaState(_waveRef)) return true;
                    _waveRef = PrototypeId.Invalid;
                }
            }

            return false;
        }

        protected class StatePickIntervalEvent : CallMethodEvent<MetaStateWaveInstance>
        {
            protected override CallbackDelegate GetCallback() => (waveState) => waveState.OnStatePickInterval();
        }
    }
}
