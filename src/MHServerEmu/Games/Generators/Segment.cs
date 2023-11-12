using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Generators
{
    public class Segment
    {
        public static readonly Segment Zero = new(Vector3.Zero, Vector3.Zero);

        public Vector3 Start { get; set; }
        public Vector3 End { get; set; }

        public Segment()
        {
            Start = Vector3.Zero;
            End = Vector3.Zero;
        }

        public Segment(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
        }

        public Vector3 GetDirection()
        {
            return End - Start;
        }

        public float Length()
        {
            return Vector3.Length(GetDirection());
        }

    }

}
