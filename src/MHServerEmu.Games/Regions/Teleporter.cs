using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Regions
{
    // Relevant protobufs:
    // CommonMessages.proto - [ChangeRegionRequestHeader, NetStructRegionLocation, NetStructRegionOrigin, NetStructTransferParams, NetStructRegionTarget]
    // PlayerMgrToGameServer.proto - [GameAndRegionForPlayer]

    /// <summary>
    /// Provides API for initiating teleports from gameplay code.
    /// </summary>
    public struct Teleporter
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Player Player { get; }
        public TeleportContextEnum Context { get; }

        public Transition TransitionEntity { get; set; }
        public int PlayerDeaths { get; set; } = 0;

        public Teleporter(Player player, TeleportContextEnum context)
        {
            Player = player;
            Context = context;
        }

        public void SetEndlessRegionData(Region region)
        {
            // TODO: Clean up the whole RegionContext thing
            var regionContext = Player.PlayerConnection.RegionContext;
            regionContext.FromRegion(region);
        }

        public bool TeleportToTarget(PrototypeId targetProtoRef, PrototypeId regionProtoRefOverride = PrototypeId.Invalid)
        {
            var targetProto = targetProtoRef.As<RegionConnectionTargetPrototype>();
            if (targetProto == null) return Logger.WarnReturn(false, "TeleportToTarget(): targetProto == null");

            Region region = Player.GetRegion();
            if (region == null) return Logger.WarnReturn(false, "TeleportToTarget(): region == null");

            RegionPrototype targetRegionProto = targetRegionProto = targetProto.Region.As<RegionPrototype>();
            if (targetRegionProto == null) return Logger.WarnReturn(false, "TeleportToTarget(): targetRegionProto == null");

            // TODO: Check difficulty and other equivalency things
            if (RegionPrototype.Equivalent(targetRegionProto, region.Prototype))
                return TeleportToLocalTarget(targetProtoRef);
            else
                return TeleportToRemoteTarget(targetProtoRef, regionProtoRefOverride);
        }

        public bool TeleportToWaypoint(PrototypeId waypointProtoRef, PrototypeId regionOverrideProtoRef, PrototypeId difficultyProtoRef)
        {
            WaypointPrototype waypointProto = waypointProtoRef.As<WaypointPrototype>();
            if (waypointProto == null) return Logger.WarnReturn(false, "TeleportToWaypoint(): waypointProto == null");

            RegionConnectionTargetPrototype targetProto = waypointProto.Destination.As<RegionConnectionTargetPrototype>();
            if (targetProto == null) return Logger.WarnReturn(false, "TeleportToWaypoint(): targetProto == null");

            // TODO: Use data from targetProto here directly

            TeleportToTarget(targetProto.DataRef, regionOverrideProtoRef);
            return true;
        }

        public bool TeleportToLastTown()
        {
            // Check last town
            PrototypeId targetProtoRef = PrototypeId.Invalid;

            PrototypeId regionProtoRef = Player.Properties[PropertyEnum.LastTownRegionForAccount];
            RegionPrototype regionProto = regionProtoRef.As<RegionPrototype>();
            if (regionProto != null)
                targetProtoRef = regionProto.StartTarget;

            // Use the fallback if no saved last town
            if (targetProtoRef == PrototypeId.Invalid)
                targetProtoRef = GameDatabase.GlobalsPrototype.DefaultStartTargetFallbackRegion;

            TeleportToTarget(targetProtoRef);
            return true;
        }

        public bool TeleportToTransition(ulong entityId)
        {
            Transition transition = Player.Game.EntityManager.GetEntity<Transition>(entityId);
            if (transition == null) return Logger.WarnReturn(false, "TeleportToTransitionEntity(): transition == null");

            TransitionPrototype transitionProto = transition.TransitionPrototype;
            if (transitionProto == null) return Logger.WarnReturn(false, "TeleportToTransitionEntity(): transitionProto == null");

            Vector3 targetPos = transition.RegionLocation.Position;
            Orientation targetRot = transition.RegionLocation.Orientation;
            targetPos += transitionProto.CalcSpawnOffset(targetRot);

            //uint cellId = transition.Properties[PropertyEnum.MapCellId];
            //uint areaId = transition.Properties[PropertyEnum.MapAreaId];
            //Logger.Debug($"TeleportToTransition(): targetPos={targetPos}, areaId={areaId}, cellId={cellId}");

            ChangePositionResult result = Player.CurrentAvatar.ChangeRegionPosition(targetPos, targetRot, ChangePositionFlags.Teleport);
            return result == ChangePositionResult.PositionChanged || result == ChangePositionResult.Teleport;
        }

        // TODO: Make Local/Remote teleport methods private once the whole DR data thing is cleaned up

        public bool TeleportToLocalTarget(PrototypeId targetProtoRef)
        {
            var targetProto = targetProtoRef.As<RegionConnectionTargetPrototype>();
            if (targetProto == null) return Logger.WarnReturn(false, "TeleportToLocalTarget(): targetProto == null");

            Region region = Player.GetRegion();
            if (region == null) return Logger.WarnReturn(false, "TeleportToLocalTarget(): region == null");

            Vector3 position = Vector3.Zero;
            Orientation orientation = Orientation.Zero;

            if (region.FindTargetLocation(ref position, ref orientation,
                targetProto.Area, GameDatabase.GetDataRefByAsset(targetProto.Cell), targetProto.Entity) == false)
            {
                return Logger.WarnReturn(false, $"TeleportToLocalTarget(): Failed to find target location for target {targetProtoRef.GetName()}");
            }

            if (Player.CurrentAvatar.Area?.PrototypeDataRef != targetProto.Area)
                region.PlayerBeginTravelToAreaEvent.Invoke(new(Player, targetProto.Area));

            Player.SendMessage(NetMessageOneTimeSnapCamera.DefaultInstance);    // Disables camera interpolation for movement

            ChangePositionResult result = Player.CurrentAvatar.ChangeRegionPosition(position, orientation, ChangePositionFlags.Teleport);
            return result == ChangePositionResult.PositionChanged || result == ChangePositionResult.Teleport;
        }

        public bool TeleportToRemoteTarget(PrototypeId targetProtoRef, PrototypeId regionProtoRefOverride)
        {
            Player.PlayerConnection.RegionContext.PlayerDeaths = PlayerDeaths;
            Player.PlayerConnection.MoveToTarget(targetProtoRef, regionProtoRefOverride);
            return true;
        }

        public static void DebugTeleportToTarget(Player player, PrototypeId targetProtoRef)
        {
            Teleporter teleporter = new(player, TeleportContextEnum.TeleportContext_Debug);
            teleporter.TeleportToTarget(targetProtoRef);
        }
    }
}
