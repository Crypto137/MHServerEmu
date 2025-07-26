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
            var destination = TransitionDestination.Find(settings.Cell, TransitionPrototype);

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
                            destination = TransitionDestination.FromTarget(instance.Target, region, transProto);
                            if (destination == null) continue;
                            _destinationList.Add(destination);
                            noDest = false;
                        }

                    // Try constructing a return to region origin 
                    if (noDest)
                    {
                        NetStructRegionOrigin origin = region.Settings.Origin;
                        if (origin != null)
                        {
                            destination = TransitionDestination.FromRegionOrigin(origin);
                            if (destination != null)
                            {
                                _destinationList.Add(destination);
                                noDest = false;
                            }
                        }
                    }

                    // Fall back to the default region
                    if (noDest)
                    {
                        targetRef = GameDatabase.GlobalsPrototype.DefaultStartTargetFallbackRegion;
                        destination = TransitionDestination.FromTarget(targetRef, region, TransitionPrototype);
                        if (destination != null) _destinationList.Add(destination);
                    }
                    break;

                case RegionTransitionType.TransitionDirect:
            
                    var avatar = Game.EntityManager.GetEntity<Avatar>(settings.SourceEntityId);
                    var player = avatar?.GetOwnerOfType<Player>();
                    if (player == null) break;
                    Properties[PropertyEnum.RestrictedToPlayerGuidParty] = player.DatabaseUniqueId;

                    targetRef = transProto.DirectTarget;
                    destination = TransitionDestination.FromTargetRef(targetRef);
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
            TransitionPrototype transitionProto = TransitionPrototype;

            Region region = Region;
            if (region == null) return Logger.WarnReturn(false, "UseTransitionDefault(): region == null");

            if (_destinationList.Count == 0)
                return Logger.WarnReturn(false, "UseTransitionDefault(): No available destinations!");

            if (_destinationList.Count > 1)
                Logger.Debug("UseTransitionDefault(): _destinationList.Count > 1");

            TransitionDestination destination = _destinationList[0];

            PrototypeId destinationRegionRef = destination.RegionRef;
            if (destinationRegionRef == PrototypeId.Invalid)
                return false;

            RegionPrototype destinationRegionProto = destinationRegionRef.As<RegionPrototype>();
            if (destinationRegionProto == null) return Logger.WarnReturn(false, "UseTransitionDefault(): destinationRegionProto == null");

            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Transition);
            teleporter.TransitionEntity = this;

            // TODO: Also check destination type?
            if (transitionProto.Type == RegionTransitionType.TransitionDirect)
                teleporter.SetAccessPortal(this);

            teleporter.DangerRoomScenarioItemDbGuid = Properties[PropertyEnum.DangerRoomScenarioItemDbGuid];
            teleporter.ItemRarity = Properties[PropertyEnum.ItemRarity];
            teleporter.DangerRoomScenarioRef = Properties[PropertyEnum.CreatorPowerPrototype];

            teleporter.Properties.CopyProperty(Properties, PropertyEnum.DifficultyIndex);
            teleporter.Properties.CopyProperty(Properties, PropertyEnum.DamageRegionMobToPlayer);
            teleporter.Properties.CopyProperty(Properties, PropertyEnum.DamageRegionPlayerToMob);

            if (Properties.HasProperty(PropertyEnum.RegionAffix))
            {
                foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.RegionAffix))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId affixProtoRef);
                    teleporter.Affixes.Add(affixProtoRef);
                }
            }

            // Copy data from the current region
            if (destinationRegionProto.HasEndlessTheme())
            {
                RegionPrototype currentRegionProto = region.Prototype;

                if (currentRegionProto.HasEndlessTheme() && currentRegionProto.DataRef == destinationRegionProto.DataRef)
                {
                    // This is a local teleport within the same endless region, copy the data without changing it
                    teleporter.CopyEndlessRegionData(region, false);
                }
                else
                {
                    // Initialize new endless region data 
                    teleporter.EndlessLevel = 1;

                    PrototypeId difficultyTierRef = Properties[PropertyEnum.DifficultyTier];
                    if (difficultyTierRef != PrototypeId.Invalid)
                        teleporter.DifficultyTierRef = difficultyTierRef;
                }
            }
            else
            {
                // Keep difficulty tier consistent outside of towns
                if (region.Behavior != RegionBehavior.Town)
                    teleporter.DifficultyTierRef = region.DifficultyTierRef;
            }

            if (destinationRegionProto.UsePrevRegionPlayerDeathCount)
                teleporter.PlayerDeaths = region.PlayerDeaths;

            if (teleporter.TeleportToTarget(destination.RegionRef, destination.AreaRef, destination.CellRef, destination.EntityRef) == false)
                return false;

            return true;
        }

        private bool UseTransitionWaypoint(Player player)
        {
            // TODO: Unlock waypoint
            return true;
        }

        private bool UseTransitionTower(Player player)
        {
            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Transition);
            return teleporter.TeleportToTransition(_destinationList[0].EntityId);
        }

        private bool UseTransitionDirectReturn(Player player)
        {
            if (_destinationList.Count == 0)
                return Logger.WarnReturn(false, $"UseTransitionDirectReturn(): No available destinations for [{this}]");
            
            if (_destinationList.Count > 1)
                return Logger.WarnReturn(false, $"UseTransitionDirectReturn(): More than one return destination for [{this}]");

            TransitionDestination destination = _destinationList[0];

            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Transition);
            teleporter.TransitionEntity = this;

            return teleporter.TeleportToRegionLocation(destination.RegionId, destination.Position);
        }

        private bool UseTransitionReturnToLastTown(Player player)
        {
            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Transition);
            teleporter.TransitionEntity = this;
            return teleporter.TeleportToLastTown();
        }
    }
}
