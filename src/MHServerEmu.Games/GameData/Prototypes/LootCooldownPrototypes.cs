using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
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

        public virtual void UpdateCooldown(Player player, PrototypeId lootDropProtoRef)
        {
        }

        public virtual bool IsOnCooldown(Game game, PropertyCollection properties)
        {
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
    }

    public class LootCooldownChannelTimePrototype : LootCooldownChannelPrototype
    {
        public float DurationMinutes { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

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

            Game game = player?.Game;

            if (game == null)
            {
                Logger.Warn("GetCooldownSettings(): game == null");
                activeOnPlayer = default;
                activeOnAvatar = default;
                cooldownTime = default;
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
    }
}
