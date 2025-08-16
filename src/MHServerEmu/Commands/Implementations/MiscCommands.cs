using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("tower")]
    [CommandGroupDescription("Teleports to Avengers Tower (original).")]
    [CommandGroupFlags(CommandGroupFlags.SingleCommand)]
    public class TowerCommand : CommandGroup
    {
        [DefaultCommand]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Tower(string[] @params, NetClient client)
        {
            Player player = ((PlayerConnection)client).Player;

            CanTeleportResult result = CanTeleport(player);
            if (result != CanTeleportResult.Success)
                return $"You cannot teleport right now ({result}).";

            player.Properties[PropertyEnum.PowerCooldownStartTime, GameDatabase.GlobalsPrototype.ReturnToHubPower] = player.Game.CurrentTime;
            Teleporter.DebugTeleportToTarget(player, (PrototypeId)16780605467179883619);    // Regions/HUBS/AvengersTowerHUB/Portals/AvengersTowerHUBEntry.prototype

            return "Teleporting to Avengers Tower (original).";
        }

        private static CanTeleportResult CanTeleport(Player player)
        {
            if (player == null)
                return CanTeleportResult.GenericError;

            // Skip checks for accounts that have access to debug commands.
            if (player.HasBadge(AvailableBadges.SiteCommands))
                return CanTeleportResult.Success;

            if (player.IsFullscreenObscured)
                return CanTeleportResult.FullscreenObscured;

            Avatar avatar = player.CurrentAvatar;
            if (avatar == null || avatar.IsInWorld == false)
                return CanTeleportResult.GenericError;

            if (avatar.Properties[PropertyEnum.IsInCombat])
                return CanTeleportResult.InCombat;

            Power returnToHubPower = avatar.GetPower(GameDatabase.GlobalsPrototype.ReturnToHubPower);
            if (avatar.CanActivatePower(returnToHubPower, avatar.Id, avatar.RegionLocation.Position) != PowerUseResult.Success)
                return CanTeleportResult.BodyslideNotAvailable;

            return CanTeleportResult.Success;
        }

        private enum CanTeleportResult
        {
            Success,
            GenericError,
            FullscreenObscured,
            InCombat,
            BodyslideNotAvailable,
        }
    }

    [CommandGroup("jail")]
    [CommandGroupDescription("Teleports to East Side: Detention Facility (old).")]
    [CommandGroupUserLevel(AccountUserLevel.Admin)]
    [CommandGroupFlags(CommandGroupFlags.SingleCommand)]
    public class JailCommand : CommandGroup
    {
        [DefaultCommand]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Jail(string[] @params, NetClient client)
        {
            Player player = ((PlayerConnection)client).Player;
            Teleporter.DebugTeleportToTarget(player, (PrototypeId)13284513933487907420);    // Regions/Story/CH04EastSide/UpperEastSide/PoliceDepartment/Portals/JailTarget.prototype
            return "Teleporting to East Side: Detention Facility (old)";
        }
    }

    [CommandGroup("position")]
    [CommandGroupDescription("Shows current position.")]
    [CommandGroupFlags(CommandGroupFlags.SingleCommand)]
    public class PositionCommand : CommandGroup
    {
        [DefaultCommand]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Position(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            return $"Current position: {avatar.RegionLocation.Position.ToStringNames()}";
        }
    }

    [CommandGroup("dance")]
    [CommandGroupDescription("Performs the Dance emote (if available).")]
    [CommandGroupFlags(CommandGroupFlags.SingleCommand)]
    public class DanceCommand : CommandGroup
    {
        [DefaultCommand]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Dance(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;

            Avatar avatar = playerConnection.Player.CurrentAvatar;
            var avatarPrototypeId = (AvatarPrototypeId)avatar.PrototypeDataRef;
            switch (avatarPrototypeId)
            {
                case AvatarPrototypeId.BlackPanther:
                case AvatarPrototypeId.BlackWidow:
                case AvatarPrototypeId.CaptainAmerica:
                case AvatarPrototypeId.Colossus:
                case AvatarPrototypeId.EmmaFrost:
                case AvatarPrototypeId.Hulk:
                case AvatarPrototypeId.IronMan:
                case AvatarPrototypeId.RocketRaccoon:
                case AvatarPrototypeId.ScarletWitch:
                case AvatarPrototypeId.Spiderman:
                case AvatarPrototypeId.Storm:
                case AvatarPrototypeId.Thing:
                case AvatarPrototypeId.Thor:
                    const PrototypeId dancePowerRef = (PrototypeId)773103106671775187;  // Powers/Emotes/EmoteDance.prototype

                    Power dancePower = avatar.GetPower(dancePowerRef);
                    if (dancePower == null) return "Dance power is not assigned to the current avatar.";

                    PowerActivationSettings settings = new(avatar.Id, avatar.RegionLocation.Position, avatar.RegionLocation.Position);
                    settings.Flags = PowerActivationSettingsFlags.NotifyOwner;
                    avatar.ActivatePower(dancePowerRef, ref settings);

                    return $"{avatarPrototypeId} begins to dance";
                default:
                    return $"{avatarPrototypeId} doesn't want to dance";
            }

        }
    }

    [CommandGroup("tp")]
    [CommandGroupDescription("Teleports to position.\nUsage:\ntp x:+1000 (relative to current position)\ntp x100 y500 z10 (absolute position)")]
    [CommandGroupUserLevel(AccountUserLevel.Admin)]
    [CommandGroupFlags(CommandGroupFlags.SingleCommand)]
    public class TeleportCommand : CommandGroup
    {
        [DefaultCommand]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Teleport(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Avatar avatar = playerConnection.Player.CurrentAvatar;
            if (avatar == null || avatar.IsInWorld == false)
                return "Avatar not found.";

            float x = 0f, y = 0f, z = 0f;
            foreach (string param in @params)
            {
                switch (param[0])
                {
                    case 'x':
                        if (float.TryParse(param.AsSpan(1), out x) == false) x = 0f;
                        break;

                    case 'y':
                        if (float.TryParse(param.AsSpan(1), out y) == false) y = 0f;
                        break;

                    case 'z':
                        if (float.TryParse(param.AsSpan(1), out z) == false) z = 0f;
                        break;

                    default:
                        return $"Invalid parameter: {param}";
                }
            }

            Vector3 teleportPoint = new(x, y, z);

            if (@params.Length < 3)
                teleportPoint += avatar.RegionLocation.Position;

            avatar.ChangeRegionPosition(teleportPoint, null, ChangePositionFlags.Teleport);

            return $"Teleporting to {teleportPoint.ToStringNames()}.";
        }

        [CommandGroup("syncmana")]
        [CommandGroupDescription("Syncs the current mana value with the server.")]
        [CommandGroupFlags(CommandGroupFlags.SingleCommand)]
        public class SyncManaCommand : CommandGroup
        {
            [DefaultCommand]
            [CommandInvokerType(CommandInvokerType.Client)]
            public string SyncMana(string[] @params, NetClient client)
            {
                Avatar avatar = ((PlayerConnection)client).Player.CurrentAvatar;
                if (avatar == null || avatar.IsInWorld == false)
                    return "Avatar not found.";

                avatar.Properties.SyncProperty(PropertyEnum.Endurance, out PropertyValue value);    // default to mana type 1
                return $"Syncing mana (server value = {(float)value}).";
            }
        }
    }
}
