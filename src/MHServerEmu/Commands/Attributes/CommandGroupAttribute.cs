namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Indicates that a class contains commands.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandGroupAttribute(string name, string help) : Attribute
    {
        public string Name { get; } = name.ToLower();
        public string Help { get; } = help;
    }
}
