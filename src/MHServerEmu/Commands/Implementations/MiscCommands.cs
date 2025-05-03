using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;

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
            PlayerConnection playerConnection = (PlayerConnection)client;

            // Regions/HUBS/AvengersTowerHUB/Portals/AvengersTowerHUBEntry.prototype
            playerConnection.MoveToTarget((PrototypeId)16780605467179883619);

            return "Teleporting to Avengers Tower (original)";
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
            PlayerConnection playerConnection = (PlayerConnection)client;

            // Regions/Story/CH04EastSide/UpperEastSide/PoliceDepartment/Portals/JailTarget.prototype
            playerConnection.MoveToTarget((PrototypeId)13284513933487907420);

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
    }
}
