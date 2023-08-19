using Google.ProtocolBuffers;

namespace MHServerEmu.Networking
{
    public static class ProtocolDispatchTable
    {
        private static Dictionary<string, Dictionary<string, byte>> _protocolDict = new()
        {
            { "AuthMessages.proto", new() },
            { "BillingCommon.proto", new() },
            { "ChatCommon.proto", new() },
            { "ClientToGameServer.proto", new() },
            { "ClientToGroupingManager.proto", new() },
            { "CommonMessages.proto", new() },
            { "FrontendProtocol.proto", new() },
            { "GameServerToClient.proto", new() },
            { "GroupingManager.proto", new() },
            { "Guild.proto", new() },
            { "MatchCommon.proto", new() },
            { "PubSubProtocol.proto", new() }
        };

        public static bool IsInitialized { get; private set; }

        static ProtocolDispatchTable()
        {
            ParseMessageEnum(typeof(AuthMessage), _protocolDict["AuthMessages.proto"]);
            ParseMessageEnum(typeof(BillingCommonMessage), _protocolDict["BillingCommon.proto"]);
            ParseMessageEnum(typeof(ChatCommonMessage), _protocolDict["ChatCommon.proto"]);
            ParseMessageEnum(typeof(ClientToGameServerMessage), _protocolDict["ClientToGameServer.proto"]);
            ParseMessageEnum(typeof(ClientToGroupingManagerMessage), _protocolDict["ClientToGroupingManager.proto"]);
            ParseMessageEnum(typeof(CommonMessage), _protocolDict["CommonMessages.proto"]);
            ParseMessageEnum(typeof(FrontendProtocolMessage), _protocolDict["FrontendProtocol.proto"]);
            ParseMessageEnum(typeof(GameServerToClientMessage), _protocolDict["GameServerToClient.proto"]);
            ParseMessageEnum(typeof(GroupingManagerMessage), _protocolDict["GroupingManager.proto"]);
            ParseMessageEnum(typeof(GuildMessage), _protocolDict["Guild.proto"]);
            ParseMessageEnum(typeof(MatchCommonMessage), _protocolDict["MatchCommon.proto"]);
            ParseMessageEnum(typeof(PubSubProtocolMessage), _protocolDict["PubSubProtocol.proto"]);

            IsInitialized = true;
        }

        public static byte GetMessageId(IMessage message)
        {
            string messageName = message.DescriptorForType.Name;
            string protocolFileName = message.DescriptorForType.File.Name;
            return _protocolDict[protocolFileName][messageName];
        }

        private static void ParseMessageEnum(Type type, Dictionary<string, byte> dict)
        {
            string[] names = Enum.GetNames(type);
            for (int i = 0; i < names.Length; i++)
                dict.Add(names[i], (byte)i);
        }
    }
}
