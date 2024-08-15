using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionClusterEnemiesCleared : MissionPlayerCondition
    {
        private MissionConditionClusterEnemiesClearedPrototype _proto;
        private Action<ClusterEnemiesClearedGameEvent> _clusterEnemiesClearedAction;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionClusterEnemiesCleared(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CH02MinorConstructionSite1
            _proto = prototype as MissionConditionClusterEnemiesClearedPrototype;
            _clusterEnemiesClearedAction = OnClusterEnemiesCleared;
        }

        private void OnClusterEnemiesCleared(ClusterEnemiesClearedGameEvent evt)
        {
            var spawnGroup = evt.SpawnGroup;
            var killer = evt.Killer;
            var region = Region;
            if (region == null) return;

            if (_proto.PlayerKillerRequired && killer == null) return;
            if (EvaluateSpawnGroup(spawnGroup))
                Count++;
        }

        private bool EvaluateSpawnGroup(SpawnGroup spawnGroup)
        {
            if (spawnGroup == null) return false;

            if (_proto.WithinRegions.HasValue())
            {
                var spawnRegion = spawnGroup.GetRegion();
                if (spawnRegion == null || spawnRegion.FilterRegions(_proto.WithinRegions) == false) return false;
            }

            if (_proto.WithinAreas.HasValue())
            {
                var spawnArea = spawnGroup.GetArea();
                if (spawnArea == null || _proto.WithinAreas.Contains(spawnArea.PrototypeDataRef) == false) return false;
            }

            if (_proto.SpecificClusters.HasValue())
            {
                var objectProto = spawnGroup.ObjectProto;
                if (objectProto == null) return false;

                var clusterRef = objectProto.DataRef;
                if (clusterRef == PrototypeId.Invalid || _proto.SpecificClusters.Contains(clusterRef) == false) return false;

                if (_proto.OnlyCountMissionClusters)
                {
                    var missionRef = spawnGroup.MissionRef;
                    if (_proto.SpawnedByMission.HasValue())
                    {
                        if (_proto.SpawnedByMission.Contains(missionRef) == false) return false;
                    }
                    else
                    {
                        if (missionRef != Mission.PrototypeDataRef) return false;
                    }
                }
            }

            return true;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.ClusterEnemiesClearedEvent.AddActionBack(_clusterEnemiesClearedAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.ClusterEnemiesClearedEvent.RemoveAction(_clusterEnemiesClearedAction);
        }
    }
}
