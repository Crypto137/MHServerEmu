using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Generators;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Navi;

namespace MHServerEmu.Games.Regions
{
    public class CellSettings
    {
        public Vector3 PositionInArea = new();
        public Orientation OrientationInArea = new();
        public PrototypeId CellRef;
        public int Seed;
        public LocaleStringId OverrideLocationName;
        public List<uint> ConnectedCells;
        public PrototypeId PopulationThemeOverrideRef;
    }

    public class Cell
    {
        //  Old
        public PrototypeId PrototypeId { get; private set; }

        // New
        public uint Id { get; }      
        public CellPrototype CellProto { get; private set; }        
        public CellSettings Settings { get; private set; }
        public Type _type { get; private set; }
        public int Seed { get; private set; }
        public PrototypeId PopulationThemeOverrideRef { get; private set; }
        public Aabb RegionBounds { get; private set; }
        public Area Area { get; private set; }
        public Game Game { get => Area?.Game; }
        public IEnumerable<Entity> Entities { get => Game.EntityManager.GetEntities(this); } // TODO: Optimize

        public List<uint> CellConnections = new();
        public List<ReservedSpawn> Encounters { get; } = new();

        private float PlayableNavArea;
        private float SpawnableNavArea;
        public float PlayableArea { get => (PlayableNavArea != -1.0) ? PlayableNavArea : 0.0f; }
        public float SpawnableArea { get => (SpawnableNavArea != -1.0) ? SpawnableNavArea : 0.0f; }
        public CellRegionSpatialPartitionLocation SpatialPartitionLocation { get; }
        public Vector3 AreaOffset { get; private set; }
        public Vector3 AreaPosition { get; private set; }
        public Orientation AreaOrientation { get; private set; }
        public Transform3 AreaTransform { get; private set; }
        public Transform3 RegionTransform { get; private set; }

        public Cell(Area area, uint id)
        {
            RegionBounds = Aabb.Zero;
            AreaPosition = Vector3.Zero;
            AreaOrientation = Orientation.Zero;
            Area = area;
            Id = id;
            PlayableNavArea = -1.0f;
            SpawnableNavArea = -1.0f;
            SpatialPartitionLocation = new(this);
        }

        public bool Initialize(CellSettings settings)
        {
            if (Area == null) return false;
            if (settings.CellRef == 0) return false;

            PrototypeId = settings.CellRef;
            CellProto = GameDatabase.GetPrototype<CellPrototype>(PrototypeId);
            if (CellProto == null) return false;

            SpawnableNavArea = CellProto.NaviPatchSource.SpawnableArea;
            PlayableNavArea = CellProto.NaviPatchSource.PlayableArea;
            if (PlayableNavArea == -1.0f) PlayableNavArea = 0.0f;

            if (SpawnableNavArea == -1.0f && PlayableNavArea >= 0.0f)
                SpawnableNavArea = PlayableNavArea;

            _type = CellProto.Type;
            Seed = settings.Seed;
            PopulationThemeOverrideRef = settings.PopulationThemeOverrideRef;

            if (settings.ConnectedCells != null && settings.ConnectedCells.Any())
                CellConnections.AddRange(settings.ConnectedCells);

            Settings = settings;
            SetAreaPosition(settings.PositionInArea, settings.OrientationInArea);

            return true;
        }

        public void SetAreaPosition(Vector3 positionInArea, Orientation orientationInArea)
        {
            if (CellProto == null) return;

            if (SpatialPartitionLocation.IsValid())
                GetRegion().PartitionCell(this, Region.PartitionContext.Remove);

            AreaPosition = positionInArea;
            AreaOrientation = orientationInArea;

            AreaTransform = Transform3.BuildTransform(positionInArea, orientationInArea);
            RegionTransform = Transform3.BuildTransform(positionInArea + Area.Origin, orientationInArea);

            AreaOffset = Area.AreaToRegion(positionInArea);

            RegionBounds = CellProto.BoundingBox.Translate(AreaOffset);
            RegionBounds.RoundToNearestInteger();

            if (!SpatialPartitionLocation.IsValid())
                GetRegion().PartitionCell(this, Region.PartitionContext.Insert);
        }

        public void AddNavigationDataToRegion()
        {
             Region region = GetRegion();
             if (region == null) return;
             NaviMesh naviMesh = region.NaviMesh;
             if (CellProto == null) return;

             Transform3 transform;
             if (CellProto.IsOffsetInMapFile == false)
                 transform = RegionTransform;
             else
                 transform = Transform3.BuildTransform(Area.Origin, Orientation.Zero);

             if (naviMesh.Stitch(CellProto.NaviPatchSource.NaviPatch, transform) == false) return;
             if (naviMesh.StitchProjZ(CellProto.NaviPatchSource.PropPatch, transform) == false) return;

             VisitPropSpawns(new NaviPropSpawnVisitor(naviMesh, transform)); 
            // VisitEncounters(new NaviEncounterVisitor(naviMesh, transform));            
        }

        public void AddCellConnection(uint id)
        {
            CellConnections.Add(id);
        }

        public static bool DetermineType(ref Type type, Vector3 position)
        {
            Vector3 northVector = new(1, 0, 0);
            Vector3 eastVector = new(0, 1, 0);

            Vector3 normalizedVector = Vector3.Normalize2D(position);

            float northDot = Vector3.Dot(northVector, normalizedVector);
            float eastDot = Vector3.Dot(eastVector, normalizedVector);

            if (northDot >= 0.75)
            {
                type |= Type.N;
                return true;
            }
            else if (northDot <= -0.75)
            {
                type |= Type.S;
                return true;
            }
            else if (eastDot >= 0.75)
            {
                type |= Type.E;
                return true;
            }
            else if (eastDot <= -0.75)
            {
                type |= Type.W;
                return true;
            }

            return false;
        }

        public bool PostInitialize()
        {
            var region = GetRegion();
            var entityManager = Game.EntityManager;
            PrototypeId areaRef = Area.PrototypeDataRef;
            ConnectionNodeList targets = region.Targets;

            foreach (var marker in CellProto.InitializeSet.Markers)
            {
                if (marker is EntityMarkerPrototype entityMarker)
                {
                    PrototypeId protoId = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
                    Prototype entity = GameDatabase.GetPrototype<Prototype>(protoId);
                    
                    // Spawn Teleports
                    if (entity is TransitionPrototype transition)
                    {
                        bool snap = entityMarker.OverrideSnapToFloor;
                        Vector3 position = CalcMarkerPosition(entityMarker.Position);
                        position.Z += transition.Bounds.GetBoundHalfHeight();
                        PrototypeId targetRef = PrototypeId.Invalid;
                        if (transition.Waypoint != 0)
                        {
                            var waypointProto = GameDatabase.GetPrototype<WaypointPrototype>(transition.Waypoint);
                            targetRef = waypointProto.Destination;
                        }
                        else
                        {
                            TargetObject node = RegionTransition.GetTargetNode(targets, areaRef, this.PrototypeId, entityMarker.EntityGuid);
                            if (node != null) targetRef = node.TargetId;
                        }
                        entityManager.SpawnTargetTeleport(this, transition, position, entityMarker.Rotation, false, targetRef, snap);
                    }
                }
            }

            return true;
        }

        public void InstanceMarkerSet(MarkerSetPrototype markerSet, Transform3 transform, MarkerSetOptions instanceMarkerSetOptions)
        {
            if (instanceMarkerSetOptions.HasFlag(MarkerSetOptions.SpawnMissionAssociated | MarkerSetOptions.NoSpawnMissionAssociated)) return;
            if (markerSet.Markers.HasValue())
                foreach (var marker in markerSet.Markers)
                    if (marker != null)
                        SpawnMarker(marker, transform, instanceMarkerSetOptions);
        }

        public void SpawnMarker(MarkerPrototype marker, Transform3 transform, MarkerSetOptions options)
        {
            if (marker is EntityMarkerPrototype entityMarker)
            {
                PrototypeId dataRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
                if (entityMarker.LastKnownEntityName.Contains("GambitMTXStore")) return; // Invisible Domino NPC
                Prototype entity = GameDatabase.GetPrototype<Prototype>(dataRef);

                // Spawn Entity from Cell
                if (entity is WorldEntityPrototype)
                    SpawnEntityMarker(entityMarker, transform, options);
            }
        }

        public void SpawnEntityMarker(EntityMarkerPrototype entityMarker, Transform3 transform, MarkerSetOptions options)
        {
            CalcMarkerTransform(entityMarker, transform, options, out Vector3 entityPosition, out Orientation entityOrientation);

            var protoRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
            var entity = GameDatabase.GetPrototype<WorldEntityPrototype>(protoRef);

            bool? snapToFloor = SpawnSpec.SnapToFloorConvert(entityMarker.OverrideSnapToFloor, entityMarker.OverrideSnapToFloorValue);
            snapToFloor ??= entity.SnapToFloorOnSpawn;
            bool overrideSnap = snapToFloor != entity.SnapToFloorOnSpawn;
            if (snapToFloor == true) // Fix Boxes in Axis Raid
            {
                float projectHeight = RegionBounds.Center.Z + RegionLocation.ProjectToFloor(CellProto, entityPosition);
                if (entityPosition.Z > projectHeight)
                    entityPosition.Z = projectHeight;
            }
            if (entity.Bounds != null)
                entityPosition.Z += entity.Bounds.GetBoundHalfHeight();

            int health = EntityManager.GetRankHealth(entity);
            WorldEntity worldEntity = Game.EntityManager.CreateWorldEntity(this, protoRef, null, entityPosition, entityOrientation, health, false, overrideSnap);
            if (worldEntity.WorldEntityPrototype is AgentPrototype)
                worldEntity.AppendOnStartActions(GetRegion().PrototypeDataRef);
        }

        public void CalcMarkerTransform(EntityMarkerPrototype entityMarker, Transform3 transform, MarkerSetOptions options,
            out Vector3 markerPosition, out Orientation markerOrientation)
        {
            Transform3 markerTransform = transform * Transform3.BuildTransform(entityMarker.Position, entityMarker.Rotation);

            if (options.HasFlag(MarkerSetOptions.NoOffset))
                markerTransform = RegionTransform * markerTransform;
            else
                markerTransform = Transform3.BuildTransform(AreaOffset, Orientation.Zero) * markerTransform;

            markerPosition = new (markerTransform.Translation);
            markerOrientation = new (Orientation.FromTransform3(markerTransform));
        }

        public static Type BuildTypeFromWalls(Walls walls)
        {
            Type type = Type.None;

            if (!walls.HasFlag(Walls.N)) type |= Type.N;
            if (!walls.HasFlag(Walls.E)) type |= Type.E;
            if (!walls.HasFlag(Walls.S)) type |= Type.S;
            if (!walls.HasFlag(Walls.W)) type |= Type.W;

            if (!walls.HasFlag(Walls.E | Walls.N) && walls.HasFlag(Walls.NE)) type |= Type.dNE;
            if (!walls.HasFlag(Walls.S | Walls.E) && walls.HasFlag(Walls.SE)) type |= Type.dSE;
            if (!walls.HasFlag(Walls.W | Walls.S) && walls.HasFlag(Walls.SW)) type |= Type.dSW;
            if (!walls.HasFlag(Walls.W | Walls.N) && walls.HasFlag(Walls.NW)) type |= Type.dNW;

            return type;
        }

        public static Walls WallsRotate(Walls walls, int clockwiseRotation)
        {
            if (clockwiseRotation == 0 || clockwiseRotation >= 8) return walls;
            int rotatedWalls = ((int)walls & 0xFF << clockwiseRotation);
            Walls ret = (walls & Walls.C) | (Walls)((rotatedWalls | (rotatedWalls >> 8)) & 0xFF);
            if (ret >= Walls.All) return walls;

            return ret;
        }

        public override string ToString()
        {
            return $"{GameDatabase.GetPrototypeName(PrototypeId)}, cellid={Id}, cellpos={RegionBounds.Center}, game={Game}";
        }

        public string PrototypeName => $"{GameDatabase.GetFormattedPrototypeName(PrototypeId)}";

        public void Shutdown()
        {
            Region region = GetRegion();
            if (region != null && SpatialPartitionLocation.IsValid())
                region.PartitionCell(this, Region.PartitionContext.Remove);
        }

        public Region GetRegion()
        {
            if (Area == null) return null;
            return Area.Region;
        }

        public bool IntersectsXY(Vector3 position)
        {
            return RegionBounds.IntersectsXY(position);
        }

        public Vector3 CalcMarkerPosition(Vector3 markerPos)
        {
            return RegionBounds.Center + markerPos - CellProto.BoundingBox.Center;
        }

        public void PostGenerate()
        {
            // SpawnMarker Prop type
            VisitPropSpawns(new InstanceMarkerSetPropSpawnVisitor(this)); 

            // SpawnMarkers not Prop type
            var population = GetRegion().PopulationManager.PopulationMarkers;
            foreach (var markerProto in CellProto.MarkerSet.Markers)
            {
                if (markerProto is EntityMarkerPrototype entityMarker)
                {
                    PrototypeId dataRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
                    Prototype entity = GameDatabase.GetPrototype<Prototype>(dataRef);

                    // Spawn Entity from Missions
                    if (entity is SpawnMarkerPrototype spawnMarker && spawnMarker.Type != MarkerType.Prop)
                        foreach (var spawn in population)
                            if (spawn.MarkerRef == spawnMarker.DataRef) spawn.Spawn(this);
                }
            }
        }

        private void VisitPropSpawns(PropSpawnVisitor visitor)
        {
            PropTable propTable = Area.PropTable;
            if (propTable != null)
            {
                int randomSeed = Seed;
                GRandom random = new(randomSeed);
                int randomNext = 0;

                CellPrototype cellProto = CellProto;
                if (cellProto == null) return;

                foreach (var marker in cellProto.MarkerSet.Markers)
                {
                    if (marker is not EntityMarkerPrototype entityMarker) continue;
                    PrototypeId propMarkerRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);

                    var propMarkerProto = entityMarker.GetMarkedPrototype<PropMarkerPrototype>();
                    if (propMarkerProto != null && propMarkerRef != PrototypeId.Invalid && propMarkerProto.Type == MarkerType.Prop)
                    {
                        PrototypeId propDensityRef = Area.AreaPrototype.PropDensity;
                        if (propDensityRef != PrototypeId.Invalid)
                        {
                            var propDensityProto = GameDatabase.GetPrototype<PropDensityPrototype>(propDensityRef);
                            if (propDensityProto != null && random.Next(0, 101) > propDensityProto.GetPropDensity(propMarkerRef)) continue;
                        }
                        if (propTable.GetRandomPropMarkerOfType(random, propMarkerRef, out var propGroup))
                            visitor.Visit(randomSeed + randomNext++, propTable, propGroup.PropSetRef, propGroup.PropGroup, entityMarker);
                    }
                }
            }
        }

        public void AddEncounter(AssetId asset, uint id, bool useMarkerOrientation) => Encounters.Add(new(asset, id, useMarkerOrientation));

        public void VisitEncounters(EncounterVisitor visitor)
        {
            // TODO: solve problem with enctounters

            foreach (var encounter in Encounters)
            {
                var encounterResourceRef = GameDatabase.GetDataRefByAsset(encounter.Asset);
                if (encounterResourceRef == PrototypeId.Invalid) continue;
                var encounterProto = GameDatabase.GetPrototype<EncounterResourcePrototype>(encounterResourceRef);
                if (encounterProto == null) continue;

                SpawnReservation reservation = null; //GetRegion().SpawnMarkerRegistry.ReserveSpawnTypeLocationById(Id, encounter.Id, encounterResourceRef, null);
                PopulationEncounterPrototype popProto = null;
                PrototypeId missionRef = PrototypeId.Invalid;
                visitor.Visit(encounterResourceRef, reservation, popProto, missionRef, encounter.UseMarkerOrientation);
            }
        }

        public IMessage MessageCellCreate()
        {
            var builder = NetMessageCellCreate.CreateBuilder()
                .SetAreaId(Area.Id)
                .SetCellId(Id)
                .SetCellPrototypeId((ulong)PrototypeId)
                .SetPositionInArea(AreaPosition.ToNetStructPoint3())
                .SetCellRandomSeed(Area.RandomSeed)
                .SetBufferwidth(0)
                .SetOverrideLocationName(0);

            foreach (ReservedSpawn reservedSpawn in Encounters)
                builder.AddEncounters(reservedSpawn.ToNetStruct());
            return builder.Build();
        }

        public bool FindTargetPosition(Vector3 markerPos, Orientation markerRot, RegionConnectionTargetPrototype target)
        {
            if (CellProto != null && CellProto.InitializeSet.Markers.HasValue())
            {
                foreach (var marker in CellProto.InitializeSet.Markers)
                {
                    if (marker is EntityMarkerPrototype entityMarker)
                    {
                        PrototypeId dataRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
                        if (dataRef == target.Entity)
                        {
                            markerPos.Set(CalcMarkerPosition(marker.Position));
                            markerRot.Set(entityMarker.Rotation);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #region Enums

        [AssetEnum((int)None)]      // DRAG/RegionGenerators/Edges.type
        [Flags]
        public enum Type
        {
            None = 0,        // 0000
            N = 1,           // 0001
            E = 2,           // 0010
            S = 4,           // 0100
            W = 8,           // 1000
            NS = 5,          // 0101
            EW = 10,         // 1010
            NE = 3,          // 0011
            NW = 9,          // 1001
            ES = 6,          // 0110
            SW = 12,         // 1100
            ESW = 14,        // 1110
            NSW = 13,        // 1101
            NEW = 11,        // 1011
            NES = 7,         // 0111
            NESW = 15,       // 1111
            // Dot
            dN = 128,        // 1000 0000
            dE = 64,         // 0100 0000
            dS = 32,         // 0010 0000
            dW = 16,         // 0001 0000
            dNE = 192,       // 1100 0000
            dSE = 96,        // 0110 0000
            dSW = 48,        // 0011 0000
            dNW = 144,       // 1001 0000
            NESWdNW = 159,   // 1001 1111
            NESWdNE = 207,   // 1100 1111
            NESWdSW = 63,    // 0011 1111
            NESWdSE = 111,   // 0110 1111
            dNESW = 240,     // 1111 0000
            DotMask = 480,   // 1 1110 0000 !!! Error Mask
            // c
            NESWcN = 351,    // 1 0101 1111
            NESWcE = 303,    // 1 0010 1111
            NESWcS = 159,    // 0 0111 1111
            NESWcW = 207,    // 0 1100 1111
        }

        [Flags]
        public enum Walls
        {
            None = 0,   // 000000000
            N = 1,      // 000000001
            NE = 2,     // 000000010
            E = 4,      // 000000100
            SE = 8,     // 000001000
            S = 16,     // 000010000
            SW = 32,    // 000100000
            W = 64,     // 001000000
            NW = 128,   // 010000000
            C = 256,    // 100000000
            All = 511,  // 111111111
        }

        [AssetEnum((int)WideNESW)]      // DRAG/CellWallTypes.type
        [Flags]
        public enum WallGroup
        {
            N = 254,
            E = 251,
            S = 239,
            W = 191,
            NE = 250,
            ES = 235,
            SW = 175,
            NW = 190,
            NS = 238,
            EW = 187,
            NES = 234,
            ESW = 171,
            NSW = 174,
            NEW = 186,
            NESW = 170,
            WideNE = 248,
            WideES = 227,
            WideSW = 143,
            WideNW = 62,
            WideNES = 224,
            WideESW = 131,
            WideNSW = 14,
            WideNEW = 56,
            WideNESW = 0,
            WideNESWcN = 130,
            WideNESWcE = 10,
            WideNESWcS = 40,
            WideNESWcW = 160,
        }

        [Flags]
        public enum Filler
        {
            N = 1,
            NE = 2,
            E = 4,
            SE = 8,
            S = 16,
            SW = 32,
            W = 64,
            NW = 128,
            C = 256,
            None = 0,
        }

        #endregion
    }
}
