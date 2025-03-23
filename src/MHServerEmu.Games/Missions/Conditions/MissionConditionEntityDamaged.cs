using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionEntityDamaged : MissionPlayerCondition
    {
        private MissionConditionEntityDamagedPrototype _proto;
        private Event<AdjustHealthGameEvent>.Action _adjustHealthAction;
        private Event<EntityStatusEffectGameEvent>.Action _entityStatusEffectAction;

        public MissionConditionEntityDamaged(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CH00NPETrainingRoom
            _proto = prototype as MissionConditionEntityDamagedPrototype;
            _adjustHealthAction = OnAdjustHealth;
            _entityStatusEffectAction = OnEntityStatusEffect;
        }

        private bool EvaluateEntity(Player player, WorldEntity entity)
        {
            if (entity == null) return false;
            bool isOpenMission = Mission.IsOpenMission;

            if (player == null)
                if (_proto.LimitToDamageFromPlayerOMOnly || isOpenMission == false) return false;

            if (isOpenMission == false && IsMissionPlayer(player) == false) return false;
            if (EvaluateEntityFilter(_proto.EntityFilter, entity) == false) return false;

            if (_proto.EncounterResource != AssetId.Invalid)
            {
                var spawnGroup = entity.SpawnGroup;
                if (spawnGroup == null) return false;
                var encounterRef = GameDatabase.GetDataRefByAsset(_proto.EncounterResource); 
                if (spawnGroup.EncounterRef != encounterRef) return false;
            }

            return true;
        }

        private void OnAdjustHealth(in AdjustHealthGameEvent evt)
        {
            if (evt.Attacker == null) return;
            var player = evt.Player;
            var entity = evt.Entity;

            long damage = -evt.Damage;
            if (damage <= 0 || evt.Dodged) return;

            if (EvaluateEntity(player, entity))
                SetCompleted();
        }

        private void OnEntityStatusEffect(in EntityStatusEffectGameEvent evt)
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
            region.AdjustHealthEvent.AddActionBack(_adjustHealthAction);
            region.EntityStatusEffectEvent.AddActionBack(_entityStatusEffectAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.AdjustHealthEvent.RemoveAction(_adjustHealthAction);
            region.EntityStatusEffectEvent.RemoveAction(_entityStatusEffectAction);
        }
    }
}
