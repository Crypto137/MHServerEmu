using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_GetRegionEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PlayerConnection _playerConnection;
        private Region _region;

        public void Initialize(PlayerConnection playerConnection, Region region)
        {
            _playerConnection = playerConnection;
            _region = region;
        }

        public override bool OnTriggered()
        {
            Logger.Trace($"Event GetRegion");
            var messages = _region.GetLoadingMessages(_playerConnection.Game.Id, _playerConnection.WaypointDataRef, _playerConnection);
            foreach (IMessage message in messages)
                _playerConnection.SendMessage(message);

            _playerConnection.AOI.Reset(_region);
            _playerConnection.AOI.Update(_playerConnection.StartPosition, true, true);

            return true;
        }
    }
}
