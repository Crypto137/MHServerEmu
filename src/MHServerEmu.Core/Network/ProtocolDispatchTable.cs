using System.Reflection;
using Google.ProtocolBuffers;
using Gazillion;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// A helper class for protobuf message serialization and deserialization.
    /// </summary>
    public static class ProtocolDispatchTable
    {
        private static readonly Assembly LibGazillionAssembly = typeof(NetMessageReadyAndLoggedIn).Assembly;
        private static readonly Type[] ParseMethodArgumentTypes = new Type[] { typeof(byte[]) };

        // Lookups
        private static readonly Dictionary<(string, string), uint> DescriptorToIdDict = new();      // Proto file name + message name -> Id
        private static readonly Dictionary<Type, ParseMessage> TypeToParseDelegateDict = new();     // Class type -> ParseMessage delegate
        private static readonly Dictionary<(Type, uint), string> IdToNameDict = new();              // Protocol enum type + id -> message name
        private static readonly Dictionary<string, Type> NameToTypeDict = new();                    // Message name -> class type 

        public static bool IsInitialized { get; }

        public delegate IMessage ParseMessage(byte[] data);

        static ProtocolDispatchTable()
        {
            // Preprocess all messages on startup to speed up (de)serialization
            ParseMessageEnum(typeof(AuthMessage), "AuthMessages.proto");
            ParseMessageEnum(typeof(BillingCommonMessage), "BillingCommon.proto");
            ParseMessageEnum(typeof(ChatCommonMessage), "ChatCommon.proto");
            ParseMessageEnum(typeof(ClientToGameServerMessage), "ClientToGameServer.proto");
            ParseMessageEnum(typeof(ClientToGroupingManagerMessage), "ClientToGroupingManager.proto");
            ParseMessageEnum(typeof(CommonMessage), "CommonMessages.proto");
            ParseMessageEnum(typeof(FrontendProtocolMessage), "FrontendProtocol.proto");
            ParseMessageEnum(typeof(GameServerToClientMessage), "GameServerToClient.proto");
            ParseMessageEnum(typeof(GroupingManagerMessage), "GroupingManager.proto");
            ParseMessageEnum(typeof(GuildMessage), "Guild.proto");
            ParseMessageEnum(typeof(MatchCommonMessage), "MatchCommon.proto");
            ParseMessageEnum(typeof(PubSubProtocolMessage), "PubSubProtocol.proto");

            IsInitialized = true;
        }

        /// <summary>
        /// Returns the id of the provided <see cref="IMessage"/>.
        /// </summary>
        public static uint GetMessageId(IMessage message)
        {
            return DescriptorToIdDict[(message.DescriptorForType.File.Name, message.DescriptorForType.Name)];
        }

        /// <summary>
        /// Returns a delegate for parsing an <see cref="IMessage"/> of the provided type.
        /// </summary>
        public static ParseMessage GetParseMessageDelegate(Type messageType)
        {
            return TypeToParseDelegateDict[messageType];
        }

        /// <summary>
        /// Returns a delegate for parsing <typeparamref name="T"/>.
        /// </summary>
        public static ParseMessage GetParseMessageDelegate<T>() where T : IMessage
        {
            return GetParseMessageDelegate(typeof(T));
        }

        /// <summary>
        /// Returns a delegate for parsing an <see cref="IMessage"/> of the specified id and protocol.
        /// </summary>
        public static ParseMessage GetParseMessageDelegate(Type protocolEnumType, uint id)
        {
            string messageName = IdToNameDict[(protocolEnumType, id)];
            Type messageType = NameToTypeDict[messageName];
            return GetParseMessageDelegate(messageType);
        }

        /// <summary>
        /// Parses a message enum to generate lookups and cache parse delegates.
        /// </summary>
        private static void ParseMessageEnum(Type protocolEnumType, string protoFileName)
        {
            string[] names = Enum.GetNames(protocolEnumType);

            // Iterate through message ids
            for (uint i = 0; i < names.Length; i++)
            {
                // Use reflection to get message type and ParseFrom MethodInfo
                Type messageType = LibGazillionAssembly.GetType($"Gazillion.{names[i]}") ?? throw new("Message type is null.");
                MethodInfo parseMethod = messageType.GetMethod("ParseFrom", ParseMethodArgumentTypes) ?? throw new("Message ParseFrom method is null.");

                // Add lookups
                DescriptorToIdDict[(protoFileName, names[i])] = i;
                IdToNameDict.Add((protocolEnumType, i), names[i]);
                NameToTypeDict.Add(names[i], messageType);

                // Create a delegate from MethodInfo to speed up deserialization
                TypeToParseDelegateDict.Add(messageType, parseMethod.CreateDelegate<ParseMessage>());
            }
        }
    }
}
