using System.Runtime.InteropServices;
using System.Text;

namespace MHServerEmu.Common.Config
{
    public class IniFile
    {
        private const int ReadBufferSize = 255;

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder returnedString, int size, string path);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileInt(string section, string key, int defaultValue, string filePath);
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        private string _path;

        public IniFile(string path)
        {
            _path = path;
        }

        public string ReadString(string section, string key)
        {
            StringBuilder stringBuilder = new(ReadBufferSize);
            GetPrivateProfileString(section, key, "", stringBuilder, ReadBufferSize, _path);
            return stringBuilder.ToString();
        }

        public int ReadInt(string section, string key) => GetPrivateProfileInt(section, key, 0, _path);
        public bool ReadBool(string section, string key) => Convert.ToBoolean(ReadString(section, key));
        public void WriteValue(string section, string key, string value) => WritePrivateProfileString(section, key, value, _path);
    }
}
