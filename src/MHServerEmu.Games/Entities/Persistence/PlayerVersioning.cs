using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities.Persistence
{
    public static class PlayerVersioning
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool Apply(Player player)
        {
            ArchiveVersion loadedVersion = player.LastSerializedArchiveVersion;

            while (loadedVersion < ArchiveVersion.Current)
            {
                bool success = true;

                switch (loadedVersion)
                {
                    case ArchiveVersion.Initial:
                        success |= V2_ClearProperties(player);
                        break;
                }

                if (success == false)
                    return Logger.WarnReturn(false, $"ApplyVersioning(): Failed to apply versioning to loaded version {loadedVersion}");

                Logger.Trace($"Applied versioning to archive version {loadedVersion} for player [{player}]");
                loadedVersion++;
            }

            return true;
        }

        #region V2

        private static bool V2_ClearProperties(Player player)
        {
            player.Properties.RemovePropertyRange(PropertyEnum.Waypoint);
            player.Properties.RemovePropertyRange(PropertyEnum.UISystemLock);

            // Team-ups seem to be invisible if they are summoned before the tutorial starts, so just unsummon them for all avatars
            foreach (Avatar avatar in new AvatarIterator(player))
            {
                avatar.Properties.RemoveProperty(PropertyEnum.AvatarTeamUpIsSummoned);
                avatar.Properties.RemoveProperty(PropertyEnum.AvatarTeamUpStartTime);
                avatar.Properties.RemoveProperty(PropertyEnum.AvatarTeamUpDuration);
            }

            return true;
        }

        #endregion
    }
}
