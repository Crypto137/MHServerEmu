using MHServerEmu.Core.Collections;

namespace MHServerEmu.Games.Navi
{
    public class NaviFunnel : FixedDeque<FunnelVertex>
    {
        private readonly NaviPoint _pathStart;
        private FunnelVertex _apex;

        public NaviFunnel(NaviPoint point): base(256) 
        {
            PushFront(new(point, NaviSide.Point));
            _apex = LeftV();
            _pathStart = point;
        }

        private FunnelVertex LeftV() => Front;
        private FunnelVertex RightV() => Back;

        public bool IsEmpty { get => Empty; }
        public NaviPoint Left { get => Front.Point; }
        public NaviPoint Right { get => Back.Point; }
        public NaviPoint LeftPrev { get => this[1].Point; }
        public NaviPoint RightPrev { get => this[Size - 2].Point; }
        public NaviPoint Apex { get => _apex.Point; }
        public NaviSide ApexSide { get => _apex.VertexSide; }
        public bool IsApexPathStart { get => Apex == _pathStart; }

        public void PopLeft() => PopFront();
        public void PopRight() => PopBack();
        public void AddVertexLeft(NaviPoint point, NaviSide vertexSide) => PushFront(new (point, vertexSide));
        public void AddVertexRight(NaviPoint point, NaviSide vertexSide) => PushBack(new (point, vertexSide));

        public void AddVertex(NaviSide funnelSide, NaviPoint point, NaviSide vertexSide)
        {
            if (funnelSide == NaviSide.Left) AddVertexLeft(point, vertexSide);
            else AddVertexRight(point, vertexSide);
        }

        public void AddApex(NaviSide funnelSide, NaviPoint point, NaviSide vertexSide)
        {
            if (funnelSide == NaviSide.Left) AddApexLeft(point, vertexSide);
            else AddApexRight(point, vertexSide);
        }

        public void AddApexLeft(NaviPoint point, NaviSide vertexSide)
        {
            PopLeft();
            AddVertexLeft(point, vertexSide);
            _apex = LeftV();
        }

        public void AddApexRight(NaviPoint point, NaviSide vertexSide)
        {
            PopRight();
            AddVertexRight(point, vertexSide);
            _apex = RightV();
        }

        public NaviPoint Vertex(NaviSide funnelSide) => (funnelSide == NaviSide.Left) ? Left : Right;
        public NaviPoint VertexPrev(NaviSide funnelSide) => (funnelSide == NaviSide.Left) ? LeftPrev : RightPrev;

        public void PopVertex(NaviSide funnelSide)
        {
            if (funnelSide == NaviSide.Left) PopLeft();
            else PopRight();
        }

        public void PopApex(NaviSide funnelSide)
        {
            if (funnelSide == NaviSide.Left) PopApexLeft();
            else PopApexRight();
        }

        public void PopApexLeft()
        {
            PopLeft();
            _apex = IsEmpty ? new(null, NaviSide.Left) : LeftV();
        }

        public void PopApexRight()
        {
            PopRight();
            _apex = IsEmpty ? new(null, NaviSide.Right) : RightV();
        }
    }

    public struct FunnelVertex
    {
        public NaviPoint Point;
        public NaviSide VertexSide;

        public FunnelVertex(NaviPoint point, NaviSide vertexSide)
        {
            Point = point;
            VertexSide = vertexSide;
        }
    }
}
