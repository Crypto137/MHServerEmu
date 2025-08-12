using MHServerEmu.Core.Logging;
using System.Reflection;
using System.Text.Json;

namespace MHServerEmu.Core.Helpers
{
    /// <summary>
    /// Makes it easier to load and save files.
    /// </summary>
    public static class FileHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public const string FileNameDateFormat = "yyyy-MM-dd_HH.mm.ss";

        public static readonly string ServerRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string DataDirectory = Path.Combine(ServerRoot, "Data");

        /// <summary>
        /// Returns a path relative to server root directory.
        /// </summary>
        public static string GetRelativePath(string filePath)
        {
            return Path.GetRelativePath(ServerRoot, filePath);
        }

        /// <summary>
        /// Returns all files that have the specified prefix in a directory.
        /// Optionally returns only files with the specified extension.
        /// </summary>
        /// <remarks>
        /// Case-sensitivity of the arguments depends on the operating system.
        /// </remarks>
        public static string[] GetFilesWithPrefix(string path, string prefix, string extension = "*")
        {
            string[] files;

            try
            {
                string searchPattern = $"{prefix}*.{extension}";
                files = Directory.GetFiles(path, searchPattern);
                Array.Sort(files);  // sort for consistency (alphabetical order)
            }
            catch (Exception e)
            {
                Logger.Warn($"GetFilesWithPrefix(): Failed to get files from path {path} - {e.Message}");
                files = Array.Empty<string>();
            }

            return files;
        }

        /// <summary>
        /// Deserializes a <typeparamref name="T"/> from a JSON file located at the specified path.
        /// </summary>
        public static T DeserializeJson<T>(string path, JsonSerializerOptions options = null)
        {
            try
            {
                string json = File.ReadAllText(path);
                T data = JsonSerializer.Deserialize<T>(json, options);
                return data;
            }
            catch (Exception e)
            {
                return Logger.WarnReturn<T>(default, $"DeserializeJson(): Failed to deserialize {path} as {typeof(T).Name} - {e.Message}");
            }
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

        /// <summary>
        /// Saves the provided <see cref="string"/> to a text file in the server root directory.
        /// </summary>
        public static void SaveTextFileToRoot(string fileName, string text)
        {
            File.WriteAllText(Path.Combine(ServerRoot, fileName), text);
        }

        /// <summary>
        /// Creates a backup of the specified file.
        /// </summary>
        public static bool CreateFileBackup(string sourceFilePath, int maxBackups)
        {
            if (PrepareFileBackup(sourceFilePath, maxBackups, out string backupFilePath) == false)
                return false;

            File.Copy(sourceFilePath, backupFilePath);
            return true;
        }

        /// <summary>
        /// Moves and deletes backups of the specified file in a circular buffer fashion.
        /// </summary>
        public static bool PrepareFileBackup(string sourceFilePath, int maxBackups, out string backupFilePath)
        {
            backupFilePath = null;

            if (maxBackups == 0)
                return false;

            if (File.Exists(sourceFilePath) == false)
                return Logger.WarnReturn(false, $"PrepareFileBackup(): File not found at {sourceFilePath}");

            // Cache backup file names for reuse.
            // NOTE: We can also reuse the same string array for multiple calls of this function,
            // but it's probably not going to be called often enough to be worth it.
            string[] backupPaths = new string[maxBackups];

            // Look for a free backup index
            int freeIndex = -1;
            for (int i = 0; i < maxBackups; i++)
            {
                // Backup path strings are created on demand so that we don't end up creating
                // a lot of unneeded strings when we don't have a lot of backup files.
                backupPaths[i] = $"{sourceFilePath}.bak{i}";

                if (File.Exists(backupPaths[i]) == false)
                {
                    freeIndex = i;
                    break;
                }
            }

            // Delete the oldest backup if there are no free spots.
            if (freeIndex == -1)
            {
                freeIndex = maxBackups - 1;
                File.Delete(backupPaths[freeIndex]);
            }

            // Move files to the right until we free up index 0.
            for (int i = freeIndex - 1; i >= 0; i--)
                File.Move(backupPaths[i], backupPaths[i + 1]);

            // Path 0 should be free now.
            if (File.Exists(backupPaths[0]))
                return Logger.WarnReturn(false, $"CreateFileBackup(): Backup file path is not free {backupPaths[0]}");

            backupFilePath = backupPaths[0];
            return true;
        }
    }
}
