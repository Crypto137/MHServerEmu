using System.Reflection;
using Google.ProtocolBuffers;
using Gazillion;

namespace MHServerEmu.Networking
{
    public class GameMessage
    {
        private static readonly Assembly LibGazillionAssembly = typeof(NetMessageReadyAndLoggedIn).Assembly;

        public byte Id { get; }
        public byte[] Payload { get; }

        public GameMessage(byte id, byte[] payload)
        {
            Id = id;
            Payload = payload;
        }

        public GameMessage(IMessage message)
        {
            Id = ProtocolDispatchTable.GetMessageId(message);
            Payload = message.ToByteArray();
        }

        public GameMessage(CodedInputStream stream)
        {
            Id = (byte)stream.ReadRawVarint32();
            Payload = stream.ReadRawBytes((int)stream.ReadRawVarint32());
        }

        public IMessage Deserialize(Type enumType)
        {
            string messageName = ProtocolDispatchTable.GetMessageName(enumType, Id);

            Type type = LibGazillionAssembly.GetType($"Gazillion.{messageName}") ?? throw new("Message type is null.");
            MethodInfo method = type.GetMethod("ParseFrom", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(byte[]) }) ?? throw new("Message ParseFrom method is null.");
            IMessage message = (IMessage)method.Invoke(null, new object[] { Payload });

            return message;
        }
    }
}
