using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionEntityAggro : MissionPlayerCondition
    {
        private MissionConditionEntityAggroPrototype _proto;
        private Action<EntityAggroedGameEvent> _entityAggroedAction;
        private Action<AdjustHealthGameEvent> _adjustHealthAction;

        public MissionConditionEntityAggro(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionEntityAggroPrototype;
            _entityAggroedAction = OnEntityAggroed;
            _adjustHealthAction = OnAdjustHealth;
        }

        private bool EvaluateEntity(Player player, WorldEntity entity)
        {
            if (player == null || entity == null || IsMissionPlayer(player) == false) return false;
            return EvaluateEntityFilter(_proto.EntityFilter, entity);
        }

        private void OnAdjustHealth(AdjustHealthGameEvent evt)
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
            region.EntityAggroedEvent.AddActionBack(_entityAggroedAction);
            region.AdjustHealthEvent.AddActionBack(_adjustHealthAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.EntityAggroedEvent.RemoveAction(_entityAggroedAction);
            region.AdjustHealthEvent.RemoveAction(_adjustHealthAction);
        }
    }
}
