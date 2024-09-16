using MHServerEmu.Core.Extensions;
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
            _spawnEvent = new MetaStateSpawnEvent(this, metaGame.Region);
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
                //if (_proto.DataRef == (PrototypeId)7730041682554854878
                //&& region.PrototypeDataRef == (PrototypeId)RegionPrototypeId.CH0402UpperEastRegion) areas = null; // Hack for Moloids
                var spawnLocation = new SpawnLocation(region, areas, _proto.RestrictToCells);
                _spawnEvent.AddRequiredObjects(_proto.PopulationObjects, spawnLocation, _proto.RemovePopObjectsOnSpawnFail);
                _spawnEvent.Schedule();
            }
        }
    }
}
