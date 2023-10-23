using System.Reflection;
using Google.ProtocolBuffers;
using Gazillion;

namespace MHServerEmu.Networking
{
    /// <summary>
    /// A helper class for protobuf message serialization and deserialization.
    /// </summary>
    public static class ProtocolDispatchTable
    {
        private static readonly Assembly LibGazillionAssembly = typeof(NetMessageReadyAndLoggedIn).Assembly;
        private static readonly Type[] ParseMethodArgumentTypes = new Type[] { typeof(byte[]) };

        private static readonly Dictionary<Type, Dictionary<byte, string>> IdToNameDict = new();        // Id -> Class name
        private static readonly Dictionary<string, Dictionary<string, byte>> MessageToIdDict = new();   // IMessage -> Id
        private static readonly Dictionary<string, Type> NameToTypeDict = new();                        // Class name -> Type 
        private static readonly Dictionary<Type, ParseMessage> TypeToParseDelegateDict = new();         // Type -> ParseMessage Delegate

        public static bool IsInitialized { get; }

        public delegate IMessage ParseMessage(byte[] data);

        static ProtocolDispatchTable()
        {
            // Preprocess all messages on startup to speed up (de)serialization
            ParseMessageEnum(typeof(AuthMessage),                       "AuthMessages.proto");
            ParseMessageEnum(typeof(BillingCommonMessage),              "BillingCommon.proto");
            ParseMessageEnum(typeof(ChatCommonMessage),                 "ChatCommon.proto");
            ParseMessageEnum(typeof(ClientToGameServerMessage),         "ClientToGameServer.proto");
            ParseMessageEnum(typeof(ClientToGroupingManagerMessage),    "ClientToGroupingManager.proto");
            ParseMessageEnum(typeof(CommonMessage),                     "CommonMessages.proto");
            ParseMessageEnum(typeof(FrontendProtocolMessage),           "FrontendProtocol.proto");
            ParseMessageEnum(typeof(GameServerToClientMessage),         "GameServerToClient.proto");
            ParseMessageEnum(typeof(GroupingManagerMessage),            "GroupingManager.proto");
            ParseMessageEnum(typeof(GuildMessage),                      "Guild.proto");
            ParseMessageEnum(typeof(MatchCommonMessage),                "MatchCommon.proto");
            ParseMessageEnum(typeof(PubSubProtocolMessage),             "PubSubProtocol.proto");

            IsInitialized = true;
        }

        public static string GetMessageName(Type enumType, byte id) => IdToNameDict[enumType][id];
        public static byte GetMessageId(IMessage message) => MessageToIdDict[message.DescriptorForType.File.Name][message.DescriptorForType.Name];
        public static Type GetMessageType(string name) => NameToTypeDict[name];
        public static ParseMessage GetParseMessageDelegate<T>() => TypeToParseDelegateDict[typeof(T)];
        public static ParseMessage GetParseMessageDelegate(Type messageType) => TypeToParseDelegateDict[messageType];

        private static void ParseMessageEnum(Type type, string protocolName)
        {
            IdToNameDict.Add(type, new());
            MessageToIdDict.Add(protocolName, new());

            string[] names = Enum.GetNames(type);
            for (int i = 0; i < names.Length; i++)
            {
                IdToNameDict[type].Add((byte)i, names[i]);
                MessageToIdDict[protocolName].Add(names[i], (byte)i);
                
                // Use reflection to get message type and ParseFrom MethodInfo
                Type messageType = LibGazillionAssembly.GetType($"Gazillion.{names[i]}") ?? throw new("Message type is null.");
                MethodInfo parseMethod = messageType.GetMethod("ParseFrom", ParseMethodArgumentTypes) ?? throw new("Message ParseFrom method is null.");

                // Create delegates from MethodInfo to speed up deserialization
                NameToTypeDict.Add(names[i], messageType);
                TypeToParseDelegateDict.Add(messageType, parseMethod.CreateDelegate<ParseMessage>());
            }
        }
    }
}
