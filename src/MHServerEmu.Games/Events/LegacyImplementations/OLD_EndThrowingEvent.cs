using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_EndThrowingEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Agent _throwerAgent;
        private bool _isCancelling;

        public void Initialize(Agent throwerAgent, bool isCancelling)
        {
            _throwerAgent = throwerAgent;
            _isCancelling = isCancelling;
        }

        public override bool OnTriggered()
        {
            Logger.Trace("Event EndThrowing");

            // Do the throwing if not cancelling
            if (_isCancelling == false)
            {
                ulong throwableEntityId = _throwerAgent.Properties[PropertyEnum.ThrowableOriginatorEntity];
                if (throwableEntityId != 0)
                {
                    var throwableEntity = _throwerAgent.Game.EntityManager.GetEntity<WorldEntity>(throwableEntityId);
                    if (throwableEntity != null)
                    {
                        // Remember spawn spec to create a replacement
                        SpawnSpec spawnSpec = throwableEntity.SpawnSpec;

                        // Destroy throwable
                        throwableEntity.Destroy();

                        // Schedule the creation of a replacement entity
                        if (spawnSpec != null)
                        {
                            Game game = _throwerAgent.Game;

                            EventPointer<TEMP_SpawnEntityEvent> spawnEntityEvent = new();
                            game.GameEventScheduler.ScheduleEvent(spawnEntityEvent, game.CustomGameOptions.WorldEntityRespawnTime);
                            spawnEntityEvent.Get().Initialize(spawnSpec);
                        }
                    }
                }

                _throwerAgent.Properties.RemoveProperty(PropertyEnum.ThrowableOriginatorEntity);
                _throwerAgent.Properties.RemoveProperty(PropertyEnum.ThrowableOriginatorAssetRef);
            }

            // Unassign throwable and throwable cancel powers
            // If we are cancelling, the entity is going to be restored in Agent.OnPowerUnassigned()
            Power throwablePower = _throwerAgent.GetThrowablePower();
            if (throwablePower != null)
                _throwerAgent.UnassignPower(throwablePower.PrototypeDataRef);

            Power throwableCancelPower = _throwerAgent.GetThrowableCancelPower();
            if (throwableCancelPower != null)
                _throwerAgent.UnassignPower(throwableCancelPower.PrototypeDataRef);

            return true;
        }
    }
}
