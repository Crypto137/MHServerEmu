using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers.Conditions;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// Manages <see cref="PropertyTicker"/> instances belonging to an <see cref="Entity"/>.
    /// </summary>
    public class PropertyTickerManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, PropertyTicker> _tickerDict = new();

        private Entity _owner;
        private ulong _nextTickerId = 1;

        public PropertyTickerManager(Entity owner)
        {
            _owner = owner;
        }

        public ulong StartTicker(PropertyCollection properties, ulong creatorId, ulong ultimateCreatorId, TimeSpan updateInterval)
        {
            return StartTickerInternal(properties, creatorId, ultimateCreatorId, updateInterval);
        }

        public ulong StartTicker(Condition condition)
        {
            return StartTickerInternal(condition.Properties, condition.CreatorId, condition.UltimateCreatorId, condition.UpdateInterval,
                condition.Id, condition.CreatorPowerPrototype, condition.ShouldApplyOverTimeEffectsToOriginator(),
                condition.TimeRemaining, condition.IsPaused);
        }

        public bool StopTicker(ulong tickerId)
        {
            // In some cases (e.g. reapplying Infinity/Omega tickers after reentering the world)
            // a ticker may no longer exist, and this is valid behavior, so stay silent.
            if (_tickerDict.Remove(tickerId, out PropertyTicker ticker) == false)
                return true;

            ticker.Stop(true);
            DeleteTicker(ticker);

            return true;
        }

        public void StopAllTickers()
        {
            // Store tickers in a temp list and clear the dict to prevent recursion on stop
            List<PropertyTicker> tickerList = ListPool<PropertyTicker>.Instance.Get();
            foreach (PropertyTicker ticker in _tickerDict.Values)
                tickerList.Add(ticker);

            _tickerDict.Clear();

            foreach (PropertyTicker ticker in tickerList)
            {
                ticker.Stop(false);
                DeleteTicker(ticker);
            }

            ListPool<PropertyTicker>.Instance.Return(tickerList);
        }

        public void UpdateTicker(ulong tickerId, TimeSpan duration, bool isPaused)
        {
            if (_tickerDict.TryGetValue(tickerId, out PropertyTicker ticker) == false)
                return;

            ticker.Update(duration, isPaused);
        }

        private ulong StartTickerInternal(PropertyCollection properties, ulong creatorId, ulong ultimateCreatorId, TimeSpan updateInterval,
            ulong conditionId = ConditionCollection.InvalidConditionId, PowerPrototype powerProto = null, bool targetsUltimateCreator = false,
            TimeSpan duration = default, bool isPaused = false)
        {
            if (properties.HasOverTimeProperties() == false)
                return PropertyTicker.InvalidId;

            ulong tickerId = _nextTickerId++;

            PropertyTicker ticker = AllocateTicker();
            _tickerDict.Add(tickerId, ticker);

            ticker.Initialize(tickerId, properties, _owner.Id, creatorId, ultimateCreatorId, updateInterval, conditionId, powerProto, targetsUltimateCreator);
            ticker.Start(duration, isPaused);

            return tickerId;
        }

        private static PropertyTicker AllocateTicker()
        {
            // TODO: Pooling
            PropertyTicker ticker = new();
            return ticker;
        }

        private static bool DeleteTicker(PropertyTicker ticker)
        {
            // TODO: Pooling
            return true;
        }
    }
}
