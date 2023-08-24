using Google.ProtocolBuffers;

namespace MHServerEmu.Networking
{
    public static class ProtocolDispatchTable
    {
        private static Dictionary<Type, Dictionary<byte, string>> _messageNameDict = new();     // For converting Id -> message class name
        private static Dictionary<string, Dictionary<string, byte>> _messageIdDict = new();     // For converting IMessage -> Id

        public static bool IsInitialized { get; private set; }

        static ProtocolDispatchTable()
        {
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

        public static string GetMessageName(Type enumType, byte id) => _messageNameDict[enumType][id];

        public static byte GetMessageId(IMessage message)
        {
            string messageName = message.DescriptorForType.Name;
            string protocolFileName = message.DescriptorForType.File.Name;
            return _messageIdDict[protocolFileName][messageName];
        }

        private static void ParseMessageEnum(Type type, string protocolName)
        {
            _messageNameDict.Add(type, new());
            _messageIdDict.Add(protocolName, new());

            string[] names = Enum.GetNames(type);
            for (int i = 0; i < names.Length; i++)
            {
                _messageNameDict[type].Add((byte)i, names[i]);
                _messageIdDict[protocolName].Add(names[i], (byte)i);
            }
        }
    }
}
