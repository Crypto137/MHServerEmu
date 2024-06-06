using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_FinishCellLoadingEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PlayerConnection _playerConnection;
        private int _loadedCellCount;

        public void Initialize(PlayerConnection playerConnection, int loadedCellCount)
        {
            _playerConnection = playerConnection;
            _loadedCellCount = loadedCellCount;
        }

        public override bool OnTriggered()
        {
            Logger.Warn($"Forсed loading");
            _playerConnection.AOI.LoadedCellCount = _loadedCellCount;
            _playerConnection.EnterGameWorld();
            return true;
        }
    }
}
