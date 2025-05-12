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
    public class CommandInvokerTypeAttribute : Attribute
    {
        public CommandInvokerType InvokerType { get; }

        public CommandInvokerTypeAttribute() : this(CommandInvokerType.Any)
        {
        }

        public CommandInvokerTypeAttribute(CommandInvokerType invokerType)
        {
            InvokerType = invokerType;
        }
    }
}
