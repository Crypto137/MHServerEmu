using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.GameServer.Common
{
    public class Aabb
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public Aabb(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public float GetWidth() { return Max.X - Min.X; }
        public float GetLength() { return Max.Y - Min.Y; }
        public float GetHeight() { return Max.Z - Min.Z; }
        public override string ToString() => $"Min:{Min} Max:{Max}";
    }
}
