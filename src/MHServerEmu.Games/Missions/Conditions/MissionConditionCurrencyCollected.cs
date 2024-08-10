using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionCurrencyCollected : MissionPlayerCondition
    {
        protected MissionConditionCurrencyCollectedPrototype Proto => Prototype as MissionConditionCurrencyCollectedPrototype;
        public Action<CurrencyCollectedGameEvent> CurrencyCollectedAction { get; private set; }
        public MissionConditionCurrencyCollected(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            CurrencyCollectedAction = OnCurrencyCollected;
        }

        public override bool OnReset()
        {
            var proto = Proto;
            if (proto == null || proto.CurrencyType == PrototypeId.Invalid) return false;
            PropertyId propId = new(PropertyEnum.Currency, proto.CurrencyType);

            bool collected = false;
            foreach (var player in Mission.GetParticipants())
            {
                int amount = player.Properties[propId];
                if (amount >= proto.AmountRequired)
                {
                    collected = true;
                    break;
                }
            }

            SetCompletion(collected);
            return true;
        }

        private void OnCurrencyCollected(CurrencyCollectedGameEvent evt)
        {
            var proto = Proto;
            var player = evt.Player;
            var currencyType = evt.CurrencyType;
            int amount = evt.Amount;

            if (proto == null || player == null || IsMissionPlayer(player) == false) return;
            if (currencyType == PrototypeId.Invalid || currencyType != proto.CurrencyType) return;
            if (proto.AmountRequired != 0 && amount < proto.AmountRequired) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.CurrencyCollectedEvent.AddActionBack(CurrencyCollectedAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.CurrencyCollectedEvent.RemoveAction(CurrencyCollectedAction);
        }
    }
}
