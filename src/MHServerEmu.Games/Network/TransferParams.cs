using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Network
{
    /// <summary>
    /// Contains data needed to put a player into a region.
    /// </summary>
    public class TransferParams
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public PlayerConnection PlayerConnection { get; }

        public ulong DestRegionId { get; set; }
        public PrototypeId DestRegionProtoRef { get; set; }

        public NetStructRegionLocation DestLocation { get; private set; }
        public NetStructRegionTarget DestTarget { get; private set; }
        public ulong DestEntityDbId { get; set; }     // TODO: Teleport directly to another player

        // TODO
        // int32 DestTeamIndex
        // bool HasInvite
        // NetStructRegionOrigin Origin

        public TransferParams(PlayerConnection playerConnection)
        {
            PlayerConnection = playerConnection;
        }

        public void SetFromProtobuf(NetStructTransferParams transferParams)
        {
            DestRegionId = transferParams.DestRegionId;
            DestRegionProtoRef = (PrototypeId)transferParams.DestRegionProtoId;

            DestLocation = transferParams.HasDestLocation ? transferParams.DestLocation : null;
            DestTarget = transferParams.HasDestTarget ? transferParams.DestTarget : null;
            DestEntityDbId = transferParams.HasDestEntityDbId ? transferParams.DestEntityDbId : 0;
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

            // Check if there is a region-specific override (e.g. divided start targets)
            if (FindStartLocationFromRegionOverride(region, ref position, ref orientation))
                return true;

            // Check if we have an entity to teleport to (e.g. another player)
            if (FindStartLocationFromEntityDbId(region, ref position, ref orientation))
                return true;

            // Try specific location (e.g. returning from town using bodyslider)
            if (FindStartLocationFromSpecificLocation(region, ref position, ref orientation))
                return true;

            // Try to use the provided target
            if (FindStartLocationFromTarget(region, ref position, ref orientation))
                return true;

            // Fall back to the start target for the region
            if (FindStartLocationFromRegionStartTarget(region, ref position, ref orientation))
                return true;

            // Fall back to the center of the first cell in the start area if all else fails (this is very bad and should never really happen!)
            position = startArea.Cells.First().Value.RegionBounds.Center;
            Logger.Error($"FindStartPosition(): Failed to find target location, falling back to {position} as the last resort!");
            return true;
        }

        private bool FindStartLocationFromRegionOverride(Region region, ref Vector3 position, ref Orientation orientation)
        {
            if (region.Prototype.Behavior != RegionBehavior.MatchPlay)
                return false;

            Player player = PlayerConnection.Player;
            PrototypeId startTarget = region.GetStartTarget(player);
            if (startTarget == PrototypeId.Invalid)
                return false;

            RegionConnectionTargetPrototype targetProto = startTarget.As<RegionConnectionTargetPrototype>();
            if (targetProto == null) return Logger.WarnReturn(false, "FindStartLocationFromRegionOverride(): targetProto == null");

            if (RegionPrototype.Equivalent(targetProto.Region.As<RegionPrototype>(), region.Prototype) == false)
                return Logger.WarnReturn(false, $"FindStartLocationFromRegionOverride(): Target region mismatch, expected {region.PrototypeDataRef.GetName()}, got {targetProto.Region.GetName()}");

            PrototypeId areaProtoRef = targetProto.Area;
            PrototypeId cellProtoRef = GameDatabase.GetDataRefByAsset(targetProto.Cell);
            PrototypeId entityProtoRef = targetProto.Entity;

            if (region.FindTargetLocation(ref position, ref orientation, areaProtoRef, cellProtoRef, entityProtoRef) == false)
                return Logger.WarnReturn(false, $"FindStartLocationFromRegionOverride(): Failed to find location for target {targetProto}");

            return true;
        }

        private bool FindStartLocationFromEntityDbId(Region region, ref Vector3 position, ref Orientation orientation)
        {
            if (DestEntityDbId == 0)
                return false;

            // TODO: Teleport to another player by DbId
            return Logger.WarnReturn(false, "FindStartLocation(): Teleport by EntityDbId is not yet implemented");
        }

        private bool FindStartLocationFromSpecificLocation(Region region, ref Vector3 position, ref Orientation orientation)
        {
            if (DestLocation == null)
                return false;

            ulong regionId = DestLocation.RegionId;
            Vector3 destPosition = new(DestLocation.Position);

            if (regionId != region.Id || Vector3.IsNearZero(destPosition))
                return Logger.WarnReturn(false, $"FindStartLocation(): Invalid location provided\n{DestLocation}");

            Avatar.AdjustStartPositionIfNeeded(region, ref destPosition);
            position = destPosition;
            return true;
        }

        private bool FindStartLocationFromTarget(Region region, ref Vector3 position, ref Orientation orientation)
        {
            if (DestTarget == null)
                return false;

            PrototypeId regionProtoRef = (PrototypeId)DestTarget.RegionProtoId;
            PrototypeId areaProtoRef = (PrototypeId)DestTarget.AreaProtoId;
            PrototypeId cellProtoRef = (PrototypeId)DestTarget.CellProtoId;
            PrototypeId entityProtoRef = (PrototypeId)DestTarget.EntityProtoId;

            if (RegionPrototype.Equivalent(regionProtoRef.As<RegionPrototype>(), region.Prototype) == false)
                return Logger.WarnReturn(false, $"FindStartLocationFromTarget(): Target region mismatch, expected {region.PrototypeDataRef.GetName()}, got {regionProtoRef.GetName()}");

            if (region.FindTargetLocation(ref position, ref orientation, areaProtoRef, cellProtoRef, entityProtoRef) == false)
                return false;

            // Check for collisions and try to adjust position so that avatars don't overlap in one point.
            Avatar.AdjustStartPositionIfNeeded(region, ref position, true);
            return true;
        }

        private bool FindStartLocationFromRegionStartTarget(Region region, ref Vector3 position, ref Orientation orientation)
        {
            Logger.Debug($"FindStartLocation(): Falling back to {region.Prototype.StartTarget.GetName()}");
            var targetProto = region.Prototype.StartTarget.As<RegionConnectionTargetPrototype>();
            if (targetProto == null)
                return false;

            DestTarget = NetStructRegionTarget.CreateBuilder()
                .SetRegionProtoId(DestTarget.RegionProtoId)     // Keep this within the same region, we are just falling back to a different position.
                .SetAreaProtoId((ulong)targetProto.Area)
                .SetCellProtoId((ulong)GameDatabase.GetDataRefByAsset(targetProto.Cell))
                .SetEntityProtoId((ulong)targetProto.Entity)
                .Build();

            return FindStartLocationFromTarget(region, ref position, ref orientation);
        }
    }
}
