using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_StartTravelEvent : ScheduledEvent
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
                    Condition ghostRiderRideCondition = avatar.ConditionCollection.GetCondition(666);
                    if (ghostRiderRideCondition != null)
                        if (ghostRiderRideCondition != null) return Logger.WarnReturn(false, "OnTriggered(): ghostRiderRideCondition != null");

                    Logger.Trace($"EventStart GhostRiderRide");

                    // Player.Avatar.EvalOnCreate.AssignProp.ProcProp.Param1 
                    // Create and add a ride condition
                    ghostRiderRideCondition = avatar.ConditionCollection.AllocateCondition();
                    ghostRiderRideCondition.InitializeFromPowerMixinPrototype(666, _powerId, 0, TimeSpan.Zero);
                    avatar.ConditionCollection.AddCondition(ghostRiderRideCondition);

                    // Assign the hotspot power to the avatar
                    PowerIndexProperties indexProps = new(0, avatar.CharacterLevel, avatar.CombatLevel);
                    avatar.AssignPower((PrototypeId)PowerPrototypes.GhostRider.RideBikeHotspotsEnd, indexProps);

                    // Notify the client
                    _playerConnection.SendMessage(NetMessagePowerCollectionAssignPower.CreateBuilder()
                        .SetEntityId(avatar.Id)
                        .SetPowerProtoId((ulong)PowerPrototypes.GhostRider.RideBikeHotspotsEnd)
                        .SetPowerRank(indexProps.PowerRank)
                        .SetCharacterLevel(indexProps.CharacterLevel)
                        .SetCombatLevel(indexProps.CombatLevel)
                        .SetItemLevel(indexProps.ItemLevel)
                        .SetItemVariation(indexProps.ItemVariation)
                        .Build());

                    break;

                case (PrototypeId)PowerPrototypes.Travel.WolverineRide:
                case (PrototypeId)PowerPrototypes.Travel.DeadpoolRide:
                case (PrototypeId)PowerPrototypes.Travel.NickFuryRide:
                case (PrototypeId)PowerPrototypes.Travel.CyclopsRide:
                case (PrototypeId)PowerPrototypes.Travel.BlackWidowRide:
                case (PrototypeId)PowerPrototypes.Travel.BladeRide:
                case (PrototypeId)PowerPrototypes.Travel.AntmanFlight:
                case (PrototypeId)PowerPrototypes.Travel.ThingFlight:
                    Condition rideCondition = avatar.ConditionCollection.GetCondition(667);
                    if (rideCondition != null) return Logger.WarnReturn(false, "OnTriggered(): rideCondition != null");

                    Logger.Trace($"EventStart Ride");

                    // Create and add a ride condition
                    rideCondition = avatar.ConditionCollection.AllocateCondition();
                    rideCondition.InitializeFromPowerMixinPrototype(667, _powerId, 0, TimeSpan.Zero);
                    avatar.ConditionCollection.AddCondition(rideCondition);

                    break;
            }

            return true;
        }
    }
}
