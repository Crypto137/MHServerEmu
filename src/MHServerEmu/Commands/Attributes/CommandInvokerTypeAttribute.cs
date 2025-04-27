namespace MHServerEmu.Commands.Attributes
{
    public enum CommandInvokerType
    {
        Any,
        Client,
        ServerConsole,
    }

    /// <summary>
    /// Specifies the <see cref="CommandInvokerType"/> required to invoke a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandInvokerTypeAttribute(CommandInvokerType invokerType = CommandInvokerType.Any) : Attribute
    {
        public CommandInvokerType InvokerType { get; } = invokerType;
    }
}
