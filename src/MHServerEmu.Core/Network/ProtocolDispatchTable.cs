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

        private static readonly Assembly ProtocolAssembly = typeof(NetMessageReadyAndLoggedIn).Assembly;

        // All ParseFrom() implementations in protobuf-csharp-port are wrappers that still use CodedInputStream,
        // so it's better to just use the CodedInputStream based one to avoid unnecessary indirection.
        private static readonly Type[] ParseMethodArgumentTypes = [typeof(ICodedInputStream)];

        private static readonly Type[] ProtocolEnumTypes =
        [
            typeof(AuthMessage),
            typeof(BillingCommonMessage),
            typeof(ChatCommonMessage),
            typeof(ClientToGameServerMessage),
            typeof(ClientToGroupingManagerMessage),
            typeof(CommonMessage),
            typeof(FrontendProtocolMessage),
            typeof(GameServerToClientMessage),
            typeof(GroupingManagerMessage),
            typeof(GuildMessage),
            typeof(MatchCommonMessage),
            typeof(PubSubProtocolMessage),
        ];

        // Lookups
        private readonly Dictionary<MessageDescriptor, uint> _protocolIdDict = new();   // MessageDescriptor -> protocol id
        private readonly Dictionary<(Type, uint), ParseMessage> _parserDict = new();    // Protocol enum type, id -> ParseMessage delegate

        private bool _isInitialized;

        public delegate IMessage ParseMessage(ICodedInputStream cis);

        public static ProtocolDispatchTable Instance { get; } = new();

        private ProtocolDispatchTable() { }

        /// <summary>
        /// Initializes the <see cref="ProtocolDispatchTable"/> instance.
        /// </summary>
        public bool Initialize()
        {
            if (_isInitialized) return false;

            var stopwatch = Stopwatch.StartNew();

            // Preprocess all protocols on startup to speed up serialization
            foreach (Type type in ProtocolEnumTypes)
                ParseProtocolEnum(type);

            Logger.Info($"Initialized in {stopwatch.ElapsedMilliseconds} ms");

            _isInitialized = true;
            return true;
        }

        /// <summary>
        /// Returns the protocol enum <see cref="Type"/> and <see cref="uint"/> id of the provided <see cref="IMessage"/>.
        /// </summary>
        public uint GetMessageProtocolId(IMessage message)
        {
            return _protocolIdDict[message.DescriptorForType];
        }

        /// <summary>
        /// Returns a delegate for parsing an <see cref="IMessage"/> of the specified id and protocol.
        /// </summary>
        public ParseMessage GetParseMessageDelegate(Type protocolEnumType, uint id)
        {
            return _parserDict[(protocolEnumType, id)];
        }

        /// <summary>
        /// Parses a message enum to generate lookups and cache parse delegates.
        /// </summary>
        private void ParseProtocolEnum(Type protocolEnumType)
        {
            string[] names = Enum.GetNames(protocolEnumType);

            // Iterate through message ids
            for (uint i = 0; i < names.Length; i++)
            {
                // Use reflection to get message type and ParseFrom MethodInfo
                Type messageType = ProtocolAssembly.GetType($"Gazillion.{names[i]}") ?? throw new("Message type is null.");
                MessageDescriptor messageDescriptor = (MessageDescriptor)messageType.GetProperty("Descriptor").GetValue(null);
                MethodInfo parseMethod = messageType.GetMethod("ParseFrom", ParseMethodArgumentTypes) ?? throw new("Message ParseFrom method is null.");

                // NOTE: Using ParseFrom() requires us to allocate buffers that match the lengths of serialized messages,
                // we should consider using ParseDelimitedFrom() or modifying the Protobuf library to suit our needs better.

                // Add lookups
                _protocolIdDict[messageDescriptor] = i;                                             // IMessage -> Protocol
                _parserDict[(protocolEnumType, i)] = parseMethod.CreateDelegate<ParseMessage>();    // Protocol -> IMessage
            }
        }
    }
}
