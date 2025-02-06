using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Populations;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStatePopulationMaintain : MetaState
    {
	    private MetaStatePopulationMaintainPrototype _proto;
        private MetaStateSpawnEvent _spawnEvent;

        public MetaStatePopulationMaintain(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStatePopulationMaintainPrototype;
            if (metaGame.Region != null)
                _spawnEvent = new MetaStateSpawnEvent(PrototypeDataRef, metaGame.Region);
        }

        public override void OnApply()
        {
            var region = MetaGame.Region;
            if (region == null) return;
            if (_proto.PopulationObjects.HasValue())
            {
                _spawnEvent.RespawnObject = _proto.Respawn;
                _spawnEvent.RespawnDelayMS = _proto.RespawnDelayMS;

                var areas = _proto.RestrictToAreas;

                // Hack for Moloids
                // if (_proto.DataRef == (PrototypeId)7730041682554854878 && region.PrototypeDataRef == (PrototypeId)RegionPrototypeId.CH0402UpperEastRegion) areas = null;

                var spawnLocation = new SpawnLocation(region, areas, _proto.RestrictToCells);
                var time = TimeSpan.FromMilliseconds(Game.Random.Next(0, 1000));

                if (MetaGame.Debug) MetaGame.Logger.Debug($"MetaStatePopulationMaintain {_proto.DataRef.GetNameFormatted()} " +
                    $"[{_proto.RespawnDelayMS}] [{_proto.PopulationObjects.Length}]");

                _spawnEvent.AddRequiredObjects(_proto.PopulationObjects, spawnLocation, PrototypeId.Invalid, true, _proto.RemovePopObjectsOnSpawnFail, time);
                _spawnEvent.Schedule();
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            _spawnEvent.Destroy();
        }
    }
}
