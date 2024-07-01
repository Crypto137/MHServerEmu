namespace MHServerEmu.Core.Collisions
{
    public interface IBounds
    {
        ContainmentType Contains(in Aabb2 bounds);
        bool Intersects(in Aabb bounds);
    }
}
