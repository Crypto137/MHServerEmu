using System.Reflection;
using System.Text;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.Commands
{
    /// <summary>
    /// A singleton that manages <see cref="CommandGroup"/> instances and parses commands.
    /// </summary>
    public class CommandManager
    {
        internal const char CommandPrefix = '!';

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<string, CommandGroup> _commandGroupDict = new();
        private IClientOutput _clientOutput;

        public static CommandManager Instance { get; } = new();

        /// <summary>
        /// Constructs the <see cref="CommandManager"/> instance.
        /// </summary>
        private CommandManager()
        {
            RegisterCommandGroupsFromAssembly(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Registers all <see cref="CommandGroup"/> classes in the provided <see cref="Assembly"/>.
        /// </summary>
        public void RegisterCommandGroupsFromAssembly(Assembly assembly)
        {
            // Find and register command group classes using reflection
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(CommandGroup)) == false)
                    continue;

                if (type.IsDefined(typeof(CommandGroupAttribute), true) == false)
                    continue;

                CommandGroupDefinition groupDefinition = new(type);
                if (_commandGroupDict.ContainsKey(groupDefinition.Name))
                    Logger.Warn($"RegisterCommandGroupsFromAssembly(): Command group {groupDefinition} is already registered");

                CommandGroup commandGroup = (CommandGroup)Activator.CreateInstance(type);
                commandGroup.Register(groupDefinition);
                _commandGroupDict.Add(groupDefinition.Name, commandGroup);
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
        /// Tries to parse the provided <see cref="string"/> input as a command.
        /// </summary>
        public bool TryParse(string input, NetClient client = null)
        {
            // Extract the command and its parameters from our input string
            if (ExtractCommandAndParameters(input, out string command, out string parameters) == false)
            {
                if (client == null)
                    Logger.Info($"Unknown command {input}");

                return false;
            }

            // Try to invoke the specified command
            string output = InvokeCommand(command, parameters, client);

            // Output the result of this command invocation
            OutputCommandResult(output, client);

            return true;
        }

        /// <summary>
        /// Extracts the command and its parameters (if any) from the provided input <see cref="string"/>.
        /// Returns <see langword="true"/> if successful.
        /// </summary>
        private static bool ExtractCommandAndParameters(string input, out string command, out string parameters)
        {
            command = string.Empty;
            parameters = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim();

            // Only input that starts with our command prefix char followed by something else can be a command
            if (input.Length < 2 || input[0] != CommandPrefix)
                return false;

            int whiteSpaceIndex = input.IndexOf(' ');

            // Get the command.
            // The command ends at the first occurrence of white space or the end of the input string.
            int commandLength = whiteSpaceIndex >= 0 ? whiteSpaceIndex - 1 : input.Length - 1;
            command = input.Substring(1, commandLength).ToLower();

            // Get parameters after the first space (if there are any)
            if (whiteSpaceIndex >= 0)
                parameters = input.Substring(whiteSpaceIndex + 1);

            return true;
        }

        /// <summary>
        /// Invokes the specified command.
        /// </summary>
        private string InvokeCommand(string command, string parameters, NetClient client)
        {
            if (_commandGroupDict.TryGetValue(command, out CommandGroup commandGroup) == false)
                return $"Unknown command: {command} {parameters}";

            if (commandGroup.IsSilent == false)
                Logger.Info($"Command invoked: invoker=[{(client != null ? client : "ServerConsole")}], command=[{command}], parameters=[{parameters}]");
            
            return commandGroup.Handle(parameters, client);
        }

        /// <summary>
        /// Outputs the result of a command invocation to the server console or the provided invoker <see cref="NetClient"/>.
        /// </summary>
        private void OutputCommandResult(string output, NetClient client)
        {
            if (string.IsNullOrWhiteSpace(output))
                return;

            if (client != null)
                _clientOutput?.Output(output, client);
            else
                Logger.Info(output);
        }

        #region Help Command Groups

        // Help command groups are inside CommandManager so that they can access _commandGroupDict

        [CommandGroup("commands")]
        [CommandGroupDescription("Lists available commands.")]
        [CommandGroupFlags(CommandGroupFlags.SilentInvocation | CommandGroupFlags.SingleCommand)]
        public class CommandsCommandGroup : CommandGroup
        {
            public override string Fallback(string[] @params = null, NetClient client = null)
            {
                StringBuilder sb = new("Available commands: ");

                foreach (CommandGroup commandGroup in Instance._commandGroupDict.Values)
                {
                    CommandGroupDefinition groupDefinition = commandGroup.GroupDefinition;

                    if (groupDefinition.CanInvoke(client) != CommandCanInvokeResult.Success)
                        continue;

                    sb.Append($"{groupDefinition.Name}, ");
                }

                // Replace the last comma / space with a period
                sb.Length -= 2;
                sb.Append('.');

                sb.Append("\nType 'help [command]' to get help.");
                return sb.ToString();
            }
        }

        [CommandGroup("help")]
        [CommandGroupDescription("Help needs no help.")]
        [CommandGroupFlags(CommandGroupFlags.SilentInvocation | CommandGroupFlags.SingleCommand)]
        public class HelpCommandGroup : CommandGroup
        {
            public override string Fallback(string[] @params = null, NetClient client = null)
            {
                return "Usage: help [command]";
            }

            public override string Handle(string parameters, NetClient client = null)
            {
                if (parameters == string.Empty)
                    return Fallback();

                string[] @params = parameters.Split(' ');
                string group = @params[0];
                string command = @params.Length > 1 ? @params[1] : string.Empty;

                if (Instance._commandGroupDict.TryGetValue(group, out CommandGroup commandGroup) == false)
                    return $"Unknown command: {group} {command}";

                if (command == string.Empty)
                    return commandGroup.GroupDefinition.Help;
                else
                    return commandGroup.GetHelp(command);
            }
        }

        [CommandGroup("generatecommanddocs")]
        [CommandGroupDescription("Generates markdown documentation for all registered command groups.")]
        [CommandGroupFlags(CommandGroupFlags.SilentInvocation | CommandGroupFlags.SingleCommand)]
        public class GenerateCommandDocsCommandGroup : CommandGroup
        {
            [DefaultCommand]
            [CommandInvokerType(CommandInvokerType.ServerConsole)]
            public string GenerateCommandDocs(string[] @params, NetClient client)
            {
                CommandDocsGenerator.GenerateDocs(Instance._commandGroupDict.Values, "ServerCommands.md");
                return string.Empty;
            }
        }

        #endregion
    }
}
