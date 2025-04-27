namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Specifies the minimum number of parameters required to invoke a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandParamCountAttribute(int paramCount = 0) : Attribute
    {
        public int ParamCount { get; } = paramCount;
    }
}
