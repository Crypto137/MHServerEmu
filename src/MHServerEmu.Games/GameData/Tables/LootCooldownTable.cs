using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Tables
{
    public class LootCooldownTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<PrototypeId, LootCooldownChannelPrototype> _lootToCooldownChannelDict = new();
        private readonly Dictionary<PrototypeId, LootCooldownChannelPrototype> _currencyToCooldownChannelDict = new();

        public PrototypeId EternitySplinterPrototypeRef { get; }

        public LootCooldownTable()
        {
            foreach (var lootCooldownRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<LootCooldownPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var cooldownProto = lootCooldownRef.As<LootCooldownPrototype>();
                PrototypeId mapKey = cooldownProto.CooldownRef;

                if (mapKey != PrototypeId.Invalid)
                {
                    if (_lootToCooldownChannelDict.ContainsKey(mapKey))
                    {
                        Logger.Warn($"LootCooldownTable(): Duplicate prototype encountered when caching item/cooldown channel mappings! Skipping mapping: {cooldownProto}");
                        continue;
                    }

                    LootCooldownChannelPrototype channelProto = cooldownProto.Channel.As<LootCooldownChannelPrototype>();

                    // Find currencies related to the item
                    var mapKeyProto = mapKey.As<Prototype>();
                    if (mapKeyProto is WorldEntityPrototype worldEntityProto && worldEntityProto.Properties != null)
                    {
                        foreach (var kvp in worldEntityProto.Properties.IteratePropertyRange(PropertyEnum.ItemCurrency))
                        {
                            Property.FromParam(kvp.Key, 0, out PrototypeId currencyRef);
                            if (currencyRef == PrototypeId.Invalid)
                            {
                                Logger.Warn("LootCooldownTable(): currencyRef == PrototypeId.Invalid");
                                continue;
                            }

                            _currencyToCooldownChannelDict[currencyRef] = channelProto;
                        }
                    }

                    _lootToCooldownChannelDict[mapKey] = channelProto;
                }
            }

            // Entity/Items/CurrencyItems/EternitySplinter.prototype
            // This guid is also hardcoded in the client.
            EternitySplinterPrototypeRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)14274455345508523748);
        }

        public LootCooldownChannelPrototype GetCooldownChannelForLoot(PrototypeId lootRef)
        {
            if (_lootToCooldownChannelDict.TryGetValue(lootRef, out LootCooldownChannelPrototype channelProto) == false)
                return null;

            return channelProto;
        }

        public LootCooldownChannelPrototype GetCooldownChannelForCurrency(PrototypeId currencyRef)
        {
            if (_currencyToCooldownChannelDict.TryGetValue(currencyRef, out LootCooldownChannelPrototype channelProto) == false)
                return null;

            return channelProto;
        }
    }
}
