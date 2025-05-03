namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Specifies the description <see cref="string"/> for a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandDescriptionAttribute : Attribute
    {
        public string Description { get; }

        public CommandDescriptionAttribute() : this("No description available.")
        {
        }

        public CommandDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}
