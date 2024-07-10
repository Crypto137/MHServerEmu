using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.DRAG.Generators.Regions;

namespace MHServerEmu.Games.DRAG.Generators.Areas
{
    public class CanyonGridAreaGenerator : Generator
    {
        private int _bridgeIndex;

        public CanyonGridAreaGenerator()
        {
            _bridgeIndex = -1;
        }

        public override Aabb PreGenerate(GRandom random)
        {
            if (Area.AreaPrototype.Generator is not CanyonGridAreaGeneratorPrototype proto) return Aabb.InvertedLimit;

            CanyonCellChoiceListPrototype cells = proto.Cells;
            if (cells == null) return Aabb.InvertedLimit;

            CellPrototype bridgeProto = GetFirstCellChoiceFromPrototypePtrList(cells.BridgeChoices);
            if (bridgeProto == null || proto.Length - 2 < 1) return Aabb.InvertedLimit;

            _bridgeIndex = random.Next(1, proto.Length - 1);

            float сellsX = 0;
            float сellsY = 0;

            if (cells.Orientation == AreaOrientation.Vertical)
            {
                сellsX = proto.Length;
                сellsY = 1.0f;
            }
            else if (cells.Orientation == AreaOrientation.Horizontal)
            {
                сellsX = 1.0f;
                сellsY = proto.Length;
            }

            float width = bridgeProto.BoundingBox.Width;
            float length = bridgeProto.BoundingBox.Length;
            float halfWidth = width / 2.0f;
            float halfLength = length / 2.0f;
            float halfHeight = bridgeProto.BoundingBox.Height / 2.0f;

            Vector3 min = new(-halfWidth, -halfLength, -halfHeight);
            Vector3 max = new(сellsX * width - halfWidth, сellsY * length - halfLength, halfHeight);

            PreGenerated = true;

            return new(min, max);
        }

        private static CellPrototype GetFirstCellChoiceFromPrototypePtrList(CellChoicePrototype[] bridgeChoices)
        {
            if (bridgeChoices.HasValue())
            {
                CellChoicePrototype firstChoice = bridgeChoices[0];
                PrototypeId cellRef = GameDatabase.GetDataRefByAsset(firstChoice.Cell);
                CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
                if (cellProto != null) return cellProto;
            }
            return null;
        }

        public override bool Generate(GRandom random, RegionGenerator regionGenerator, List<PrototypeId> areas)
        {
            if (Area.AreaPrototype.Generator is not CanyonGridAreaGeneratorPrototype proto) return false;

            CanyonCellChoiceListPrototype cells = proto.Cells;
            if (cells == null) return false;

            Area area = Area;
            if (area == null) return false;

            CellPrototype bridgeProto = GetFirstCellChoiceFromPrototypePtrList(proto.Cells.BridgeChoices);
            if (bridgeProto == null) return false;

            float width = bridgeProto.BoundingBox.Width;
            float length = bridgeProto.BoundingBox.Length;

            for (int i = 0; i < proto.Length; ++i)
            {
                Vector3 position = new();
                if (proto.Cells.Orientation == AreaOrientation.Vertical)
                {
                    position = new(width * i, 0.0f, 0.0f);
                }
                else if (proto.Cells.Orientation == AreaOrientation.Horizontal)
                {
                    position = new(0.0f, length * i, 0.0f);
                }

                CellSettings settings = new()
                {
                    PositionInArea = position
                };

                if (i == _bridgeIndex)
                    settings.CellRef = PickCellChoiceFromPrototypePtrList(random, proto.Cells.BridgeChoices);
                else if (i == 0)
                    settings.CellRef = PickCellChoiceFromPrototypePtrList(random, proto.Cells.LeftOrBottomChoices);
                else if (i == proto.Length - 1)
                    settings.CellRef = PickCellChoiceFromPrototypePtrList(random, proto.Cells.RightOrTopChoices);
                else
                    settings.CellRef = PickCellChoiceFromPrototypePtrList(random, proto.Cells.NormalChoices);


                Cell cell = area.AddCell(AllocateCellId(), settings);
                if (cell == null) return false;
            }

            return true;
        }

        private static PrototypeId PickCellChoiceFromPrototypePtrList(GRandom random, CellChoicePrototype[] cellChoices)
        {
            if (cellChoices.IsNullOrEmpty()) return 0;
            Picker<AssetId> picker = new(random);

            foreach (CellChoicePrototype choiceProto in cellChoices)
            {
                if (choiceProto != null && choiceProto.Weight > 0)
                    picker.Add(choiceProto.Cell, choiceProto.Weight);
            }

            if (!picker.Empty() && picker.Pick(out AssetId assetRef))
            {
                PrototypeId cellRef = GameDatabase.GetDataRefByAsset(assetRef);
                CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);

                if (cellProto != null) return cellRef;
            }
            return 0;
        }

        public override bool GetPossibleConnections(ConnectionList connections, in Segment segment)
        {
            connections.Clear();

            Area area = Area;
            if (area == null) return false;

            if (Area.AreaPrototype.Generator is not CanyonGridAreaGeneratorPrototype proto) return false;
            if (proto.Cells == null) return false;

            int sizeX;
            int sizeY;

            Cell.Type direction = Cell.Type.None;
            Vector3 origin = area.Origin;

            if (proto.Cells.Orientation == AreaOrientation.Vertical)
            {
                sizeX = proto.Length;
                sizeY = 1;
                if (segment.End.X == segment.Start.X) return false;
                if (segment.End.Y > origin.Y) direction = Cell.Type.E;
                if (segment.End.Y < origin.Y) direction = Cell.Type.W;
            }
            else if (proto.Cells.Orientation == AreaOrientation.Horizontal)
            {
                sizeX = 1;
                sizeY = proto.Length;
                if (segment.End.Y == segment.Start.Y) return false;
                if (segment.End.X > origin.X) direction = Cell.Type.N;
                if (segment.End.X < origin.X) direction = Cell.Type.S;
            }
            else
            {
                return false;
            }

            CellPrototype bridgeProto = GetFirstCellChoiceFromPrototypePtrList(proto.Cells.BridgeChoices);
            if (bridgeProto == null) return false;

            float width = bridgeProto.BoundingBox.Width;
            float length = bridgeProto.BoundingBox.Length;

            bool found = false;

            bool westBridgeOnly = proto.ConnectOnBridgeOnlyDirection.HasFlag(RegionDirection.West) && direction == Cell.Type.W;
            bool eastBridgeOnly = proto.ConnectOnBridgeOnlyDirection.HasFlag(RegionDirection.East) && direction == Cell.Type.E;

            if ((westBridgeOnly || eastBridgeOnly) && proto.Cells.Orientation == AreaOrientation.Vertical)
            {
                Vector3 offset = origin + new Vector3(_bridgeIndex * width, 0.0f, 0.0f);
                if (GetConnectionPointOnSegment(out Vector3 connectionPoint, bridgeProto, segment, offset))
                {
                    connections.Add(connectionPoint);
                    return true;
                }
            }

            bool northBridgeOnly = proto.ConnectOnBridgeOnlyDirection.HasFlag(RegionDirection.North) && direction == Cell.Type.N;
            bool southBridgeOnly = proto.ConnectOnBridgeOnlyDirection.HasFlag(RegionDirection.South) && direction == Cell.Type.S;

            if ((northBridgeOnly || southBridgeOnly) && proto.Cells.Orientation == AreaOrientation.Horizontal)
            {
                Vector3 offset = origin + new Vector3(0.0f, _bridgeIndex * length, 0.0f);
                if (GetConnectionPointOnSegment(out Vector3 connectionPoint, bridgeProto, segment, offset))
                {
                    connections.Add(connectionPoint);
                    return true;
                }
            }

            for (int y = 0; y < sizeY; ++y)
            {
                for (int x = 0; x < sizeX; ++x)
                {
                    Vector3 offset = origin + new Vector3(x * width, y * length, 0.0f);
                    if (GetConnectionPointOnSegment(out Vector3 connectionPoint, bridgeProto, segment, offset))
                    {
                        connections.Add(connectionPoint);
                        found = true;
                    }
                }
            }

            return found;
        }

    }

}
