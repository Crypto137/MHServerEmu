using MHServerEmu.Common.Config;
using MHServerEmu.Games.Regions;
using MHServerEmu.PlayerManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.Common.Commands
{
    internal class MapGenerationCommand
    {
        TextWriter _originalOutput;

        string RegionName() => ((RegionPrototypeId)RegionManager.RegionPrototypeIdFromCommand).ToString();

        public MapGenerationCommand() { }

        public string Execute(string[] @params)
        {
            _originalOutput = Console.Out;
            if (@params.Length < 1)
                return CommandResult("Parameters missing \n => Usage: generation map [RegionPrototypeId]");
            if (@params.Length > 2)
                return CommandResult("Too many Paramaters \n => Usage: generation map [RegionPrototypeId] (optional:SeedNumber)");

            if (!ulong.TryParse(@params[0], out RegionManager.RegionPrototypeIdFromCommand))
                return CommandResult("TegionPrototypeId is not an ulong");

            if (@params.Length == 2)
            {
                if (!int.TryParse(@params[1], out RegionManager.SeedNumberFromCommand))
                    return CommandResult("Seed is not a seed");
            }

            Console.SetOut(TextWriter.Null);
            if (RegionManager.SeedNumberFromCommand == 0)
            {
                GameManager gameManager = new();
                RegionManager.SeedNumberFromCommand = gameManager.GetAvailableGame().Random.Next();
                Log($"Seed generated : {RegionManager.SeedNumberFromCommand}");
            }

            ServerProcess();
            ClientProcess();

            return CommandResult("Process completed");
        }

        void ServerProcess()
        {
            Log("--- SERVER LOGS ---");
            RegionManager.GenerationModeFromCommand = GenerationMode.Server;
            LaunchClient();

            using (StringWriter stringWriter = new StringWriter())
            {
                Log("Wait for Marvel Heroes auth screen and log in with your credentials");
                Console.SetOut(stringWriter);
                WaitingForUser();
                WaitForXSeconds(5);
                string outputContent = stringWriter.ToString();
                WriteFile($"{RegionName()}-S-{RegionManager.SeedNumberFromCommand}.txt", outputContent);
                Console.SetOut(TextWriter.Null);
            }
            Log("Server Logs generated");
        }

        void ClientProcess()
        {
            Log("--- CLIENT LOGS ---");
            RegionManager.GenerationModeFromCommand = GenerationMode.Client;
            
            LaunchClient();
            string filePath = ConfigManager.CommandConfig.MarvelHeroesOmegaLogClientPath;

            if (File.Exists(filePath))
            {
                FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath));
                watcher.Filter = Path.GetFileName(filePath);
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Changed += OnClientLogFileChanged;
                watcher.EnableRaisingEvents = true;
            }
            else
            {
                Log($"Client Log File {filePath} not found.", true);
            }

            Log("With CheatEngine, go to MarvelHeroesOmega.exe+15F9692 and set the value 00 to 01");
            WaitingForUser();

            Log("Wait for Marvel Heroes auth screen and log in with your credentials");
            
            WaitingForUser();

            WaitForXSeconds(10);
            KillRunningClient();
        }

        void WaitForXSeconds(int seconds)
        {
            Log($"Please wait {seconds} seconds");
            Thread.Sleep(seconds * 1000);
        }

        void WriteFile(string fileName, string content)
        {
            string logFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs generated");
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);

            File.WriteAllText(Path.Combine(logFolder, fileName), content);
        }

        void Log(string message, bool isError = false)
        {
            TextWriter textWriter = Console.Out;
            Console.SetOut(_originalOutput);
            Console.ForegroundColor = isError ? ConsoleColor.Red : ConsoleColor.DarkGreen;
            Console.WriteLine(message);
            Console.SetOut(textWriter);
            SetForeground();
        }

        string CommandResult(string message)
        {
            Log(message);
            Console.SetOut(_originalOutput);
            Console.ForegroundColor = ConsoleColor.White;
            return string.Empty;
        }

        void WaitingForUser()
        {
            Log("Press Enter to continue...");
            Console.ReadLine();
        }

        bool IsClientRunning()
        {
            Process[] processes = Process.GetProcesses().Where(k => k.ProcessName.Contains("MarvelHeroesOmega")).ToArray();
            return processes.Count(k => k.MainModule.FileName.Contains("MarvelHeroesOmega.exe")) > 0;
        }

        private static void SetForeground()
        {
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("kernel32.dll")]
            static extern IntPtr GetConsoleWindow();

            IntPtr hWnd = GetConsoleWindow();

            if (hWnd != IntPtr.Zero)
                SetForegroundWindow(hWnd);
        }

        void KillRunningClient()
        {
            Process[] processes = Process.GetProcesses().Where(k => k.ProcessName.Contains("MarvelHeroesOmega")).ToArray();
            foreach (Process process in processes.Where(k => k.MainModule.FileName.Contains("MarvelHeroesOmega.exe")))
                process.Kill();
        }

        void LaunchClient()
        {
            if (IsClientRunning())
            {
                Log("Closing running Marvel Heroes Omega client");
                KillRunningClient();
            }

            string moveCommand = $"cd \"{ConfigManager.CommandConfig.MarvelHeroesOmegax86ExeFolderPath}\"";
            string launchGameCommand = "MarvelHeroesOmega.exe -robocopy -nobitraider -nosteam -log LoggingLevel=EXTRA_VERBOSE LoggingChannels=-ALL,+GAME,+GENERATION";

            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {moveCommand} & {launchGameCommand}";
            process.Start();
        }

        private void OnClientLogFileChanged(object sender, FileSystemEventArgs e)
        {
            if (RegionManager.GenerationModeFromCommand != GenerationMode.Client)
                return;

            string filePath = e.FullPath;
            try
            {
                string content = File.ReadAllText(filePath);
                WriteFile($"{RegionName()}-C-{RegionManager.SeedNumberFromCommand}.txt", content);
            }
            catch (IOException ex) { }
        }
    }
}
