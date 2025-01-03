using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Avatars
{
    public class PendingPowerData
    {
        public PrototypeId PowerProtoRef { get; protected set; }
        public ulong TargetId { get; protected set; }
        public Vector3 TargetPosition { get; protected set; }
        public ulong SourceItemId { get; protected set; }

        public int RandomSeed { get; set; }
        public bool Interrupted { get; set; }

        public virtual bool SetData(PrototypeId powerProtoRef, ulong targetId, Vector3 targetPosition, ulong sourceItemId)
        {
            PowerProtoRef = powerProtoRef;
            TargetId = targetId;
            TargetPosition = targetPosition;
            SourceItemId = sourceItemId;

            return true;
        }

        public virtual bool Clear()
        {
            PowerProtoRef = PrototypeId.Invalid;
            TargetId = Entity.InvalidId;
            TargetPosition = Vector3.Zero;
            RandomSeed = 0;
            Interrupted = false;

            return true;
        }
    }
}
