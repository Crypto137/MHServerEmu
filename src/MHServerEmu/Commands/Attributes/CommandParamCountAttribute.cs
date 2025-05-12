namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Specifies the minimum number of parameters required to invoke a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandParamCountAttribute : Attribute
    {
        public int ParamCount { get; }

        public CommandParamCountAttribute() : this(0)
        {
        }

        public CommandParamCountAttribute(int paramCount)
        {
            ParamCount = paramCount;
        }
    }
}
