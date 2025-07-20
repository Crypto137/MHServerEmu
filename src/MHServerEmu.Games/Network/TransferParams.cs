using Gazillion;
using MHServerEmu.Core.Collisions;
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

        public void FromProtobuf(NetStructTransferParams transferParams)
        {
            DestRegionId = transferParams.DestRegionId;
            DestRegionProtoRef = (PrototypeId)transferParams.DestRegionProtoId;

            DestLocation = transferParams.HasDestLocation ? transferParams.DestLocation : null;
            DestTarget = transferParams.HasDestTarget ? transferParams.DestTarget : null;
            DestEntityDbId = transferParams.HasDestEntityDbId ? transferParams.DestEntityDbId : 0;
        }

        public NetStructTransferParams ToProtobuf()
        {
            NetStructTransferParams.Builder transferParams = NetStructTransferParams.CreateBuilder()
                .SetTransferId(0)   // TODO
                .SetDestRegionId(DestRegionId)
                .SetDestRegionProtoId((ulong)DestRegionProtoRef);

            if (DestLocation != null)
                transferParams.SetDestLocation(DestLocation);

            if (DestTarget != null)
                transferParams.SetDestTarget(DestTarget);

            if (DestEntityDbId != 0)
                transferParams.SetDestEntityDbId(DestEntityDbId);

            return transferParams.Build();
        }

        public bool SetLocation(NetStructRegionLocation destLocation)
        {
            DestLocation = destLocation;
            DestTarget = null;
            DestEntityDbId = 0;
            return true;
        }

        public bool SetLocation(ulong regionId, Vector3 position)
        {
            NetStructRegionLocation destLocation = NetStructRegionLocation.CreateBuilder()
                .SetRegionId(regionId)
                .SetPosition(position.ToNetStructPoint3())
                .Build();

            return SetLocation(destLocation);
        }

        public bool SetTarget(NetStructRegionTarget destTarget)
        {
            DestRegionProtoRef = (PrototypeId)destTarget.RegionProtoId;

            DestLocation = null;
            DestTarget = destTarget;
            DestEntityDbId = 0;
            return true;
        }

        public bool SetTarget(PrototypeId regionProtoRef, PrototypeId areaProtoRef, PrototypeId cellProtoRef, PrototypeId entityProtoRef)
        {
            NetStructRegionTarget destTarget = NetStructRegionTarget.CreateBuilder()
                .SetRegionProtoId((ulong)regionProtoRef)
                .SetAreaProtoId((ulong)areaProtoRef)
                .SetCellProtoId((ulong)cellProtoRef)
                .SetEntityProtoId((ulong)entityProtoRef)
                .Build();

            return SetTarget(destTarget);
        }

        public bool SetTarget(PrototypeId targetProtoRef)
        {
            var targetProto = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(targetProtoRef);
            if (targetProto == null) return Logger.WarnReturn(false, "SetTarget(): targetProto == null");

            return SetTarget(targetProto.Region, targetProto.Area, GameDatabase.GetDataRefByAsset(targetProto.Cell), targetProto.Entity);
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

            // Get start target from match region
            if (region.Prototype.Behavior == RegionBehavior.MatchPlay)
            {
                var player = PlayerConnection.Player;
                var startTarget = region.GetStartTarget(player);
                if (startTarget != PrototypeId.Invalid)
                    SetTarget(startTarget);
            }

            // TODO: Teleport to another player by DbId
            if (DestEntityDbId != 0)
            {
                Logger.Warn("FindStartLocation(): Teleport by EntityDbId is not yet implemented");
            }

            // Try specific location
            if (DestLocation != null)
            {
                ulong regionId = DestLocation.RegionId;
                Vector3 destPosition = new(DestLocation.Position);

                if (regionId == region.Id && Vector3.IsNearZero(destPosition) == false)
                {
                    if (FindValidPositionAt(region, ref destPosition))
                    {
                        position = destPosition;
                        return true;
                    }
                }
                else
                {
                    Logger.Warn($"FindStartLocation(): Invalid location provided\n{DestLocation}");
                }
            }

            // Use the provided target
            if (DestTarget == null)
            {
                PrototypeId regionDefaultStartTarget = region.Prototype.StartTarget;
                Logger.Warn($"FindStartPosition(): No target specified, falling back to {regionDefaultStartTarget.GetName()}");
                SetTarget(regionDefaultStartTarget);
            }

            PrototypeId regionProtoRef = (PrototypeId)DestTarget.RegionProtoId;
            PrototypeId areaProtoRef = (PrototypeId)DestTarget.AreaProtoId;
            PrototypeId cellProtoRef = (PrototypeId)DestTarget.CellProtoId;
            PrototypeId entityProtoRef = (PrototypeId)DestTarget.EntityProtoId;

            if (RegionPrototype.Equivalent(regionProtoRef.As<RegionPrototype>(), region.Prototype) == false)
                Logger.Warn($"FindStartPosition(): Target region mismatch, expected {region.PrototypeDataRef.GetName()}, got {regionProtoRef.GetName()}");

            if (region.FindTargetLocation(ref position, ref orientation, areaProtoRef, cellProtoRef, entityProtoRef))
                return true;

            // Fall back to the center of the first cell in the start area if all else fails (this is very bad and should never really happen)
            position = startArea.Cells.First().Value.RegionBounds.Center;
            Logger.Warn($"FindStartPosition(): Failed to find target location, falling back to {position} as the last resort!");
            return true;
        }

        private bool FindValidPositionAt(Region region, ref Vector3 position)
        {
            // TODO: Check collisions
            return true;
        }
    }
}
