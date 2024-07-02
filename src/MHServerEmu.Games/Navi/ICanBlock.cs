using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Navi
{
    public interface ICanBlock
    {
        public bool CanBlock(WorldEntity other);
    }
}
