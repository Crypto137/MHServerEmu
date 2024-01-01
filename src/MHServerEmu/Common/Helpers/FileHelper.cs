using System.Reflection;
using System.Text.Json;

namespace MHServerEmu.Common.Helpers
{
    /// <summary>
    /// Makes it easier to load and save files.
    /// </summary>
    public static class FileHelper
    {
        public static readonly string ServerRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string DataDirectory = Path.Combine(ServerRoot, "Data");

        /// <summary>
        /// Deserializes an object from a JSON file located at the specified path.
        /// </summary>
        public static T DeserializeJson<T>(string path)
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
        }
    }
}
