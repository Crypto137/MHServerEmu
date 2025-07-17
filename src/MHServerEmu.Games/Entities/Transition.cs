using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class Transition : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private string _transitionName = string.Empty;          // Seemingly unused
        private List<TransitionDestination> _destinationList = new();

        public IReadOnlyList<TransitionDestination> Destinations { get => _destinationList; }

        public TransitionPrototype TransitionPrototype { get => Prototype as TransitionPrototype; }

        public Transition(Game game) : base(game) 
        {
            SetFlag(EntityFlags.IsNeverAffectedByPowers, true);
        }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);

            // old
            var destination = TransitionDestination.FindDestination(settings.Cell, TransitionPrototype);

            if (destination != null)
                _destinationList.Add(destination);

            return true;
        }

        public override void OnEnteredWorld(EntitySettings settings)
        {
            var transProto = TransitionPrototype;
            if (transProto.Waypoint != PrototypeId.Invalid)
            {
                var waypointHotspotRef = GameDatabase.GlobalsPrototype.WaypointHotspot;

                using EntitySettings hotspotSettings = ObjectPoolManager.Instance.Get<EntitySettings>();
                hotspotSettings.EntityRef = waypointHotspotRef;
                hotspotSettings.RegionId = Region.Id;
                hotspotSettings.Position = RegionLocation.Position;

                var inventory = SummonedInventory;
                if (inventory != null) hotspotSettings.InventoryLocation = new(Id, inventory.PrototypeDataRef);

                var hotspot = Game.EntityManager.CreateEntity(hotspotSettings);
                if (hotspot != null) hotspot.Properties[PropertyEnum.WaypointHotspotUnlock] = transProto.Waypoint;
            }

            TransitionDestination destination;
            PrototypeId targetRef;

            switch (transProto.Type) 
            {
                case RegionTransitionType.Transition:
                case RegionTransitionType.TransitionDirectReturn:

                    var area = Area;
                    var entityRef = PrototypeDataRef;
                    var cellRef = Cell.PrototypeDataRef;
                    var region = Region;
                    bool noDest = _destinationList.Count == 0;
                    if (noDest && area.RandomInstances.Count > 0)
                        foreach(var instance in area.RandomInstances)
                        {
                            var instanceCell = GameDatabase.GetDataRefByAsset(instance.OriginCell);
                            if (instanceCell == PrototypeId.Invalid || cellRef != instanceCell) continue;
                            if (instance.OriginEntity != entityRef) continue;
                            destination = TransitionDestination.DestinationFromTarget(instance.Target, region, transProto);
                            if (destination == null) continue;
                            _destinationList.Add(destination);
                            noDest = false;
                        }

                    if (noDest)
                    {
                        // TODO destination from region origin target
                        var targets = region.Targets;
                        if (targets.Count == 1)
                        {
                            destination = TransitionDestination.DestinationFromTarget(targets[0].TargetId, region, TransitionPrototype);
                            if (destination != null)
                            {
                                _destinationList.Add(destination);
                                noDest = false;
                            }
                        }
                    }

                    // Get default region
                    if (noDest)
                    {
                        targetRef = GameDatabase.GlobalsPrototype.DefaultStartTargetFallbackRegion;
                        destination = TransitionDestination.DestinationFromTarget(targetRef, region, TransitionPrototype);
                        if (destination != null) _destinationList.Add(destination);
                    }
                    break;

                case RegionTransitionType.TransitionDirect:
            
                    var avatar = Game.EntityManager.GetEntity<Avatar>(settings.SourceEntityId);
                    var player = avatar?.GetOwnerOfType<Player>();
                    if (player == null) break;
                    Properties[PropertyEnum.RestrictedToPlayerGuidParty] = player.DatabaseUniqueId;

                    targetRef = transProto.DirectTarget;
                    destination = TransitionDestination.DestinationFromTargetRef(targetRef);
                    if (destination != null) _destinationList.Add(destination);
                    break;
            }

            base.OnEnteredWorld(settings);
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);

            //if (archive.IsTransient)
            success &= Serializer.Transfer(archive, ref _transitionName);
            success &= Serializer.Transfer(archive, ref _destinationList);

            return success;
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"{nameof(_transitionName)}: {_transitionName}");
            for (int i = 0; i < _destinationList.Count; i++)
                sb.AppendLine($"{nameof(_destinationList)}[{i}]: {_destinationList[i]}");
        }

        public void ConfigureTowerGen(Transition transition)
        {
            TransitionDestination destination;

            if (_destinationList.Count == 0)
            {
                destination = new();
                _destinationList.Add(destination);
            }
            else
            {
                destination = _destinationList[0];
            }

            destination.EntityId = transition.Id;
            destination.EntityRef = transition.PrototypeDataRef;
            destination.Type = TransitionPrototype.Type;
        }

        public bool UseTransition(Player player)
        {
            switch (TransitionPrototype.Type)
            {
                case RegionTransitionType.Transition:
                case RegionTransitionType.TransitionDirect:
                    return UseTransitionDefault(player);

                case RegionTransitionType.Waypoint:
                    return UseTransitionWaypoint(player);

                case RegionTransitionType.TowerUp:
                case RegionTransitionType.TowerDown:
                    return UseTransitionTower(player);

                case RegionTransitionType.TransitionDirectReturn:
                    return UseTransitionDirectReturn(player);

                case RegionTransitionType.ReturnToLastTown:
                    return UseTransitionReturnToLastTown(player);

                default:
                    return Logger.WarnReturn(false, $"UseTransition(): Unimplemented region transition type {TransitionPrototype.Type}");
            }
        }

        private bool UseTransitionDefault(Player player)
        {
            Region region = player.GetRegion();
            if (region == null) return Logger.WarnReturn(false, "UseTransition(): region == null");

            if (_destinationList.Count == 0) return Logger.WarnReturn(false, "UseTransition(): No available destinations!");
            if (_destinationList.Count > 1) Logger.Debug("UseTransition(): _destinationList.Count > 1");

            TransitionDestination destination = _destinationList[0];

            PrototypeId targetRegionProtoRef = destination.RegionRef;

            Teleporter teleporter = new(player, TeleportContextEnum.TeleportContext_Transition);
            teleporter.TransitionEntity = this;

            // TODO: Clean up this whole endless hackery
            if (targetRegionProtoRef != PrototypeId.Invalid && region.PrototypeDataRef != targetRegionProtoRef)
            {
                var regionContext = player.PlayerConnection.RegionContext;
                regionContext.ResetRegionSettings();

                if (TransitionPrototype.Type == RegionTransitionType.TransitionDirect)
                {
                    var regionProto = GameDatabase.GetPrototype<RegionPrototype>(targetRegionProtoRef);

                    if (regionProto.HasEndless())
                        regionContext.EndlessLevel = 1;

                    regionContext.CopyScenarioProperties(Properties);

                    if (regionProto.UsePrevRegionPlayerDeathCount)
                        teleporter.PlayerDeaths = region.PlayerDeaths;

                    // Lifespan for destory Teleport
                    ResetLifespan(TimeSpan.FromMinutes(2));
                    regionContext.PortalId = Id;
                }

                return teleporter.TeleportToRemoteTarget(destination.TargetRef, PrototypeId.Invalid);
            }

            // No need to transfer if we are already in the target region
            return teleporter.TeleportToLocalTarget(destination.TargetRef);
        }

        private bool UseTransitionWaypoint(Player player)
        {
            // TODO: Unlock waypoint
            return true;
        }

        private bool UseTransitionTower(Player player)
        {
            Teleporter teleporter = new(player, TeleportContextEnum.TeleportContext_Transition);
            return teleporter.TeleportToTransition(_destinationList[0].EntityId);
        }

        private bool UseTransitionDirectReturn(Player player)
        {
            if (_destinationList.Count == 0) return Logger.WarnReturn(false, "UseTransition(): No available destinations!");
            if (_destinationList.Count > 1) Logger.Debug("UseTransition(): _destinationList.Count > 1");

            TransitionDestination destination = _destinationList[0];
            // TODO teleport to Position in region
            Teleporter teleporter = new(player, TeleportContextEnum.TeleportContext_Transition);
            teleporter.TransitionEntity = this;
            return teleporter.TeleportToTarget(destination.TargetRef);
        }

        private bool UseTransitionReturnToLastTown(Player player)
        {
            Teleporter teleporter = new(player, TeleportContextEnum.TeleportContext_Transition);
            teleporter.TransitionEntity = this;
            return teleporter.TeleportToLastTown();
        }
    }
}
