using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LootCooldownPrototype : Prototype
    {
        public PrototypeId Channel { get; protected set; }

        //---

        [DoNotCopy]
        public virtual PrototypeId CooldownRef { get => PrototypeId.Invalid; }
    }

    public class LootCooldownEntityPrototype : LootCooldownPrototype
    {
        public PrototypeId Entity { get; protected set; }

        //---

        [DoNotCopy]
        public override PrototypeId CooldownRef { get => Entity; }
    }

    public class LootCooldownVendorTypePrototype : LootCooldownPrototype
    {
        public PrototypeId VendorType { get; protected set; }

        //---

        [DoNotCopy]
        public override PrototypeId CooldownRef { get => VendorType; }
    }

    public class LootCooldownHierarchyPrototype : Prototype
    {
        public PrototypeId Entity { get; protected set; }
        public PrototypeId[] LocksOut { get; protected set; }

        //---
    }

    public class LootCooldownRolloverTimeEntryPrototype : Prototype
    {
        public float WallClockTime24Hr { get; protected set; }
        public Weekday WallClockTimeDay { get; protected set; }

        //---

    }

    public class LootCooldownChannelPrototype : Prototype
    {
        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public virtual void UpdateCooldown(Player player, PrototypeId dropProtoRef)
        {
        }

        public virtual bool IsOnCooldown(Game game, PropertyCollection properties)
        {
            Logger.Warn($"IsOnCooldown(): {this}");
            return false;
        }

        public virtual void GetCooldownSettings(Player player, out PropertyEnum propertyEnum, out bool activeOnPlayer, out bool activeOnAvatar, out TimeSpan cooldownTime)
        {
            propertyEnum = PropertyEnum.LootCooldownTimeStartChannel;
            activeOnPlayer = false;
            activeOnAvatar = false;
            cooldownTime = TimeSpan.Zero;

            Logger.Warn($"GetCooldownSettings(): This cooldown channel doesn't support this functionality!\n{this}");
        }

        public virtual void SetCooldown(Player player, int count)
        {
            Logger.Warn($"SetCooldown(): This cooldown channel doesn't support this functionality!\n{this}");
        }
    }

    public class LootCooldownChannelRollOverPrototype : LootCooldownChannelPrototype
    {
        public LootCooldownRolloverTimeEntryPrototype[] RolloverTimeEntries { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool IsOnCooldown(Game game, PropertyCollection properties)
        {
            if (RolloverTimeEntries.IsNullOrEmpty())
                return false;

            using PropertyCollection rolloverProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();

            for (int i = 0; i < RolloverTimeEntries.Length; i++)
            {
                LootCooldownRolloverTimeEntryPrototype entryProto = RolloverTimeEntries[i];
                if (entryProto == null)
                {
                    Logger.Warn("IsOnCooldown(): entryProto == null");
                    continue;
                }

                rolloverProperties[PropertyEnum.LootCooldownRolloverWallTime, (PropertyParam)i, (PropertyParam)entryProto.WallClockTimeDay] = entryProto.WallClockTime24Hr;
            }

            if (LootUtilities.GetLastLootCooldownRolloverWallTime(rolloverProperties, Clock.UnixTime, out TimeSpan lastRolloverTime) == false)
                return Logger.WarnReturn(false, "IsOnCooldown(): Failed to get last loot cooldown rollover wall time");

            return properties[PropertyEnum.LootCooldownTimeStartChannel, DataRef] > lastRolloverTime;
        }

        public override void GetCooldownSettings(Player player, out PropertyEnum propertyEnum, out bool activeOnPlayer, out bool activeOnAvatar, out TimeSpan cooldownTime)
        {
            propertyEnum = PropertyEnum.LootCooldownTimeStartChannel;
            activeOnPlayer = default;
            activeOnAvatar = default;
            cooldownTime = default;

            if (RolloverTimeEntries.IsNullOrEmpty())
            {
                Logger.Warn("GetCooldownSettings(): RolloverTimeEntries.IsNullOrEmpty()");
                return;
            }

            using PropertyCollection rolloverProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();

            for (int i = 0; i < RolloverTimeEntries.Length; i++)
            {
                LootCooldownRolloverTimeEntryPrototype entryProto = RolloverTimeEntries[i];
                if (entryProto == null)
                {
                    Logger.Warn("GetCooldownSettings(): entryProto == null");
                    continue;
                }

                rolloverProperties[PropertyEnum.LootCooldownRolloverWallTime, (PropertyParam)i, (PropertyParam)entryProto.WallClockTimeDay] = entryProto.WallClockTime24Hr;
            }

            if (LootUtilities.GetLastLootCooldownRolloverWallTime(rolloverProperties, Clock.UnixTime, out TimeSpan lastRolloverTime) == false)
            {
                Logger.Warn("GetCooldownSettings(): Failed to get last loot cooldown rollover wall time");
                return;
            }

            if (lastRolloverTime <= TimeSpan.Zero)
                return;

            activeOnPlayer = player.Properties[propertyEnum, DataRef] > lastRolloverTime;

            Avatar avatar = player?.CurrentAvatar;
            if (avatar != null)
            {
                activeOnAvatar = avatar.Properties[propertyEnum, DataRef] > lastRolloverTime;
            }
            else
            {
                Logger.Warn("GetCooldownSettings(): avatar == null");
                activeOnAvatar = true;
            }

            cooldownTime = Clock.UnixTime;
        }

        public TimeSpan GetTimeUntilNextRollover(TimeSpan currentTime)
        {
            // client-only? Gazillion::ClientUIEventManager::UIEvent_DailyMissionsListUpdate()
            return default;
        }
    }

    public class LootCooldownChannelTimePrototype : LootCooldownChannelPrototype
    {
        public float DurationMinutes { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void PostProcess()
        {
            base.PostProcess();

            // Apply Eternity Splinter cooldown override if needed
            if (DataRef == (PrototypeId)16132487603695459127)   // Loot/Cooldowns/Channels/LootCooldownChannelEternitySplin.prototype
            {
                CustomGameOptionsConfig config = ConfigManager.Instance.GetConfig<CustomGameOptionsConfig>();
                if (config.ESCooldownOverrideMinutes >= 0f)
                {
                    DurationMinutes = config.ESCooldownOverrideMinutes;
                    Logger.Info($"Applied Eternity Splinter cooldown override - {DurationMinutes} minutes");
                }
            }
        }

        public override bool IsOnCooldown(Game game, PropertyCollection properties)
        {
            TimeSpan cooldownTimeStart = properties[PropertyEnum.LootCooldownTimeStartChannel, DataRef];
            TimeSpan cooldownDuration = TimeSpan.FromMinutes(DurationMinutes);
            TimeSpan currentTime = game.CurrentTime;

            return cooldownDuration > (currentTime - cooldownTimeStart);
        }

        public override void GetCooldownSettings(Player player, out PropertyEnum propertyEnum, out bool activeOnPlayer, out bool activeOnAvatar, out TimeSpan cooldownTime)
        {
            propertyEnum = PropertyEnum.LootCooldownTimeStartChannel;
            activeOnPlayer = default;
            activeOnAvatar = default;
            cooldownTime = default;

            Game game = player?.Game;

            if (game == null)
            {
                Logger.Warn("GetCooldownSettings(): game == null");
                return;
            }

            TimeSpan cooldownDuration = TimeSpan.FromMinutes(DurationMinutes);
            TimeSpan currentTime = player.Game.CurrentTime;

            activeOnPlayer = cooldownDuration > (currentTime - player.Properties[propertyEnum, DataRef]);

            Avatar avatar = player.CurrentAvatar;
            if (avatar != null)
            {
                activeOnAvatar = cooldownDuration > (currentTime - avatar.Properties[propertyEnum, DataRef]);
            }
            else
            {
                Logger.Warn("GetCooldownSettings(): avatar == null");
                activeOnAvatar = true;
            }

            cooldownTime = currentTime;
        }

        public override void SetCooldown(Player player, int count)
        {
            Game game = player?.Game;
            if (game == null)
            {
                Logger.Warn("SetCooldown(): game == null");
                return;
            }

            player.Properties[PropertyEnum.LootCooldownTimeStartChannel, DataRef] = game.CurrentTime;
        }
    }

    public class LootCooldownChannelCountPrototype : LootCooldownChannelPrototype
    {
        public int MaxDrops { get; protected set; }
        public LootCooldownRolloverTimeEntryPrototype[] RolloverTimeEntries { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void SetCooldown(Player player, int count)
        {
            if (player.IsInGame == false)
                return;

            player.Properties.AdjustProperty(count, new(PropertyEnum.LootCooldownCount, DataRef));
        }

        public override void UpdateCooldown(Player player, PrototypeId dropProtoRef)
        {
            int count = player.Properties[PropertyEnum.LootCooldownCount, DataRef];

            if (RolloverTimeEntries.IsNullOrEmpty())
            {
                Logger.Warn("UpdateCooldown(): RolloverTimeEntries.IsNullOrEmpty()");
                return;
            }

            using PropertyCollection rolloverProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();

            for (int i = 0; i < RolloverTimeEntries.Length; i++)
            {
                LootCooldownRolloverTimeEntryPrototype entryProto = RolloverTimeEntries[i];
                if (entryProto == null)
                {
                    Logger.Warn("UpdateCooldown(): entryProto == null");
                    continue;
                }

                rolloverProperties[PropertyEnum.LootCooldownRolloverWallTime, (PropertyParam)i, (PropertyParam)entryProto.WallClockTimeDay] = entryProto.WallClockTime24Hr;
            }

            if (LootUtilities.GetLastLootCooldownRolloverWallTime(rolloverProperties, Clock.UnixTime, out TimeSpan lastActualRolloverTime) == false)
            {
                Logger.Warn("UpdateCooldown(): Failed to get last loot cooldown rollover wall time");
                return;
            }

            PropertyId cooldownStartProperty = new(PropertyEnum.LootCooldownTimeStartChannel, DataRef);
            if (count == 0 && player.Properties.HasProperty(cooldownStartProperty) == false)
            {
                player.Properties[PropertyEnum.LootCooldownTimeStartChannel, DataRef] = lastActualRolloverTime;
                return;
            }

            if (lastActualRolloverTime == TimeSpan.Zero)
            {
                Logger.Warn("UpdateCooldown(): lastActualRolloverTime == TimeSpan.Zero");
                return;
            }

            TimeSpan cooldownStartTime = player.Properties[PropertyEnum.LootCooldownTimeStartChannel, DataRef];
            if (cooldownStartTime < lastActualRolloverTime)
            {
                // Reset the count if a rollover has happened since the last saved time
                player.Properties[PropertyEnum.LootCooldownCount, DataRef] = 0;
                player.Properties[PropertyEnum.LootCooldownTimeStartChannel, DataRef] = lastActualRolloverTime;
            }
        }

        public override bool IsOnCooldown(Game game, PropertyCollection properties)
        {
            int count = properties[PropertyEnum.LootCooldownCount, DataRef];
            return count >= MaxDrops;
        }
    }
}
