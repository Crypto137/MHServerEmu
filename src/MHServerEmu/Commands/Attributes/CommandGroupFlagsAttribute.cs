namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Specifies the <see cref="CommandGroupFlags"/> for this <see cref="CommandGroup"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandGroupFlagsAttribute : Attribute
    {
        public CommandGroupFlags Flags { get; }

        public CommandGroupFlagsAttribute() : this(CommandGroupFlags.None)
        {
        }

        public CommandGroupFlagsAttribute(CommandGroupFlags flags)
        {
            Flags = flags;
        }
    }
}
