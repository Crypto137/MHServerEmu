using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionEntityDeath : MissionPlayerCondition
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private MissionConditionEntityDeathPrototype _proto;
        private Event<AdjustHealthGameEvent>.Action _adjustHealthAction;
        private Event<EntityDeadGameEvent>.Action _entityDeadAction;
        private EventGroup _pendingEvents = new();
        private bool _deathEventRegistred;

        protected override long RequiredCount => _proto.Count; 

        public MissionConditionEntityDeath(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // TrainingRoomBehaviorController
            _proto = prototype as MissionConditionEntityDeathPrototype;
            _adjustHealthAction = OnAdjustHealth;
            _entityDeadAction = OnEntityDead;
        }

        private bool EvaluateEntity(Player killer, WorldEntity entity)
        {
            if (entity == null || entity is Hotspot) return false;
            var tagPlayers = entity.TagPlayers;

            if (Mission.IsOpenMission == false) 
            {
                if (entity is not Avatar)
                {
                    bool tagged = false;
                    foreach (var tagPlayer in tagPlayers.GetPlayers())
                        if (IsMissionPlayer(tagPlayer))
                        {
                            tagged = true;
                            break;
                        }

                    if (tagged == false && (killer == null || IsMissionPlayer(killer) == false)) return false;
                }
            }
            else if (_proto.MustBeTaggedByPlayer && killer == null)
            {
                if (tagPlayers.HasTags == false) return false;
            }

            if (EvaluateEntityFilter(_proto.EntityFilter, entity) == false) return false;

            if (entity.WorldEntityPrototype.MissionEntityDeathCredit == false) return false;

            if (_proto.EncounterResource != AssetId.Invalid)
            {
                var skipEntity = entity;
                var spawnGroup = entity.SpawnGroup;
                if (spawnGroup == null) return false;
                var encounterRef = GameDatabase.GetDataRefByAsset(_proto.EncounterResource);
                if (spawnGroup.EncounterRef != encounterRef)
                {
                    if (spawnGroup.SpawnerId == 0) return false;
                    var spawner = Game.EntityManager.GetEntity<Spawner>(spawnGroup.SpawnerId);
                    if (spawner == null) return false;
                    var spawnerGroup = spawner.SpawnGroup;
                    if (spawnerGroup == null || spawnerGroup.EncounterRef != encounterRef) return false;
                    skipEntity = null;
                }

                if (killer != null) Mission.AddParticipant(killer);

                var filterFlag = SpawnGroupEntityQueryFilterFlags.Hostiles | SpawnGroupEntityQueryFilterFlags.NotDeadDestroyedControlled;
                bool hasHostiles = spawnGroup.FilterEntity(filterFlag, skipEntity, _proto.EntityFilter, new(Mission.PrototypeDataRef));
                if (hasHostiles) return false;
            }

            return true;
        }

        private void OnAdjustHealth(in AdjustHealthGameEvent evt)
        {
            var entity = evt.Entity;
            var attacker = evt.Attacker;
            long damage = -evt.Damage;

            if (entity == null || attacker == null || damage == 0) return;
            if (entity is Avatar && attacker is not Avatar && damage > 0)
            {
                var player = entity.GetOwnerOfType<Player>();
                if (player == null || attacker.MissionPrototype != Mission.PrototypeDataRef) return;
                if (Mission.IsOpenMission) Mission.AddParticipant(player);
                attacker.AddDamageContributor(player, damage);
            }    
            else if (entity is not Avatar && attacker is Avatar && damage > 0 && evt.Dodged == false)
            {
                var player = attacker.GetOwnerOfType<Player>();
                if (player == null || entity.MissionPrototype != Mission.PrototypeDataRef) return;
                if (Mission.IsOpenMission) 
                { 
                    Mission.AddParticipant(player);
                    Mission.ScheduleIdleTimeout();
                }
                entity.AddTankingContributor(player, damage);
            }
        }

        private void OnEntityDead(in EntityDeadGameEvent evt)
        {
            var entity = evt.Defender;
            var killer = evt.Killer;

            if (EvaluateEntity(killer, entity))
            {
                if (_proto.DelayDeathMS > 0)
                {
                    var scheduler = Mission.GameEventScheduler;
                    if (scheduler == null) return;                    
                    EventPointer<DelayDeathEvent> delayDeathEvent = new();
                    scheduler.ScheduleEvent(delayDeathEvent, TimeSpan.FromMilliseconds(_proto.DelayDeathMS), _pendingEvents);
                    delayDeathEvent.Get().Initialize(this, entity, killer);
                    return;
                }

                OnDeath(entity, killer);
            }
        }

        private void OnDeath(WorldEntity entity, Player killer)
        {
            bool killerTagged = false;

            if (entity != null)
            {
                if (MissionManager.Debug) Logger.Warn($"[{Mission.PrototypeName}] EntityDeath OnDeath entity [{entity.PrototypeName}] [{Count + 1}/{RequiredCount}]");
                if (_proto.OpenMissionContribValueDamage != 0.0f)
                    SetContributions(entity.TankingContributors, _proto.OpenMissionContribValueDamage);
                if (_proto.OpenMissionContribValueTanking != 0.0f)
                    SetContributions(entity.DamageContributors, _proto.OpenMissionContribValueTanking);

                foreach (var player in entity.TagPlayers.GetPlayers())
                    if (IsMissionPlayer(player))
                    {
                        if (killer == player) killerTagged = true;
                        UpdatePlayerContribution(player);
                    }
            }

            if (killerTagged == false && killer != null && Mission.IsOpenMission)
                UpdatePlayerContribution(killer);

            Count++;
        }

        private void SetContributions(Dictionary<ulong, long> contributors, double damageValue)
        {
            if (contributors == null) return;

            float damage = 0;
            foreach (long damageContributor in contributors.Values)
                damage += damageContributor;
            if (damage <= 0) return;

            float damageRate = (float)damageValue / damage;

            var manager = Game.EntityManager;
            foreach (var kvp in contributors)
            {
                if (kvp.Value <= 0) continue;
                var player = manager.GetEntityByDbGuid<Player>(kvp.Key);
                if (player == null) continue;
                SetPlayerContribution(player, kvp.Value * damageRate);
            }
        }

        private void OnDelayDeath(WorldEntity defender, Player killer)
        {
            if (_deathEventRegistred) OnDeath(defender, killer);
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.EntityDeadEvent.AddActionBack(_entityDeadAction);
            _deathEventRegistred = true;

            if (Mission.IsOpenMission)
                region.AdjustHealthEvent.AddActionBack(_adjustHealthAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.EntityDeadEvent.RemoveAction(_entityDeadAction);
            _deathEventRegistred = false;
            var scheduler = Mission.GameEventScheduler;
            scheduler?.CancelAllEvents(_pendingEvents);

            if (Mission.IsOpenMission)
                region.AdjustHealthEvent.RemoveAction(_adjustHealthAction);
        }

        public class DelayDeathEvent : CallMethodEventParam2<MissionConditionEntityDeath, WorldEntity, Player>
        {
            protected override CallbackDelegate GetCallback() => (condition, entityId, killerId) => condition.OnDelayDeath(entityId, killerId);
        }

    }
}
