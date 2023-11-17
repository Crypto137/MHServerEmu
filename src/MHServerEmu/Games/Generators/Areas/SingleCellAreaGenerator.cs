using MHServerEmu.Common;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Generators.Prototypes;
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

            if (Area.AddCell(AllocateCellId(), cellSettings) == null) return false;

            return true;
        }
        
        public override bool GetPossibleConnections(List<Vector3> connections, Segment segment)
        {
            bool connected = false;
            connections.Clear();

            CellPrototype cellProto = GetCellPrototype();
            if (cellProto == null) return false;

            Vector3 origin = Area.Origin;
            Vector3 cellPos = new();

            if (cellProto.MarkerSet != null)
            {      
                foreach (var marker in cellProto.MarkerSet)
                {
                    if (marker is not CellConnectorMarkerPrototype cellConnector) continue;

                    Vector3 connection = origin + cellPos + cellConnector.Position;

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
            Aabb bounds = new(Aabb.InvertedLimit);

            CellPrototype cellProto = GetCellPrototype(); 

            if (cellProto != null)
            {
                bounds = new(cellProto.Boundbox);
                PreGenerated = true;
            }

            return bounds;
        }

    }
}
