using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class Spawner : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
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
            _flags |= EntityFlags.NoCollide;
            var spawnerProto = SpawnerPrototype;
            Logger.Debug($"{GameDatabase.GetFormattedPrototypeName(BaseData.PrototypeId)} [{spawnerProto.StartEnabled}] [{spawnerProto.SpawnDistanceMin}] {position.ToStringFloat()}");
            // if (spawnerProto.StartEnabled)
            Spawn();
        }

        public void Spawn()
        {
            var spawnerProto = SpawnerPrototype;
            /*
            spawnerProto.SpawnSimultaneousMax;
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
            for (int i = 0; i < entry.Count; i++) {
                var popObject = entry.GetPopObject();
                SpawnObject(popObject);
            }
        }

        private void SpawnObject(PopulationObjectPrototype popObject)
        {
            var region = Location.Region;
            var random = Game.Random;
            var spawnerProto = SpawnerPrototype;
            ClusterGroup clusterGroup = new(region, random, popObject, null, Properties, SpawnFlags.None);
            clusterGroup.Initialize();
            Vector3 pos = new(Location.GetPosition());
            var rot = Location.GetOrientation();
            clusterGroup.PickPositionInSector(pos, rot, spawnerProto.SpawnDistanceMin, spawnerProto.SpawnDistanceMax);
            // spawn Entity from Group
            clusterGroup.Spawn();
        }
    }
}
