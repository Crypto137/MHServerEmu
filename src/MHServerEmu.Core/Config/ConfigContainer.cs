using System.Reflection;

namespace MHServerEmu.Core.Config
{
    /// <summary>
    /// Provides access to config values.
    /// </summary>
    /// <remarks>
    /// Classes that implement this should have the Config suffix in their names (e.g. FooConfig). Ini section and key names are derived from class and property names.
    /// </remarks>
    public abstract class ConfigContainer
    {
        /// <summary>
        /// Initializes this <see cref="ConfigContainer"/> instance from the provided <see cref="IniFile"/> using reflection.
        /// </summary>
        internal void Initialize(IniFile configFile, IniFile overrideFile)
        {
            SetFromIniFile(configFile);

            if (overrideFile != null)
                SetFromIniFile(overrideFile);
        }

        /// <summary>
        /// Initializes this <see cref="ConfigContainer"/> instance from the provided <see cref="IniFile"/> using reflection.
        /// </summary>
        private void SetFromIniFile(IniFile iniFile)
        {
            Type type = GetType();                                          // Use reflection to populate our config

            // Remove the Config suffix from the config class
            string section = type.Name;
            if (section.EndsWith("Config", StringComparison.OrdinalIgnoreCase))
                section = section.Substring(0, section.Length - 6);  

            // Read and set values for each property from the ini file
            foreach (var property in type.GetProperties())
            {
                // Ignore specified properties
                if (property.IsDefined(typeof(ConfigIgnoreAttribute)))
                    continue;

                object value = Type.GetTypeCode(property.PropertyType) switch
                {
                    TypeCode.String     => iniFile.GetString(section, property.Name),
                    TypeCode.Boolean    => iniFile.GetBool(section, property.Name),
                    TypeCode.Int32      => iniFile.GetInt32(section, property.Name),
                    TypeCode.UInt32     => iniFile.GetUInt32(section, property.Name),
                    TypeCode.Int64      => iniFile.GetInt64(section, property.Name),
                    TypeCode.UInt64     => iniFile.GetUInt64(section, property.Name),
                    TypeCode.Single     => iniFile.GetSingle(section, property.Name),
                    _ => throw new NotImplementedException($"Value type {property.PropertyType} is not supported for config files."),
                };

                // Skip assignment if we weren't able to get the value from config
                if (value == null)
                    continue;
                
                property.SetValue(this, value);
            }
        }
    }
}
