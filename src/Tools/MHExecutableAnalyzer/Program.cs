namespace MHExecutableAnalyzer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Drag and drop a Marvel Heroes executable on this tool to analyze it.");
                Console.ReadLine();
                return;
            }

            ExecutableLoader exeLoader = new(args[0]);

            if (exeLoader.IsValid == false)
            {
                Console.ReadLine();
                return;
            }

            FilePathExtractor filePathExtractor = new(exeLoader.Data);
            filePathExtractor.SaveSourceFilePathList(Path.Combine(Directory.GetCurrentDirectory(), $"SourceFilePaths_{Path.GetFileNameWithoutExtension(args[0])}_{exeLoader.Version}.txt"));

            Console.WriteLine("Finished");
            Console.ReadLine();
        }
    }
}
