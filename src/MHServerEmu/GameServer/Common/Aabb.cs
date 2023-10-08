namespace MHServerEmu.GameServer.Common
{
    public class Aabb
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public float Width { get => Max.X - Min.X; }
        public float Length { get => Max.Y - Min.Y; }
        public float Height { get => Max.Z - Min.Z; }

        public Aabb(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public override string ToString() => $"Min:{Min} Max:{Max}";
    }
}
