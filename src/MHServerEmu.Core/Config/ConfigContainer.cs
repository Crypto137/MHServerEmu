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
        internal void Initialize(IniFile configFile)
        {
            Type type = GetType();                                          // Use reflection to populate our config

            // Remove the Config suffix from the config class
            string section = type.Name;
            if (section.EndsWith("Config", StringComparison.OrdinalIgnoreCase))
                section = section.Substring(0, section.Length - 6);  

            // Read and set values for each property from the ini file
            foreach (var property in type.GetProperties())
            {
                if (property.IsDefined(typeof(ConfigIgnoreAttribute))) continue;   // Ignore specified properties

                object value = Type.GetTypeCode(property.PropertyType) switch
                {
                    TypeCode.String     => configFile.GetString(section, property.Name),
                    TypeCode.Boolean    => configFile.GetBool(section, property.Name),
                    TypeCode.Int32      => configFile.GetInt32(section, property.Name),
                    TypeCode.UInt32     => configFile.GetUInt32(section, property.Name),
                    TypeCode.Int64      => configFile.GetInt64(section, property.Name),
                    TypeCode.UInt64     => configFile.GetUInt64(section, property.Name),
                    TypeCode.Single     => configFile.GetSingle(section, property.Name),
                    _ => throw new NotImplementedException($"Value type {property.PropertyType} is not supported for config files."),
                };

                if (value == null) continue;    // Skip assignment if we weren't able to get the value from config
                property.SetValue(this, value);
            }
        }
    }
}
