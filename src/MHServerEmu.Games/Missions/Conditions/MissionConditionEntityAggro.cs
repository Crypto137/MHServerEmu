using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionEntityAggro : MissionPlayerCondition
    {
        protected MissionConditionEntityAggroPrototype Proto => Prototype as MissionConditionEntityAggroPrototype;
        public Action<EntityAggroedGameEvent> EntityAggroedAction { get; private set; }
        public Action<AdjustHealthGameEvent> AdjustHealthAction { get; private set; }

        public MissionConditionEntityAggro(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            EntityAggroedAction = OnEntityAggroed;
            AdjustHealthAction = OnAdjustHealthAction;
        }

        private bool EvaluateEntity(Player player, WorldEntity entity)
        {
            var proto = Proto;
            if (proto == null || player == null || entity == null || IsMissionPlayer(player) == false) return false;
            return EvaluateEntityFilter(proto.EntityFilter, entity);
        }

        private void OnAdjustHealthAction(AdjustHealthGameEvent evt)
        {
            var player = evt.Player;
            var entity = evt.Entity;

            long damage = -evt.Damage;
            if (damage <= 0) return;

            if (EvaluateEntity(player, entity) == false) return;
            UpdatePlayerContribution(player);
            Count++;
        }

        private void OnEntityAggroed(EntityAggroedGameEvent evt)
        {
            var player = evt.Player;
            var entity = evt.AggroEntity;

            if (EvaluateEntity(player, entity) == false) return;
            UpdatePlayerContribution(player);
            Count++;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.EntityAggroedEvent.AddActionBack(EntityAggroedAction);
            region.AdjustHealthEvent.AddActionBack(AdjustHealthAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.EntityAggroedEvent.RemoveAction(EntityAggroedAction);
            region.AdjustHealthEvent.RemoveAction(AdjustHealthAction);
        }
    }
}
