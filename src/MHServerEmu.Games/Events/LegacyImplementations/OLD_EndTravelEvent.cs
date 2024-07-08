using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_EndTravelEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PlayerConnection _playerConnection;
        private PrototypeId _powerId;

        public void Initialize(PlayerConnection playerConnection, PrototypeId powerId)
        {
            _playerConnection = playerConnection;
            _powerId = powerId;
        }

        public override bool OnTriggered()
        {
            Avatar avatar = _playerConnection.Player.CurrentAvatar;

            switch (_powerId)
            {
                case (PrototypeId)OLD_PowerPrototypes.Travel.GhostRiderRide:
                    if (avatar.ConditionCollection.GetCondition(666) == null) return false;

                    Logger.Trace($"EventEnd GhostRiderRide");

                    // Remove the ride condition and unassign bike hotspots power
                    avatar.ConditionCollection.RemoveCondition(666);
                    avatar.UnassignPower((PrototypeId)OLD_PowerPrototypes.GhostRider.RideBikeHotspotsEnd);

                    break;

                case (PrototypeId)OLD_PowerPrototypes.Travel.WolverineRide:
                case (PrototypeId)OLD_PowerPrototypes.Travel.DeadpoolRide:
                case (PrototypeId)OLD_PowerPrototypes.Travel.NickFuryRide:
                case (PrototypeId)OLD_PowerPrototypes.Travel.CyclopsRide:
                case (PrototypeId)OLD_PowerPrototypes.Travel.BlackWidowRide:
                case (PrototypeId)OLD_PowerPrototypes.Travel.BladeRide:
                case (PrototypeId)OLD_PowerPrototypes.Travel.AntmanFlight:
                case (PrototypeId)OLD_PowerPrototypes.Travel.ThingFlight:
                    if (avatar.ConditionCollection.GetCondition(667) == null) return false;

                    Logger.Trace($"EventEnd Ride");

                    // Remove the ride condition
                    avatar.ConditionCollection.RemoveCondition(667);

                    break;
            }

            return true;
        }
    }
}
