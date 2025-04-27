namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Indicates that a method is a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute(string name, string help) : Attribute
    {
        public string Name { get; } = name.ToLower();
        public string Help { get; } = help;
    }
}
