using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;

namespace MHServerEmu.Games.Powers
{
    public readonly struct MovementPowerEntityCollideFunc : ICanBlock
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly int _blockTypeFlags;       // BoundsMovementPowerBlockType

        public MovementPowerEntityCollideFunc(int blockTypeFlags)
        {
            _blockTypeFlags = blockTypeFlags;
            if (_blockTypeFlags == 0)
                Logger.Warn("Blocking query with movement power blocking type of None doesn't make sense");
        }

        public bool CanBlock(WorldEntity otherEntity)
        {
            var otherEntityProto = otherEntity.WorldEntityPrototype;
            BoundsPrototype otherEntityBoundsProto = otherEntityProto?.Bounds;
            
            if (otherEntityBoundsProto == null)
                return false;

            return (1 << (int)otherEntityBoundsProto.BlocksMovementPowers) != 0;
        }
    }
}
