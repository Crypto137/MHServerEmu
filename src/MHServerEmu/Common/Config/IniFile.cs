using IniParser;
using IniParser.Model;

namespace MHServerEmu.Common.Config
{
    /// <summary>
    /// A wrapper for reading and writing ini files.
    /// </summary>
    public class IniFile
    {
        private readonly FileIniDataParser _parser;
        private readonly string _path;
        private readonly IniData _iniData;

        public IniFile(string path)
        {
            try
            {
                _path = path;
                _parser = new();
                _iniData = _parser.ReadFile(_path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Reads a value from the ini file as <see cref="string"/>.
        /// </summary>
        public string ReadString(string section, string key) => _iniData[section][key];

        /// <summary>
        /// Reads a value from the ini file as <see cref="bool"/>.
        /// </summary>
        public bool ReadBool(string section, string key) => Convert.ToBoolean(ReadString(section, key));

        /// <summary>
        /// Reads a value from the ini file as <see cref="int"/>.
        /// </summary>
        public int ReadInt32(string section, string key) => Convert.ToInt32(ReadString(section, key));

        /// <summary>
        /// Reads a value from the ini file as <see cref="uint"/>.
        /// </summary>
        public uint ReadUInt32(string section, string key) => Convert.ToUInt32(ReadString(section, key));

        /// <summary>
        /// Reads a value from the ini file as <see cref="long"/>.
        /// </summary>
        public long ReadInt64(string section, string key) => Convert.ToInt64(ReadString(section, key));

        /// <summary>
        /// Reads a value from the ini file as <see cref="ulong"/>.
        /// </summary>
        public ulong ReadUInt64(string section, string key) => Convert.ToUInt64(ReadString(section, key));

        /// <summary>
        /// Writes an <see cref="object"/> value to the ini file using ToString() representation.
        /// </summary>
        public void WriteValue(string section, string key, object value)
        {
            _iniData[section][key] = value.ToString();
            _parser.WriteFile(_path, _iniData);
        }
    }
}
