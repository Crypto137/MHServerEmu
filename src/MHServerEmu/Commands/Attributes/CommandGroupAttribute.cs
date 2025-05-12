namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Indicates that a class contains commands.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandGroupAttribute : Attribute
    {
        public string Name { get; }

        public CommandGroupAttribute(string name)
        {
            Name = name.ToLower();
        }
    }
}
