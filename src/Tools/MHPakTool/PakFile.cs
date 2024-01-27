using K4os.Compression.LZ4;

namespace MHPakTool
{
    public class PakFile
    {
        private const uint Signature = 1196441931;  // KAPG
        private const uint Version = 1;

        private readonly Dictionary<string, byte[]> _fileDict = new();

        public List<PakEntry> EntryList { get; private set; } = new();

        /// <summary>
        /// Read a pak file from the specified path.
        /// </summary>
        public PakFile(string pakFilePath)
        {
            // Make sure the specified file exists
            if (File.Exists(pakFilePath) == false)
            {
                Console.WriteLine($"{Path.GetFileName(pakFilePath)} not found");
                return;
            }

            // Read pak file
            using (FileStream stream = File.OpenRead(pakFilePath))
            using (BinaryReader reader = new(stream))
            {
                // Read file header
                uint signature = reader.ReadUInt32();
                if (signature != Signature)
                {
                    Console.WriteLine($"Invalid pak file signature {signature}, expected {Signature}");
                    return;
                }

                uint version = reader.ReadUInt32();
                if (version != Version)
                {
                    Console.WriteLine($"Invalid pak file version {version}, expected {Version}");
                    return;
                }

                // Read all entries
                int numEntries = reader.ReadInt32();
                for (int i = 0; i < numEntries; i++)
                    EntryList.Add(new(reader));

                // Read and decompress the actual data
                foreach (PakEntry entry in EntryList)
                {
                    entry.CompressedData = new byte[entry.CompressedSize];
                    entry.UncompressedData = new byte[entry.UncompressedSize];

                    stream.Read(entry.CompressedData, 0, entry.CompressedSize);
                    LZ4Codec.Decode(entry.CompressedData, 0, entry.CompressedSize,
                        entry.UncompressedData, 0, entry.UncompressedData.Length);

                    _fileDict.Add(entry.FilePath, entry.UncompressedData);  // Add data lookup
                }
            }

            Console.WriteLine($"Loaded {EntryList.Count} entries from {Path.GetFileName(pakFilePath)}");
        }

        /// <summary>
        /// Create a new empty pak file.
        /// </summary>
        public PakFile() { }

        public byte[] GetFile(string filePath)
        {
            if (_fileDict.TryGetValue(filePath, out var file) == false)
            {
                Console.WriteLine($"File {filePath} not found");
                return Array.Empty<byte>();
            }

            return file;
        }

        public void ExtractData(string outputDirectory)
        {
            Console.WriteLine("Extracting pak data...");

            foreach (PakEntry entry in EntryList)
            {
                Console.WriteLine($"Extracting {entry.FilePath}...");

                string filePath = Path.Combine(outputDirectory, entry.FilePath);
                string directory = Path.GetDirectoryName(filePath);                 // Paks have their own directory structure that we need to keep in mind.

                if (Directory.Exists(directory) == false) Directory.CreateDirectory(directory);
                File.WriteAllBytes(filePath, entry.UncompressedData);
            }
        }

        public void AddDirectory(string path)
        {
            Console.WriteLine($"Adding files from {path}...");

            string root = Path.GetFullPath(Path.Combine(path, ".."));   // Get root of the directory that's being added

            int start = EntryList.Count;

            // Iterate through all files
            foreach (string file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');  // Use forward slashes, same as original pak files
                Console.WriteLine($"Adding {relativePath}...");
                EntryList.Add(new(relativePath, File.ReadAllBytes(file)));
            }

            Console.WriteLine($"Added {EntryList.Count - start} files");
        }

        public void WritePak(string filePath)
        {
            Console.WriteLine($"Writing pak to {filePath}...");

            // Sort by hash
            EntryList = EntryList.OrderBy(o => o.FileHash).ToList();

            // Set offsets
            int offset = 0;
            foreach (PakEntry entry in EntryList)
            {
                entry.Offset = offset;
                offset += entry.CompressedSize;
            }

            using (FileStream stream = File.OpenWrite(filePath))
            using (BinaryWriter writer = new(stream))
            {
                writer.Write(Signature);
                writer.Write(Version);
                writer.Write(EntryList.Count);

                foreach (PakEntry entry in EntryList)
                    entry.WriteMetadata(writer);

                foreach (PakEntry entry in EntryList)
                    writer.Write(entry.CompressedData);
            }
        }
    }
}
