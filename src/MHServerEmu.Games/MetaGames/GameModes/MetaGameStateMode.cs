using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class MetaGameStateMode : MetaGameMode
    {
        private MetaGameStateModePrototype _proto;
        private PrototypeId _stateRef;
        private EventPointer<StatePickIntervalEvent> _pickIntervalEvent;

        public MetaGameStateMode(MetaGame metaGame, MetaGameModePrototype proto) : base(metaGame, proto)
        {
            _proto = proto as MetaGameStateModePrototype;
            _pickIntervalEvent = new();
        }

        public override void OnActivate()
        {
            var region = Region;
            if (region == null) return;

            MetaGame.Properties[PropertyEnum.MetaGameWaveCount] = 0;
            SetModeText(_proto.Name);

            base.OnActivate();

            if (_proto.DifficultyPerStateActivate > 0)
                SendSetModeText();

            if (_proto.StatePickIntervalMS > 0)
                ScheduleStatePickInterval(TimeSpan.FromMilliseconds(_proto.StatePickIntervalMS));
        }

        public override void OnDeactivate()
        {
            MetaGame.RemoveState(_stateRef);
            base.OnDeactivate();
        }

        public override void OnRemoveState(PrototypeId removeStateRef)
        {
            if (removeStateRef == PrototypeId.Invalid || _proto.States.IsNullOrEmpty()) return;
            if (_proto.States.Contains(removeStateRef) || _stateRef == removeStateRef)
            {
                ScheduleStatePickInterval(TimeSpan.FromMilliseconds(_proto.StatePickIntervalMS), _proto.StatePickIntervalLabelOverride);
                _stateRef = PrototypeId.Invalid;
            }
        }

        public override void OnAddPlayer(Player player)
        {
            base.OnAddPlayer(player);
            if (player != null) OnUpdatePlayerNotification(player);
        }

        public override void OnUpdatePlayerNotification(Player player)
        {
            base.OnUpdatePlayerNotification(player);
            if (player != null) SendDifficultyChange(player);
        }

        private void ScheduleStatePickInterval(TimeSpan timeOffset, LocaleStringId labelOverride = LocaleStringId.Blank)
        {
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null || timeOffset <= TimeSpan.Zero) return;
            if (_pickIntervalEvent.IsValid) return;

            SendStartPvPTimer(timeOffset, TimeSpan.Zero, timeOffset * 0.25f, timeOffset * 0.1f, null, labelOverride);
            MetaGame.SetUIWidgetGenericFraction(_proto.UIStatePickIntervalWidget, PropertyEnum.MetaGameWaveCount, timeOffset);

            scheduler.ScheduleEvent(_pickIntervalEvent, timeOffset, _pendingEvents);
            _pickIntervalEvent.Get().Initialize(this);
        }

        public void OnStatePickInterval()
        {
            var region = Region;
            if (region == null) return;

            var wavePropId = new PropertyId(PropertyEnum.MetaGameWaveCount);
            MetaGame.Properties.AdjustProperty(1, wavePropId);

            bool applyState = false;

            MetaGame.ResetUIWidgetGenericFraction(_proto.UIStatePickIntervalWidget);

            if (_proto.EvalModeEnd != null)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = Game;
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, MetaGame.Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, region.Properties);
                int evalMode = Eval.RunInt(_proto.EvalModeEnd, evalContext);
                if (evalMode >= 0)
                {
                    MetaGame.ScheduleActivateGameMode(evalMode);
                    return;
                }
            }

            _stateRef = PrototypeId.Invalid;

            var forsePropId = new PropertyId(PropertyEnum.MetaGameStateForce);
            if (MetaGame.Properties.HasProperty(forsePropId))
            {
                _stateRef = MetaGame.Properties[forsePropId];
                applyState = ApplyMetaState(_stateRef, true);
                if (applyState) MetaGame.Properties.RemoveProperty(forsePropId);
                else _stateRef = PrototypeId.Invalid;
            }

            if (applyState == false && _proto.EvalStateSelection != null)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = Game;
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, MetaGame.Properties);
                _stateRef = Eval.RunPrototypeId(_proto.EvalStateSelection, evalContext);
                applyState = ApplyMetaState(_stateRef);
                if (applyState) MetaGame.Properties.RemoveProperty(forsePropId);
                else _stateRef = PrototypeId.Invalid;
            }

            if (applyState == false && _proto.States.HasValue())
                applyState = PickState();

            if (applyState == false)
            {
                MetaGame.Properties.AdjustProperty(-1, wavePropId);
                bool hasState = false;
                if (_proto.States.HasValue())
                    foreach (var state in _proto.States)
                        if (MetaGame.HasState(state))
                        {
                            hasState = true;
                            break;
                        }
                if (hasState == false)
                    ScheduleStatePickInterval(TimeSpan.FromMilliseconds(_proto.StatePickIntervalMS), _proto.StatePickIntervalLabelOverride);
            }
        }

        private bool PickState()
        {
            if (_proto.States.HasValue())
            {
                int count = _proto.States.Length;
                int pick = MetaGame.Random.Next(0, count);
                for (int i = 0; i < count; i++)
                {
                    int index = (pick + i) % count;
                    _stateRef = _proto.States[index];
                    if (ApplyMetaState(_stateRef)) return true;
                    _stateRef = PrototypeId.Invalid;
                }
            }

            return false;
        }

        private bool ApplyMetaState(PrototypeId stateRef, bool skipCooldown = false)
        {
            if (stateRef == PrototypeId.Invalid) return false;
            var table = Region?.TuningTable;
            if (table == null) return false;

            if (MetaGame.ApplyMetaState(stateRef, skipCooldown) == false) return false;

            var interestedClients = GetInterestedClients();

            List<long> intArgs = new() { (int)MetaGame.Properties[PropertyEnum.MetaGameWaveCount] };

            if (_proto.DifficultyPerStateActivate > 0)
            {
                table.DifficultyIndex += _proto.DifficultyPerStateActivate;
                table.GetUIIntArgs(intArgs);
            }

            SendMetaGameBanner(interestedClients, _proto.UIStateChangeBannerText, intArgs);

            return true;
        }

        public PrototypeId GetCurrentStateRef() => _stateRef;

        protected class StatePickIntervalEvent : CallMethodEvent<MetaGameStateMode>
        {
            protected override CallbackDelegate GetCallback() => (metaStateMode) => metaStateMode.OnStatePickInterval();
        }
    }
}
