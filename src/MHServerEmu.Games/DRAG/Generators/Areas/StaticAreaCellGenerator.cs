using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.DRAG.Generators.Regions;

namespace MHServerEmu.Games.DRAG.Generators.Areas
{
    public class StaticAreaCellGenerator : Generator
    {
        public StaticAreaCellGenerator() { }

        public override Aabb PreGenerate(GRandom random)
        {
            Aabb bounds = Aabb.InvertedLimit;
            DistrictPrototype protoDistrict = GetDistrictPrototype();
            if (protoDistrict == null || protoDistrict.CellMarkerSet.Markers.IsNullOrEmpty()) return bounds;

            foreach (var cellMarker in protoDistrict.CellMarkerSet.Markers)
            {
                if (cellMarker is not ResourceMarkerPrototype resourceMarker) continue;
                PrototypeId cellRef = GameDatabase.GetPrototypeRefByName(resourceMarker.Resource);
                if (cellRef == 0)
                {
                    if (Log) Logger.Warn($"Unable to link Resource {resourceMarker.Resource} to a corresponding .cell file");
                    continue;
                }

                CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
                if (cellProto == null) continue;
                bounds += cellProto.BoundingBox + resourceMarker.Position;

            }

            PreGenerated = true;
            return bounds;
        }

        public override bool Generate(GRandom random, RegionGenerator regionGenerator, List<PrototypeId> areas)
        {

            DistrictPrototype protoDistrict = GetDistrictPrototype();
            if (protoDistrict == null) return false;

            foreach (var cellMarker in protoDistrict.CellMarkerSet.Markers)
            {
                if (cellMarker is not ResourceMarkerPrototype resourceMarker) continue;

                PrototypeId cellRef = GameDatabase.GetPrototypeRefByName(resourceMarker.Resource); // GetDataRefByResourceGuid 
                if (cellRef == 0) continue;

                CellSettings cellSettings = new()
                {
                    PositionInArea = resourceMarker.Position,
                    OrientationInArea = resourceMarker.Rotation,
                    CellRef = cellRef
                };

                Area.AddCell(AllocateCellId(), cellSettings);
            }

            Area area = Area;
            foreach (var cell in area.CellIterator())
            {
                if (cell != null)
                {
                    Vector3 origin = cell.RegionBounds.Center;
                    float x = cell.RegionBounds.Width;
                    float y = cell.RegionBounds.Length;

                    void TryCreateConnection(Vector3 direction)
                    {
                        Vector3 position = origin + direction;
                        if (area.IntersectsXY(position))
                        {
                            Cell otherCell = area.GetCellAtPosition(position);
                            if (otherCell != null) area.CreateCellConnection(cell, otherCell);
                        }
                    }

                    TryCreateConnection(new(x, 0.0f, 0.0f));
                    TryCreateConnection(new(-x, 0.0f, 0.0f));
                    TryCreateConnection(new(0.0f, y, 0.0f));
                    TryCreateConnection(new(0.0f, -y, 0.0f));
                }
            }

            return true;
        }

        public DistrictPrototype GetDistrictPrototype()
        {
            Area area = Area;
            if (area == null)
            {
                if (Log) Logger.Warn("Unable to get SArea");
                return null;
            }

            if (Area.AreaPrototype.Generator is not DistrictAreaGeneratorPrototype proto) return null;

            AssetId districtAssetRef = proto.District;
            if (districtAssetRef == 0)
            {
                if (Log) Logger.Warn("StaticAreaCellGenerator called with no layout specified.");
                return null;
            }

            PrototypeId districtRef = GameDatabase.GetDataRefByAsset(districtAssetRef);
            DistrictPrototype protoDistrict = GameDatabase.GetPrototype<DistrictPrototype>(districtRef);
            if (protoDistrict == null)
                if (Log) Logger.Warn($"District Prototype is not available. Likely a missing file. Looking for Asset: {GameDatabase.GetAssetName(districtAssetRef)}");

            area.DistrictDataRef = districtRef;
            return protoDistrict;
        }

        public override bool GetPossibleConnections(ConnectionList connections, in Segment segment)
        {
            connections.Clear();

            bool connected = false;
            Vector3 origin = Area.Origin;

            DistrictPrototype protoDistrict = GetDistrictPrototype();
            if (protoDistrict == null)
            {
                if (Log) Logger.Warn($"StaticArea's District is Invalid");
                return false;
            }

            if (protoDistrict.CellMarkerSet.Markers.IsNullOrEmpty())
            {
                if (Log) Logger.Warn($"StaticArea's District contains no cells");
                return false;
            }

            foreach (var cellMarker in protoDistrict.CellMarkerSet.Markers)
            {
                if (cellMarker is not ResourceMarkerPrototype resourceMarker) continue;

                PrototypeId cellRef = GameDatabase.GetPrototypeRefByName(resourceMarker.Resource);
                if (cellRef == 0) continue;

                Vector3 offset = resourceMarker.Position;
                CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
                if (cellProto == null) continue;

                if (cellProto.MarkerSet.Markers.HasValue())
                {
                    foreach (var marker in cellProto.MarkerSet.Markers)
                    {
                        if (marker is not CellConnectorMarkerPrototype cellConnector) continue;

                        Vector3 connection = origin + offset + cellConnector.Position;

                        if (segment.Start.X == segment.End.X)
                        {
                            if (Segment.EpsilonTest(connection.X, segment.Start.X, 10.0f) &&
                               connection.Y >= segment.Start.Y && connection.Y <= segment.End.Y)
                            {
                                connections.Add(connection);
                                connected = true;
                            }
                        }
                        else if (segment.Start.Y == segment.End.Y)
                        {
                            if (Segment.EpsilonTest(connection.Y, segment.Start.Y, 10.0f) &&
                                connection.X >= segment.Start.X && connection.X <= segment.End.X)
                            {
                                connections.Add(connection);
                                connected = true;
                            }
                        }
                    }
                }
            }
            return connected;
        }

    }
}
