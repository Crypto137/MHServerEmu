using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public static class LootUtilities
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool PickValidItem(IItemResolver resolver, Picker<Prototype> basePicker, AgentPrototype teamUpProto, DropFilterArguments args,
            ref ItemPrototype pickedItemProto, RestrictionTestFlags restrictionTestFlags, ref PrototypeId? rarityProtoRef)
        {
            pickedItemProto = null;
            DropFilterArguments currentArgs = new(args);     // Copy arguments to compare to what we started

            while (pickedItemProto == null && (restrictionTestFlags.HasFlag(RestrictionTestFlags.Rarity) || currentArgs.Rarity != PrototypeId.Invalid))
            {
                Picker<Prototype> iterationPicker = new(basePicker);

                while (iterationPicker.PickRemove(out Prototype proto))
                {
                    if (proto is not ItemPrototype itemProto)
                    {
                        Logger.Warn("PickValidItem(): itemProto == null");
                        continue;
                    }

                    currentArgs.ItemProto = itemProto;
                    currentArgs.RollFor = itemProto.GetRollForAgent(currentArgs.RollFor, teamUpProto);

                    if (resolver.CheckItem(in currentArgs, restrictionTestFlags, false))
                    {
                        pickedItemProto = itemProto;
                        if (rarityProtoRef != null)
                            rarityProtoRef = currentArgs.Rarity;
                        break;
                    }
                }

                // Check other rarities if we have one a base one provided
                if (rarityProtoRef == null || pickedItemProto != null || restrictionTestFlags.HasFlag(RestrictionTestFlags.Rarity) == false)
                    break;

                RarityPrototype rarityProto = currentArgs.Rarity.As<RarityPrototype>();
                if (rarityProto == null)
                {
                    Logger.Warn("PickValidItem(): rarityProto == null");
                    break;
                }

                currentArgs.Rarity = rarityProto.DowngradeTo;

                if (currentArgs.Rarity == args.Rarity)
                {
                    Logger.Warn($"PickValidItem(): Rarity loop detected [{currentArgs.Rarity.GetName()}]");
                    break;
                }
            }

            return pickedItemProto != null;
        }
    }
}
