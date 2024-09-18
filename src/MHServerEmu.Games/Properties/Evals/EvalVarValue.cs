using System.Runtime.InteropServices;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties.Evals
{
    [StructLayout(LayoutKind.Explicit)]
    public struct EvalVarValue
    {
        [FieldOffset(0)]
        public bool Bool = false;
        [FieldOffset(0)]
        public float Float = 0.0f;
        [FieldOffset(0)]
        public long Int = 0;
        [FieldOffset(0)]
        public ulong EntityId = 0;
        [FieldOffset(0)]
        public AssetId AssetId = 0;
        [FieldOffset(0)]
        public PrototypeId Proto = 0;
        [FieldOffset(0)]
        public ulong PropId = 0;
        [FieldOffset(8)]
        public PropertyCollection Props = null;
        [FieldOffset(8)]
        public List<PrototypeId> ProtoRefList = null;
        [FieldOffset(8)]
        public PrototypeId[] ProtoRefVector = null;
        [FieldOffset(8)]
        public ConditionCollection Conditions = null;
        [FieldOffset(8)]
        public Entity Entity = null;
        [FieldOffset(0)]
        public ulong EntityGuid = 0;

        public EvalVarValue() { }
    }
}
