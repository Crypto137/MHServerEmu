using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.UI;

namespace MHServerEmu.Games.Entities
{
    public class Transition : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Dictionary<ulong, ulong> _dialogs;
        private Action<ulong, DialogResponse> _onDialogResponse;

        private string _transitionName = string.Empty;          // Seemingly unused
        private List<TransitionDestination> _destinationList = new();

        public IReadOnlyList<TransitionDestination> Destinations { get => _destinationList; }

        public TransitionPrototype TransitionPrototype { get => Prototype as TransitionPrototype; }

        public Transition(Game game) : base(game) 
        {
            SetFlag(EntityFlags.IsNeverAffectedByPowers, true);
        }

        public override void OnEnteredWorld(EntitySettings settings)
        {
            TransitionPrototype transitionProto = TransitionPrototype;
            
            // Create waypoint hotspot if needed.
            if (transitionProto.Waypoint != PrototypeId.Invalid)
            {
                PrototypeId waypointHotspotRef = GameDatabase.GlobalsPrototype.WaypointHotspot;

                using EntitySettings hotspotSettings = ObjectPoolManager.Instance.Get<EntitySettings>();
                hotspotSettings.EntityRef = waypointHotspotRef;
                hotspotSettings.RegionId = Region.Id;
                hotspotSettings.Position = RegionLocation.Position;

                Inventory inventory = SummonedInventory;
                if (inventory != null)
                    hotspotSettings.InventoryLocation = new(Id, inventory.PrototypeDataRef);

                Entity hotspot = Game.EntityManager.CreateEntity(hotspotSettings);
                if (hotspot != null)
                    hotspot.Properties[PropertyEnum.WaypointHotspotUnlock] = transitionProto.Waypoint;
            }

            // Populate destinations
            TransitionDestination destination;
            PrototypeId targetRef;

            switch (transitionProto.Type) 
            {
                case RegionTransitionType.Transition:
                case RegionTransitionType.TransitionDirectReturn:
                    Area area = Area;
                    PrototypeId entityRef = PrototypeDataRef;
                    PrototypeId cellRef = Cell.PrototypeDataRef;
                    Region region = Region;

                    // Early out if we already have destinations for whatever reason.
                    if (_destinationList.Count > 0)
                        break;

                    // Region connection targets have the highest priority (if there are any).
                    if (TransitionDestination.AddDestinationsFromConnectionTargets(settings.Cell, transitionProto, _destinationList))
                        break;

                    // Then check random instances.
                    if (area.RandomInstances.Count > 0)
                    {
                        foreach (RandomInstanceRegionPrototype instance in area.RandomInstances)
                        {
                            PrototypeId instanceCell = GameDatabase.GetDataRefByAsset(instance.OriginCell);
                            if (instanceCell == PrototypeId.Invalid || cellRef != instanceCell)
                                continue;

                            if (instance.OriginEntity != entityRef)
                                continue;

                            destination = TransitionDestination.FromTarget(instance.Target, region, transitionProto);
                            if (destination == null)
                                continue;

                            _destinationList.Add(destination);
                        }

                        if (_destinationList.Count > 0)
                            break;
                    }

                    // Try constructing a return to region origin if we still don't have a destination.
                    destination = TransitionDestination.FromRegionOrigin(region.Settings.Origin);
                    if (destination != null)
                    {
                        _destinationList.Add(destination);
                        break;
                    }

                    // Fall back to the default region if all else fails.
                    targetRef = GameDatabase.GlobalsPrototype.DefaultStartTargetFallbackRegion;
                    destination = TransitionDestination.FromTarget(targetRef, region, TransitionPrototype);
                    if (destination != null)
                        _destinationList.Add(destination);

                    break;

                case RegionTransitionType.TransitionDirect:
                    // Restrict direct transitions (e.g. Cow Level portals) to specific players/parties.
                    Avatar avatar = Game.EntityManager.GetEntity<Avatar>(settings.SourceEntityId);
                    Player player = avatar?.GetOwnerOfType<Player>();
                    if (player == null)
                        break;
                    Properties[PropertyEnum.RestrictedToPlayerGuidParty] = player.DatabaseUniqueId;

                    targetRef = transitionProto.DirectTarget;
                    destination = TransitionDestination.FromTargetRef(targetRef);
                    if (destination != null)
                        _destinationList.Add(destination);

                    break;
            }

            _destinationList.Sort((destA, destB) => destA.UISortOrder.CompareTo(destB.UISortOrder));

            base.OnEnteredWorld(settings);
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);

            if (archive.IsTransient)
            {
                success &= Serializer.Transfer(archive, ref _transitionName);
                success &= Serializer.Transfer(archive, ref _destinationList);
            }

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

            destination.SetEntity(transition);
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

        private bool UseTransitionDefault(Player player, int destinationIndex = -1)
        {
            TransitionPrototype transitionProto = TransitionPrototype;

            Region region = Region;
            if (region == null) return Logger.WarnReturn(false, "UseTransitionDefault(): region == null");

            if (_destinationList.Count == 0)
                return Logger.WarnReturn(false, $"UseTransitionDefault(): No available destinations for [{this}]");

            if (_destinationList.Count == 1)
            {
                destinationIndex = 0;
            }
            else if (_destinationList.Count > 1 && destinationIndex == -1)
            {
                ShowDestinationDialog(player);
                return true;
            }

            if (destinationIndex < 0 || destinationIndex >= _destinationList.Count)
                return Logger.WarnReturn(false, $"UseTransitionDefault(): Destination index out of range for [{this}]");
            
            TransitionDestination destination = _destinationList[destinationIndex];

            PrototypeId destinationRegionRef = destination.RegionRef;
            if (destinationRegionRef == PrototypeId.Invalid)
                return false;

            if (destination.IsAvailable(player) == false)
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

        private void ShowDestinationDialog(Player player)
        {
            ulong playerDbId = player.DatabaseUniqueId;

            GameDialogInstance dialog = null;

            // Allocate dialog data structures on demand because the vast majority of transitions don't use them.
            _dialogs ??= new();
            _onDialogResponse ??= OnDialogResponse;

            // Create a new dialog if we don't have one or the one we had is no longer valid.
            if (_dialogs.TryGetValue(playerDbId, out ulong dialogServerId))
                dialog = Game.GameDialogManager.GetInstance(dialogServerId);

            if (dialog == null)
            {
                dialog = Game.GameDialogManager.CreateInstance(playerDbId);
                dialog.OnResponse = _onDialogResponse;
                dialog.Options |= DialogOptionEnum.ScreenBottom | DialogOptionEnum.WorldClick;
                dialog.InteractorId = player.CurrentAvatar.Id;
                dialog.TargetId = Id;

                _dialogs[playerDbId] = dialog.ServerId;
            }

            if (dialog == null)
                return;

            dialog.Buttons.Clear();

            if (_destinationList.Count > 0)
            {
                TransitionDestination destination = _destinationList[0];
                LocaleStringId text = destination.GetDisplayName();
                bool isEnabled = destination.IsAvailable(player);
                dialog.AddButton(GameDialogResultEnum.eGDR_Option1, text, ButtonStyle.SecondaryPositive, isEnabled);
            }

            if (_destinationList.Count > 1)
            {
                TransitionDestination destination = _destinationList[1];
                LocaleStringId text = destination.GetDisplayName();
                bool isEnabled = destination.IsAvailable(player);
                dialog.AddButton(GameDialogResultEnum.eGDR_Option2, text, ButtonStyle.SecondaryPositive, isEnabled);
            }

            if (_destinationList.Count > 2)
                Logger.Warn($"ShowDestinationDialog(): Transition [{this}] has more than 2 destinations, the remaining destinations will not be included in the dialog");

            Game.GameDialogManager.ShowDialog(dialog);            
        }

        private void OnDialogResponse(ulong playerDbId, DialogResponse response)
        {
            if (response.ButtonIndex < GameDialogResultEnum.eGDR_Option1)
                return;

            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
            if (player == null)
            {
                Logger.Warn("OnDialogResponse(): player == null");
                return;
            }

            Avatar avatar = player.CurrentAvatar;
            if (avatar == null)
            {
                Logger.Warn("OnDialogResponse(): avatar == null");
                return;
            }

            if (avatar.InInteractRange(this, Dialog.InteractionMethod.Use) == false)
            {
                Logger.Warn($"OnDialogResponse(): Avatar [{avatar}] is outside of interact range of [{this}]");
                return;
            }

            int destinationIndex = response.ButtonIndex switch
            {
                GameDialogResultEnum.eGDR_Option1 => 0,
                GameDialogResultEnum.eGDR_Option2 => 1,
                _                                 => -1,
            };

            if (destinationIndex == -1)
            {
                Logger.Warn("OnDialogResponse(): destinationIndex == -1");
                return;
            }

            UseTransitionDefault(player, destinationIndex);
        }
    }
}
