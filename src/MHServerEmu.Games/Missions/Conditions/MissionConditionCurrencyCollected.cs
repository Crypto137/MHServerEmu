using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionCurrencyCollected : MissionPlayerCondition
    {
        private MissionConditionCurrencyCollectedPrototype _proto;
        private Action<CurrencyCollectedGameEvent> _currencyCollectedAction;

        public MissionConditionCurrencyCollected(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // RaftNPETutorialTipsController
            _proto = prototype as MissionConditionCurrencyCollectedPrototype;
            _currencyCollectedAction = OnCurrencyCollected;
        }

        public override bool OnReset()
        {
            if (_proto.CurrencyType == PrototypeId.Invalid) return false;
            PropertyId propId = new(PropertyEnum.Currency, _proto.CurrencyType);

            bool collected = false;

            List<Player> participants = ListPool<Player>.Instance.Get();
            if (Mission.GetParticipants(participants))
            {
                foreach (var player in participants)
                {
                    int amount = player.Properties[propId];
                    if (amount >= _proto.AmountRequired)
                    {
                        collected = true;
                        break;
                    }
                }
            }
            ListPool<Player>.Instance.Return(participants);

            SetCompletion(collected);
            return true;
        }

        private void OnCurrencyCollected(CurrencyCollectedGameEvent evt)
        {
            var player = evt.Player;
            var currencyType = evt.CurrencyType;
            int amount = evt.Amount;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (currencyType == PrototypeId.Invalid || currencyType != _proto.CurrencyType) return;
            if (_proto.AmountRequired != 0 && amount < _proto.AmountRequired) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.CurrencyCollectedEvent.AddActionBack(_currencyCollectedAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.CurrencyCollectedEvent.RemoveAction(_currencyCollectedAction);
        }
    }
}
