using System.Reflection;
using System.Security.Cryptography;

namespace SetupSorcererSupreme
{
    internal static class Program
    {
        const string WindowTitle = "MHServerEmu Setup Sorcerer Supreme";
        const string ExecutableHash = "6DC9BCDB145F98E5C2D7A1F7E25AEB75507A9D1A";  // Win64 1.52.0.1700

        static readonly string CalligraphyPath = Path.Combine("Data", "Game", "Calligraphy.sip");
        static readonly string ResourcePath = Path.Combine("Data", "Game", "mu_cdata.sip");
        static readonly string ConfigPath = Path.Combine("Data", "Configs", "ClientConfig.xml");

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ShowMessage("Please choose the folder in which Marvel Heroes game files are located.");

            using (FolderBrowserDialog dialog = new())
            {
                DialogResult dialogResult = dialog.ShowDialog();
                if (dialogResult != DialogResult.OK) return;
                (bool, string) runResult = Run(dialog.SelectedPath);
                ShowMessage(runResult.Item2, runResult.Item1 ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
        }

        static void ShowMessage(string message, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            MessageBox.Show(message, WindowTitle, MessageBoxButtons.OK, icon,
                MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
        }

        static (bool, string) Run(string clientDir)
        {
            // Validate directory path
            if (string.IsNullOrWhiteSpace(clientDir))
                return (false, "Invalid file path.");

            // Validate game executable
            string executablePath = Path.Combine(clientDir, "UnrealEngine3", "Binaries", "Win64", "MarvelHeroesOmega.exe");
            if (File.Exists(executablePath) == false)
                return (false, "Marvel Heroes game files not found.");

            byte[] executableData = File.ReadAllBytes(executablePath);
            string executableHash = Convert.ToHexString(SHA1.HashData(executableData));

            if (ExecutableHash != executableHash)
                return (false, "Game version mismatch. Make sure you have version 1.52.0.1700.");

            // Validate data files
            string clientCalligraphyPath = Path.Combine(clientDir, CalligraphyPath);
            string clientResourcePath = Path.Combine(clientDir, ResourcePath);

            if (File.Exists(clientCalligraphyPath) == false || File.Exists(clientResourcePath) == false)
                return (false, "Game data files are missing. Please reinstall the game.");

            // Create server data directory if needed
            string serverDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
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

            // Create .bat file for launching the client
            using (StreamWriter writer = new(Path.Combine(serverDir, "StartClient.bat")))
                writer.WriteLine($"@start \"\" \"{executablePath}\" -robocopy -nosteam -siteconfigurl=localhost/SiteConfig.xml");

            return (true, "Setup successful. Run StartClient.bat to launch the game.");
        }
    }
}