using System.Reflection;
using System.Text;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Logging;
using MHServerEmu.Frontend;

namespace MHServerEmu.Commands
{
    /// <summary>
    /// Contains command implementations.
    /// </summary>
    public abstract class CommandGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<CommandAttribute, MethodInfo> _commandDict = new();

        public CommandGroupAttribute GroupAttribute { get; private set; }

        /// <summary>
        /// Registers the <see cref="CommandGroupAttribute"/> and all commands for this <see cref="CommandGroup"/>.
        /// </summary>
        public void Register(CommandGroupAttribute groupAttribute)
        {
            GroupAttribute = groupAttribute;
            RegisterDefaultCommand();
            RegisterCommands();
        }

        /// <summary>
        /// Handles a command.
        /// </summary>
        public virtual string Handle(string parameters, FrontendClient client = null)
        {
            // Check if the user has enough privileges to access this command group
            if (client != null && GroupAttribute.MinUserLevel > client.Session.Account.UserLevel)
                return "You don't have enough privileges to invoke this command.";

            string[] @params = Array.Empty<string>();
            CommandAttribute target;

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
            if (client != null && target.MinUserLevel > client.Session.Account.UserLevel)
                return "You don't have enough privileges to invoke this command.";

            return (string)_commandDict[target].Invoke(this, new object[] { @params, client });
        }

        /// <summary>
        /// Returns the help <see cref="string"/> for the specified command in this <see cref="CommandGroup"/>.
        /// </summary>
        public string GetHelp(string command)
        {
            foreach (var kvp in _commandDict)
            {
                if (command != kvp.Key.Name) continue;
                return kvp.Key.Help;
            }

            return string.Empty;
        }

        /// <summary>
        /// The fallback default command for command groups.
        /// </summary>
        [DefaultCommand]
        public virtual string Fallback(string[] @params = null, FrontendClient client = null)
        {
            StringBuilder sb = new("Available subcommands: ");

            foreach (var kvp in _commandDict)
            {
                // Skip the fallback command
                if (kvp.Key.Name.Trim() == string.Empty) continue;

                // Skip commands that are not available for this account's user level
                if (client != null && kvp.Key.MinUserLevel > client.Session.Account.UserLevel) continue;

                sb.Append($"{kvp.Key.Name}, ");
            }

            // Replace the last comma / space with a period
            sb.Length -= 2;
            sb.Append('.');

            return sb.ToString();
        }

        /// <summary>
        /// Returns the <see cref="CommandAttribute"/> for the default command.
        /// </summary>
        protected CommandAttribute GetDefaultSubcommand() => _commandDict.Keys.First(command => command is DefaultCommandAttribute);

        /// <summary>
        /// Returns the <see cref="CommandAttribute"/> for the command with the specified name.
        /// </summary>
        protected CommandAttribute GetSubcommand(string name) => _commandDict.Keys.FirstOrDefault(command => command.Name == name);

        /// <summary>
        /// Registers commands for this <see cref="CommandGroup"/>.
        /// </summary>
        private void RegisterCommands()
        {
            foreach (MethodInfo method in GetType().GetMethods())
            {
                object[] attributes = method.GetCustomAttributes(typeof(CommandAttribute), true);
                if (attributes.Length == 0) continue;

                CommandAttribute attribute = (CommandAttribute)attributes[0];
                if (attribute is DefaultCommandAttribute) continue;

                if (_commandDict.ContainsKey(attribute) == false)
                    _commandDict.Add(attribute, method);
                else
                    Logger.Warn($"Command {attribute.Name} is already registered to group {GroupAttribute.Name}");
            }
        }

        /// <summary>
        /// Registers the default command for this <see cref="CommandGroup"/>.
        /// </summary>
        private void RegisterDefaultCommand()
        {
            // First look for any custom default command for this field group
            foreach (MethodInfo method in GetType().GetMethods())
            {
                object[] attributes = method.GetCustomAttributes(typeof(DefaultCommandAttribute), true);
                if (attributes.Length == 0) continue;
                if (method.Name.ToLower() == "fallback") continue;

                _commandDict.Add(new DefaultCommandAttribute(GroupAttribute.MinUserLevel), method);
                return;
            }

            // Use the fallback method as the default command if no other default command is defined
            _commandDict.Add(new DefaultCommandAttribute(GroupAttribute.MinUserLevel), GetType().GetMethod("Fallback"));
        }
    }
}
