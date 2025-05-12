namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Indicates that a method is the default command for the <see cref="CommandGroup"/> it belongs to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DefaultCommandAttribute : CommandAttribute
    {
        public DefaultCommandAttribute() : base(string.Empty) { }
    }
}
