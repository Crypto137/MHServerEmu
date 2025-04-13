using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEntityCreate : MissionAction
    {
        private MissionActionEntityCreatePrototype _proto;
        public MissionActionEntityCreate(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // MGSHIELDBaseDefense
            _proto = prototype as MissionActionEntityCreatePrototype;
        }

        public override void Run()
        {
            var entityProto = GameDatabase.GetPrototype<WorldEntityPrototype>(_proto.EntityPrototype);
            if (entityProto == null) return;
            var region = Region;
            if (region == null) return;

            Vector3 position;
            float spawnRadius = 128.0f;

            if (Mission.IsOpenMission)
            {
                var hotspot = Mission.GetFirstMissionHotspot();
                if (hotspot == null) return;
                position = hotspot.RegionLocation.Position;
            }
            else
            {
                var player = Mission.GetFirstParticipant();
                if (player == null) return;
                var avatar = player.CurrentAvatar;
                if (avatar == null) return;
                position = avatar.RegionLocation.Position + (avatar.Forward * spawnRadius);
            }

            Bounds bounds = new(entityProto.Bounds, position);
            PathFlags pathFlags = Region.GetPathFlagsForEntity(entityProto);
            Vector3 spawnPosition = ChooseSpawnPosition(region, position, bounds, pathFlags, spawnRadius);
            var cell = region.GetCellAtPosition(spawnPosition);
            if (cell == null) return;
            spawnPosition = RegionLocation.ProjectToFloor(region, spawnPosition);

            var manager = region.PopulationManager;
            var group = manager.CreateSpawnGroup();
            group.Transform = Transform3.BuildTransform(spawnPosition, Orientation.Zero);
            group.MissionRef = MissionRef;

            var spec = manager.CreateSpawnSpec(group);
            spec.EntityRef = _proto.EntityPrototype;
            spec.Transform = Transform3.Identity();
            spec.SnapToFloor = true;

            int level = region.GetAreaLevel(cell.Area);
            spec.Properties[PropertyEnum.CharacterLevel] = level;
            spec.Properties[PropertyEnum.CombatLevel] = level;
            spec.Properties[PropertyEnum.MissionPrototype] = MissionRef;

            spec.Spawn();

            var entity = spec.ActiveEntity;
            if (entity == null) manager.RemoveSpawnGroup(group.Id);
        }

        private static Vector3 ChooseSpawnPosition(Region region, Vector3 position, Bounds bounds, PathFlags pathFlags, float radius)
        {
            Vector3 spawnPosition = position;
            var posFlags = PositionCheckFlags.CanBeBlockedEntity;
            var blockFlags = BlockingCheckFlags.None;
            bool spawnFound = false;

            if (region.IsLocationClear(bounds, pathFlags, posFlags, blockFlags))
                return bounds.Center;

            float minDistance;
            float maxDistance = 0.0f;

            while (spawnFound == false)
            {
                minDistance = maxDistance;
                maxDistance += radius;
                if (maxDistance > 600.0f) return position;
                spawnFound = region.ChooseRandomPositionNearPoint(bounds, pathFlags, posFlags, blockFlags, minDistance, maxDistance, out spawnPosition);
            }
            return spawnPosition;
        }
    }
}
