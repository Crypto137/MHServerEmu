using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Population
{
    [Flags]
    public enum MarkerSetOptions
    {
        None = 0,
        NoOffset = 1,
        Default = 2,
        SpawnMissionAssociated = 4,
        NoSpawnMissionAssociated = 8
    }

    public class PropSpawnVisitor
    {
        public PropSpawnVisitor()
        {
        }

        public virtual void Visit(int randomSeed, PropTable propTable, AssetId propSetRef, ProceduralPropGroupPrototype propGroup, EntityMarkerPrototype markerPrototype)
        {
        }
    }

    public class NaviPropSpawnVisitor : PropSpawnVisitor
    {
        private NaviMesh _naviMesh;
        private Transform3 _cellToRegion;

        public NaviPropSpawnVisitor(NaviMesh naviMesh, Transform3 cellToRegion)
        {
            _naviMesh = naviMesh;
            _cellToRegion = cellToRegion;
        }

        public override void Visit(int randomSeed, PropTable propTable, AssetId propSetRef, ProceduralPropGroupPrototype propGroup, EntityMarkerPrototype markerPrototype)
        {
        }
    }

    public class NaviEncounterVisitor : PropSpawnVisitor
    {
        private NaviMesh _naviMesh;
        private Transform3 _cellToRegion;

        public NaviEncounterVisitor(NaviMesh naviMesh, Transform3 cellToRegion)
        {
            _naviMesh = naviMesh;
            _cellToRegion = cellToRegion;
        }

        public override void Visit(int randomSeed, PropTable propTable, AssetId propSetRef, ProceduralPropGroupPrototype propGroup, EntityMarkerPrototype markerPrototype)
        {
        }
    }

    public class InstanceMarkerSetPropSpawnVisitor : PropSpawnVisitor
    {
        private Cell _cell;

        public InstanceMarkerSetPropSpawnVisitor(Cell cell)
        {
            _cell = cell;
        }

        public override void Visit(int randomSeed, PropTable propTable, AssetId propSetRef, ProceduralPropGroupPrototype propGroup, EntityMarkerPrototype markerPrototype)
        {
            if (_cell != null && propTable != null && propGroup != null && markerPrototype != null)
            {
                MarkerSetPrototype markerSet = propGroup.Objects;

                PropTable.GetPropRandomOffsetAndRotation(out Vector3 randomOffset, out float randomRotation, randomSeed, propGroup);

                Vector3 position = new(markerPrototype.Position);
                position += randomOffset;

                Orientation rotation = new(markerPrototype.Rotation);
                rotation.Yaw += randomRotation;

                Transform3 transform = Transform3.BuildTransform(position, rotation);

                MarkerSetOptions instanceMarkerSetOptions = MarkerSetOptions.Default;
                if (!_cell.CellProto.IsOffsetInMapFile) instanceMarkerSetOptions |= MarkerSetOptions.NoOffset;

                _cell.InstanceMarkerSet(markerSet, transform, instanceMarkerSetOptions, propGroup.PrefabPath);
            }
        }

    }
}
