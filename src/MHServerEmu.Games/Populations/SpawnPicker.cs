using MHServerEmu.Core.Collections;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Populations
{
    public class SpawnPicker
    {
        public Picker<PopulationObjectPrototype> Picker;
        public int Count;

        public SpawnPicker(Picker<PopulationObjectPrototype> picker, int count)
        {
            Picker = picker;
            Count = count;
        }
    }

    public class SpawnMapPicker
    {
        public GRandom Random { get; }
        public SpawnMap SpawnMap { get; }
        public int Count { get; private set; }

        private int[] _pickMap;

        public SpawnMapPicker(GRandom random, SpawnMap spawnMap)
        {
            Random = random;
            SpawnMap = spawnMap;
            Count = 0;
        }

        public bool Pick(out int index)
        {
            index = -1;
            if (Count == 0) return false;

            int pickIndex = Random.Next(Count);
            index = _pickMap[pickIndex];
            _pickMap[pickIndex] = _pickMap[--Count];

            return true;
        }

        public bool AddHorizon(Point2 coord, int horizon, bool full)
        {
            Count = 0;
            _pickMap = new int[horizon * 2 * 5]; // 2 side * 4 dir + reserve 
            _pickMap.AsSpan().Fill(-1);

            Point2 min = new(coord.X - horizon, coord.Y - horizon);
            Point2 max = new(coord.X + horizon, coord.Y + horizon);

            for (int i = 0; i < horizon * 2; i++)
            {
                TryAddCoord(min.X + i, min.Y, full); // SW N
                TryAddCoord(max.X, min.Y + i, full); // NW E
                TryAddCoord(max.X - i, max.Y, full); // NE S
                TryAddCoord(min.X, max.Y - i, full); // SE W
            }

            return Count > 0;
        }

        private readonly int[,] _offsets = {
            { 1, 0 },  // N
            { 1, 1 },  // NE
            { 0, 1 },  // E
            { -1, 1 }, // SE
            { -1, 0 }, // S
            { -1, -1 },// SW
            { 0, -1 }, // W
            { 1, -1 }  // NW
        };

        public enum Dir
        {
            N, NE, E, ES, S, SW, W, NW
        }

        private readonly (Dir, Dir)[] _dirTable = {
            (Dir.E, Dir.W),   // N
            (Dir.S, Dir.W),   // NE
            (Dir.S, Dir.N),   // E
            (Dir.W, Dir.N),   // SE
            (Dir.W, Dir.E),   // S
            (Dir.N, Dir.E),   // SW
            (Dir.N, Dir.S),   // W
            (Dir.E, Dir.S)    // NW
        };

        public bool AddSpread(Point2 start, Point2 coord, int spread, int distance, bool full)
        {
            Count = 0;
            _pickMap = new int[spread * 2 + 1]; // 2 side + center
            _pickMap.AsSpan().Fill(-1);

            int dir = (int)DirectionSpread(start, coord);
            (Dir minDir, Dir maxDir) = _dirTable[dir];

            Point2 center = new(_offsets[dir, 0], _offsets[dir, 1]);
            Point2 min = new(_offsets[(int)minDir, 0], _offsets[(int)minDir, 1]);
            Point2 max = new(_offsets[(int)maxDir, 0], _offsets[(int)maxDir, 1]);

            center = new(coord.X + center.X * distance, coord.Y + center.Y * distance);
            TryAddCoord(center.X, center.Y, full);
            for (int i = 1; i <= spread; i++)
            {
                TryAddCoord(center.X + i * min.X, center.Y + i * min.Y, full);
                TryAddCoord(center.X + i * max.X, center.Y + i * max.Y, full);
            }

            return Count > 0;
        }

        private static Dir DirectionSpread(Point2 start, Point2 end)
        {
            var dir = new Point2(end.X - start.X, end.Y - start.Y);

            int absX = Math.Abs(dir.X);
            int absY = Math.Abs(dir.Y);

            if (absX == absY)
            {
                if (dir.X > 0 && dir.Y > 0) return Dir.NE;
                if (dir.X > 0 && dir.Y < 0) return Dir.NW;
                if (dir.X < 0 && dir.Y < 0) return Dir.SW;
                if (dir.X < 0 && dir.Y > 0) return Dir.ES;
            } 

            if (absX > absY) 
                return dir.X > 0 ? Dir.N : Dir.S;
            else 
                return dir.Y > 0 ? Dir.E : Dir.W;
        }

        private void TryAddCoord(int x, int y, bool full)
        {
            if (SpawnMap.GetHeatData(x, y, out int index, out HeatData heatData))
            {
                if ((heatData & HeatData.FlagMask) != 0) return;
                if (full && (heatData & HeatData.ValueMask) == HeatData.ValueMask) return;
                _pickMap[Count++] = index;
            }
        }
    }
}
