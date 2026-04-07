using MHServerEmu.Core.Helpers;
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
                LootCooldownPrototype cooldownProto = lootCooldownRef.As<LootCooldownPrototype>();
                AddMapping(cooldownProto.CooldownRef, cooldownProto.Channel);
            }

            LoadCustomMappings();

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

        private void LoadCustomMappings()
        {
            string customMappingFilePath = Path.Combine(FileHelper.DataDirectory, "Game", "LootCooldownChannels.json");
            if (File.Exists(customMappingFilePath) == false)
                return;

            Dictionary<string, string> customMappings = FileHelper.DeserializeJson<Dictionary<string, string>>(customMappingFilePath);
            if (customMappings == null || customMappings.Count == 0)
                return;

            int addedCount = 0;

            foreach (var kvp in customMappings)
            {
                PrototypeId mapKey = GameDatabase.GetPrototypeRefByName(kvp.Key);
                if (mapKey == PrototypeId.Invalid)
                {
                    Logger.Warn($"LoadCustomMappings(): Invalid key {kvp.Key}");
                    continue;
                }

                PrototypeId channel = GameDatabase.GetPrototypeRefByName(kvp.Value);
                if (channel == PrototypeId.Invalid)
                {
                    Logger.Warn($"LoadCustomMappings(): Invalid channel {kvp.Value}");
                    continue;
                }

                if (AddMapping(mapKey, channel))
                    addedCount++;
            }

            Logger.Info($"Loaded {addedCount} custom cooldown mappings");
        }

        private bool AddMapping(PrototypeId mapKey, PrototypeId channel)
        {
            if (mapKey == PrototypeId.Invalid)
                return false;

            if (_lootToCooldownChannelDict.ContainsKey(mapKey))
                return Logger.WarnReturn(false, $"LootCooldownTable(): Duplicate prototype encountered when caching item/cooldown channel mappings! Skipping mapping: {mapKey.GetNameFormatted()} => {channel.GetName()}");

            LootCooldownChannelPrototype channelProto = channel.As<LootCooldownChannelPrototype>();

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

            return true;
        }
    }
}
