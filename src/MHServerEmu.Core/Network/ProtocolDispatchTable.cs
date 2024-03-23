using System.Diagnostics;
using System.Reflection;
using Google.ProtocolBuffers;
using Google.ProtocolBuffers.Descriptors;
using Gazillion;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// A helper singleton for <see cref="IMessage"/> serialization.
    /// </summary>
    public class ProtocolDispatchTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly Assembly LibGazillionAssembly = typeof(NetMessageReadyAndLoggedIn).Assembly;
        private static readonly Type[] ParseMethodArgumentTypes = new Type[] { typeof(byte[]) };

        // Lookups
        private readonly Dictionary<MessageDescriptor, (Type, uint)> _messageDescriptorToProtocolIdDict = new();  // MessageDescriptor -> protocol enum type, id
        private readonly Dictionary<(Type, uint), Type> _protocolIdToMessageTypeDict = new();                     // Protocol enum type, id -> Message type
        private readonly Dictionary<Type, ParseMessage> _messageTypeToParseDelegateDict = new();                  // Message type -> ParseMessage delegate

        private bool _isInitialized;

        public delegate IMessage ParseMessage(byte[] data);

        public static ProtocolDispatchTable Instance { get; } = new();

        private ProtocolDispatchTable() { }

        /// <summary>
        /// Initializes the <see cref="ProtocolDispatchTable"/> instance.
        /// </summary>
        public bool Initialize()
        {
            if (_isInitialized) return false;

            var stopwatch = Stopwatch.StartNew();

            // Preprocess all messages on startup to speed up serialization
            ParseMessageEnum(typeof(AuthMessage));
            ParseMessageEnum(typeof(BillingCommonMessage));
            ParseMessageEnum(typeof(ChatCommonMessage));
            ParseMessageEnum(typeof(ClientToGameServerMessage));
            ParseMessageEnum(typeof(ClientToGroupingManagerMessage));
            ParseMessageEnum(typeof(CommonMessage));
            ParseMessageEnum(typeof(FrontendProtocolMessage));
            ParseMessageEnum(typeof(GameServerToClientMessage));
            ParseMessageEnum(typeof(GroupingManagerMessage));
            ParseMessageEnum(typeof(GuildMessage));
            ParseMessageEnum(typeof(MatchCommonMessage));
            ParseMessageEnum(typeof(PubSubProtocolMessage));

            Logger.Info($"Initialized in {stopwatch.ElapsedMilliseconds} ms");

            _isInitialized = true;
            return true;
        }

        /// <summary>
        /// Returns the protocol enum <see cref="Type"/> and <see cref="uint"/> id of the provided <see cref="IMessage"/>.
        /// </summary>
        public (Type, uint) GetMessageProtocolId(IMessage message)
        {
            return _messageDescriptorToProtocolIdDict[message.DescriptorForType];
        }

        /// <summary>
        /// Returns a delegate for parsing an <see cref="IMessage"/> of the specified id and protocol.
        /// </summary>
        public ParseMessage GetParseMessageDelegate(Type protocolEnumType, uint id)
        {
            Type messageType = _protocolIdToMessageTypeDict[(protocolEnumType, id)];
            return _messageTypeToParseDelegateDict[messageType];
        }

        /// <summary>
        /// Parses a message enum to generate lookups and cache parse delegates.
        /// </summary>
        private void ParseMessageEnum(Type protocolEnumType)
        {
            string[] names = Enum.GetNames(protocolEnumType);

            // Iterate through message ids
            for (uint i = 0; i < names.Length; i++)
            {
                // Use reflection to get message type and ParseFrom MethodInfo
                Type messageType = LibGazillionAssembly.GetType($"Gazillion.{names[i]}") ?? throw new("Message type is null.");
                var messageDescriptor = (MessageDescriptor)messageType.GetProperty("Descriptor").GetValue(null);
                MethodInfo parseMethod = messageType.GetMethod("ParseFrom", ParseMethodArgumentTypes) ?? throw new("Message ParseFrom method is null.");

                // Add lookups
                _messageDescriptorToProtocolIdDict[messageDescriptor] = (protocolEnumType, i);
                _protocolIdToMessageTypeDict[(protocolEnumType, i)] = messageType;

                // Create a delegate from MethodInfo to speed up deserialization
                _messageTypeToParseDelegateDict.Add(messageType, parseMethod.CreateDelegate<ParseMessage>());
            }
        }
    }
}
