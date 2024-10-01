using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SetupSorcererSupreme
{
    public partial class MainForm : Form
    {
        private enum SetupState
        {
            Invalid,
            Start,
            SelectFolder,
            Complete
        }

        private SetupState _state = SetupState.Invalid;

        public MainForm()
        {
            InitializeComponent();
            AdvanceState();
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            switch (_state)
            {
                case SetupState.Start:
                    AdvanceState();
                    return;

                case SetupState.SelectFolder:
                    (bool, string) runResult = RunSetup(folderBrowseTextBox.Text);
                    if (runResult.Item1 == false)
                    {
                        MessageBox.Show(runResult.Item2, "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    AdvanceState();
                    return;

                case SetupState.Complete:
                    Application.Exit();
                    return;
            }
        }

        private void folderBrowseButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new())
            {
                DialogResult dialogResult = dialog.ShowDialog(this);
                if (dialogResult != DialogResult.OK) return;
                folderBrowseTextBox.Text = dialog.SelectedPath;
            }
        }

        private void AdvanceState()
        {
            _state++;
            UpdateForm();
        }

        private void UpdateForm()
        {
            switch (_state)
            {
                case SetupState.Start:
                    headerLabel.Text = "Welcome to the MHServerEmu Setup Sorcerer Supreme";
                    bodyLabel.Text = "This program will help you set up MHServerEmu.\r\n\r\nTo continue, click Next.";
                    break;

                case SetupState.SelectFolder:
                    headerLabel.Text = "Marvel Heroes Files";
                    bodyLabel.Text = "Please choose the folder in which Marvel Heroes game files are located.";

                    folderBrowseTextBox.Visible = true;
                    folderBrowseButton.Visible = true;

                    break;

                case SetupState.Complete:
                    headerLabel.Text = "Setup Complete";
                    bodyLabel.Text = "Setup successful.\r\n\r\nRun StartClient.bat to launch the game normally.\r\n\r\nRun StartClientAutoLogin.bat to launch the game and automatically log in with a default account.";
                    nextButton.Text = "Exit";

                    folderBrowseTextBox.Visible = false;
                    folderBrowseButton.Visible = false;

                    break;

                default:
                    headerLabel.Text = "Invalid Setup State";
                    bodyLabel.Text = string.Empty;
                    break;
            }
        }

        #region Setup Logic

        const string ExecutableHash = "6DC9BCDB145F98E5C2D7A1F7E25AEB75507A9D1A";  // Win64 1.52.0.1700

        static readonly string CalligraphyPath = Path.Combine("Data", "Game", "Calligraphy.sip");
        static readonly string ResourcePath = Path.Combine("Data", "Game", "mu_cdata.sip");

        static (bool, string) RunSetup(string clientDir)
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

            // Find server directory
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string serverDir = assemblyDir;

            if (File.Exists(Path.Combine(serverDir, "MHServerEmu.exe")) == false)
            {
                // Try looking in the MHServerEmu subdirectory if it's not in the same directory as the setup tool
                serverDir = Path.Combine(serverDir, "MHServerEmu");
                if (File.Exists(Path.Combine(serverDir, "MHServerEmu.exe")) == false)
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

            // Create a .bat file for launching the client normally
            using (StreamWriter writer = new(Path.Combine(assemblyDir, "StartClient.bat")))
                writer.WriteLine($"@start \"\" \"{executablePath}\" -robocopy -nosteam -siteconfigurl=localhost/SiteConfig.xml");

            // Create a .bat file for launching the client with auto login
            using (StreamWriter writer = new(Path.Combine(assemblyDir, "StartClientAutoLogin.bat")))
                writer.WriteLine($"@start \"\" \"{executablePath}\" -robocopy -nosteam -siteconfigurl=localhost/SiteConfig.xml -emailaddress=test1@test.com -password=123");

            return (true, "Setup successful.");
        }

        #endregion
    }
}
