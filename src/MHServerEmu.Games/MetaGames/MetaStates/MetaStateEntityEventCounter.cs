using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateEntityEventCounter : MetaState
    {
	    private MetaStateEntityEventCounterPrototype _proto;
        private Action<EntityEnteredWorldGameEvent> _entityEnteredWorldAction;
        private Action<EntityExitedWorldGameEvent> _entityExitedWorldAction;
        private Action<EntityDeadGameEvent> _entityDeadAction;
        private Action<PlayerInteractGameEvent> _playerInteractAction;
        private Dictionary<EntityGameEventEnum, HashSet<ulong>> _eventEntities;

        public MetaStateEntityEventCounter(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateEntityEventCounterPrototype;
            _entityEnteredWorldAction = OnEntityEnteredWorld;
            _entityExitedWorldAction = OnEntityExitedWorld;
            _entityDeadAction = OnEntityDead;
            _playerInteractAction = OnPlayerInteract;
            _eventEntities = new();
        }

        public override void OnApply()
        {
            var region = Region;
            if (region == null) return;

            if (_proto.EntityFilter != null)
            {
                region.EntityEnteredWorldEvent.AddActionBack(_entityEnteredWorldAction);
                region.EntityExitedWorldEvent.AddActionBack(_entityExitedWorldAction);
                region.EntityDeadEvent.AddActionBack(_entityDeadAction);
                region.PlayerInteractEvent.AddActionBack(_playerInteractAction);
            }
        }

        public override void OnRemove()
        {
            var region = Region;
            if (region == null) return;

            if (_proto.EntityFilter != null)
            {
                region.EntityEnteredWorldEvent.RemoveAction(_entityEnteredWorldAction);
                region.EntityExitedWorldEvent.RemoveAction(_entityExitedWorldAction);
                region.EntityDeadEvent.RemoveAction(_entityDeadAction);
                region.PlayerInteractEvent.RemoveAction(_playerInteractAction);
            }

            _eventEntities.Clear();
            base.OnRemove();
        }

        private void OnEntityEnteredWorld(EntityEnteredWorldGameEvent evt)
        {
            CountEntity(evt.Entity, EntityGameEventEnum.EntityEnteredWorld);
        }

        private void OnEntityExitedWorld(EntityExitedWorldGameEvent evt)
        {
            CountEntity(evt.Entity, EntityGameEventEnum.EntityExitedWorld);
        }

        private void OnEntityDead(EntityDeadGameEvent evt)
        {
            CountEntity(evt.Defender, EntityGameEventEnum.EntityDead);
        }

        private void OnPlayerInteract(PlayerInteractGameEvent evt)
        {
            CountEntity(evt.InteractableObject, EntityGameEventEnum.PlayerInteract);
        }

        private void CountEntity(WorldEntity entity, EntityGameEventEnum eventEnum)
        {
            if (entity == null) return;
            if (_proto.EntityFilter == null) return;

            if (_proto.EntityFilter.Evaluate(entity, new()))
            {
                if (_eventEntities.TryGetValue(eventEnum, out var entities) == false)
                {
                    entities = new();
                    _eventEntities[eventEnum] = entities;
                }

                if (entities.Contains(entity.Id) == false)
                {
                    entities.Add(entity.Id);
                    var propId = new PropertyId(PropertyEnum.MetaGameEntityEventCount, PrototypeDataRef, (int)eventEnum);
                    MetaGame.Properties.AdjustProperty(1, propId);
                }
            }
        }
    }
}
