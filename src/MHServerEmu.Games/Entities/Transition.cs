using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public enum WaypointPrototypeId : ulong
    {
        NPEAvengersTowerHub = 10137590415717831231,
        AvengersTowerHub = 15322252936284737788,
    }

    public class Transition : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private string _transitionName = string.Empty;          // Seemingly unused
        private List<Destination> _destinationList = new();

        public List<Destination> DestinationList { get => _destinationList; }

        public TransitionPrototype TransitionPrototype { get => Prototype as TransitionPrototype; }

        public Transition(Game game) : base(game) { }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);

            // old
            Destination destination = Destination.FindDestination(settings.Cell, TransitionPrototype);

            if (destination != null)
                _destinationList.Add(destination);

            return true;
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
            Destination destination;
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

        public bool UseTransition(Avatar avatar, Player player)
        {
            if (TransitionPrototype.Type == RegionTransitionType.ReturnToLastTown)
            {
                TeleportToLastTown(player.PlayerConnection);
                return true;
            }

            if (DestinationList.Count == 0 || DestinationList[0].Type == RegionTransitionType.Waypoint)
                return true;

            Logger.Trace($"Destination entity {DestinationList[0].EntityRef}");

            /*
            if (DestinationList[0].Type == RegionTransitionType.TowerUp ||
                DestinationList[0].Type == RegionTransitionType.TowerDown)
            {
                TeleportToEntity(this, teleport.DestinationList[0].EntityId);
                return true;
            }
            */

            PrototypeId targetRegionProtoRef = DestinationList[0].RegionRef;

            if (targetRegionProtoRef != PrototypeId.Invalid && player.PlayerConnection.TransferParams.DestRegionProtoRef != targetRegionProtoRef)
            {
                TeleportClient(player.PlayerConnection);
                return true;
            }

            if (Game.EntityManager.GetTransitionInRegion(DestinationList[0], RegionLocation.RegionId) is not Transition target)
                return true;

            TransitionPrototype targetTransitionProto = target.TransitionPrototype;
            if (targetTransitionProto == null) return true;
            Vector3 targetPos = target.RegionLocation.Position;
            Orientation targetRot = target.RegionLocation.Orientation;

            targetTransitionProto.CalcSpawnOffset(ref targetRot, ref targetPos);

            Logger.Trace($"Transitioning to {targetPos}");

            uint cellId = target.Properties[PropertyEnum.MapCellId];
            uint areaId = target.Properties[PropertyEnum.MapAreaId];
            Logger.Trace($"Transitioning to areaId={areaId} cellId={cellId}");

            avatar.ChangeRegionPosition(targetPos, targetRot, ChangePositionFlags.Teleport);
            return true;
        }

        private void TeleportClient(PlayerConnection connection)
        {
            Logger.Trace(string.Format("TeleportClient(): Destination region {0} [{1}]",
                GameDatabase.GetFormattedPrototypeName(_destinationList[0].RegionRef),
                GameDatabase.GetFormattedPrototypeName(_destinationList[0].EntityRef)));
            connection.Game.MovePlayerToRegion(connection, _destinationList[0].RegionRef, _destinationList[0].TargetRef);
        }

        private void TeleportToEntity(PlayerConnection connection, ulong entityId)
        {
            Logger.Trace($"TeleportToEntity(): Destination EntityId [{entityId}] [{GameDatabase.GetFormattedPrototypeName(_destinationList[0].EntityRef)}]");
            connection.Game.MovePlayerToEntity(connection, _destinationList[0].EntityId);
        }

        private void TeleportToLastTown(PlayerConnection connection)
        {
            // TODO back to last saved hub
            Logger.Trace($"TeleportToLastTown(): Destination LastTown");
            connection.Game.MovePlayerToRegion(connection, (PrototypeId)RegionPrototypeId.AvengersTowerHUBRegion, (PrototypeId)WaypointPrototypeId.NPEAvengersTowerHub);
        }
    }
}
