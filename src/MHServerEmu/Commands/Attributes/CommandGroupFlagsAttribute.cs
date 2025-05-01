namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Specifies the <see cref="CommandGroupFlags"/> for this <see cref="CommandGroup"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandGroupFlagsAttribute(CommandGroupFlags flags = CommandGroupFlags.None) : Attribute
    {
        public CommandGroupFlags Flags { get; } = flags;
    }
}
