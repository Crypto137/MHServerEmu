using MHServerEmu.Core.Collections;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public static class LootUtilities
    {
        public static bool PickValidItem(IItemResolver resolver, Picker<Prototype> picker, AgentPrototype agentProto, in DropFilterArguments dropFilterArgs,
            ref ItemPrototype pickedItemProto, RestrictionTestFlags restrictionTestFlags, PrototypeId rarityProtoRef)
        {
            pickedItemProto = null;


            return pickedItemProto != null;
        }
    }
}
