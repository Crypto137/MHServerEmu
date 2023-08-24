using IniParser;
using IniParser.Model;

namespace MHServerEmu.Common.Config
{
    public class IniFile
    {
        private string _path;
        private FileIniDataParser _parser;
        private IniData _iniData;

        public IniFile(string path)
        {
            _path = path;
            _parser = new();
            _iniData = _parser.ReadFile(_path);
        }

        public string ReadString(string section, string key) => _iniData[section][key];
        public int ReadInt(string section, string key) => Convert.ToInt32(_iniData[section][key]);
        public bool ReadBool(string section, string key) => Convert.ToBoolean(ReadString(section, key));

        public void WriteValue(string section, string key, string value)
        {
            _iniData[section][key] = value;
            _parser.WriteFile(_path, _iniData);
        }
    }
}
