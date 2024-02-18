
namespace MHServerEmu.Games.Common
{
    public interface IBounds
    {
        ContainmentType Contains(Aabb2 bounds);
        bool Intersects(Aabb bounds);
    }
}
