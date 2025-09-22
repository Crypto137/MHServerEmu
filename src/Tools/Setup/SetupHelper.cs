using System.Security.Cryptography;

namespace Setup
{
    public enum SetupResult
    {
        Success,
        InvalidFilePath,
        ClientNotFound,
        ClientVersionMismatch,
        ClientDataNotFound,
        ServerNotFound
    }

    internal static class SetupHelper
    {
        private const string ExecutableHash = "6DC9BCDB145F98E5C2D7A1F7E25AEB75507A9D1A";  // Win64 1.52.0.1700

        private const string DefaultAccountEmail = "test1@test.com";
        private const string DefaultAccountPassword = "123";

        private static readonly string CalligraphyPath = Path.Combine("Data", "Game", "Calligraphy.sip");
        private static readonly string ResourcePath = Path.Combine("Data", "Game", "mu_cdata.sip");

        /// <summary>
        /// Sets up MHServerEmu using the client in the specified directory.
        /// </summary>
        public static SetupResult RunSetup(string clientRootDirectory)
        {
            // Validate directory path
            if (string.IsNullOrWhiteSpace(clientRootDirectory))
                return SetupResult.InvalidFilePath;

            // Find and verify the client executable
            if (FindClientExecutablePath(clientRootDirectory, out string clientDirectory, out string clientExecutablePath) == false)
                return SetupResult.ClientNotFound;           

            byte[] executableData = File.ReadAllBytes(clientExecutablePath);
            string executableHash = Convert.ToHexString(SHA1.HashData(executableData));

            if (ExecutableHash != executableHash)
                return SetupResult.ClientVersionMismatch;

            // Verify data files
            string clientCalligraphyPath = Path.Combine(clientDirectory, CalligraphyPath);
            string clientResourcePath = Path.Combine(clientDirectory, ResourcePath);

            if (File.Exists(clientCalligraphyPath) == false || File.Exists(clientResourcePath) == false)
                return SetupResult.ClientDataNotFound;

            // Find the server executable
            string serverRootDirectory = Path.GetDirectoryName(AppContext.BaseDirectory);
            if (FindServerExecutablePath(serverRootDirectory, out string serverDirectory, out string serverExecutablePath) == false)
                return SetupResult.ServerNotFound;

            // Create server data directory if needed
            string serverDataDir = Path.Combine(serverDirectory, "Data", "Game");
            if (Directory.Exists(serverDataDir) == false)
                Directory.CreateDirectory(serverDataDir);

            // Copy data files
            string serverCalligraphyPath = Path.Combine(serverDirectory, CalligraphyPath);
            string serverResourcePath = Path.Combine(serverDirectory, ResourcePath);

            if (File.Exists(serverCalligraphyPath) == false)
                File.Copy(clientCalligraphyPath, serverCalligraphyPath);

            if (File.Exists(serverResourcePath) == false)
                File.Copy(clientResourcePath, serverResourcePath);

            CreateBatFiles(serverRootDirectory, clientExecutablePath, serverExecutablePath);

            return SetupResult.Success;
        }

        /// <summary>
        /// Returns the text message for the specified <see cref="SetupResult"/>.
        /// </summary>
        public static string GetResultText(SetupResult result)
        {
            // TODO: translations
            return result switch
            {
                SetupResult.Success =>                  "Setup successful.",
                SetupResult.InvalidFilePath =>          "Invalid file path.",
                SetupResult.ClientNotFound =>           "Marvel Heroes game client not found.",
                SetupResult.ClientVersionMismatch =>    "Game client version mismatch. Please make sure you have version 1.52.0.1700.",
                SetupResult.ClientDataNotFound =>       "Game data files are missing. Please reinstall the game client.",
                SetupResult.ServerNotFound =>           "MHServerEmu not found.",
                _ =>                                    "Unknown error.",
            };
        }

        /// <summary>
        /// Searches for the game client in the specified directory.
        /// </summary>
        private static bool FindClientExecutablePath(string rootDirectory, out string clientDirectory, out string clientExecutablePath)
        {
            // Check if we are in the client root directory
            clientDirectory = rootDirectory;
            clientExecutablePath = Path.Combine(clientDirectory, "UnrealEngine3", "Binaries", "Win64", "MarvelHeroesOmega.exe");

            if (File.Exists(clientExecutablePath))
                return true;

            // Check if we are in the executable directory instead of the client root
            clientExecutablePath = Path.Combine(rootDirectory, "MarvelHeroesOmega.exe");
            if (File.Exists(clientExecutablePath))
            {
                // Adjust client directory
                clientDirectory = Path.GetFullPath(Path.Combine(rootDirectory, "..", "..", ".."));
                return true;
            }

            // Not found
            return false;
        }

        /// <summary>
        /// Searches for MHServerEmu in the specified directory.
        /// </summary>
        private static bool FindServerExecutablePath(string rootDirectory, out string serverDirectory, out string serverExecutablePath)
        {
            serverDirectory = rootDirectory;

            serverExecutablePath = Path.Combine(serverDirectory, "MHServerEmu.exe");
            if (File.Exists(serverExecutablePath) == false)
            {
                // Try looking in the MHServerEmu subdirectory if it's not in the same directory as the setup tool
                serverDirectory = Path.Combine(serverDirectory, "MHServerEmu");
                serverExecutablePath = Path.Combine(serverDirectory, "MHServerEmu.exe");

                return File.Exists(serverExecutablePath);
            }

            return true;
        }

        /// <summary>
        /// Creates .bat files required for managing the server and the client.
        /// </summary>
        private static void CreateBatFiles(string rootDirectory, string clientExecutablePath, string serverExecutablePath)
        {
            string relativeServerExecutablePath = Path.GetRelativePath(rootDirectory, serverExecutablePath);

            // Launching the client normally
            using (StreamWriter writer = new(Path.Combine(rootDirectory, "StartClient.bat")))
                writer.WriteLine($"@start \"\" \"{clientExecutablePath}\" -robocopy -nosteam -siteconfigurl=localhost/SiteConfig.xml");

            // Launching the client with auto-login
            using (StreamWriter writer = new(Path.Combine(rootDirectory, "StartClientAutoLogin.bat")))
                writer.WriteLine($"@start \"\" \"{clientExecutablePath}\" -robocopy -nosteam -siteconfigurl=localhost/SiteConfig.xml -emailaddress={DefaultAccountEmail} -password={DefaultAccountPassword}");

            // Starting servers
            using (StreamWriter writer = new(Path.Combine(rootDirectory, "StartServer.bat")))
            {
                // We now use %~dp0 instead of %cd% because the current directory may not necessarily be the one where the bat file is located.
                writer.WriteLine("@echo off");
                writer.WriteLine($"set APACHE_SERVER_ROOT={Path.Combine("%~dp0", "Apache24")}");
                writer.WriteLine($"start /min \"\" \"{Path.Combine("%APACHE_SERVER_ROOT%", "bin", "httpd.exe")}\"");
                writer.WriteLine($"start \"\" \"{Path.Combine("%~dp0", relativeServerExecutablePath)}\"");
            }

            // Stopping servers
            using (StreamWriter writer = new(Path.Combine(rootDirectory, "StopServer.bat")))
            {
                writer.WriteLine("@echo off");
                writer.WriteLine("taskkill /f /im MHServerEmu.exe");
                writer.WriteLine("taskkill /f /im httpd.exe");
            }
        }
    }
}
