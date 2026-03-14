using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;

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
                    // add versioning code here as needed
                    case ArchiveVersion.Initial:
                        break;
                }

                if (success == false)
                    return Logger.WarnReturn(false, $"ApplyVersioning(): Failed to apply versioning to loaded version {loadedVersion}");

                Logger.Trace($"Applied versioning to archive version {loadedVersion} for player [{player}]");
                loadedVersion++;
            }

            return true;
        }
    }
}
