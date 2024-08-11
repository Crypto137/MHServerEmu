using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionEntityDamaged : MissionPlayerCondition
    {
        protected MissionConditionEntityDamagedPrototype Proto => Prototype as MissionConditionEntityDamagedPrototype;
        public Action<AdjustHealthGameEvent> AdjustHealthAction { get; private set; }
        public Action<EntityStatusEffectGameEvent> EntityStatusEffectAction { get; private set; }
        public MissionConditionEntityDamaged(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            AdjustHealthAction = OnAdjustHealth;
            EntityStatusEffectAction = OnEntityStatusEffect;
        }

        private bool EvaluateEntity(Player player, WorldEntity entity)
        {
            var proto = Proto;
            if (proto == null || entity == null) return false;
            bool isOpenMission = Mission.IsOpenMission;

            if (player == null)
                if (proto.LimitToDamageFromPlayerOMOnly || isOpenMission == false) return false;

            if (isOpenMission == false && IsMissionPlayer(player) == false) return false;
            if (EvaluateEntityFilter(proto.EntityFilter, entity) == false) return false;

            if (proto.EncounterResource != AssetId.Invalid)
            {
                var spawnGroup = entity.SpawnGroup;
                if (spawnGroup == null) return false;
                var encounterRef = GameDatabase.GetDataRefByAsset(proto.EncounterResource); 
                if (spawnGroup.EncounterRef != encounterRef) return false;
            }

            return true;
        }

        private void OnAdjustHealth(AdjustHealthGameEvent evt)
        {
            if (evt.Attacker == null) return;
            var player = evt.Player;
            var entity = evt.Entity;

            long damage = -evt.Damage;
            if (damage <= 0 || evt.Dodged) return;

            if (EvaluateEntity(player, entity))
                SetCompleted();
        }

        private void OnEntityStatusEffect(EntityStatusEffectGameEvent evt)
        {
            if (evt.NegStatusEffect == false) return;
            var player = evt.Player;
            if (player == null) return;
            var entity = evt.Entity;

            if (EvaluateEntity(player, entity))
                SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.AdjustHealthEvent.AddActionBack(AdjustHealthAction);
            region.EntityStatusEffectEvent.AddActionBack(EntityStatusEffectAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.AdjustHealthEvent.RemoveAction(AdjustHealthAction);
            region.EntityStatusEffectEvent.RemoveAction(EntityStatusEffectAction);
        }
    }
}
