using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;

namespace MHServerEmu.Games.Powers
{
    public readonly struct MovementPowerEntityCollideFunc : ICanBlock
    {
        private readonly int _blockTypeFlags;       // BoundsMovementPowerBlockType

        public MovementPowerEntityCollideFunc(int blockTypeFlags)
        {
            _blockTypeFlags = blockTypeFlags;
            Verify.IsTrue(_blockTypeFlags != 0, "Blocking query with movement power blocking type of None doesn't make sense.");
        }

        public bool CanBlock(WorldEntity otherEntity)
        {
            WorldEntityPrototype otherEntityProto = otherEntity.WorldEntityPrototype;
            BoundsPrototype otherEntityBoundsProto = otherEntityProto?.Bounds;
            
            if (otherEntityBoundsProto == null)
                return false;

            return ((1 << (int)otherEntityBoundsProto.BlocksMovementPowers) & _blockTypeFlags) != 0;
        }
    }
}
