using System.Reflection;
using Google.ProtocolBuffers;

// This was previously used for our packet parsing functionality, which we no longer need. I am leaving this here just for reference.

#if false
namespace MHServerEmu.Games.Network.Parsing
{
    /// <summary>
    /// Prints <see cref="IMessage"/> instances using custom methods.
    /// </summary>
    public static partial class MessagePrinter
    {
        private static readonly Dictionary<Type, Func<IMessage, string>> PrintMethodDict = new();
        private static readonly Game DummyGame = new(0);

        static MessagePrinter()
        {
            foreach (var method in typeof(MessagePrinter).GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            {
                if (method.IsDefined(typeof(PrintMethodAttribute)) == false) continue;
                Type messageType = method.GetCustomAttribute<PrintMethodAttribute>().MessageType;
                PrintMethodDict.Add(messageType, method.CreateDelegate<Func<IMessage, string>>());
            }
        }

        /// <summary>
        /// Prints the provided <see cref="IMessage"/> to <see cref="string"/>.
        /// Uses a custom printing method if there is one defined for the <see cref="Type"/> of the provided message.
        /// </summary>
        public static string Print(IMessage message)
        {
            Type messageType = message.GetType();

            if (PrintMethodDict.TryGetValue(messageType, out var print) == false)
                return message.ToString();  // No custom print method is defined

            // Print using our custom method
            return print(message);
        }

        [AttributeUsage(AttributeTargets.Method)]
        private class PrintMethodAttribute : Attribute
        {
            public Type MessageType { get; }

            public PrintMethodAttribute(Type messageType)
            {
                MessageType = messageType;
            }
        }
    }
}
#endif
