using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
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
        private static readonly Logger Logger = LogManager.CreateLogger();
        public bool DebugLog;

        private GRandom _random;
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

        public override void EnterWorld(Cell cell, Vector3 position, Orientation orientation)
        {
            base.EnterWorld(cell, position, orientation);
            _flags |= EntityFlags.NoCollide;
            var spawnerProto = SpawnerPrototype;
            DebugLog = false;
            if (DebugLog) Logger.Debug($"[{BaseData.EntityId}] {PrototypeName} [{spawnerProto.StartEnabled}] Distance[{spawnerProto.SpawnDistanceMin}-{spawnerProto.SpawnDistanceMax}] Sequence[{spawnerProto.SpawnSequence.Length}] {position}");
            if (EntityManager.InvSpawners.Contains((EntityManager.InvSpawner)BaseData.PrototypeId)) return;
            _random = Game.Random; //new(cell.Seed);
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
                int i = 0;
                foreach (var entry in spawnerProto.SpawnSequence)
                {
                    if (DebugLog) Logger.Debug($"SpawnEntry[{i++}] {entry.GetType().Name} Unique[{entry.Unique}] Count[{entry.Count}]");
                    SpawnEntry(entry);
                }
            }
        }

        private void SpawnEntry(SpawnerSequenceEntryPrototype entry)
        {
            // entry.Unique;
            // entry.Count;
            var popObject = entry.GetPopObject();
            for (int i = 0; i < entry.Count; i++) { 
                if (DebugLog) Logger.Debug($"SpawnObject[{i}] {popObject.GetType().Name}");
                SpawnObject(popObject);
            }
        }

        private void SpawnObject(PopulationObjectPrototype popObject)
        {
            var region = Location.Region;
            var random = _random;
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
