using System.Reflection;
using Gazillion;
using MHServerEmu.Common.Config;
using MHServerEmu.Networking;

namespace MHServerEmu.Common.Commands
{
    public static class CommandManager
    {
        private const char CommandPrefix = '!';

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly Dictionary<CommandGroupAttribute, CommandGroup> CommandGroupDict = new();

        static CommandManager()
        {
            // Register command groups
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(CommandGroup)) == false) continue;

                CommandGroupAttribute[] attributes = (CommandGroupAttribute[])type.GetCustomAttributes(typeof(CommandGroupAttribute), true);
                if (attributes.Length == 0) continue;

                CommandGroupAttribute groupAttribute = attributes[0];
                if (CommandGroupDict.ContainsKey(groupAttribute)) Logger.Warn($"Command group {groupAttribute} is already registered");

                CommandGroup commandGroup = (CommandGroup)Activator.CreateInstance(type);
                commandGroup.Register(groupAttribute);
                CommandGroupDict.Add(groupAttribute, commandGroup);
            }
        }

        public static void Parse(string input)
        {
            string output = string.Empty;
            string command;
            string parameters;
            bool found = false;

            if (input == null || input.Trim() == string.Empty) return;

            if (ExtractCommandAndParameters(input, out command, out parameters) == false)
            {
                output = $"Unknown command {input}";
                Logger.Info(output);
                return;
            }

            foreach (var kvp in CommandGroupDict)
            {
                if (kvp.Key.Name != command) continue;
                output = kvp.Value.Handle(parameters);
                found = true;
                break;
            }

            if (found == false) output = $"Unknown command: {command} {parameters}";
            if (output != string.Empty) Logger.Info(output);
        }

        public static bool TryParse(string input, FrontendClient client)
        {
            string output = string.Empty;
            string command;
            string parameters;
            bool found = false;

            if (ExtractCommandAndParameters(input, out command, out parameters) == false) return false;

            foreach (var kvp in CommandGroupDict)
            {
                if (kvp.Key.Name != command) continue;
                output = kvp.Value.Handle(parameters, client);
                found = true;
                break;
            }

            if (found == false) output = $"Unknown command: {command} {parameters}";
            if (output == string.Empty) return true;

            if (client != null) SendClientResponse(output, client);

            return true;
        }

        // We need commands and help groups inside CommandManager so that they can access CommandGroupDict
        [CommandGroup("commands", "Lists available commands.")]
        public class CommandsCommandGroup : CommandGroup
        {
            public override string Fallback(string[] @params = null, FrontendClient client = null)
            {
                string output = "Available commands: ";

                foreach (var kvp in CommandGroupDict)
                {
                    output = $"{output}{kvp.Key.Name}, ";
                }

                output = $"{output.Substring(0, output.Length - 2)}.\nType 'help [command]' to get help.";
                return output;
            }
        }

        [CommandGroup("help", "Help needs no help.")]
        public class HelpCommandGroup : CommandGroup
        {
            public override string Fallback(string[] @params = null, FrontendClient client = null)
            {
                return "usage: help [command]"; ;
            }

            public override string Handle(string parameters, FrontendClient client = null)
            {
                if (parameters == string.Empty) return Fallback();

                string output = string.Empty;
                bool found = false;
                string[] @params = parameters.Split(' ');
                string group = @params[0];
                string command = @params.Length > 1 ? @params[1] : string.Empty;

                foreach (var kvp in CommandGroupDict)
                {
                    if (group != kvp.Key.Name) continue;
                    if (command == string.Empty) return kvp.Key.Help;

                    output = kvp.Value.GetHelp(command);
                    found = true;
                }

                if (found == false) output = $"Unknown command: {group} {command}";

                return output;
            }
        }

        private static bool ExtractCommandAndParameters(string input, out string command, out string parameters)
        {
            input = input.Trim();
            command = string.Empty;
            parameters = string.Empty;

            if (input == string.Empty || input[0] != CommandPrefix) return false;                   // Filter empty strings and input that doesn't have the prefix
            
            input = input.Substring(1);                                                             // Remove the prefix
            command = input.Split(' ')[0].ToLower();                                                // Get the command
            if (input.Contains(' ')) parameters = input.Substring(input.IndexOf(' ') + 1).Trim();   // Get parameters after the first space (if there's any)

            return true;
        }

        private static void SendClientResponse(string output, FrontendClient client)
        {
            var chatMessage = ChatNormalMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(output))
                .SetPrestigeLevel(6)
                .Build().ToByteArray();

            client.SendMessage(2, new(GroupingManagerMessage.ChatNormalMessage, chatMessage));
        }
    }
}
