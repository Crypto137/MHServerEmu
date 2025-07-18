using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.DatabaseAccess.Models;
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

        public PrototypeId DestTargetRegionProtoRef { get; private set; }
        public PrototypeId DestTargetAreaProtoRef { get; private set; }
        public PrototypeId DestTargetCellProtoRef { get; private set; }
        public PrototypeId DestTargetEntityProtoRef { get; private set; }

        public ulong DestEntityDbId { get; set; }     // TODO: Teleport directly to another player

        public TransferParams(PlayerConnection playerConnection)
        {
            PlayerConnection = playerConnection;
        }

        public bool SetTarget(PrototypeId regionProtoRef, PrototypeId areaProtoRef, PrototypeId cellProtoRef, PrototypeId entityProtoRef)
        {
            DestTargetRegionProtoRef = regionProtoRef;
            DestTargetAreaProtoRef = areaProtoRef;
            DestTargetCellProtoRef = cellProtoRef;
            DestTargetEntityProtoRef = entityProtoRef;

            return true;
        }

        public bool SetTarget(PrototypeId targetProtoRef)
        {
            var targetProto = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(targetProtoRef);
            if (targetProto == null) return Logger.WarnReturn(false, "SetTarget(): targetProto == null");

            return SetTarget(targetProto.Region, targetProto.Area, GameDatabase.GetDataRefByAsset(targetProto.Cell), targetProto.Entity);
        }

        public bool SetTarget(MigrationData migrationData)
        {
            if (migrationData.DestTargetRegionProtoId == 0)
                return false;

            PrototypeId regionProtoRef = (PrototypeId)migrationData.DestTargetRegionProtoId;
            PrototypeId areaProtoRef = (PrototypeId)migrationData.DestTargetAreaProtoId;
            PrototypeId cellProtoRef = (PrototypeId)migrationData.DestTargetCellProtoId;
            PrototypeId entityProtoRef = (PrototypeId)migrationData.DestTargetEntityProtoId;

            return SetTarget(regionProtoRef, areaProtoRef, cellProtoRef, entityProtoRef);
        }

        public void SaveTargetForMigration(MigrationData migrationData)
        {
            migrationData.DestTargetRegionProtoId = (ulong)DestTargetRegionProtoRef;
            migrationData.DestTargetAreaProtoId = (ulong)DestTargetAreaProtoRef;
            migrationData.DestTargetCellProtoId = (ulong)DestTargetCellProtoRef;
            migrationData.DestTargetEntityProtoId = (ulong)DestTargetEntityProtoRef;
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
            if (DestTargetRegionProtoRef == PrototypeId.Invalid)
            {
                PrototypeId regionDefaultStartTarget = region.Prototype.StartTarget;
                SetTarget(regionDefaultStartTarget);
                Logger.Warn($"FindStartPosition(): No target specified, falling back to {regionDefaultStartTarget.GetName()}");
            }

            if (region.FindTargetLocation(ref position, ref orientation, DestTargetAreaProtoRef, DestTargetCellProtoRef, DestTargetEntityProtoRef))
                return true;

            // Fall back to the center of the first cell in the start area if all else fails
            position = startArea.Cells.First().Value.RegionBounds.Center;
            Logger.Warn($"FindStartPosition(): Failed to find target location, falling back to {position} as the last resort!");
            return true;
        }
    }
}
