using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
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
            if (properties.HasOverTimeProperties() == false)
                return PropertyTicker.InvalidId;

            PropertyTicker ticker = new();  // TODO: pool tickers?
            ticker.Initialize(_nextTickerId++);
            _tickerDict.Add(ticker.Id, ticker);

            //Logger.Debug($"StartTicker(): tickerId={ticker.Id} (custom)");

            return ticker.Id;
        }

        public ulong StartTicker(Condition condition)
        {
            if (condition.Properties.HasOverTimeProperties() == false)
                return PropertyTicker.InvalidId;

            PropertyTicker ticker = new();  // TODO: pool tickers?
            ticker.Initialize(_nextTickerId++);
            _tickerDict.Add(ticker.Id, ticker);

            //Logger.Debug($"StartTicker(): tickerId={ticker.Id} ({condition})");

            return ticker.Id;
        }

        public bool StopTicker(ulong tickerId)
        {
            //Logger.Debug($"StopTicker(): tickerId={tickerId}");
            if (_tickerDict.Remove(tickerId) == false)
                return Logger.WarnReturn(false, $"StopTicker(): TickerId {tickerId} not found");

            return true;
        }

        public void StopAllTickers()
        {
            // TODO
            //Logger.Debug("StopAllTickers()");
            _tickerDict.Clear();
        }

        public void UpdateTicker(ulong tickerId, TimeSpan duration, bool isPaused)
        {
            //Logger.Debug($"UpdateTicker(): tickerId={tickerId}");
        }
    }
}
