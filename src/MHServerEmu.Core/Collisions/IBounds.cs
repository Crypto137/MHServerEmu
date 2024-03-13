namespace MHServerEmu.Core.Collisions
{
    public interface IBounds
    {
        ContainmentType Contains(Aabb2 bounds);
        bool Intersects(Aabb bounds);
    }
}
