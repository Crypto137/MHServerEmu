using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_ErrorInRegionEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PlayerConnection _playerConnection;
        private PrototypeId _regionProtoId;

        public void Initialize(PlayerConnection playerConnection, PrototypeId regionProtoId)
        {
            _playerConnection = playerConnection;
            _regionProtoId = regionProtoId;
        }

        public override bool OnTriggered()
        {
            Logger.Error($"Event ErrorInRegion {GameDatabase.GetFormattedPrototypeName(_regionProtoId)}");
            _playerConnection.Disconnect();
            return true;
        }
    }
}
