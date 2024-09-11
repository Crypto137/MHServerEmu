using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Items
{
    public struct BuiltInAffixDetails
    {
        public AffixEntryPrototype AffixEntryProto;
        public int LevelRequirement;
        public PrototypeId AvatarProtoRef;
        public PrototypeId ScopeProtoRef;
        public int Seed;

        public BuiltInAffixDetails(AffixEntryPrototype affixEntryProto)
        {
            AffixEntryProto = affixEntryProto;
            LevelRequirement = 0;
            AvatarProtoRef = PrototypeId.Invalid;
            ScopeProtoRef = PrototypeId.Invalid;
            Seed = 0;
        }
    }
}
