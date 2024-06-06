using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_ToTeleportEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PlayerConnection _playerConnection;
        private Vector3 _targetPos;

        public void Initialize(PlayerConnection playerConnection, Vector3 targetPos)
        {
            _playerConnection = playerConnection;
            _targetPos = targetPos;
        }

        public override bool OnTriggered()
        {
            Vector3 targetRot = new();

            uint cellid = 1;
            uint areaid = 1;

            _playerConnection.SendMessage(NetMessageEntityPosition.CreateBuilder()
                .SetIdEntity(_playerConnection.Player.CurrentAvatar.Id)
                .SetFlags((uint)ChangePositionFlags.Teleport)
                .SetPosition(_targetPos.ToNetStructPoint3())
                .SetOrientation(targetRot.ToNetStructPoint3())
                .SetCellId(cellid)
                .SetAreaId(areaid)
                .SetEntityPrototypeId((ulong)_playerConnection.Player.CurrentAvatar.Prototype.DataRef)
                .Build());

            _playerConnection.LastPosition = _targetPos;
            Logger.Trace($"Teleporting to {_targetPos}");

            return true;
        }
    }
}
