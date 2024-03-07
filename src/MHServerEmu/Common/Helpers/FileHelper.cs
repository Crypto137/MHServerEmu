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
        /// Deserializes a <typeparamref name="T"/> from a JSON file located at the specified path.
        /// </summary>
        public static T DeserializeJson<T>(string path, JsonSerializerOptions options = null)
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), options);
        }

        /// <summary>
        /// Serializes a <typeparamref name="T"/> to JSON and saves it to the specified path.
        /// </summary>
        public static void SerializeJson<T>(string path, T @object, JsonSerializerOptions options = null)
        {
            string dirName = Path.GetDirectoryName(path);
            if (Directory.Exists(dirName) == false)
                Directory.CreateDirectory(dirName);

            string json = JsonSerializer.Serialize(@object, options);
            File.WriteAllText(path, json);
        }
    }
}
