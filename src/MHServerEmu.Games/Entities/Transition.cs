using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
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
        public static readonly Logger Logger = LogManager.CreateLogger();
        public string TransitionName { get; set; }
        public List<Destination> Destinations { get; set; }

        public TransitionPrototype TransitionPrototype { get { return EntityPrototype as TransitionPrototype; } }

        // New
        public Transition(Game game) : base(game) { }

        public override void Initialize(EntitySettings settings)
        {
            base.Initialize(settings);
            // old
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity | AOINetworkPolicyValues.AOIChannelDiscovery;
            Destination destination = Destination.FindDestination(settings.Cell, TransitionPrototype);

            TransitionName = "";
            Destinations = new();
            if (destination != null)
                Destinations.Add(destination);
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

            TransitionName = "";
            Destinations = new();
            if (destination != null)
                Destinations.Add(destination);
        }

        public Transition(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public Transition(EntityBaseData baseData, EntityTrackingContextMap trackingContextMap, ConditionCollection conditionCollection,
            PowerCollection powerCollection, int unkEvent, 
            string transitionName, List<Destination> destinations) : base(baseData)
        {
            _trackingContextMap = trackingContextMap;
            _conditionCollection = conditionCollection;
            _powerCollection = powerCollection;
            _unkEvent = unkEvent;
            TransitionName = transitionName;
            Destinations = destinations;
        }

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);

            TransitionName = stream.ReadRawString();

            Destinations = new();
            int destinationsCount = (int)stream.ReadRawVarint64();
            for (int i = 0; i < destinationsCount; i++)
            {
                Destination destination = new();
                destination.Decode(stream);
                Destinations.Add(destination);
            }

        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);

            stream.WriteRawString(TransitionName);
            stream.WriteRawVarint64((ulong)Destinations.Count);
            foreach (Destination destination in Destinations) destination.Encode(stream);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"TransitionName: {TransitionName}");
            for (int i = 0; i < Destinations.Count; i++) sb.AppendLine($"Destination{i}: {Destinations[i]}");
        }

        public void ConfigureTowerGen(Transition transition)
        {
            Destination destination;
            if (Destinations.Count == 0)
            {
                destination = new();
                Destinations.Add(destination);
            }
            else
            {
                destination = Destinations[0];
            }
            destination.EntityId = transition.Id;
            destination.EntityRef = transition.BaseData.PrototypeId;
            destination.Type = TransitionPrototype.Type;
        }

        public void TeleportClient(PlayerConnection connection)
        {
            Logger.Trace($"Destination region {GameDatabase.GetFormattedPrototypeName(Destinations[0].RegionRef)} [{GameDatabase.GetFormattedPrototypeName(Destinations[0].EntityRef)}]");
            connection.Game.MovePlayerToRegion(connection, Destinations[0].RegionRef, Destinations[0].TargetRef);
        }

        public void TeleportToEntity(PlayerConnection connection, ulong entityId)
        {
            Logger.Trace($"Destination EntityId [{entityId}] [{GameDatabase.GetFormattedPrototypeName(Destinations[0].EntityRef)}]");
            connection.Game.MovePlayerToEntity(connection, Destinations[0].EntityId);
        }

        public void TeleportToLastTown(PlayerConnection connection)
        {
            // TODO back to last saved hub
            Logger.Trace($"Destination LastTown");
            connection.Game.MovePlayerToRegion(connection, (PrototypeId)RegionPrototypeId.AvengersTowerHUBRegion, (PrototypeId)WaypointPrototypeId.NPEAvengersTowerHub);
        }
    }
}
