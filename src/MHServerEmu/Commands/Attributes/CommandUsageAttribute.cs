namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Specifies the usage <see cref="string"/> for a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandUsageAttribute : Attribute
    {
        public string Usage { get; }

        public CommandUsageAttribute() : this(string.Empty)
        {
        }

        public CommandUsageAttribute(string usage)
        {
            Usage = usage;
        }
    }
}
