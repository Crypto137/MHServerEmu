using System.Reflection;
using Gazillion;
using Google.ProtocolBuffers;

namespace MHServerEmu.Networking
{
    public class GameMessage
    {
        private static readonly Assembly LibGazillionAssembly = typeof(NetMessageReadyAndLoggedIn).Assembly;

        public byte Id { get; }
        public byte[] Content { get; }

        public GameMessage(byte id, byte[] content)
        {
            Id = id;
            Content = content;
        }

        public GameMessage(IMessage message)
        {
            Id = ProtocolDispatchTable.GetMessageId(message);
            Content = message.ToByteArray();
        }

        public IMessage Deserialize(Type enumType)
        {
            string messageName = ProtocolDispatchTable.GetMessageName(enumType, Id);

            Type type = LibGazillionAssembly.GetType($"Gazillion.{messageName}") ?? throw new();
            MethodInfo method = type.GetMethod("ParseFrom", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(byte[]) }) ?? throw new();
            IMessage message = (IMessage)method.Invoke(null, new object[] { Content });

            return message;
        }
    }
}
