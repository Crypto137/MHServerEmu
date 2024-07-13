using System.Globalization;
using IniParser;
using IniParser.Model;

namespace MHServerEmu.Core.Config
{
    /// <summary>
    /// A wrapper for reading and writing ini files.
    /// </summary>
    public class IniFile
    {
        private readonly FileIniDataParser _parser;
        private readonly string _path;
        private readonly IniData _iniData;

        /// <summary>
        /// Constructs a new <see cref="IniFile"/> instance for the specified path.
        /// </summary>
        public IniFile(string path)
        {
            _path = path;
            _parser = new();

            try
            {
                _iniData = File.Exists(_path) ? _parser.ReadFile(_path) : new();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Gets the value with the specified key from the specified section of this <see cref="IniFile"/> as <see cref="string"/>.
        /// </summary>
        public string GetString(string section, string key) => _iniData[section][key];

        /// <summary>
        /// Gets the value with the specified key from the specified section of this <see cref="IniFile"/> as <see cref="bool"/>.
        /// </summary>
        public bool? GetBool(string section, string key)
        {
            string stringValue = GetString(section, key);

            if (bool.TryParse(stringValue, out bool parsedValue) == false)
                return null;

            return parsedValue;
        }

        /// <summary>
        /// Gets the value with the specified key from the specified section of this <see cref="IniFile"/> as <see cref="int"/>.
        /// </summary>
        public int? GetInt32(string section, string key)
        {
            string stringValue = GetString(section, key);

            if (int.TryParse(stringValue, out int parsedValue) == false)
                return null;

            return parsedValue;
        }

        /// <summary>
        /// Gets the value with the specified key from the specified section of this <see cref="IniFile"/> as <see cref="uint"/>.
        /// </summary>
        public uint? GetUInt32(string section, string key)
        {
            string stringValue = GetString(section, key);

            if (uint.TryParse(stringValue, out uint parsedValue) == false)
                return null;

            return parsedValue;
        }

        /// <summary>
        /// Gets the value with the specified key from the specified section of this <see cref="IniFile"/> as <see cref="long"/>.
        /// </summary>
        public long? GetInt64(string section, string key)
        {
            string stringValue = GetString(section, key);

            if (long.TryParse(stringValue, out long parsedValue) == false)
                return null;

            return parsedValue;
        }

        /// <summary>
        /// Gets the value with the specified key from the specified section of this <see cref="IniFile"/> as <see cref="ulong"/>.
        /// </summary>
        public ulong? GetUInt64(string section, string key)
        {
            string stringValue = GetString(section, key);

            if (ulong.TryParse(stringValue, out ulong parsedValue) == false)
                return null;

            return parsedValue;
        }

        /// <summary>
        /// Gets the value with the specified key from the specified section of this <see cref="IniFile"/> as <see cref="float"/>.
        /// </summary>
        public float? GetSingle(string section, string key)
        {
            string stringValue = GetString(section, key);

            if (float.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue) == false)
                return null;

            return parsedValue;
        }

        /// <summary>
        /// Writes an <see cref="object"/> value to this <see cref="IniFile"/> using its ToString() representation.
        /// </summary>
        public void WriteValue(string section, string key, object value)
        {
            _iniData[section][key] = value.ToString();
            _parser.WriteFile(_path, _iniData);
        }
    }
}
