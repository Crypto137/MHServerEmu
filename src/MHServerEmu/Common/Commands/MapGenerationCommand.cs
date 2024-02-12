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
        TextWriter _originalOutput = null;

        string RegionName() => ((RegionPrototypeId)RegionManager.RegionPrototypeIdFromCommand).ToString();

        public MapGenerationCommand() { }

        public string Execute(string[] @params)
        {
            if (_originalOutput == null)
                _originalOutput = Console.Out;
            Console.SetOut(TextWriter.Null);

            RegionManager.ClearRegionDict();

            if (@params.Length < 1)
                return CommandResult("Parameters missing \n => Usage: generation map [RegionPrototypeId]");
            if (@params.Length > 2)
                return CommandResult("Too many Paramaters \n => Usage: generation map [RegionPrototypeId] (optional:SeedNumber)");

            List<ulong> regions = new List<ulong>();

            if (@params[0] == "all")
            {
                foreach (RegionPrototypeId regionPrototypeId in Enum.GetValues(typeof(RegionPrototypeId)))
                    regions.Add((ulong)regionPrototypeId);
            }
            else
            {
                if (!ulong.TryParse(@params[0], out RegionManager.RegionPrototypeIdFromCommand))
                    return CommandResult("TegionPrototypeId is not an ulong");

                regions.Add(RegionManager.RegionPrototypeIdFromCommand);
            }

            if (@params.Length == 2)
            {
                if (!int.TryParse(@params[1], out RegionManager.SeedNumberFromCommand))
                    return CommandResult("Seed is not a seed");
            }
            else
            {
                GameManager gameManager = new();
                RegionManager.SeedNumberFromCommand = gameManager.GetAvailableGame().Random.Next();
                Log($"Seed generated : {RegionManager.SeedNumberFromCommand}");
            }

            foreach (ulong regionPrototypeId in regions)
            {
                RegionManager.RegionPrototypeIdFromCommand = regionPrototypeId;

                ServerProcess();
                ClientProcess();

                Log($"Logs of region {RegionName()} done");
            }

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
                WaitGeneration();

                WaitForXSeconds(10);
                string outputContent = stringWriter.ToString();

                List<string> clearText = new();
                bool beginFound = false;

                foreach (string line in outputContent.Split("\r"))
                {
                    if (!beginFound)
                    {
                        if (line.Contains("[PlayerManagerService] Logging in"))
                            beginFound = true;
                        else
                            continue;
                    }

                    if (beginFound && line.Contains("[Debug] [EntityManager] [Marker]"))
                        break;

                    clearText.Add(line);
                }

                WriteFile($"{RegionName()}-{RegionManager.SeedNumberFromCommand}[S].txt", clearText);
                Console.SetOut(TextWriter.Null);
            }
            Log("Server Logs generated");
        }

        private void WaitGeneration()
        {
            RegionManager.GenerationAsked = false;
            while (!RegionManager.GenerationAsked)
                Thread.Sleep(1000);
        }

        void ClientProcess()
        {
            Log("--- CLIENT LOGS ---");
            RegionManager.GenerationModeFromCommand = GenerationMode.Client;

            LaunchClient(true);
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

            Log("Wait for Marvel Heroes auth screen and log in with your credentials");
            WaitGeneration();

            WaitForXSeconds(10);
            KillRunningClient();
        }

        void WaitForXSeconds(int seconds)
        {
            Log($"Please wait {seconds} seconds");
            Thread.Sleep(seconds * 1000);
        }

        void WriteFile(string fileName, List<string> content)
        {
            string logFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs generated");
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);

            File.WriteAllText(Path.Combine(logFolder, fileName), string.Join('\r', content));
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
            if (processes == null)
                return false;
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
            foreach (Process process in processes.Where(k => k.MainModule.FileName.Contains("MarvelHeroesOmega.exe")
            || k.MainModule.FileName.Contains("MarvelHeroesOmegaRegionGenerationOn.exe")))
                process.Kill();
        }

        void LaunchClient(bool patchedVersion = false)
        {
            if (IsClientRunning())
            {
                Log("Closing running Marvel Heroes Omega client");
                KillRunningClient();
            }

            string moveCommand = $"cd \"{ConfigManager.CommandConfig.MarvelHeroesOmegax86ExeFolderPath}\"";
            string exeName = patchedVersion ? "MarvelHeroesOmegaRegionGenerationOn" : "MarvelHeroesOmega";
            string launchGameCommand = $"{exeName}.exe -robocopy -nobitraider -nosteam -log LoggingLevel=EXTRA_VERBOSE LoggingChannels=-ALL,+GAME,+GENERATION";

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
                //List<string> clearText = new();
                //bool beginFound = false;

                //foreach (string line in )
                //{
                //    if (!beginFound)
                //    {
                //        if (line.Contains("CLIENT: RegionChange to Unknown"))
                //            beginFound = true;
                //        else
                //            continue;
                //    }

                //    if (beginFound && line.Contains("########### Finished loading"))
                //        break;

                //    clearText.Add(line);
                //}

                WriteFile($"{RegionName()}-{RegionManager.SeedNumberFromCommand}[C].txt", File.ReadAllLines(filePath).ToList());
            }
            catch (IOException ex) { }
        }
    }
}
