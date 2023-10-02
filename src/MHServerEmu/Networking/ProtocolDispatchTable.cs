using Google.ProtocolBuffers;

namespace MHServerEmu.Networking
{
    public static class ProtocolDispatchTable
    {
        private static readonly Dictionary<Type, Dictionary<byte, string>> MessageNameDict = new();     // Id -> message class name
        private static readonly Dictionary<string, Dictionary<string, byte>> MessageIdDict = new();     // IMessage -> Id

        public static bool IsInitialized { get; }

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

        public static string GetMessageName(Type enumType, byte id) => MessageNameDict[enumType][id];
        public static byte GetMessageId(IMessage message) => MessageIdDict[message.DescriptorForType.File.Name][message.DescriptorForType.Name];

        private static void ParseMessageEnum(Type type, string protocolName)
        {
            MessageNameDict.Add(type, new());
            MessageIdDict.Add(protocolName, new());

            string[] names = Enum.GetNames(type);
            for (int i = 0; i < names.Length; i++)
            {
                MessageNameDict[type].Add((byte)i, names[i]);
                MessageIdDict[protocolName].Add(names[i], (byte)i);
            }
        }
    }
}
