using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// Contains context data for rolling loot tables.
    /// </summary>
    public class ItemResolverContext
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public LootContext LootContext { get; private set; }
        public Player Player { get; private set; }

        public Region Region { get => Player?.GetRegion(); }

        public void Set(LootContext lootContext, Player player)
        {
            LootContext = lootContext;
            Player = player;
        }

        public float GetDropChance(LootRollSettings settings, float noDropPercent)
        {
            // Do not drop if there are any hard restrictions (this should have already been handled when selecting the loot table node)
            if (settings.IsRestrictedByLootDropChanceModifier())
                return Logger.WarnReturn(0f, $"GetDropChance(): Restricted by loot drop chance modifiers [{settings.DropChanceModifiers}]");

            // Do not drop cooldown-based loot for now
            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.CooldownOncePerXHours))
                return Logger.WarnReturn(0f, "GetDropChance(): Unimplemented modifier CooldownOncePerXHours");

            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.CooldownOncePerRollover))
                return Logger.WarnReturn(0f, "GetDropChance(): Unimplemented modifier CooldownOncePerRollover");

            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.CooldownByChannel))
                return Logger.WarnReturn(0f, "GetDropChance(): Unimplemented modifier CooldownByChannel");

            // Start with a base drop chance based on the specified NoDrop percent
            float dropChance = 1f - noDropPercent;

            // Apply live tuning multiplier
            dropChance *= LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_LootDropRate);

            // Apply difficulty multiplier
            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.DifficultyTierNoDropModified))
                dropChance *= settings.NoDropModifier;

            // Add more multipliers here as needed

            return dropChance;
        }

        public bool IsOnCooldown(PrototypeId dropProtoRef, int count)
        {
            // TODO
            //Logger.Debug($"CheckDropCooldown(): {dropProtoRef.GetName()} x{amount}");
            return false;
        }
    }
}
