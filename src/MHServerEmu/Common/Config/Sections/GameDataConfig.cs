namespace MHServerEmu.Common.Config.Sections
{
    public class GameDataConfig
    {
        private const string Section = "GameData";

        public bool LoadAllPrototypes { get; }

        public GameDataConfig(IniFile configFile)
        {
            LoadAllPrototypes = configFile.ReadBool(Section, nameof(LoadAllPrototypes));
        }
    }
}
