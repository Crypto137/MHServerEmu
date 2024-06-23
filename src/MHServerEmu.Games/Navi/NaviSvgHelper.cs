using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using System.Text;

namespace MHServerEmu.Games.Navi
{
    public class NaviSvgHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private readonly List<string> _elements;
        private readonly int _width;
        private readonly int _height;
        private readonly float _scaleX;
        private readonly float _scaleY;
        private readonly Vector2 _offset;
        private readonly NaviCdt _cdt;
        private Stack<NaviTriangle> _triangles;

        public NaviSvgHelper(NaviCdt naviCdt, int width = 1000, int height = 1000)
        {
            _cdt = naviCdt;
            _width = width;
            _height = height;
            _scaleX = width / naviCdt.Bounds.Width;
            _scaleY = height / naviCdt.Bounds.Height;
            _offset = CalculateOffset();
            _elements = new ();
            _triangles = new ();
            InitializeSvg();            
        }

        private void InitializeSvg()
        {
            _elements.Add($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{_width}\" height=\"{_height}\" viewBox=\"0 0 {_width} {_height}\">");
        }

        private Vector2 CalculateOffset()
        {
            var min = _cdt.Bounds.Min;
            return new Vector2(min.X, min.Y);
        }

        private Vector2 Normalize(Vector3 point)
        {
            return new Vector2(point.X - _offset.X, point.Y - _offset.Y);
        }

        private Vector2 Scale(Vector3 point)
        {
            Vector2 normalizedPoint = Normalize(point);
            return new Vector2(normalizedPoint.X * _scaleX, normalizedPoint.Y * _scaleY);
        }

        public uint RandomHash()
        {
            Random random = new();
            byte[] buffer = new byte[4];
            random.NextBytes(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public string GetColorFromHash(uint hash = 0)
        {
            if (hash == 0) hash = RandomHash();
            uint r = (hash & 0xFF) % 200;
            uint g = ((hash >> 8) & 0xFF) % 200;
            uint b = ((hash >> 16) & 0xFF) % 200;

            return $"#{r:X2}{g:X2}{b:X2}";
        }

        public void AddTriangle(NaviTriangle triangle)
        {
            if (_triangles.Contains(triangle)) return; 
            _triangles.Push(triangle);

            var p1 = Scale(triangle.PointCW(0).Pos);
            var p2 = Scale(triangle.PointCW(1).Pos);
            var p3 = Scale(triangle.PointCW(2).Pos);

            string color = "#834C4C";
            if (triangle.PathingFlags.HasFlag(PathFlags.Walk)) color = "#238D43";

            _elements.Add($"<polygon points=\"{p1.X},{p1.Y} {p2.X},{p2.Y} {p3.X},{p3.Y}\" style=\"fill:{color};stroke:black;stroke-width:1\" />");
        }

        public void AddPath(List<NaviPathNode> path)
        {
            if (path.Count < 2) return;
            string color = GetColorFromHash();
            var pathData = new StringBuilder();
            var firstPoint = Scale(path[0].Vertex);
            pathData.Append($"M {firstPoint.X},{firstPoint.Y} ");

            for (int i = 1; i < path.Count; i++)
            {
                var point = Scale(path[i].Vertex);
                pathData.Append($"L {point.X},{point.Y} ");
            }

            _elements.Add($"<path d=\"{pathData}\" style=\"fill:none;stroke:{color};stroke-width:2\" />");
        }

        public void AddCircle(Vector3 centerIn, float radius, string color = "blue")
        {
            var center = Scale(centerIn);
            if (radius == 0) radius = 10;
            float scaledRadius = radius * Math.Min(_scaleX, _scaleY);
            _elements.Add($"<circle cx=\"{center.X}\" cy=\"{center.Y}\" r=\"{scaledRadius}\" style=\"fill:none;stroke:{color};stroke-width:1\" />");
        }

        public void SaveToFile(string filePath)
        {
            _elements.Add("</svg>");
            File.WriteAllLines(filePath, _elements);
            Logger.Debug($"{filePath} saved");
        }
    }
}
