using System.Reflection;

namespace MHServerEmu.Common.Config
{
    /// <summary>
    /// Provides access to config values.
    /// </summary>
    public abstract class ConfigContainer
    {
        /// <summary>
        /// Constructs a new instance of ConfigContainer and populates its property values using reflection.
        /// </summary>
        public ConfigContainer(IniFile configFile)
        {
            Type type = GetType();                                          // Use reflection to populate our config
            string section = type.Name.Replace("Config", string.Empty);     // Remove the Config suffix from the config class

            // Read and set values for each property from the ini file
            foreach (var property in type.GetProperties())
            {
                if (property.IsDefined(typeof(ConfigIgnoreAttribute))) continue;   // Ignore specified properties

                switch (Type.GetTypeCode(property.PropertyType))
                {
                    case TypeCode.String:
                        property.SetValue(this, configFile.ReadString(section, property.Name));
                        break;
                    case TypeCode.Boolean:
                        property.SetValue(this, configFile.ReadBool(section, property.Name));
                        break;
                    case TypeCode.Int32:
                        property.SetValue(this, configFile.ReadInt32(section, property.Name));
                        break;
                    case TypeCode.UInt32:
                        property.SetValue(this, configFile.ReadUInt32(section, property.Name));
                        break;
                    case TypeCode.Int64:
                        property.SetValue(this, configFile.ReadInt64(section, property.Name));
                        break;
                    case TypeCode.UInt64:
                        property.SetValue(this, configFile.ReadUInt64(section, property.Name));
                        break;

                    default:
                        throw new NotImplementedException($"Value type {property.PropertyType} is not supported for config files.");
                }
            }
        }
    }
}
