namespace MHExecutableAnalyzer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: MHExecutableAnalyzer.exe [ExePath] [Params]");
                Console.WriteLine("Params:");
                Console.WriteLine("-rfs: Recreate file structure");
                Console.ReadLine();
                return;
            }

            ExecutableLoader exeLoader = new(args[0]);

            if (exeLoader.IsValid == false)
            {
                Console.ReadLine();
                return;
            }

            // Extract params from arguments
            string[] @params = Array.Empty<string>();
            if (args.Length > 1)
            {
                @params = new string[args.Length - 1];
                Array.Copy(args, 1, @params, 0, @params.Length);
            }

            FilePathExtractor filePathExtractor = new(exeLoader.Data);
            filePathExtractor.SaveSourceFilePathList(Path.Combine(Directory.GetCurrentDirectory(), $"SourceFilePaths_{Path.GetFileNameWithoutExtension(args[0])}_{exeLoader.Version}.txt"));

            // Recreate file structure if needed
            if (@params.Contains("-rfs", StringComparer.OrdinalIgnoreCase))
                filePathExtractor.RecreateFileStructure(Path.Combine(Directory.GetCurrentDirectory(), "FileStructure"));

            Console.WriteLine("Finished");
            Console.ReadLine();
        }
    }
}
