using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionPartySize : MissionPlayerCondition
    {
        protected MissionConditionPartySizePrototype Proto => Prototype as MissionConditionPartySizePrototype;
        public Action<PartySizeChangedGameEvent> PartySizeChangedAction { get; private set; }

        public MissionConditionPartySize(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            PartySizeChangedAction = OnPartySizeChanged;
        }

        public override bool OnReset()
        {
            var proto = Proto;
            if (proto == null) return false;

            foreach (var player in Mission.GetParticipants())
            {
                int partySize = 1;
                var party = player.Party;
                if (party != null) partySize = party.NumMembers;
                if (partySize >= proto.MinSize && partySize <= proto.MaxSize)
                {
                    SetCompleted();
                    return true;
                }
            }

            ResetCompleted();
            return true;
        }

        private void OnPartySizeChanged(PartySizeChangedGameEvent evt)
        {
            var proto = Proto;
            var player = evt.Player;
            int partySize = evt.PartySize;
            if (proto == null || player == null || IsMissionPlayer(player) == false) return;
            if (partySize < proto.MinSize || partySize > proto.MaxSize) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PartySizeChangedEvent.AddActionBack(PartySizeChangedAction);            
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PartySizeChangedEvent.RemoveAction(PartySizeChangedAction);
        }
    }
}
