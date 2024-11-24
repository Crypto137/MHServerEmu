using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Network
{
    public class TransferParams
    {
        // This class determines where a player needs to be put after loading into a game.
        // According to PlayerMgrToGameServer protocol from 1.53, it was sent as a NetStructTransferParams
        // in a GameAndRegionForPlayer message from the player manager to the GIS when a player connects.

        private static readonly Logger Logger = LogManager.CreateLogger();

        public PlayerConnection PlayerConnection { get; }

        public ulong DestRegionId { get; set; }

        public PrototypeId DestTargetProtoRef { get; private set; }     // region connection target or waypoint (TODO: get rid of waypoint refs here)

        public PrototypeId DestTargetRegionProtoRef { get; private set; }
        public PrototypeId DestTargetAreaProtoRef { get; private set; }
        public PrototypeId DestTargetCellProtoRef { get; private set; }
        public PrototypeId DestTargetEntityProtoRef { get; private set; }

        public ulong DestEntityDbId { get; set; }     // TODO: Teleport directly to another player

        public TransferParams(PlayerConnection playerConnection)
        {
            PlayerConnection = playerConnection;
        }

        public bool SetTarget(PrototypeId targetProtoRef, PrototypeId regionProtoRefOverride = PrototypeId.Invalid)
        {
            // REMOVEME: Convert invalid / waypoint refs to connection target refs
            FindRegionConnectionTarget(ref targetProtoRef);
            
            var targetProto = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(targetProtoRef);
            if (targetProto == null) return Logger.WarnReturn(false, "SetTarget(): targetProto == null");

            DestTargetProtoRef = targetProtoRef;    // we keep this to save to the database

            DestTargetRegionProtoRef = regionProtoRefOverride != PrototypeId.Invalid ? regionProtoRefOverride : targetProto.Region;
            DestTargetAreaProtoRef = targetProto.Area;
            DestTargetCellProtoRef = GameDatabase.GetDataRefByAsset(targetProto.Cell);
            DestTargetEntityProtoRef = targetProto.Entity;

            return true;
        }

        // TODO: Manually specify target data (e.g. from a transition destination)

        public void ClearTarget()
        {
            DestTargetProtoRef = PrototypeId.Invalid;

            DestTargetRegionProtoRef = PrototypeId.Invalid;
            DestTargetAreaProtoRef = PrototypeId.Invalid;
            DestTargetCellProtoRef = PrototypeId.Invalid;
            DestTargetEntityProtoRef = PrototypeId.Invalid;
        }
        
        public bool FindStartLocation(out Vector3 position, out Orientation orientation)
        {
            position = Vector3.Zero;
            orientation = Orientation.Zero;

            Game game = PlayerConnection.Game;

            Region region = game.RegionManager.GetRegion(DestRegionId);
            if (region == null) return Logger.WarnReturn(false, "FindStartLocation(): region == null");

            Area startArea = region.GetStartArea();
            if (startArea == null) return Logger.WarnReturn(false, "FindStartLocation(): startArea == null");

            // TODO: Teleport to another player by DbId
            if (DestEntityDbId != 0)
            {
                Logger.Warn("FindStartLocation(): Teleport by EntityDbId is not yet implemented");
            }

            // Get start target from match region
            if (region.Prototype.Behavior == RegionBehavior.MatchPlay)
            {
                var player = PlayerConnection.Player;
                var startTarget = region.GetStartTarget(player);
                if (startTarget != PrototypeId.Invalid)
                    SetTarget(startTarget);
            }

            // Fall back to default start target for the region if we don't have one
            if (DestTargetProtoRef == PrototypeId.Invalid)
            {
                SetTarget(region.Prototype.StartTarget);
                Logger.Warn($"FindStartPosition(): No target specified, falling back to {DestTargetProtoRef.GetName()}");
            }

            if (region.FindTargetLocation(ref position, ref orientation, DestTargetAreaProtoRef, DestTargetCellProtoRef, DestTargetEntityProtoRef))
                return true;

            // Fall back to the center of the first cell in the start area if all else fails
            position = startArea.Cells.First().Value.RegionBounds.Center;
            Logger.Warn($"FindStartPosition(): Failed to find target location, falling back to {position} as the last resort!");
            return true;
        }

        private static void FindRegionConnectionTarget(ref PrototypeId targetProtoRef)
        {
            // REMOVEME: Get rid of this once we remove all code that initiates transfer by waypoint prototype ref

            Prototype proto = GameDatabase.GetPrototype<Prototype>(targetProtoRef);

            if (proto != null)
            {
                // No need to do anything if we already have a connection target
                if (proto is RegionConnectionTargetPrototype)
                    return;

                // Get connection target from the waypoint
                if (proto is WaypointPrototype waypointProto && waypointProto.Destination != PrototypeId.Invalid)
                {
                    targetProtoRef = waypointProto.Destination;
                    return;
                }
            }

            // Fall back to the default start target
            targetProtoRef = GameDatabase.GlobalsPrototype.DefaultStartTargetFallbackRegion;
            Logger.Warn($"FindRegionConnectionTarget(): Invalid target data ref specified, falling back to {targetProtoRef.GetName()}");
        }
    }
}
