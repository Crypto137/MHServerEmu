using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class TEMP_ActivatePowerEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private WorldEntity _worldEntity;
        private PrototypeId _powerProtoRef;

        public void Initialize(WorldEntity worldEntity, PrototypeId powerProtoRef)
        {
            _worldEntity = worldEntity;
            _powerProtoRef = powerProtoRef;
        }

        public override bool OnTriggered()
        {
            Logger.Trace($"Activating {GameDatabase.GetPrototypeName(_powerProtoRef)} for {_worldEntity}");

            ActivatePowerArchive activatePower = new()
            {
                Flags = ActivatePowerMessageFlags.TargetIsUser | ActivatePowerMessageFlags.HasTargetPosition |
                ActivatePowerMessageFlags.TargetPositionIsUserPosition | ActivatePowerMessageFlags.HasFXRandomSeed |
                ActivatePowerMessageFlags.HasPowerRandomSeed,

                PowerPrototypeRef = _powerProtoRef,
                UserEntityId = _worldEntity.Id,
                TargetPosition = _worldEntity.RegionLocation.Position,
                FXRandomSeed = (uint)_worldEntity.Game.Random.Next(),
                PowerRandomSeed = (uint)_worldEntity.Game.Random.Next()
            };

            var activatePowerMessage = NetMessageActivatePower.CreateBuilder().SetArchiveData(activatePower.ToByteString()).Build();
            _worldEntity.Game.NetworkManager.SendMessageToInterested(activatePowerMessage, _worldEntity, AOINetworkPolicyValues.AOIChannelProximity);

            return true;
        }
    }
}
