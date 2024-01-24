using MHServerEmu.Common;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Areas
{
    public class SingleCellAreaGenerator : Generator
    {
        public SingleCellAreaGenerator(){}

        public override bool Generate(GRandom random, RegionGenerator regionGenerator, List<ulong> areas)
        {
            if (Area.AreaPrototype.Generator is not SingleCellAreaGeneratorPrototype proto) return false;

            ulong cellAssetRef = proto.Cell;
            if (cellAssetRef == 0) return false;

            ulong cellRef = GameDatabase.GetDataRefByAsset(cellAssetRef);
            if (cellRef == 0) return false;

            CellSettings cellSettings = new ()
            {
                CellRef = cellRef,
                Seed = Area.RandomSeed
            };

            return Area.AddCell(AllocateCellId(), cellSettings) != null;
        }
        
        public override bool GetPossibleConnections(ConnectionList connections, Segment segment)
        {
            bool connected = false;
            connections.Clear();

            CellPrototype cellProto = GetCellPrototype();
            if (cellProto == null) return false;

            Vector3 origin = Area.Origin;
            Vector3 offset = Vector3.Zero;

            if (cellProto.MarkerSet.Markers.IsNullOrEmpty() == false)
            {      
                foreach (var marker in cellProto.MarkerSet.Markers)
                {
                    if (marker is not CellConnectorMarkerPrototype cellConnector) continue;

                    Vector3 connection = origin + offset + cellConnector.Position;

                    if (segment.Start.X == segment.End.X)
                    {
                        if (Segment.EpsilonTest(connection.X, segment.Start.X, 10.0f) &&
                           (connection.Y >= segment.Start.Y) && (connection.Y <= segment.End.Y))
                        {
                            connections.Add(connection);
                            connected = true;
                        }
                    }
                    else if (segment.Start.Y == segment.End.Y)
                    {
                        if (Segment.EpsilonTest(connection.Y, segment.Start.Y, 10.0f) &&
                            (connection.X >= segment.Start.X) && (connection.X <= segment.End.X))
                        {
                            connections.Add(connection);
                            connected = true;
                        }
                    }
                }
            }
            return connected;
        }

        public ulong GetCellPrototypeDataRef()
        {
            if (Area.AreaPrototype.Generator is not SingleCellAreaGeneratorPrototype generatorProto) return 0;

            ulong cellAssetRef = generatorProto.Cell;
            if (cellAssetRef == 0) return 0;

            ulong cellRef = GameDatabase.GetDataRefByAsset(cellAssetRef);
            if (cellRef == 0) return 0;

            return cellRef;
        }

        public CellPrototype GetCellPrototype()
        {
            ulong cellRef = GetCellPrototypeDataRef();
            if (cellRef == 0) return null;

            CellPrototype proto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
            if (proto == null) return null;

            return proto;
        }

        public override Aabb PreGenerate(GRandom random)
        {
            Aabb bounds = Aabb.InvertedLimit;

            CellPrototype cellProto = GetCellPrototype(); 

            if (cellProto != null)
            {
                bounds.Set(cellProto.BoundingBox);
                PreGenerated = true;
            }

            return bounds;
        }

        public static bool CellGridBorderBehavior(Area area)
        {
            if (area == null) return false;

            GeneratorPrototype generatorProto = area.AreaPrototype.Generator;
            var singleCellGeneratorProto = generatorProto as SingleCellAreaGeneratorPrototype;

            if (singleCellGeneratorProto != null && singleCellGeneratorProto.BorderCellSets.IsNullOrEmpty() == false && singleCellGeneratorProto.Cell != 0)
            {
                ulong assetRef = singleCellGeneratorProto.Cell;
                ulong cellRef = GameDatabase.GetDataRefByAsset(assetRef);
                CellPrototype cellP = GameDatabase.GetPrototype<CellPrototype>(cellRef);

                if (cellP == null) return false;

                CellSetRegistry registry = new ();
                registry.Initialize(true);
                foreach (var cellSetEntry in singleCellGeneratorProto.BorderCellSets)
                {
                    if (cellSetEntry == null) continue;
                    registry.LoadDirectory(cellSetEntry.CellSet, cellSetEntry, cellSetEntry.Weight, cellSetEntry.Unique);
                }

                return DoBorderBehavior(area, singleCellGeneratorProto.BorderWidth, registry, cellP.BoundingBox.Width, 1, 1);
            }

            return false;
        }

    }
}
