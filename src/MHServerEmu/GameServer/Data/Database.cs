using MHServerEmu.Common;

namespace MHServerEmu.GameServer.Data
{
    public static class Database
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool IsInitialized { get; private set; }
        public static ulong[] PrototypeTable { get; private set; }

        static Database()
        {
            Logger.Info("Loading prototype table...");
            PrototypeTable = LoadPrototypeTable($"{Directory.GetCurrentDirectory()}\\Assets\\PrototypeTable1.bin");

            if (PrototypeTable.Length > 0)
            {
                // -1 is here because the first entry is 0 to offset values to align with the data we get
                Logger.Info($"Loaded {PrototypeTable.Length - 1} prototype ids");
                IsInitialized = true;
            }
            else
            {
                Logger.Error("Failed to initialize");
                IsInitialized = false;
            }
        }

        private static ulong[] LoadPrototypeTable(string path)
        {
            if (File.Exists(path))
            {
                using (MemoryStream memoryStream = new(File.ReadAllBytes(path)))
                using (BinaryReader binaryReader = new(memoryStream))
                {
                    ulong[] prototypes = new ulong[memoryStream.Length / 8];

                    for (int i = 0; i < prototypes.Length; i++)
                    {
                        prototypes[i] = binaryReader.ReadUInt64();

                        if (prototypes[i] == 12534955053251630387 || prototypes[i] == 609524195563675455 || prototypes[i] == 684619884231794915)
                        {
                            Logger.Debug($"Found {prototypes[i]} (index {i})");
                        }
                    }

                    return prototypes;
                }
            }
            else
            {
                Logger.Error($"Failed to locate {Path.GetFileName(path)}");
                return Array.Empty<ulong>();
            }
        }
    }
}
