namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Specifies the description <see cref="string"/> for a <see cref="CommandGroup"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandGroupDescriptionAttribute : Attribute
    {
        public string Description { get; }

        public CommandGroupDescriptionAttribute() : this("No description available.")
        {
        }

        public CommandGroupDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}
