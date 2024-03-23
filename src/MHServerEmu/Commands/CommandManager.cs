using System.Reflection;
using System.Text;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Logging;
using MHServerEmu.Frontend;

namespace MHServerEmu.Commands
{
    /// <summary>
    /// A singleton that manages <see cref="CommandGroup"/> instances and parses commands.
    /// </summary>
    public class CommandManager
    {
        private const char CommandPrefix = '!';

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<CommandGroupAttribute, CommandGroup> _commandGroupDict = new();
        private IClientOutput _clientOutput;

        public static CommandManager Instance { get; } = new();

        /// <summary>
        /// Constructs the <see cref="CommandManager"/> instance.
        /// </summary>
        private CommandManager()
        {
            // Find and register command group classes using reflection
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                // TODO: If we ever move the command system to Core, move command group registration to a separate method.

                if (type.IsSubclassOf(typeof(CommandGroup)) == false) continue;

                CommandGroupAttribute[] attributes = (CommandGroupAttribute[])type.GetCustomAttributes(typeof(CommandGroupAttribute), true);
                if (attributes.Length == 0) continue;

                CommandGroupAttribute groupAttribute = attributes[0];
                if (_commandGroupDict.ContainsKey(groupAttribute))
                    Logger.Warn($"Command group {groupAttribute} is already registered");

                var commandGroup = (CommandGroup)Activator.CreateInstance(type);
                commandGroup.Register(groupAttribute);
                _commandGroupDict.Add(groupAttribute, commandGroup);
            }
        }

        /// <summary>
        /// Sets the <see cref="IClientOutput"/> to use for outputting messages to clients.
        /// </summary>
        public void SetClientOutput(IClientOutput clientOutput)
        {
            _clientOutput = clientOutput;
        }

        /// <summary>
        /// Parses a command from the provided input.
        /// </summary>
        public void Parse(string input)
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

            foreach (var kvp in _commandGroupDict)
            {
                if (kvp.Key.Name != command) continue;
                output = kvp.Value.Handle(parameters);
                found = true;
                break;
            }

            if (found == false) output = $"Unknown command: {command} {parameters}";
            if (output != string.Empty) Logger.Info(output);
        }

        /// <summary>
        /// Tries to parse the provided <see cref="FrontendClient"/> input as a command.
        /// </summary>
        public bool TryParse(string input, FrontendClient client)
        {
            string output = string.Empty;
            string command;
            string parameters;
            bool found = false;

            if (ExtractCommandAndParameters(input, out command, out parameters) == false) return false;

            foreach (var kvp in _commandGroupDict)
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

        /// <summary>
        /// Extracts the command and its parameters (if any) from the provided input <see cref="string"/>.
        /// Returns <see langword="true"/> if successful.
        /// </summary>
        private bool ExtractCommandAndParameters(string input, out string command, out string parameters)
        {
            input = input.Trim();
            command = string.Empty;
            parameters = string.Empty;

            if (input == string.Empty || input[0] != CommandPrefix) return false;

            // Remove the prefix
            input = input.Substring(1);

            // Get the command
            command = input.Split(' ')[0].ToLower();

            // Get parameters after the first space (if there are any)
            if (input.Contains(' '))
                parameters = input.Substring(input.IndexOf(' ') + 1).Trim();

            return true;
        }

        /// <summary>
        /// Sends a response to the specified <see cref="FrontendClient"/> using the registered <see cref="IClientOutput"/>.
        /// </summary>
        private void SendClientResponse(string output, FrontendClient client)
        {
            _clientOutput?.Output(output, client);
        }

        #region Help Command Groups

        // Help command groups are inside CommandManager so that they can access _commandGroupDict

        [CommandGroup("commands", "Lists available commands.")]
        public class CommandsCommandGroup : CommandGroup
        {
            public override string Fallback(string[] @params = null, FrontendClient client = null)
            {
                StringBuilder sb = new("Available commands: ");

                foreach (var kvp in Instance._commandGroupDict)
                {
                    // Skip commands that are not available for this account's user level
                    if (client != null && kvp.Key.MinUserLevel > client.Session.Account.UserLevel) continue;

                    sb.Append($"{kvp.Key.Name}, ");
                }

                // Replace the last comma / space with a period
                sb.Length -= 2;
                sb.Append('.');

                sb.Append("\nType 'help [command]' to get help.");
                return sb.ToString();
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

                foreach (var kvp in Instance._commandGroupDict)
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

        #endregion
    }
}
