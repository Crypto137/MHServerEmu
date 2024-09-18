using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class TEMP_RemoveConditionEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ulong _entityId;
        private ulong _conditionId;

        public void Initialize(ulong entityId, ulong conditionId)
        {
            _entityId = entityId;
            _conditionId = conditionId;
        }

        public override bool OnTriggered()
        {
            WorldEntity worldEntity = Game.Current?.EntityManager.GetEntity<WorldEntity>(_entityId);
            if (worldEntity == null) return Logger.WarnReturn(false, $"OnTriggered(): worldEntity == null");

            Condition condition = worldEntity.ConditionCollection?.GetCondition(_conditionId);
            if (condition == null) return Logger.WarnReturn(false, "OnTriggered(): condition == null");

            worldEntity.ConditionCollection.RemoveCondition(_conditionId);

            return true;
        }
    }
}
