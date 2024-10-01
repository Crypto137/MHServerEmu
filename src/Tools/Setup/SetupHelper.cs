using System.Reflection;
using System.Security.Cryptography;

namespace Setup
{
    internal static class SetupHelper
    {
        private const string ExecutableHash = "6DC9BCDB145F98E5C2D7A1F7E25AEB75507A9D1A";  // Win64 1.52.0.1700

        private static readonly string CalligraphyPath = Path.Combine("Data", "Game", "Calligraphy.sip");
        private static readonly string ResourcePath = Path.Combine("Data", "Game", "mu_cdata.sip");

        /// <summary>
        /// Sets up MHServerEmu using the client in the specified directory.
        /// </summary>
        public static (bool, string) RunSetup(string clientDir)
        {
            // Validate directory path
            if (string.IsNullOrWhiteSpace(clientDir))
                return (false, "Invalid file path.");

            // Validate game executable
            string clientExecutablePath = Path.Combine(clientDir, "UnrealEngine3", "Binaries", "Win64", "MarvelHeroesOmega.exe");
            if (File.Exists(clientExecutablePath) == false)
                return (false, "Marvel Heroes game files not found.");

            byte[] executableData = File.ReadAllBytes(clientExecutablePath);
            string executableHash = Convert.ToHexString(SHA1.HashData(executableData));

            if (ExecutableHash != executableHash)
                return (false, "Game version mismatch. Make sure you have version 1.52.0.1700.");

            // Validate data files
            string clientCalligraphyPath = Path.Combine(clientDir, CalligraphyPath);
            string clientResourcePath = Path.Combine(clientDir, ResourcePath);

            if (File.Exists(clientCalligraphyPath) == false || File.Exists(clientResourcePath) == false)
                return (false, "Game data files are missing. Please reinstall the game.");

            // Find server directory
            string rootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string serverDir = rootDirectory;

            string serverExecutablePath = Path.Combine(serverDir, "MHServerEmu.exe");
            if (File.Exists(serverExecutablePath) == false)
            {
                // Try looking in the MHServerEmu subdirectory if it's not in the same directory as the setup tool
                serverDir = Path.Combine(serverDir, "MHServerEmu");
                serverExecutablePath = Path.Combine(serverDir, "MHServerEmu.exe");
                if (File.Exists(serverExecutablePath) == false)
                    return (false, "Failed to find MHServerEmu.");
            }

            // Create server data directory if needed
            string serverDataDir = Path.Combine(serverDir, "Data", "Game");
            if (Directory.Exists(serverDataDir) == false)
                Directory.CreateDirectory(serverDataDir);

            // Copy data files
            string serverCalligraphyPath = Path.Combine(serverDir, CalligraphyPath);
            string serverResourcePath = Path.Combine(serverDir, ResourcePath);

            if (File.Exists(serverCalligraphyPath) == false)
                File.Copy(clientCalligraphyPath, serverCalligraphyPath);

            if (File.Exists(serverResourcePath) == false)
                File.Copy(clientResourcePath, serverResourcePath);

            CreateBatFiles(rootDirectory, serverExecutablePath, clientExecutablePath);

            return (true, "Setup successful.");
        }

        /// <summary>
        /// Creates .bat files required for managing the server and the client.
        /// </summary>
        private static void CreateBatFiles(string rootDirectory, string serverExecutablePath, string clientExecutablePath)
        {
            string relativeServerExecutablePath = Path.GetRelativePath(rootDirectory, serverExecutablePath);

            // Starting servers
            using (StreamWriter writer = new(Path.Combine(rootDirectory, "StartServers.bat")))
            {
                writer.WriteLine("@echo off");
                writer.WriteLine($"set APACHE_SERVER_ROOT={Path.Combine("%cd%", "Apache24")}");
                writer.WriteLine($"start /min \"\" \"{Path.Combine("%APACHE_SERVER_ROOT%", "bin", "httpd.exe")}\"");
                writer.WriteLine($"start \"\" \"{Path.Combine("%cd%", relativeServerExecutablePath)}\"");
            }

            // Stopping servers
            using (StreamWriter writer = new(Path.Combine(rootDirectory, "StopServers.bat")))
            {
                writer.WriteLine("@echo off");
                writer.WriteLine("taskkill /f /im MHServerEmu.exe");
                writer.WriteLine("taskkill /f /im httpd.exe");
            }

            // Launching the client normally
            using (StreamWriter writer = new(Path.Combine(rootDirectory, "StartClient.bat")))
                writer.WriteLine($"@start \"\" \"{clientExecutablePath}\" -robocopy -nosteam -siteconfigurl=localhost/SiteConfig.xml");

            // Launching the client with auto-login
            using (StreamWriter writer = new(Path.Combine(rootDirectory, "StartClientAutoLogin.bat")))
                writer.WriteLine($"@start \"\" \"{clientExecutablePath}\" -robocopy -nosteam -siteconfigurl=localhost/SiteConfig.xml -emailaddress=test1@test.com -password=123");
        }
    }
}
