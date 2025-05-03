using System.Reflection;
using System.Text;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.Commands
{
    [Flags]
    public enum CommandGroupFlags
    {
        None                = 0,
        SilentInvocation    = 1 << 0,   // Skips logging command group invocation
        SingleCommand       = 1 << 1,   // Indicates that this command group contains only a single default command (for docs generation)
    }

    /// <summary>
    /// Contains command implementations.
    /// </summary>
    public abstract class CommandGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly HashSet<CommandDefinition> _commands = new();

        public CommandGroupDefinition GroupDefinition { get; private set; }
        public bool IsSilent { get => GroupDefinition.Flags.HasFlag(CommandGroupFlags.SilentInvocation); }

        public IEnumerable<CommandDefinition> CommandDefinitions { get => _commands; }    // This is used only to generate docs, so it's fine to have it as IEnumerable

        /// <summary>
        /// Registers the <see cref="CommandGroupAttribute"/> and all commands for this <see cref="CommandGroup"/>.
        /// </summary>
        public void Register(CommandGroupDefinition groupDefinition)
        {
            GroupDefinition = groupDefinition;
            RegisterDefaultCommand();
            RegisterCommands();
        }

        /// <summary>
        /// Handles a command.
        /// </summary>
        public virtual string Handle(string parameters, NetClient client = null)
        {
            // Check if the user can access this command group
            CommandCanInvokeResult canInvoke = GroupDefinition.CanInvoke(client);
            if (canInvoke != CommandCanInvokeResult.Success)
                return CommandDefinition.GetCanInvokeResultString(canInvoke);

            string[] @params = Array.Empty<string>();
            CommandDefinition target;

            if (parameters != string.Empty)
            {
                @params = parameters.Split(' ');
                target = GetSubcommand(@params[0]) ?? GetDefaultSubcommand();

                if (target != GetDefaultSubcommand())
                    @params = @params.Skip(1).ToArray();
            }
            else
            {
                // Invoke the default command if no parameters are specified
                target = GetDefaultSubcommand();
            }

            // Check if the user has enough privileges to invoke the command
            canInvoke = target.CanInvoke(client, @params);
            if (canInvoke != CommandCanInvokeResult.Success)
                return CommandDefinition.GetCanInvokeResultString(canInvoke);

            return target.Invoke(this, @params, client);
        }

        /// <summary>
        /// Returns the help <see cref="string"/> for the specified command in this <see cref="CommandGroup"/>.
        /// </summary>
        public string GetHelp(string command)
        {
            foreach (CommandDefinition commandDefinition in _commands)
            {
                if (command != commandDefinition.Name)
                    continue;

                return commandDefinition.Help;
            }

            return string.Empty;
        }

        /// <summary>
        /// The fallback default command for command groups.
        /// </summary>
        [DefaultCommand]
        public virtual string Fallback(string[] @params = null, NetClient client = null)
        {
            StringBuilder sb = new("Available subcommands: ");

            foreach (CommandDefinition commandDefinition in _commands)
            {
                // Skip the fallback command
                if (commandDefinition.Name.Trim() == string.Empty)
                    continue;

                // Skip commands that are not available for this invoker
                if (commandDefinition.CanInvoke(client, null) != CommandCanInvokeResult.Success)
                    continue;

                sb.Append($"{commandDefinition.Name}, ");
            }

            // Replace the last comma / space with a period
            sb.Length -= 2;
            sb.Append('.');

            return sb.ToString();
        }

        /// <summary>
        /// Returns the <see cref="CommandAttribute"/> for the default command.
        /// </summary>
        protected CommandDefinition GetDefaultSubcommand()
        {
            foreach (CommandDefinition commandDefinition in _commands)
            {
                if (commandDefinition.IsDefaultCommand)
                    return commandDefinition;
            }

            return null;
        }

        /// <summary>
        /// Returns the <see cref="CommandAttribute"/> for the command with the specified name.
        /// </summary>
        protected CommandDefinition GetSubcommand(string name)
        {
            foreach (CommandDefinition commandDefinition in _commands)
            {
                if (commandDefinition.Name == name)
                    return commandDefinition;
            }

            return null;
        }

        /// <summary>
        /// Registers the default command for this <see cref="CommandGroup"/>.
        /// </summary>
        private void RegisterDefaultCommand()
        {
            // First look for any custom default command for this field group
            foreach (MethodInfo methodInfo in GetType().GetMethods())
            {
                if (methodInfo.IsDefined(typeof(DefaultCommandAttribute), true) == false)
                    continue;

                if (methodInfo.Name == nameof(Fallback))
                    continue;

                CommandDefinition commandDefinition = new(methodInfo);
                _commands.Add(commandDefinition);
                return;
            }

            // Use the fallback method as the default command if no other default command is defined
            CommandDefinition fallbackCommandDefinition = new(GetType().GetMethod(nameof(Fallback)));
            _commands.Add(fallbackCommandDefinition);
        }

        /// <summary>
        /// Registers commands for this <see cref="CommandGroup"/>.
        /// </summary>
        private void RegisterCommands()
        {
            foreach (MethodInfo methodInfo in GetType().GetMethods())
            {
                object[] attributes = methodInfo.GetCustomAttributes(typeof(CommandAttribute), true);
                if (attributes.Length == 0)
                    continue;

                if (attributes[0] is DefaultCommandAttribute)
                    continue;

                CommandDefinition commandDefinition = new(methodInfo);
                if (_commands.Add(commandDefinition) == false)
                    Logger.Warn($"Command {commandDefinition.Name} is already registered to group {GroupDefinition.Name}");
            }
        }
    }
}
