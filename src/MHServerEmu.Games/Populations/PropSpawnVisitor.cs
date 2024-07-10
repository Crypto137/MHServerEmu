using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Regions;
using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Games.Populations
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
        public virtual void Visit(int randomSeed, PropTable propTable, AssetId propSetRef,
            ProceduralPropGroupPrototype propGroup, EntityMarkerPrototype markerPrototype)
        { }
    }

    public class NaviPropSpawnVisitor : PropSpawnVisitor
    {
        private NaviMesh _naviMesh;
        private Transform3 _transform;

        public NaviPropSpawnVisitor(NaviMesh naviMesh, in Transform3 transform)
        {
            _naviMesh = naviMesh;
            _transform = transform;
        }

        public override void Visit(int randomSeed, PropTable propTable, AssetId propSetRef, ProceduralPropGroupPrototype propGroup, EntityMarkerPrototype markerPrototype)
        {
            if (_naviMesh == null || propTable == null || propGroup == null || markerPrototype == null) return;
            if (propGroup.NaviPatchSource.NaviPatch.Points.IsNullOrEmpty()) return; // skip
            PropTable.GetPropRandomOffsetAndRotation(out Vector3 randomOffset, out float randomRotation, randomSeed, propGroup);
            Vector3 position = markerPrototype.Position + randomOffset;
            Orientation rotation = markerPrototype.Rotation;
            rotation.Yaw += randomRotation;

            var markerTransform = Transform3.BuildTransform(position, rotation);
            _naviMesh.Stitch(propGroup.NaviPatchSource.NaviPatch, _transform * markerTransform);
            _naviMesh.StitchProjZ(propGroup.NaviPatchSource.PropPatch, _transform * markerTransform);
        }
    }

    public class EncounterVisitor
    {
        public virtual void Visit(PrototypeId encounterRef, SpawnReservation reservation, PopulationEncounterPrototype populationEncounter,
            PrototypeId missionRef, bool useMarkerOrientation)
        { }
    }

    public class NaviEncounterVisitor : EncounterVisitor
    {
        private NaviMesh _naviMesh;
        private Transform3 _transform;

        public NaviEncounterVisitor(NaviMesh naviMesh, in Transform3 transform)
        {
            _naviMesh = naviMesh;
            _transform = transform;
        }

        public override void Visit(PrototypeId encounterRef, SpawnReservation reservation, PopulationEncounterPrototype populationEncounter, PrototypeId missionRef, bool useMarkerOrientation)
        {
            if (_naviMesh == null) return;

            var encounterResourceProto = GameDatabase.GetPrototype<EncounterResourcePrototype>(encounterRef);
            if (encounterResourceProto == null) return;

            Orientation orientation = Orientation.Zero;
            if (useMarkerOrientation || populationEncounter != null && populationEncounter.UseMarkerOrientation)
                orientation = reservation.MarkerRot;

            var patchSource = encounterResourceProto.NaviPatchSource;
            var markerTransform = Transform3.BuildTransform(reservation.MarkerPos, orientation);
            _naviMesh.Stitch(patchSource.NaviPatch, _transform * markerTransform);
            _naviMesh.StitchProjZ(patchSource.PropPatch, _transform * markerTransform);
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
                Vector3 position = markerPrototype.Position;
                position += randomOffset;
                Orientation rotation = markerPrototype.Rotation;
                rotation.Yaw += randomRotation;

                Transform3 transform = Transform3.BuildTransform(position, rotation);

                MarkerSetOptions instanceMarkerSetOptions = MarkerSetOptions.Default;
                if (_cell.CellProto.IsOffsetInMapFile == false) instanceMarkerSetOptions |= MarkerSetOptions.NoOffset;

                _cell.InstanceMarkerSet(markerSet, transform, instanceMarkerSetOptions/*, propGroup.PrefabPath*/);
            }
        }

    }
}
