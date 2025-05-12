namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Indicates that a method is a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; }

        public CommandAttribute(string name)
        {
            Name = name.ToLower();
        }
    }
}
