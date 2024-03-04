using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class Spawner : WorldEntity
    {
        public SpawnerPrototype SpawnerPrototype => EntityPrototype as SpawnerPrototype;
        public Spawner(EntityBaseData baseData) : base(baseData)
        {
        }

        public Spawner(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData)
        {
        }

        public Spawner(EntityBaseData baseData, AOINetworkPolicyValues replicationPolicy, ReplicatedPropertyCollection properties) : 
            base(baseData, replicationPolicy, properties)
        {
        }
        public override void EnterWorld(Cell cell, Vector3 position, Vector3 orientation)
        {
            base.EnterWorld(cell, position, orientation);
            if (SpawnerPrototype.StartEnabled)
                Spawn();
        }

        public void Spawn()
        {
            var spawnerProto = SpawnerPrototype;
            /*
            spawnerProto.SpawnSimultaneousMax;
            spawnerProto.SpawnDistanceMax;
            spawnerProto.SpawnDistanceMin;
            spawnerProto.SpawnIntervalMS;
            */
            if (spawnerProto.SpawnSequence.HasValue())
            {
                var entry = spawnerProto.SpawnSequence.First();
                SpawnEntry(entry);
            }
        }

        private void SpawnEntry(SpawnerSequenceEntryPrototype entry)
        {
            // entry.Unique;
            // entry.Count;
            var popObject = entry.GetPopObject();
            SpawnObject(popObject);
        }

        private void SpawnObject(PopulationObjectPrototype popObject)
        {
            var region = Location.Region;
            var random = Game.Random;
            ClusterGroup clusterGroup = new(region, random, popObject, null, Properties, SpawnFlags.None);
            clusterGroup.Initialize();
            var pos = Location.GetPosition();
            var rot = Location.GetOrientation();
            // TODO Add Random to Position in spawnerProto.SpawnDistanceMax
            clusterGroup.SetParentRelativePosition(pos);
            clusterGroup.SetParentRelativeOrientation(rot); // can be random?
            // spawn Entity from Group
            clusterGroup.Spawn();
        }
    }
}
