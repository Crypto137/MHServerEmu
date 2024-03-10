namespace MHServerEmu.Common.Config.Containers
{
    public class GameDataConfig : ConfigContainer
    {
        public bool LoadAllPrototypes { get; private set; }

        public GameDataConfig(IniFile configFile) : base(configFile) { }
    }
}
