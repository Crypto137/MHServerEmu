using MHServerEmu.Core.Helpers;

namespace MHServerEmu.Core.Config
{
    /// <summary>
    /// A singleton that provides access to config value containers.
    /// </summary>
    public class ConfigManager
    {
        private readonly Dictionary<Type, ConfigContainer> _configContainerDict = new();
        private readonly IniFile _iniFile;

        /// <summary>
        /// Provides access to the <see cref="ConfigManager"/> instance.
        /// </summary>
        public static ConfigManager Instance { get; } = new();

        /// <summary>
        /// Constructs the <see cref="ConfigManager"/> instance.
        /// </summary>
        private ConfigManager()
        {
            string path = Path.Combine(FileHelper.ServerRoot, "Config.ini");
            _iniFile = new(path);
        }

        /// <summary>
        /// Initializes if needed and returns <typeparamref name="T"/>.
        /// </summary>
        public T GetConfig<T>() where T: ConfigContainer, new()
        {
            if (_configContainerDict.TryGetValue(typeof(T), out ConfigContainer container) == false)
            {
                container = new T();
                container.Initialize(_iniFile);
                _configContainerDict.Add(typeof(T), container);
            }

            return (T)container;
        }
    }
}
