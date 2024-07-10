using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.LegacyImplementations;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("tower", "Changes region to Avengers Tower (original).", AccountUserLevel.User)]
    public class TowerCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Tower(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);
            game.MovePlayerToRegion(playerConnection, (PrototypeId)RegionPrototypeId.AvengersTowerHUBRegion, (PrototypeId)WaypointPrototypeId.AvengersTowerHub);

            return "Changing region to Avengers Tower (original)";
        }
    }

    [CommandGroup("jail", "Travel to East Side: Detention Facility (old).", AccountUserLevel.User)]
    public class JailCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Jail(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);
            game.MovePlayerToRegion(playerConnection, (PrototypeId)RegionPrototypeId.UpperEastSideRegion, (PrototypeId)TargetPrototypeId.JailTarget);

            return "Travel to East Side: Detention Facility (old)";
        }
    }

    [CommandGroup("position", "Shows current position.", AccountUserLevel.User)]
    public class PositionCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Position(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            return $"Current position: {playerConnection.LastPosition.ToStringNames()}";
        }
    }

    [CommandGroup("dance", "Performs the Dance emote", AccountUserLevel.User)]
    public class DanceCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Dance(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);

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

    [CommandGroup("tp", "Teleports to position.\nUsage:\ntp x:+1000 (relative to current position)\ntp x100 y500 z10 (absolute position)", AccountUserLevel.User)]
    public class TeleportCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Teleport(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help teleport' to get help.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);

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
                teleportPoint += playerConnection.LastPosition;

            EventPointer<OLD_ToTeleportEvent> eventPointer = new();
            game.GameEventScheduler.ScheduleEvent(eventPointer, TimeSpan.Zero);
            eventPointer.Get().Initialize(playerConnection, teleportPoint);

            return $"Teleporting to {teleportPoint.ToStringNames()}.";
        }
    }
}
