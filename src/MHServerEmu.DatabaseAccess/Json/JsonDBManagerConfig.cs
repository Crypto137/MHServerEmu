using MHServerEmu.Core.Config;

namespace MHServerEmu.DatabaseAccess.Json
{
    public class JsonDBManagerConfig : ConfigContainer
    {
        public string FileName { get; private set; } = "DefaultAccount.json";
        public int MaxBackupNumber { get; private set; } = 5;
        public int BackupIntervalMinutes { get; private set; } = 15;

        public string PlayerName { get; private set; } = "Player";
    }
}
