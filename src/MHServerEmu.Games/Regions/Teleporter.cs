using Gazillion;
using MHServerEmu.Core.Logging;
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
            // TODO: Clean up the whole RegionContextThing
            var regionContext = Player.PlayerConnection.RegionContext;
            regionContext.FromRegion(region);
        }

        public bool TeleportToTarget(PrototypeId targetProtoRef, PrototypeId regionProtoRefOverride = PrototypeId.Invalid)
        {
            Player.PlayerConnection.RegionContext.PlayerDeaths = PlayerDeaths;
            Player.PlayerConnection.MoveToTarget(targetProtoRef, regionProtoRefOverride);
            return true;
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

        public static void DebugTeleportToTarget(Player player, PrototypeId targetProtoRef)
        {
            Teleporter teleporter = new(player, TeleportContextEnum.TeleportContext_Debug);
            teleporter.TeleportToTarget(targetProtoRef);
        }
    }
}
