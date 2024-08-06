using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.DRAG.Generators.Regions;
using MHServerEmu.Games.Entities;
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

        // TODO: Currently this is more of a collection of older hacks. Implement this properly in the future.

        private static readonly Logger Logger = LogManager.CreateLogger();

        public PlayerConnection PlayerConnection { get; }

        public ulong DestRegionId { get; set; }
        public PrototypeId DestRegionProtoRef { get; set; }
        public PrototypeId DestTargetProtoRef { get; set; }
        public ulong DestEntityId { get; set; }     // DestEntityDbId

        public TransferParams(PlayerConnection playerConnection)
        {
            PlayerConnection = playerConnection;
        }

        public void SetPersistentData(PrototypeId regionProtoRef, PrototypeId targetEntityProtoRef)
        {
            DataDirectory dataDir = DataDirectory.Instance;

            if (dataDir.PrototypeIsA<RegionPrototype>(regionProtoRef) == false)
            {
                regionProtoRef = (PrototypeId)RegionPrototypeId.NPEAvengersTowerHUBRegion;
                Logger.Warn($"SetPersistentData(): Invalid region data ref specified in DBAccount, defaulting to {regionProtoRef.GetName()}");
            }

            if ((dataDir.PrototypeIsA<WaypointPrototype>(targetEntityProtoRef) || dataDir.PrototypeIsA<RegionConnectionTargetPrototype>(targetEntityProtoRef)) == false)
            {
                targetEntityProtoRef = GameDatabase.GetPrototype<RegionPrototype>(regionProtoRef).StartTarget;
                Logger.Warn($"SetPersistentData(): Invalid waypoint data ref specified in DBAccount, defaulting to {targetEntityProtoRef.GetName()}");
            }

            DestRegionProtoRef = regionProtoRef;
            DestTargetProtoRef = targetEntityProtoRef;
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

            if (DestEntityId != Entity.InvalidId)
            {
                WorldEntity targetEntity = game.EntityManager.GetEntity<WorldEntity>(DestEntityId);
                if (targetEntity == null) return Logger.WarnReturn(false, "FindStartLocation(): targetEntity == null");

                if (targetEntity.IsInWorld == false)
                    return Logger.WarnReturn(false, $"FindStartLocation(): targetEntity {targetEntity} is not in world");

                position = targetEntity.RegionLocation.Position;
                orientation = targetEntity.RegionLocation.Orientation;
                if (targetEntity.Prototype is TransitionPrototype transitionProto && transitionProto.SpawnOffset > 0)
                    transitionProto.CalcSpawnOffset(ref orientation, ref position);

                // reset target
                DestEntityId = Entity.InvalidId;
                return true;
            }
            
            if (RegionTransition.FindStartPosition(region, DestTargetProtoRef, out position, out orientation))
            {
                return true;
            }

            // Fall back to the center of the first cell in the start area if all else fails
            position = startArea.Cells.First().Value.RegionBounds.Center;
            return true;
        }
    }
}
