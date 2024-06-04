using System.Diagnostics;
using System.Text;

namespace MHExecutableAnalyzer
{
    public class FilePathExtractor
    {
        private static readonly byte[] PathSignature = Convert.FromHexString("3A5C6D"); // :\m (from D:\mirrorBuilds\);

        private readonly List<string> _sourceFilePathList = new();

        public FilePathExtractor(byte[] data)
        {
            var stopwatch = Stopwatch.StartNew();

            // Look for our source file path signature
            Console.WriteLine();

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != PathSignature[0]) continue;  // Check the entire signature only if the first character matches

                if (PathSignature.SequenceEqual(data.Skip(i).Take(PathSignature.Length)))
                {
                    List<byte> byteList = new();

                    // Our signature contains beginning of a path after the drive letter
                    // because the letter can be both lower and upper case.
                    // We start our second loop one position before and then read bytes until
                    // we reach a null, since paths are null-terminated strings.
                    for (int j = i - 1; data[j] != 0x00; j++)
                        byteList.Add(data[j]);

                    string filePath = Encoding.UTF8.GetString(byteList.ToArray());
                    _sourceFilePathList.Add(filePath);
                    Console.WriteLine(filePath);
                }
            }

            Console.WriteLine();
            stopwatch.Stop();
            Console.WriteLine($"Found {_sourceFilePathList.Count} file path strings in {stopwatch.ElapsedMilliseconds} ms");

            // Clean up our list
            Console.WriteLine("Cleaning up the list...");

            // Sort
            _sourceFilePathList.Sort();

            // Fix directory separator chars
            for (int i = 0; i < _sourceFilePathList.Count; i++)
                _sourceFilePathList[i] = _sourceFilePathList[i].Replace('/', '\\');

            // Remove duplicates
            _sourceFilePathList = _sourceFilePathList.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Invalid file paths (in case our signature caught random junk)
            _sourceFilePathList.RemoveAll(filePath => filePath.Contains("MarvelGame", StringComparison.OrdinalIgnoreCase) == false);

            Console.WriteLine($"Found {_sourceFilePathList.Count} unique file paths");
        }

        public void SaveSourceFilePathList(string path)
        {
            Console.WriteLine($"Saving file path list to {path}...");
            File.WriteAllLines(path, _sourceFilePathList);
        }

        public void RecreateFileStructure(string outputDirectoryPath)
        {
            foreach (string sourceFilePath in _sourceFilePathList)
            {
                int startIndex = sourceFilePath.IndexOf("MarvelGame", StringComparison.OrdinalIgnoreCase);
                string outputFilePath = Path.Combine(outputDirectoryPath, sourceFilePath[startIndex..]);
                outputFilePath = outputFilePath.Split('?')[0];  // Some source file paths contain additional info after a ? sign
                string outputFilePathDirectory = Path.GetDirectoryName(outputFilePath);

                if (Directory.Exists(outputFilePathDirectory) == false)
                    Directory.CreateDirectory(outputFilePathDirectory);

                // Create an empty file for the path
                try
                {
                    File.WriteAllText(outputFilePath, string.Empty);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message} ({outputFilePath})");
                }
            }
        }
    }
}
