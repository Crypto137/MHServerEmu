using System.Reflection;
using MHServerEmu.Networking;

namespace MHServerEmu.Common.Commands
{
    public class CommandGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<CommandAttribute, MethodInfo> _commandDict = new();

        public CommandGroupAttribute GroupAttribute { get; private set; }

        public void Register(CommandGroupAttribute groupAttribute)
        {
            GroupAttribute = groupAttribute;
            RegisterDefaultCommand();
            RegisterCommands();
        }

        public virtual string Handle(string parameters, FrontendClient client = null)
        {
            string[] @params = null;
            CommandAttribute target = null;

            if (parameters == string.Empty)
            {
                target = GetDefaultSubcommand();
            }
            else
            {
                @params = parameters.Split(' ');
                target = GetSubcommand(@params[0]) ?? GetDefaultSubcommand();

                if (target != GetDefaultSubcommand()) @params = @params.Skip(1).ToArray();
            }

            return (string)_commandDict[target].Invoke(this, new object[] { @params, client });
        }

        public string GetHelp(string command)
        {
            foreach (var kvp in _commandDict)
            {
                if (command != kvp.Key.Name) continue;
                return kvp.Key.Help;
            }

            return string.Empty;
        }

        [DefaultCommand]
        public virtual string Fallback(string[] @params = null, FrontendClient client = null)
        {
            string output = "Available subcommands: ";

            foreach (var kvp in _commandDict)
            {
                if (kvp.Key.Name.Trim() == string.Empty) continue;      // Skip fallback command
                output = $"{output}{kvp.Key.Name}, ";
            }

            return output.Substring(0, output.Length - 2) + ".";
        }

        protected CommandAttribute GetDefaultSubcommand() => _commandDict.Keys.First();
        protected CommandAttribute GetSubcommand(string name) => _commandDict.Keys.FirstOrDefault(command => command.Name == name);

        private void RegisterCommands()
        {
            foreach (MethodInfo method in GetType().GetMethods())
            {
                object[] attributes = method.GetCustomAttributes(typeof(CommandAttribute), true);
                if (attributes.Length == 0) continue;

                CommandAttribute attribute = (CommandAttribute)attributes[0];
                if (attribute is DefaultCommand) continue;

                if (_commandDict.ContainsKey(attribute) == false)
                    _commandDict.Add(attribute, method);
                else
                    Logger.Warn($"Command {attribute.Name} is already registered to group {GroupAttribute.Name}");
            }
        }

        private void RegisterDefaultCommand()
        {
            foreach (MethodInfo method in GetType().GetMethods())
            {
                object[] attributes = method.GetCustomAttributes(typeof(DefaultCommand), true);
                if (attributes.Length == 0) continue;
                if (method.Name.ToLower() == "fallback") continue;

                _commandDict.Add(new DefaultCommand(), method);
                return;
            }

            _commandDict.Add(new DefaultCommand(), GetType().GetMethod("Fallback"));
        }
    }
}
