using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Entities.Options
{
    public class ChatChannelFilter
    {
        public ulong ChannelProtoId { get; set; }
        public bool IsSubscribed { get; set; }

        public ChatChannelFilter(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            ChannelProtoId = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            IsSubscribed = boolDecoder.ReadBool(stream);
        }

        public ChatChannelFilter(ulong channelProtoId, bool isSubscribed)
        {
            ChannelProtoId = channelProtoId;
            IsSubscribed = isSubscribed;
        }

        public ChatChannelFilter(NetStructChatChannelFilterState netStruct)
        {
            ChannelProtoId = netStruct.ChannelProtoId;
            IsSubscribed = netStruct.IsSubscribed;
        }

        public void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WritePrototypeEnum(ChannelProtoId, PrototypeEnumType.All);
            boolEncoder.WriteBuffer(stream);   // IsSubscribed
        }

        public NetStructChatChannelFilterState ToNetStruct() => NetStructChatChannelFilterState.CreateBuilder().SetChannelProtoId(ChannelProtoId).SetIsSubscribed(IsSubscribed).Build();

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ChannelProtoId: {GameDatabase.GetPrototypeName(ChannelProtoId)}");
            sb.AppendLine($"IsSubscribed: {IsSubscribed}");
            return sb.ToString();
        }
    }
}
