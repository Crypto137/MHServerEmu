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
                case (PrototypeId)PowerPrototypes.Travel.GhostRiderRide:
                    if (avatar.ConditionCollection.GetCondition(666) == null) return false;

                    Logger.Trace($"EventEnd GhostRiderRide");

                    // Remove the ride condition and unassign bike hotspots power
                    avatar.ConditionCollection.RemoveCondition(666);
                    avatar.UnassignPower((PrototypeId)PowerPrototypes.GhostRider.RideBikeHotspotsEnd);

                    break;

                case (PrototypeId)PowerPrototypes.Travel.WolverineRide:
                case (PrototypeId)PowerPrototypes.Travel.DeadpoolRide:
                case (PrototypeId)PowerPrototypes.Travel.NickFuryRide:
                case (PrototypeId)PowerPrototypes.Travel.CyclopsRide:
                case (PrototypeId)PowerPrototypes.Travel.BlackWidowRide:
                case (PrototypeId)PowerPrototypes.Travel.BladeRide:
                case (PrototypeId)PowerPrototypes.Travel.AntmanFlight:
                case (PrototypeId)PowerPrototypes.Travel.ThingFlight:
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
