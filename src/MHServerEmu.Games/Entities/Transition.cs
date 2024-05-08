using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.PowerCollections;
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

    public enum TargetPrototypeId : ulong
    {
        CosmicDoopSectorSpaceStartTarget = 15872240608618488803,
        AsgardCowLevelStartTarget = 12083387244127461092,
        BovineSectorStartTarget = 2342633323497265984,
        JailTarget = 13284513933487907420,
    }

    public class Transition : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private string _transitionName = string.Empty;          // Seemingly unused
        private List<Destination> _destinationList = new();

        public List<Destination> DestinationList { get => _destinationList; }

        public TransitionPrototype TransitionPrototype { get { return EntityPrototype as TransitionPrototype; } }

        // New
        public Transition(Game game) : base(game) { }

        public override void Initialize(EntitySettings settings)
        {
            base.Initialize(settings);
            // old
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity | AOINetworkPolicyValues.AOIChannelDiscovery;
            Destination destination = Destination.FindDestination(settings.Cell, TransitionPrototype);

            if (destination != null)
                _destinationList.Add(destination);
        }

        // Old
        public Transition(EntityBaseData baseData, ReplicatedPropertyCollection properties, Destination destination) : base(baseData)
        {
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity | AOINetworkPolicyValues.AOIChannelDiscovery;
            Properties = properties;
            _trackingContextMap = new();
            _conditionCollection = new(this);
            _powerCollection = new(this);
            _unkEvent = 0;

            if (destination != null)
                _destinationList.Add(destination);
        }

        public Transition(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public Transition(EntityBaseData baseData, EntityTrackingContextMap trackingContextMap, ConditionCollection conditionCollection,
            PowerCollection powerCollection, int unkEvent, 
            string transitionName, IEnumerable<Destination> destinations) : base(baseData)
        {
            _trackingContextMap = trackingContextMap;
            _conditionCollection = conditionCollection;
            _powerCollection = powerCollection;
            _unkEvent = unkEvent;
            _transitionName = transitionName;
            _destinationList.AddRange(destinations);
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);

            //if (archive.IsTransient)
            success &= Serializer.Transfer(archive, ref _transitionName);
            success &= Serializer.Transfer(archive, ref _destinationList);

            return success;
        }

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);

            _transitionName = stream.ReadRawString();

            _destinationList.Clear();
            uint numDestinations = stream.ReadRawVarint32();
            for (uint i = 0; i < numDestinations; i++)
            {
                Destination destination = new();
                destination.Decode(stream);
                _destinationList.Add(destination);
            }
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);

            stream.WriteRawString(_transitionName);
            stream.WriteRawVarint32((uint)_destinationList.Count);
            foreach (Destination destination in _destinationList)
                destination.Encode(stream);
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
            destination.EntityRef = transition.BaseData.PrototypeId;
            destination.Type = TransitionPrototype.Type;
        }

        public void TeleportClient(PlayerConnection connection)
        {
            Logger.Trace(string.Format("TeleportClient(): Destination region {0} [{1}]",
                GameDatabase.GetFormattedPrototypeName(_destinationList[0].RegionRef),
                GameDatabase.GetFormattedPrototypeName(_destinationList[0].EntityRef)));
            connection.Game.MovePlayerToRegion(connection, _destinationList[0].RegionRef, _destinationList[0].TargetRef);
        }

        public void TeleportToEntity(PlayerConnection connection, ulong entityId)
        {
            Logger.Trace($"TeleportToEntity(): Destination EntityId [{entityId}] [{GameDatabase.GetFormattedPrototypeName(_destinationList[0].EntityRef)}]");
            connection.Game.MovePlayerToEntity(connection, _destinationList[0].EntityId);
        }

        public void TeleportToLastTown(PlayerConnection connection)
        {
            // TODO back to last saved hub
            Logger.Trace($"TeleportToLastTown(): Destination LastTown");
            connection.Game.MovePlayerToRegion(connection, (PrototypeId)RegionPrototypeId.AvengersTowerHUBRegion, (PrototypeId)WaypointPrototypeId.NPEAvengersTowerHub);
        }
    }
}
