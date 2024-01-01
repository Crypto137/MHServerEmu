using MHServerEmu.Common.Helpers;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.GameData
{
    public enum PakFileId
    {
        Default,        // mu_cdata.sip
        Calligraphy     // Introduced in 1.31, in versions prior to that everything is stored in a single pak
    }

    /// <summary>
    /// A singleton that loads and provides access to pak data files.
    /// </summary>
    public class PakFileSystem
    {
        private static readonly string PakDirectory = Path.Combine(FileHelper.DataDirectory, "GPAK");
        private static readonly string[] PakFilePaths = new string[]
        {
            Path.Combine(PakDirectory, "mu_cdata.sip"),
            Path.Combine(PakDirectory, "Calligraphy.sip")
        };

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PakFile[] _paks = new PakFile[PakFilePaths.Length];

        public static PakFileSystem Instance { get; } = new();

        private PakFileSystem() { }

        /// <summary>
        /// Loads and initializes pak files from disk.
        /// </summary>
        public bool Initialize()
        {
            for (int i = 0; i < PakFilePaths.Length; i++)
            {
                if (File.Exists(PakFilePaths[i]) == false)
                {
                    Logger.Fatal($"mu_cdata.sip and/or Calligraphy.sip are missing! Make sure you copied these files to {PakDirectory}.");
                    return false;
                }

                _paks[i] = new PakFile(PakFilePaths[i]);
            }

            return true;
        }

        /// <summary>
        /// Returns a stream of decompressed pak data from the specified pak file.
        /// </summary>
        public MemoryStream LoadFromPak(string filePath, PakFileId pakId)
        {
            return _paks[(int)pakId].LoadFileDataInPak(filePath);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> collection of resource file paths with the specified prefix.
        /// </summary>
        public IEnumerable<string> GetResourceFiles(string prefix)
        {
            // Note: the original implementation iterates through all paks rather than
            // just going straight for the default one.
            return _paks[(int)PakFileId.Default].GetFilesFromPak(prefix);
        }
    }
}
