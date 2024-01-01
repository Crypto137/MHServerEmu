namespace MHPakTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Drag and drop a .sip file on this tool to unpack it or a folder to pack it to .sip.");
                Console.ReadLine();
                return;
            }

            string path = args[0];

            if ((Directory.Exists(path) || File.Exists(path)) == false)
            {
                Console.WriteLine($"{path} is not a valid path");
                Console.ReadLine();
                return;
            }

            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                WritePak(path);
            else
                ReadPak(path);

            Console.WriteLine("Finished");
            Console.ReadLine();
        }

        private static void ReadPak(string path)
        {
            PakFile pak = new(path);
            pak.ExtractData(Path.GetDirectoryName(path));
        }

        private static void WritePak(string path)
        {
            Console.WriteLine(path);
            PakFile pak = new();
            pak.AddDirectory(path);
            pak.WritePak(Path.Combine(Directory.GetCurrentDirectory(), $"{new DirectoryInfo(path).Name}.sip"));
        }
    }
}
