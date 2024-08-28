using MHServerEmu.Core.Config;

namespace MHServerEmu.DatabaseAccess.Json
{
    public class JsonDBManagerConfig : ConfigContainer
    {
        public string FileName { get; private set; } = "DefaultAccount.json";
        public string PlayerName { get; private set; } = "Player";
    }
}
